using System;
using System.Collections.Generic;
using UnityBuild.VCS;
using System.Diagnostics;

namespace UnityBuild
{
    /// <summary>
    /// 
    /// Text replacement variables:
    /// %project_name% -- the name for the entire project
    /// %build_target_name% -- the specific target that was built
    /// %app_version% -- version number as major.minor.patch
    /// %build_date% -- the date that the build was completed
    /// %commit_identifier% -- the current commit number or hash
    /// </summary>
    public class Builder
    {
        BuilderConfig config;

        public Builder(BuilderConfig config)
        {
            this.config = config;
        }

        public void ExecuteBuild(ProjectConfig projectConfig, string buildTargetName)
        {
            Console.WriteLine("Starting build process:");

            BuildConsole.WriteLine($"Project: {projectConfig.ProjectName}");
            BuildConsole.WriteLine($"Target:  {buildTargetName}");
            BuildConsole.WriteLine("");

            // Establish build target
            BuildTargetConfig targetConfig = null;
            foreach (var target in projectConfig.BuildTargets)
            {
                if (target.TargetName == buildTargetName)
                {
                    targetConfig = target;
                }
            }
            if (targetConfig == null)
            {
                throw new Exception("Could not find target named " + buildTargetName);
            }

            // Setup
            var pipeline = ProjectPipeline.Create(config.ProjectsPath, projectConfig, buildTargetName);
            var replacements = pipeline.Workspace.Replacements;

            // Grab changes from version control system
            VCSResult updateResult = UpdateWorkingCopy(pipeline);

            // TODO handle VCS failure, pass the result through to the Build step

            // Build
            var buildInfo = Build(pipeline);
            replacements["project_name"] = buildInfo.ProjectName;
            replacements["build_target_name"] = buildInfo.BuildTargetName;
            replacements["app_version"] = buildInfo.AppVersion.ToString ();
            replacements["build_date"] = buildInfo.BuildDate.ToString("yyyy-dd-M--HH-mm-ss");
            replacements["commit_identifier"] = buildInfo.CommitIdentifier;

            // Archive
            var archiveInfos = Archive(buildInfo, pipeline);

            // Distribute
            var distributeInfos = Distribute(archiveInfos, pipeline);

            // Notify
            Notify(distributeInfos, pipeline);

            // Done
            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteLine("");
            Console.WriteLine("Build process completed.");
        }

        VCSResult UpdateWorkingCopy(ProjectPipeline pipeline)
        {
            VersionControlSystem vcs = pipeline.VersionControlSystem;

            BuildConsole.WriteBullet($"Update working copy ({vcs.TypeName})");
            BuildConsole.IndentLevel++;

            if (vcs.IsWorkingCopyInitialized)
            {
                vcs.Update();
            }
            else
            {
                vcs.Download();
            }

            // FIXME return a result object that indicates success or failure
            return new VCSResult { Success = true };
        }

        BuildInfo Build(ProjectPipeline pipeline)
        {
            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteBullet("Build");
            BuildConsole.IndentLevel = 1;

            // Bail if nothing to build
            if (pipeline.BuildSteps.Count == 0)
            {
                BuildConsole.WriteAlert("No build steps");
                return new BuildInfo { Status = BuildCompletionStatus.Faulted };
            }

            // Delete all files in the build output directory
            pipeline.Workspace.CleanBuildOutputDirectory();

            // TODO add commit identifier and app version to BuildInfo
            BuildInfo buildInfo = new BuildInfo
            {
                BuildDate = DateTime.Now,
                ProjectName = pipeline.ProjectConfig.ProjectName,
                BuildTargetName = pipeline.TargetConfig.TargetName,
                Status = BuildCompletionStatus.Running
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
                BuildConsole.WriteBullet($"{step.TypeName} (step {stepIndex}/{pipeline.BuildSteps.Count})");
                BuildConsole.IndentLevel++;

                var stepResult = step.Execute();

                BuildConsole.IndentLevel--;
                if (stepResult.Status == BuildCompletionStatus.Faulted)
                {
                    buildInfo.Status = BuildCompletionStatus.Faulted;
                    break;
                }
            }

            stopwatch.Stop();
            buildInfo.BuildDuration = stopwatch.Elapsed;

            if (buildInfo.Status != BuildCompletionStatus.Faulted)
            {
                buildInfo.Status = BuildCompletionStatus.Completed;
                BuildConsole.WriteSuccess("Build steps completed in " + buildInfo.BuildDuration.ToString(@"hh\:mm\:ss"));
            }
            else
            {
                BuildConsole.WriteFailure($"Build failed on step {stepIndex} ({currentStep.TypeName})");
            }

            return buildInfo;
        }

        List<ArchiveInfo> Archive(BuildInfo buildInfo, ProjectPipeline pipeline)
        {
            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteBullet("Archive");
            BuildConsole.IndentLevel++;

            if (pipeline.ArchiveSteps.Count == 0)
            {
                BuildConsole.WriteAlert("No archive steps");
                return null;
            }

            if (buildInfo.Status == BuildCompletionStatus.Faulted)
            {
                BuildConsole.WriteFailure("Skipping, previous build step failed");

                // TODO return faulted status
                return null;
            }

            List<ArchiveInfo> archiveInfos = new List<ArchiveInfo>();

            foreach (var step in pipeline.ArchiveSteps)
            {
                BuildConsole.WriteBullet(step.TypeName);
                var info = step.CreateArchive(buildInfo, pipeline.Workspace);
                archiveInfos.Add(info);
            }

            return archiveInfos;
        }

        List<DistributeInfo> Distribute(List<ArchiveInfo> archiveInfos, ProjectPipeline pipeline)
        {
            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteBullet("Distribute");
            BuildConsole.IndentLevel++;

            if (pipeline.DistributeSteps.Count == 0)
            {
                BuildConsole.WriteAlert("No distribute steps");
                return null;
            }

            // TODO handle faulted state from the previous step, bail out, forward the error

            List<DistributeInfo> distributeInfos = new List<DistributeInfo>();

            foreach (var step in pipeline.DistributeSteps)
            {
                BuildConsole.WriteBullet(step.TypeName);
                step.Distribute(archiveInfos, pipeline.Workspace);
            }

            return distributeInfos;
        }

        void Notify(List<DistributeInfo> distributeInfos, ProjectPipeline pipeline)
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

            foreach (var step in pipeline.NotifySteps)
            {
                BuildConsole.WriteBullet(step.TypeName);
                step.Notify();
            }

            BuildConsole.IndentLevel--;
        }
    }
}
