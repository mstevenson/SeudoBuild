using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SeudoBuild.Modules;

namespace SeudoBuild.Pipeline
{
    /// <summary>
    /// 
    /// </summary>
    public class PipelineRunner
    {
        PipelineConfig builderConfig;

        public PipelineRunner(PipelineConfig config)
        {
            this.builderConfig = config;
        }

        public void ExecutePipeline(ProjectConfig projectConfig, string buildTargetName, ModuleLoader modules)
        {
            if (projectConfig == null)
            {
                throw new ArgumentNullException(nameof(projectConfig), "A project configuration definition must be specified.");
            }
            if (string.IsNullOrEmpty (buildTargetName))
            {
                throw new ArgumentNullException(nameof(buildTargetName), "A project configuration definition must be specified.");
            }

            BuildTargetConfig targetConfig = projectConfig.BuildTargets.FirstOrDefault(t => t.TargetName == buildTargetName);
            if (targetConfig == null)
            {
                throw new ArgumentException("The specified build target could not be found in the project.", "buildTargetName");
            }

            Console.WriteLine("");
            BuildConsole.WriteLine("Running Pipeline");
            BuildConsole.IndentLevel++;
            BuildConsole.WritePlus($"Project:  {projectConfig.ProjectName}");
            BuildConsole.WritePlus($"Target:   {buildTargetName}");
            //BuildConsole.WritePlus($"Location: {projectsBaseDirectory}/{projectNameSanitized}"); 
            BuildConsole.IndentLevel--;
            Console.WriteLine("");

            // Setup pipeline
            var pipeline = new ProjectPipeline(projectConfig, buildTargetName);
            pipeline.InitializeWorkspace(builderConfig.ProjectsPath, new FileSystem());
            pipeline.LoadBuildStepModules(modules);
            var macros = pipeline.Workspace.Macros;
            macros["project_name"] = pipeline.ProjectConfig.ProjectName;
            macros["build_target_name"] = pipeline.TargetConfig.TargetName;
            macros["build_date"] = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss");
            macros["app_version"] = pipeline.TargetConfig.Version.ToString();

            // Clean
            pipeline.Workspace.CleanBuildOutputDirectory();

            // Run pipeline
            var sourceResults = ExecuteSequence("Update Source", pipeline.GetPipelineSteps<ISourceStep>(), pipeline.Workspace);
            macros["commit_id"] = sourceResults.StepResults[0].CommitIdentifier; // fixme don't hard-code index
            var buildResults = ExecuteSequence("Build", pipeline.GetPipelineSteps<IBuildStep>(), sourceResults, pipeline.Workspace);
            var archiveResults = ExecuteSequence("Archive", pipeline.GetPipelineSteps<IArchiveStep>(), buildResults, pipeline.Workspace);
            var distributeResults = ExecuteSequence("Distribute", pipeline.GetPipelineSteps<IDistributeStep>(), archiveResults, pipeline.Workspace);
            var notifyResults = ExecuteSequence("Notify", pipeline.GetPipelineSteps<INotifyStep>(), distributeResults, pipeline.Workspace);

            // Done
            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteLine("");
            Console.WriteLine("Build process completed.");
        }

        TOutSeq InitializeSequence<TOutSeq>(string sequenceName, IReadOnlyCollection<IPipelineStep> sequenceSteps)
            where TOutSeq : PipelineSequenceResults, new()
        {
            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteBullet(sequenceName);
            BuildConsole.IndentLevel++;

            if (sequenceSteps.Count == 0)
            {
                BuildConsole.WriteAlert($"No {sequenceName} steps.");
                return new TOutSeq {
                    IsSuccess = true,
                    IsSkipped = true,
                    Exception = new Exception($"No {sequenceName} steps.")
                };
            }
            return new TOutSeq { IsSuccess = true };
        }

        // First step of pipeline execution
        TOutSeq ExecuteSequence<TOutSeq, TOutStep>(string sequenceName, IReadOnlyCollection<IPipelineStep<TOutSeq, TOutStep>> sequenceSteps, Workspace workspace)
            where TOutSeq : PipelineSequenceResults<TOutStep>, new() // current sequence results
            where TOutStep : PipelineStepResults, new() // current step results
        {
            // Initialize the sequence
            TOutSeq results = InitializeSequence<TOutSeq>(sequenceName, sequenceSteps);
            if (!results.IsSuccess)
            {
                return results;
            }

            // Run the sequence
            results = ExecuteSequenceInternal<TOutSeq, TOutStep, IPipelineStep<TOutSeq, TOutStep>>(sequenceName, sequenceSteps, (step) =>
            {
                return step.ExecuteStep(workspace);
            });
            return results;
        }

        // Pipeline execution step that had a step before it
        TOutSeq ExecuteSequence<TInSeq, TOutSeq, TOutStep>(string sequenceName, IReadOnlyCollection<IPipelineStep<TInSeq, TOutSeq, TOutStep>> sequenceSteps, TInSeq previousSequence, Workspace workspace)
            where TInSeq : PipelineSequenceResults // previous sequence results
            where TOutSeq : PipelineSequenceResults<TOutStep>, new() // current sequence results
            where TOutStep : PipelineStepResults, new() // current step results
        {
            // Initialize the sequence
            TOutSeq results = InitializeSequence<TOutSeq>(sequenceName, sequenceSteps);
            if (!results.IsSuccess)
            {
                return results;
            }

            // Verify that the pipeline has not previously failed
            if (!previousSequence.IsSuccess)
            {
                BuildConsole.WriteFailure("Skipping, previous pipeline step failed");
                results = new TOutSeq
                {
                    IsSuccess = false,
                    Exception = new Exception($"Skipped {sequenceName} sequence, previous sequence failed.")
                };
                return results;
            }

            // Run the sequence
            results = ExecuteSequenceInternal<TOutSeq, TOutStep, IPipelineStep<TInSeq, TOutSeq, TOutStep>>(sequenceName, sequenceSteps, (step) =>
            {
                return step.ExecuteStep(previousSequence, workspace);
            });
            return results;
        }

        TOutSeq ExecuteSequenceInternal<TOutSeq, TOutStep, TPipeStep>(string sequenceName, IReadOnlyCollection<TPipeStep> sequenceSteps, Func<TPipeStep, TOutStep> stepExecuteCallback)
            where TOutSeq : PipelineSequenceResults<TOutStep>, new() // current sequence results
            where TOutStep : PipelineStepResults, new() // current step results
            where TPipeStep : class, IPipelineStep
        {
            // Pipeline sequence result
            var results = new TOutSeq();

            //const int startIndex = -1;
            //int stepIndex = startIndex;
            TPipeStep currentStep = null;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            foreach (var step in sequenceSteps)
            {
                //stepIndex++;
                currentStep = step;

                BuildConsole.WriteBullet(step.Type);

                TOutStep stepResult = null;
                stepResult = stepExecuteCallback.Invoke(step);

                results.StepResults.Add(stepResult);
                if (!stepResult.IsSuccess)
                {
                    results.IsSuccess = false;
                    results.Exception = stepResult.Exception;
                    string error = $"{sequenceName} sequence failed on step {currentStep.Type}";
                    BuildConsole.WriteFailure(error);
                    break;
                }
            }

            stopwatch.Stop();
            results.Duration = stopwatch.Elapsed;

            results.IsSuccess = true;
            return results;
        }
    }
}
