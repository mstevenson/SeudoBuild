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

            // TODO handle VCS failure, pass the result through to the Build step

            // Build
            var buildResults = Build(vcsResults, pipeline);
            var step = buildResults.StepResults[0];
            replacements["project_name"] = step.ProjectName;
            replacements["build_target_name"] = step.BuildTargetName;
            replacements["app_version"] = step.AppVersion.ToString ();
            replacements["build_date"] = step.BuildDate.ToString("yyyy-dd-M--HH-mm-ss");
            replacements["commit_identifier"] = step.CommitIdentifier;

            // Archive
            //var archiveResults = Archive(buildResults, pipeline);
            var archiveResults = ExecuteSequence<BuildSequenceResults, BuildStepResults, ArchiveSequenceResults, ArchiveStepResults>
                ("Archive", pipeline.ArchiveSteps, buildResults, pipeline.Workspace);

            // Distribute
            var distributeResults = ExecuteSequence<ArchiveSequenceResults, ArchiveStepResults, DistributeSequenceResults, DistributeStepResults>
                ("Distribute", pipeline.DistributeSteps, archiveResults, pipeline.Workspace);

            // Notify
            var notifyResults = ExecuteSequence<DistributeSequenceResults, DistributeStepResults, NotifySequenceResults, NotifyStepResults>
                ("Notify", pipeline.NotifySteps, distributeResults, pipeline.Workspace);

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

            // Bail if nothing to build
            if (pipeline.BuildSteps.Count == 0)
            {
                BuildConsole.WriteAlert("No build steps");
                //return new BuildStepResults { Status = BuildCompletionStatus.Faulted };
                return new BuildSequenceResults { IsSuccess = false, Exception = new Exception("No build steps.") };
            }

            bool isFaulted = false;

            // Delete all files in the build output directory
            pipeline.Workspace.CleanBuildOutputDirectory();

            var sequenceResults = new BuildSequenceResults();

            // TODO add app version
            var stepResults = new BuildStepResults
            {
                BuildDate = DateTime.Now,
                ProjectName = pipeline.ProjectConfig.ProjectName,
                BuildTargetName = pipeline.TargetConfig.TargetName,
                CommitIdentifier = vcsResults.CurrentCommitIdentifier,
                //Status = BuildCompletionStatus.Running
            };

            sequenceResults.StepResults.Add(stepResults);

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
                sequenceResults.IsSuccess = true;
                BuildConsole.WriteSuccess("Build steps completed in " + stepResults.BuildDuration.ToString(@"hh\:mm\:ss"));
            }
            else
            {
                sequenceResults.IsSuccess = false;
                string error = $"Build failed on step {stepIndex} ({currentStep.Type})";
                sequenceResults.Exception = new Exception(error);
                BuildConsole.WriteFailure(error);
            }

            return sequenceResults;
        }

        OutSeq ExecuteSequence<InSeq, InStep, OutSeq, OutStep>(string sequenceName, IEnumerable<IPipelineStep<InSeq,InStep,OutStep>> sequenceSteps, InSeq previousSequence, Workspace workspace)
            where InSeq : PipelineSequenceResults<InStep> // previous sequence results
            where InStep : IPipelineStepResults, new() // previous step results
            where OutSeq : PipelineSequenceResults<OutStep>, new() // current sequence results
            where OutStep : IPipelineStepResults, new() // current step results
        {
            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteBullet(sequenceName);
            BuildConsole.IndentLevel++;

            // Pipeline sequence result
            var results = new OutSeq();

            if (!previousSequence.IsSuccess)
            {
                BuildConsole.WriteFailure("Skipping, previous pipeline step failed");
                results.IsSuccess = false;
                results.Exception = new Exception($"Skipped {sequenceName} sequence, previous sequence failed.");
                return results;
            }

            int stepCount = -1;
            foreach (var step in sequenceSteps)
            {
                stepCount++;
                BuildConsole.WriteBullet(step.Type);
                var stepResult = step.ExecuteStep(previousSequence, workspace);
                results.StepResults.Add(stepResult);
            }

            if (stepCount == 0)
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
