using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace SeudoBuild.Pipeline
{
    /// <summary>
    /// Executes all pipeline steps in a given project for a given build target.
    /// </summary>
    public class PipelineRunner
    {
        readonly PipelineConfig _builderConfig;

        private readonly ILogger _logger;

        public PipelineRunner(PipelineConfig config, ILogger logger)
        {
            _builderConfig = config;
            _logger = logger;
        }

        public void ExecutePipeline(ProjectConfig projectConfig, string buildTargetName, IModuleLoader moduleLoader)
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
                throw new ArgumentException("The specified build target could not be found in the project.", nameof(buildTargetName));
            }

            Console.WriteLine("");
            _logger.Write("Running Pipeline\n", LogType.Header);
            _logger.IndentLevel++;
            _logger.Write($"Project:  {projectConfig.ProjectName}", LogType.Plus);
            _logger.Write($"Target:   {buildTargetName}", LogType.Plus);
            //logger.WritePlus($"Location: {projectsBaseDirectory}/{projectNameSanitized}"); 
            _logger.IndentLevel--;
            Console.WriteLine("");

            // Create project and target workspaces
            string projectNameSanitized = projectConfig.ProjectName.SanitizeFilename();
            string projectDirectory = $"{_builderConfig.BaseDirectory}/{projectNameSanitized}";
            var filesystem = new FileSystem();
            var projectWorkspace = new ProjectWorkspace(projectDirectory, filesystem);
            projectWorkspace.CreateSubdirectories();
            ITargetWorkspace targetWorkspace = projectWorkspace.CreateTarget(buildTargetName);

            // Save a copy of the build project configuration
            var serializer = new Serializer(filesystem);
            string configFilePath = $"{projectDirectory}/{projectNameSanitized}{serializer.FileExtension}";
            serializer.SerializeToFile(projectConfig, configFilePath);

            // Setup build pipeline
            var pipeline = new ProjectPipeline(projectConfig, buildTargetName);
            pipeline.LoadBuildStepModules(moduleLoader, targetWorkspace, _logger);

            var macros = targetWorkspace.Macros;
            macros["project_name"] = pipeline.ProjectConfig.ProjectName;
            macros["build_target_name"] = pipeline.TargetConfig.TargetName;
            macros["build_date"] = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss");
            macros["app_version"] = pipeline.TargetConfig.Version.ToString();

            // Clean
            targetWorkspace.CleanOutputDirectory();

            // Run pipeline
            var sourceResults = ExecuteSequence("Update Source", pipeline.GetPipelineSteps<ISourceStep>(), targetWorkspace);
            if (sourceResults.StepResults.Count > 0) {
                // fixme don't hard-code step results index
                macros["commit_id"] = sourceResults.StepResults[0].CommitIdentifier;
            }
            var buildResults = ExecuteSequence("Build", pipeline.GetPipelineSteps<IBuildStep>(), sourceResults, targetWorkspace);
            var archiveResults = ExecuteSequence("Archive", pipeline.GetPipelineSteps<IArchiveStep>(), buildResults, targetWorkspace);
            var distributeResults = ExecuteSequence("Distribute", pipeline.GetPipelineSteps<IDistributeStep>(), archiveResults, targetWorkspace);
            var notifyResults = ExecuteSequence("Notify", pipeline.GetPipelineSteps<INotifyStep>(), distributeResults, targetWorkspace);

            // Done
            _logger.IndentLevel = 0;
            _logger.Write("\nBuild process completed.", logStyle: LogStyle.Bold);
        }

        private TOutSeq InitializeSequence<TOutSeq>(string sequenceName, IReadOnlyCollection<IPipelineStep> sequenceSteps)
            where TOutSeq : PipelineSequenceResults, new()
        {
            _logger.IndentLevel = 1;
            _logger.Write(sequenceName, LogType.Bullet);
            _logger.IndentLevel++;

            if (sequenceSteps.Count == 0)
            {
                _logger.Write($"No {sequenceName} steps.", LogType.Alert);
                return new TOutSeq {
                    IsSuccess = true,
                    IsSkipped = true,
                    Exception = new Exception($"No {sequenceName} steps.")
                };
            }
            return new TOutSeq { IsSuccess = true };
        }

        // First step of pipeline execution
        private TOutSeq ExecuteSequence<TOutSeq, TOutStep>(string sequenceName, IReadOnlyCollection<IPipelineStep<TOutSeq, TOutStep>> sequenceSteps, ITargetWorkspace workspace)
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
        private TOutSeq ExecuteSequence<TInSeq, TOutSeq, TOutStep>(string sequenceName, IReadOnlyCollection<IPipelineStep<TInSeq, TOutSeq, TOutStep>> sequenceSteps, TInSeq previousSequence, ITargetWorkspace workspace)
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
                _logger.Write("Skipping, previous pipeline step failed", LogType.Failure);
                results = new TOutSeq
                {
                    IsSuccess = false,
                    Exception = new Exception($"Skipped {sequenceName} sequence, previous sequence failed.")
                };
                return results;
            }

            // Run the sequence
            results = ExecuteSequenceInternal<TOutSeq, TOutStep, IPipelineStep<TInSeq, TOutSeq, TOutStep>>(sequenceName,
                sequenceSteps, step => step.ExecuteStep(previousSequence, workspace));
            return results;
        }

        private TOutSeq ExecuteSequenceInternal<TOutSeq, TOutStep, TPipeStep>(string sequenceName, IReadOnlyCollection<TPipeStep> sequenceSteps, Func<TPipeStep, TOutStep> stepExecuteCallback)
            where TOutSeq : PipelineSequenceResults<TOutStep>, new() // current sequence results
            where TOutStep : PipelineStepResults, new() // current step results
            where TPipeStep : class, IPipelineStep
        {
            // Pipeline sequence result
            var results = new TOutSeq();

            //const int startIndex = -1;
            //int stepIndex = startIndex;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            foreach (var step in sequenceSteps)
            {
                //stepIndex++;
                var currentStep = step;

                _logger.Write(step.Type, LogType.Bullet);
                _logger.IndentLevel++;

                TOutStep stepResult = null;
                stepResult = stepExecuteCallback.Invoke(step);

                results.StepResults.Add(stepResult);
                if (!stepResult.IsSuccess)
                {
                    results.IsSuccess = false;
                    results.Exception = stepResult.Exception;
                    string error = $"{sequenceName} sequence failed on step {currentStep.Type}:\n      {results.Exception.Message}";
                    _logger.Write(error, LogType.Failure);
                    break;
                }

                _logger.IndentLevel--;
            }

            stopwatch.Stop();
            results.Duration = stopwatch.Elapsed;

            results.IsSuccess = true;
            return results;
        }
    }
}
