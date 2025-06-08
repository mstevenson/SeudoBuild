namespace SeudoCI.Pipeline;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Core;
using Core.FileSystems;

/// <summary>
/// Executes all pipeline steps in a given project for a given build target.
/// </summary>
public class PipelineRunner(PipelineConfig config, ILogger logger) : IPipelineRunner
{
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
        logger.Write("Running Pipeline\n", LogType.Header);
        logger.IndentLevel++;
        logger.Write($"Project:  {projectConfig.ProjectName}", LogType.Plus);
        logger.Write($"Target:   {buildTargetName}", LogType.Plus);
        //logger.WritePlus($"Location: {projectsBaseDirectory}/{projectNameSanitized}"); 
        logger.IndentLevel--;
        Console.WriteLine("");

        // Create project and target workspaces
        string projectNameSanitized = projectConfig.ProjectName.SanitizeFilename();
        string projectDirectory = $"{config.BaseDirectory}/{projectNameSanitized}";
        var filesystem = new WindowsFileSystem();
        var projectWorkspace = new ProjectWorkspace(projectDirectory, filesystem);
        projectWorkspace.InitializeDirectories();
        var targetWorkspace = projectWorkspace.CreateTarget(buildTargetName);

        // Save a copy of the build project configuration
        var serializer = new Serializer(filesystem);
        string configFilePath = $"{projectDirectory}/{projectNameSanitized}{serializer.FileExtension}";
        serializer.SerializeToFile(projectConfig, configFilePath);

        // Setup build pipeline
        var pipeline = new ProjectPipeline(projectConfig, buildTargetName);
        pipeline.LoadBuildStepModules(moduleLoader, targetWorkspace, logger);

        var macros = projectWorkspace.Macros;
        macros["project_name"] = pipeline.ProjectConfig.ProjectName;
        macros["build_target_name"] = pipeline.TargetConfig.TargetName;
        macros["build_date"] = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss");
        macros["app_version"] = pipeline.TargetConfig.Version.ToString();

        // Clean
        targetWorkspace.CleanDirectory(TargetDirectory.Output);

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
        logger.IndentLevel = 0;
        logger.Write("\nBuild process completed.", logStyle: LogStyle.Bold);
    }

    private TOutSeq InitializeSequence<TOutSeq>(string? sequenceName, IReadOnlyCollection<IPipelineStep> sequenceSteps)
        where TOutSeq : PipelineSequenceResults, new()
    {
        logger.IndentLevel = 1;
        logger.Write(sequenceName, LogType.Bullet);
        logger.IndentLevel++;

        if (sequenceSteps.Count == 0)
        {
            logger.Write($"No {sequenceName} steps.", LogType.Alert);
            return new TOutSeq {
                IsSuccess = true,
                IsSkipped = true,
                Exception = new Exception($"No {sequenceName} steps.")
            };
        }
        return new TOutSeq { IsSuccess = true };
    }

    // First step of pipeline execution
    private TOutSeq ExecuteSequence<TOutSeq, TOutStep>(string? sequenceName, IReadOnlyCollection<IPipelineStep<TOutSeq, TOutStep>> sequenceSteps, ITargetWorkspace workspace)
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
    private TOutSeq ExecuteSequence<TInSeq, TOutSeq, TOutStep>(string? sequenceName, IReadOnlyCollection<IPipelineStep<TInSeq, TOutSeq, TOutStep>> sequenceSteps, TInSeq previousSequence, ITargetWorkspace workspace)
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
            logger.Write("Skipping, previous pipeline step failed", LogType.Failure);
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

    private TOutSeq ExecuteSequenceInternal<TOutSeq, TOutStep, TPipeStep>(string? sequenceName, IReadOnlyCollection<TPipeStep> sequenceSteps, Func<TPipeStep, TOutStep?> stepExecuteCallback)
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

            logger.Write(step.Type, LogType.Bullet);
            logger.IndentLevel++;

            var stepResult = stepExecuteCallback.Invoke(step);

            results.StepResults.Add(stepResult);
            if (!stepResult.IsSuccess)
            {
                results.IsSuccess = false;
                results.Exception = stepResult.Exception;
                string error = $"{sequenceName} sequence failed on step {currentStep.Type}:\n      {results.Exception.Message}";
                logger.Write(error, LogType.Failure);
                break;
            }

            logger.IndentLevel--;
        }

        stopwatch.Stop();
        results.Duration = stopwatch.Elapsed;

        results.IsSuccess = true;
        return results;
    }
}