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
            VCSResults vcsResults = UpdateWorkingCopy(pipeline);
            replacements["commit_identifier"] = vcsResults.CurrentCommitIdentifier;

            // Run pipeline
            var buildResults = ExecuteSequence("Build", pipeline.BuildSteps, vcsResults, pipeline.Workspace);
            var archiveResults = ExecuteSequence("Archive", pipeline.ArchiveSteps, buildResults, pipeline.Workspace);
            var distributeResults = ExecuteSequence("Distribute", pipeline.DistributeSteps, archiveResults, pipeline.Workspace);
            var notifyResults = ExecuteSequence("Notify", pipeline.NotifySteps, distributeResults, pipeline.Workspace);

            // Done
            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteLine("");
            Console.WriteLine("Build process completed.");
        }

        VCSResults UpdateWorkingCopy(ProjectPipeline pipeline)
        {
            VersionControlSystem vcs = pipeline.VersionControlSystem;

            BuildConsole.WriteBullet($"Update working copy ({vcs.TypeName})");
            BuildConsole.IndentLevel++;

            var results = new VCSResults();

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

        TOutSeq ExecuteSequence<TInSeq, TInStep, TOutSeq, TOutStep>(string sequenceName, IEnumerable<IPipelineStep<TInSeq, TInStep, TOutSeq, TOutStep>> sequenceSteps, TInSeq previousSequence, Workspace workspace)
            where TInSeq : PipelineSequenceResults<TInStep> // previous sequence results
            where TInStep : PipelineStepResults, new() // previous step results
            where TOutSeq : PipelineSequenceResults<TOutStep>, new() // current sequence results
            where TOutStep : PipelineStepResults, new() // current step results
        {
            return ExecuteSequence<TInSeq, TOutSeq, TOutStep>(sequenceName, sequenceSteps, previousSequence, workspace);
        }

        TOutSeq ExecuteSequence<TInSeq, TOutSeq, TOutStep>(string sequenceName, IEnumerable<IPipelineStep<TInSeq, TOutSeq, TOutStep>> sequenceSteps, TInSeq previousSequence, Workspace workspace)
            where TInSeq : PipelineSequenceResults // previous sequence results
            where TOutSeq : PipelineSequenceResults<TOutStep>, new() // current sequence results
            where TOutStep : PipelineStepResults, new() // current step results
        {
            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteBullet(sequenceName);
            BuildConsole.IndentLevel++;

            // Pipeline sequence result
            var results = new TOutSeq();

            if (!previousSequence.IsSuccess)
            {
                BuildConsole.WriteFailure("Skipping, previous pipeline step failed");
                results.IsSuccess = false;
                results.Exception = new Exception($"Skipped {sequenceName} sequence, previous sequence failed.");
                return results;
            }

            int stepIndex = -1;
            IPipelineStep<TInSeq, TOutSeq, TOutStep> currentStep = null;

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            foreach (var step in sequenceSteps)
            {
                stepIndex++;
                currentStep = step;

                BuildConsole.WriteBullet(step.Type);

                var stepResult = step.ExecuteStep(previousSequence, workspace);
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
