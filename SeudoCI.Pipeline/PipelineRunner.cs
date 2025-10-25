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
        if (string.IsNullOrEmpty(buildTargetName))
        {
            throw new ArgumentNullException(nameof(buildTargetName), "A build target name must be specified.");
        }

        BuildTargetConfig? targetConfig = projectConfig.BuildTargets.FirstOrDefault(t => t.TargetName == buildTargetName);
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
        var filesystem = PlatformUtils.RunningPlatform == Platform.Windows ? new WindowsFileSystem() : new MacFileSystem();
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

        // Set runtime macros on the target workspace (which inherits from project workspace)
        var macros = targetWorkspace.Macros;
        macros["project_name"] = pipeline.ProjectConfig.ProjectName;
        macros["build_target_name"] = pipeline.TargetConfig.TargetName;
        macros["build_date"] = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss");
        macros["app_version"] = pipeline.TargetConfig.Version.ToString();

        // Clean
        targetWorkspace.CleanDirectory(TargetDirectory.Output);

        // Run pipeline
        var sourceResults = ExecuteSequence("Update Source", pipeline.GetPipelineSteps<ISourceStep>(), targetWorkspace);
        if (sourceResults.StepResults.Count > 0 && sourceResults.StepResults[0] != null && !string.IsNullOrEmpty(sourceResults.StepResults[0].CommitIdentifier)) 
        {
            targetWorkspace.Macros["commit_id"] = sourceResults.StepResults[0].CommitIdentifier;
        }
        
        var buildResults = ExecuteSequence("Build", pipeline.GetPipelineSteps<IBuildStep>(), sourceResults, targetWorkspace);
        var archiveResults = ExecuteSequence("Archive", pipeline.GetPipelineSteps<IArchiveStep>(), buildResults, targetWorkspace);
        var distributeResults = ExecuteSequence("Distribute", pipeline.GetPipelineSteps<IDistributeStep>(), archiveResults, targetWorkspace);
        var notifyResults = ExecuteSequence("Notify", pipeline.GetPipelineSteps<INotifyStep>(), distributeResults, targetWorkspace);

        // Determine overall pipeline success
        bool overallSuccess = sourceResults.IsSuccess && buildResults.IsSuccess && archiveResults.IsSuccess && distributeResults.IsSuccess && notifyResults.IsSuccess;

        // Done
        logger.IndentLevel = 0;
        if (overallSuccess)
        {
            logger.Write("\nBuild process completed successfully.", LogType.Success, LogStyle.Bold);
        }
        else
        {
            logger.Write("\nBuild process failed.", LogType.Failure, LogStyle.Bold);
            
            // Log which sequence failed
            if (!sourceResults.IsSuccess) logger.Write("Source sequence failed", LogType.Failure);
            if (!buildResults.IsSuccess) logger.Write("Build sequence failed", LogType.Failure);
            if (!archiveResults.IsSuccess) logger.Write("Archive sequence failed", LogType.Failure);
            if (!distributeResults.IsSuccess) logger.Write("Distribute sequence failed", LogType.Failure);
            if (!notifyResults.IsSuccess) logger.Write("Notify sequence failed", LogType.Failure);
        }
    }

    private TOutSeq InitializeSequence<TOutSeq>(string? sequenceName, IReadOnlyCollection<IPipelineStep> sequenceSteps)
        where TOutSeq : PipelineSequenceResults, new()
    {
        logger.IndentLevel = 1;
        logger.Write(sequenceName, LogType.Bullet);
        logger.IndentLevel++;

        if (sequenceSteps.Count == 0)
        {
            logger.Write($"No {sequenceName} steps configured, skipping sequence.", LogType.Alert);
            logger.IndentLevel--;
            return new TOutSeq
            {
                IsSuccess = true,  // Skipped sequences are considered successful
                IsSkipped = true,
                Exception = null   // No exception for skipped sequences
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
        if (!results.IsSuccess || results.IsSkipped)
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
        if (!results.IsSuccess || results.IsSkipped)
        {
            return results;
        }

        // Verify that the pipeline has not previously failed
        if (!previousSequence.IsSuccess)
        {
            logger.Write("Skipping, previous pipeline step failed", LogType.Failure);
            logger.IndentLevel--;
            results = new TOutSeq
            {
                IsSuccess = false,
                Exception = new Exception($"Skipped {sequenceName} sequence, previous sequence failed.")
            };
            return results;
        }

        if (previousSequence.IsSkipped)
        {
            logger.Write($"Skipping {sequenceName} sequence, previous sequence was skipped.", LogType.Alert);
            logger.IndentLevel--;
            return new TOutSeq
            {
                IsSuccess = true,
                IsSkipped = true
            };
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
        var results = new TOutSeq { IsSuccess = true }; // Start optimistic, set to false on any failure

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        foreach (var step in sequenceSteps)
        {
            logger.Write(step.Type, LogType.Bullet);
            logger.IndentLevel++;

            try
            {
                var stepResult = stepExecuteCallback.Invoke(step);
                
                if (stepResult == null)
                {
                    results.IsSuccess = false;
                    results.Exception = new InvalidOperationException($"Step {step.Type} returned null result");
                    logger.Write($"Step {step.Type} returned null result", LogType.Failure);
                    logger.IndentLevel--;
                    break;
                }

                results.StepResults.Add(stepResult);
                
                if (!stepResult.IsSuccess)
                {
                    results.IsSuccess = false;
                    results.Exception = stepResult.Exception;
                    string error = $"{sequenceName} sequence failed on step {step.Type}:\n      {results.Exception?.Message ?? "Unknown error"}";
                    logger.Write(error, LogType.Failure);
                    logger.IndentLevel--;
                    break;
                }
            }
            catch (Exception ex)
            {
                results.IsSuccess = false;
                results.Exception = ex;
                logger.Write($"Exception in step {step.Type}: {ex.Message}", LogType.Failure);
                logger.IndentLevel--;
                break;
            }

            logger.IndentLevel--;
        }

        stopwatch.Stop();
        results.Duration = stopwatch.Elapsed;

        // Only set success if no failures occurred and we have the expected results
        if (results.IsSuccess && results.StepResults.Count == sequenceSteps.Count)
        {
            logger.Write($"{sequenceName} sequence completed successfully", LogType.Success);
        }

        return results;
    }
}