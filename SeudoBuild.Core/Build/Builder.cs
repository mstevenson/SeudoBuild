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

            Console.WriteLine("Starting build process:");

            BuildConsole.WriteLine($"Project: {projectConfig.ProjectName}");
            BuildConsole.WriteLine($"Target:  {buildTargetName}");
            BuildConsole.WriteLine("");

            // Setup pipelin
            var pipeline = ProjectPipeline.Create(builderConfig.ProjectsPath, projectConfig, buildTargetName);
            var replacements = pipeline.Workspace.Replacements;

            // Grab changes from version control system
            VCSResults vcsResults = UpdateWorkingCopy(pipeline);

            // Build
            BuildSequenceResults buildResults = Build(vcsResults, pipeline);
            BuildStepResults step = buildResults.StepResults[0];
            replacements["project_name"] = step.ProjectName;
            replacements["build_target_name"] = step.BuildTargetName;
            replacements["app_version"] = step.AppVersion.ToString ();
            replacements["build_date"] = step.BuildDate.ToString("yyyy-dd-M--HH-mm-ss");
            replacements["commit_identifier"] = step.CommitIdentifier;

            // Archive
            //var archiveResults = Archive(buildResults, pipeline);
            var archiveResults = ExecuteSequence("Archive", pipeline.ArchiveSteps, buildResults, pipeline.Workspace);

            // Distribute
            var distributeResults = ExecuteSequence("Distribute", pipeline.DistributeSteps, archiveResults, pipeline.Workspace);

            // Notify
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
                results.Success = false;
                results.Exception = e;
            }

            results.CurrentCommitIdentifier = vcs.CurrentCommit;
            results.Success = true;
            return results;
        }

        BuildSequenceResults Build(VCSResults vcsResults, ProjectPipeline pipeline)
        {
            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteBullet("Build");
            BuildConsole.IndentLevel = 1;

            BuildSequenceResults results = new BuildSequenceResults();

            if (!vcsResults.Success)
            {
                BuildConsole.WriteFailure("Skipping, previous pipeline step failed");
                results.IsSuccess = false;
                results.Exception = new Exception($"Skipped build sequence, VCS update failed.");
                return results;
            }

            // Bail if nothing to build
            if (pipeline.BuildSteps.Count == 0)
            {
                BuildConsole.WriteAlert("No build steps");
                //return new BuildStepResults { Status = BuildCompletionStatus.Faulted };
                results.IsSuccess = false;
                results.Exception = new Exception("No build steps.");
                return results;
            }

            bool isFaulted = false;

            // Delete all files in the build output directory
            pipeline.Workspace.CleanBuildOutputDirectory();

            // TODO add app version
            var stepResults = new BuildStepResults
            {
                BuildDate = DateTime.Now,
                ProjectName = pipeline.ProjectConfig.ProjectName,
                BuildTargetName = pipeline.TargetConfig.TargetName,
                CommitIdentifier = vcsResults.CurrentCommitIdentifier,
                //Status = BuildCompletionStatus.Running
            };

            results.StepResults.Add(stepResults);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            IBuildStep currentStep = null;
            int stepIndex = 0;

            // Build
            foreach (var step in pipeline.BuildSteps)
            {
                stepIndex++;
                currentStep = step;
                BuildConsole.WriteBullet($"{step.Type} (step {stepIndex}/{pipeline.BuildSteps.Count})");
                BuildConsole.IndentLevel++;

                var stepResult = step.Execute();

                BuildConsole.IndentLevel--;
                if (stepResult.Status == BuildCompletionStatus.Faulted)
                {
                    isFaulted = true;
                    //buildInfo.Status = BuildCompletionStatus.Faulted;
                    break;
                }
            }

            stopwatch.Stop();
            stepResults.BuildDuration = stopwatch.Elapsed;

            if (!isFaulted)
            {
                //buildInfo.Status = BuildCompletionStatus.Completed;
                results.IsSuccess = true;
                BuildConsole.WriteSuccess("Build steps completed in " + stepResults.BuildDuration.ToString(@"hh\:mm\:ss"));
            }
            else
            {
                results.IsSuccess = false;
                string error = $"Build failed on step {stepIndex} ({currentStep.Type})";
                results.Exception = new Exception(error);
                BuildConsole.WriteFailure(error);
            }

            return results;
        }

        TOutSeq ExecuteSequence<TInSeq, TInStep, TOutSeq, TOutStep>(string sequenceName, IEnumerable<IPipelineStep<TInSeq,TInStep,TOutSeq,TOutStep>> sequenceSteps, TInSeq previousSequence, Workspace workspace)
            where TInSeq : PipelineSequenceResults<TInStep> // previous sequence results
            where TInStep : PipelineStepResults, new() // previous step results
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
            IPipelineStep<TInSeq, TInStep, TOutSeq, TOutStep> currentStep = null;
            foreach (var step in sequenceSteps)
            {
                BuildConsole.WriteBullet(step.Type);

                stepIndex++;
                currentStep = step;

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

            if (stepIndex == 0)
            {
                BuildConsole.WriteAlert($"No {sequenceName} steps");
                results.IsSuccess = false;
                results.Exception = new Exception($"No {sequenceName} steps.");
                return results;
            }

            return results;
        }
    }
}
