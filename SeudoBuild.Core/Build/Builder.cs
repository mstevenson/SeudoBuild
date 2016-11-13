using System;
using System.Collections.Generic;
using SeudoBuild.VCS;
using System.Diagnostics;
using System.Linq;

namespace SeudoBuild
{
    /// <summary>
    /// 
    /// </summary>
    public class Builder
    {
        BuilderConfig builderConfig;

        public Builder(BuilderConfig config)
        {
            this.builderConfig = config;
        }

        public void ExecutePipeline(ProjectConfig projectConfig, string buildTargetName)
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

            Console.WriteLine("\nRunning SeudoBuild Pipeline:");

            BuildConsole.WriteLine($"Project: {projectConfig.ProjectName}");
            BuildConsole.WriteLine($"Target:  {buildTargetName}");
            Console.WriteLine("");

            // Setup pipeline
            var pipeline = ProjectPipeline.Create(builderConfig.ProjectsPath, projectConfig, buildTargetName);
            var replacements = pipeline.Workspace.Replacements;
            replacements["project_name"] = pipeline.ProjectConfig.ProjectName;
            replacements["build_target_name"] = pipeline.TargetConfig.TargetName;
            replacements["build_date"] = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss");

            // TODO
            //replacements["app_version"] = 

            // Clean
            pipeline.Workspace.CleanBuildOutputDirectory();

            // Grab changes from version control system
            // FIXME remove vcsResults, use sourceResults instead
            SourceSequenceResults vcsResults = UpdateWorkingCopy(pipeline);
            replacements["commit_identifier"] = vcsResults.CurrentCommitIdentifier;

            // TODO move VCS interaction into an ExecuteSequence call

            // Run pipeline
            var sourceResults = ExecuteSequence("Source", pipeline.SourceSteps, pipeline.Workspace);
            var buildResults = ExecuteSequence("Build", pipeline.BuildSteps, sourceResults, pipeline.Workspace);
            var archiveResults = ExecuteSequence("Archive", pipeline.ArchiveSteps, buildResults, pipeline.Workspace);
            var distributeResults = ExecuteSequence("Distribute", pipeline.DistributeSteps, archiveResults, pipeline.Workspace);
            var notifyResults = ExecuteSequence("Notify", pipeline.NotifySteps, distributeResults, pipeline.Workspace);

            // Done
            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteLine("");
            Console.WriteLine("Build process completed.");
        }

        // FIXME move UpdateWorkingCopy into a SourceStep
        SourceSequenceResults UpdateWorkingCopy(ProjectPipeline pipeline)
        {
            VersionControlSystem vcs = pipeline.VersionControlSystem;

            BuildConsole.WriteBullet($"Update working copy ({vcs.TypeName})");
            BuildConsole.IndentLevel++;

            var results = new SourceSequenceResults();

            try
            {
                if (vcs.IsWorkingCopyInitialized)
                {
                    vcs.Update();
                }
                else
                {
                    vcs.Download();
                }
            }
            catch (Exception e)
            {
                results.IsSuccess = false;
                results.Exception = e;
            }

            results.CurrentCommitIdentifier = vcs.CurrentCommit;
            results.IsSuccess = true;
            return results;
        }

        void PrintSequenceHeader(string sequenceName)
        {
            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteBullet(sequenceName);
            BuildConsole.IndentLevel++;
        }

        TOutSeq ExecuteSequence<TOutSeq, TOutStep>(string sequenceName, IEnumerable<IPipelineStep<TOutSeq, TOutStep>> sequenceSteps, Workspace workspace)
            where TOutSeq : PipelineSequenceResults<TOutStep>, new() // current sequence results
            where TOutStep : PipelineStepResults, new() // current step results
        {
            PrintSequenceHeader(sequenceName);

            Func<IPipelineStep<TOutSeq, TOutStep>, TOutStep> stepExecuteCallback = (step) =>
            {
                return step.ExecuteStep(workspace);
            };

            TOutSeq result = ExecuteSequenceInternal<TOutSeq, TOutStep, IPipelineStep<TOutSeq, TOutStep>>(sequenceName, sequenceSteps, stepExecuteCallback, workspace);
            return result;
        }

        TOutSeq ExecuteSequence<TInSeq, TOutSeq, TOutStep>(string sequenceName, IEnumerable<IPipelineStep<TInSeq, TOutSeq, TOutStep>> sequenceSteps, TInSeq previousSequence, Workspace workspace)
            where TInSeq : PipelineSequenceResults // previous sequence results
            where TOutSeq : PipelineSequenceResults<TOutStep>, new() // current sequence results
            where TOutStep : PipelineStepResults, new() // current step results
        {
            PrintSequenceHeader(sequenceName);

            if (!previousSequence.IsSuccess)
            {
                BuildConsole.WriteFailure("Skipping, previous pipeline step failed");
                var results = new TOutSeq
                {
                    IsSuccess = false,
                    Exception = new Exception($"Skipped {sequenceName} sequence, previous sequence failed.")
                };
                return results;
            }

            Func<IPipelineStep<TInSeq, TOutSeq, TOutStep>, TOutStep> stepExecuteCallback = (step) =>
            {
                return step.ExecuteStep(previousSequence, workspace);
            };

            TOutSeq result = ExecuteSequenceInternal<TOutSeq, TOutStep, IPipelineStep<TInSeq, TOutSeq, TOutStep>>(sequenceName, sequenceSteps, stepExecuteCallback, workspace, previousSequence.IsSuccess);
            return result;
        }

        TOutSeq ExecuteSequenceInternal<TOutSeq, TOutStep, TPipeStep>(string sequenceName, IEnumerable<TPipeStep> sequenceSteps, Func<TPipeStep, TOutStep> stepExecuteCallback, Workspace workspace, bool prevSequenceIsSuccess = true)
            where TOutSeq : PipelineSequenceResults<TOutStep>, new() // current sequence results
            where TOutStep : PipelineStepResults, new() // current step results
            where TPipeStep : class, IPipelineStep
        {
            // Pipeline sequence result
            var results = new TOutSeq();

            int stepIndex = -1;
            TPipeStep currentStep = null;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            foreach (var step in sequenceSteps)
            {
                stepIndex++;
                currentStep = step;

                BuildConsole.WriteBullet(step.Type);

                TOutStep stepResult = null;
                stepResult = stepExecuteCallback.Invoke(step);

                results.StepResults.Add(stepResult);
                if (!stepResult.IsSuccess)
                {
                    results.IsSuccess = false;
                    results.Exception = stepResult.Exception;
                    string error = $"{sequenceName} sequence failed on step {stepIndex} ({currentStep.Type})";
                    BuildConsole.WriteFailure(error);
                    break;
                }
            }

            stopwatch.Stop();
            results.Duration = stopwatch.Elapsed;

            if (stepIndex == 0)
            {
                BuildConsole.WriteAlert($"No {sequenceName} steps.");
                results.IsSuccess = false;
                results.Exception = new Exception($"No {sequenceName} steps.");
                return results;
            }

            results.IsSuccess = true;
            return results;
        }
    }
}
