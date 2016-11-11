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
            replacements["project_name"] = buildResults.ProjectName;
            replacements["build_target_name"] = buildResults.BuildTargetName;
            replacements["app_version"] = buildResults.AppVersion.ToString ();
            replacements["build_date"] = buildResults.BuildDate.ToString("yyyy-dd-M--HH-mm-ss");
            replacements["commit_identifier"] = buildResults.CommitIdentifier;

            // Archive
            var archiveResults = Archive(buildResults, pipeline);

            // Distribute
            var distributeResults = Distribute(archiveResults, pipeline);

            // Notify
            var notifyresults = Notify(archiveResults, distributeResults, pipeline);

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

        BuildStepResults Build(VCSResults vcsResults, ProjectPipeline pipeline)
        {
            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteBullet("Build");
            BuildConsole.IndentLevel = 1;

            // Bail if nothing to build
            if (pipeline.BuildSteps.Count == 0)
            {
                BuildConsole.WriteAlert("No build steps");
                //return new BuildStepResults { Status = BuildCompletionStatus.Faulted };
                return new BuildStepResults { IsSuccess = false, Exception = new Exception("No build steps.") };
            }

            bool isFaulted = false;

            // Delete all files in the build output directory
            pipeline.Workspace.CleanBuildOutputDirectory();

            // TODO add app version to BuildInfo
            BuildStepResults buildInfo = new BuildStepResults
            {
                BuildDate = DateTime.Now,
                ProjectName = pipeline.ProjectConfig.ProjectName,
                BuildTargetName = pipeline.TargetConfig.TargetName,
                CommitIdentifier = vcsResults.CurrentCommitIdentifier,
                //Status = BuildCompletionStatus.Running
            };

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
            buildInfo.BuildDuration = stopwatch.Elapsed;

            if (!isFaulted)
            {
                //buildInfo.Status = BuildCompletionStatus.Completed;
                buildInfo.IsSuccess = true;
                BuildConsole.WriteSuccess("Build steps completed in " + buildInfo.BuildDuration.ToString(@"hh\:mm\:ss"));
            }
            else
            {
                buildInfo.IsSuccess = false;
                string error = $"Build failed on step {stepIndex} ({currentStep.Type})";
                buildInfo.Exception = new Exception(error);
                BuildConsole.WriteFailure(error);
            }

            return buildInfo;
        }

        ArchiveStepResults Archive(BuildStepResults buildResults, ProjectPipeline pipeline)
        {
            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteBullet("Archive");
            BuildConsole.IndentLevel++;

            var results = new ArchiveStepResults();

            if (pipeline.ArchiveSteps.Count == 0)
            {
                BuildConsole.WriteAlert("No archive steps");
                results.IsSuccess = false;
                results.Exception = new Exception("No archive steps.");
                return results;
            }

            if (!buildResults.IsSuccess)
            {
                BuildConsole.WriteFailure("Skipping, previous build step failed");
                results.IsSuccess = false;
                results.Exception = new Exception("Skipped archival, previous step failed.");
                return results;
            }

            foreach (var step in pipeline.ArchiveSteps)
            {
                BuildConsole.WriteBullet(step.Type);
                var info = step.CreateArchive(buildResults, pipeline.Workspace);
                results.Infos.Add(info);
            }

            return results;
        }

        DistributeStepResults Distribute(ArchiveStepResults archiveResults, ProjectPipeline pipeline)
        {
            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteBullet("Distribute");
            BuildConsole.IndentLevel++;

            var results = new DistributeStepResults();

            if (pipeline.DistributeSteps.Count == 0)
            {
                BuildConsole.WriteAlert("No distribute steps");
                results.IsSuccess = false;
                results.Exception = new Exception("No distribute steps.");
                return results;
            }

            if (!archiveResults.IsSuccess)
            {
                BuildConsole.WriteFailure("Skipping, previous build step failed");
                results.IsSuccess = false;
                results.Exception = new Exception("Skipped distribute, previous step failed.");
                return results;
            }

            foreach (var step in pipeline.DistributeSteps)
            {
                BuildConsole.WriteBullet(step.Type);
                var info = step.Distribute(archiveResults, pipeline.Workspace);
                results.Infos.Add(info);
            }

            return results;
        }

        NotifyStepResults Notify(ArchiveStepResults archiveResults, DistributeStepResults distributeResults, ProjectPipeline pipeline)
        {
            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteBullet("Notify");
            BuildConsole.IndentLevel++;

            if (pipeline.NotifySteps.Count == 0)
            {
                BuildConsole.WriteAlert("No notify steps");
            }

            // TODO handle faulted state from the previous step, bail out, forward the error

            BuildConsole.IndentLevel++;

            var results = new NotifyStepResults();

            if (!archiveResults.IsSuccess || !distributeResults.IsSuccess)
            {
                BuildConsole.WriteFailure("Skipping, previous step failed");
                results.IsSuccess = false;
                results.Exception = new Exception("Skipped distribute, previous step failed.");
                return results;
            }

            foreach (var step in pipeline.NotifySteps)
            {
                BuildConsole.WriteBullet(step.Type);
                var info = step.Notify();
                results.Infos.Add(info);
            }

            BuildConsole.IndentLevel--;

            return results;
        }
    }
}
