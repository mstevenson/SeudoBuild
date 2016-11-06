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
            BuildConsole.WriteLine("Starting build process:");
            BuildConsole.IndentLevel = 1;
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
            UpdateWorkingCopy(pipeline);

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
            BuildConsole.WriteLine("Build process completed.");
        }

        void UpdateWorkingCopy(ProjectPipeline pipeline)
        {
            VersionControlSystem vcs = pipeline.VersionControlSystem;

            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteLine($"+ Update working copy ({vcs.TypeName})");
            BuildConsole.IndentLevel = 2;

            if (vcs.IsWorkingCopyInitialized)
            {
                vcs.Update();
            }
            else
            {
                vcs.Download();
            }
        }

        BuildInfo Build(ProjectPipeline pipeline)
        {
            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteLine("+ Build");
            BuildConsole.IndentLevel = 1;

            // Bail if nothing to build
            if (pipeline.BuildSteps.Count == 0)
            {
                BuildConsole.WriteLine("No build steps");
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
                BuildConsole.WriteLine("+ " + step.TypeName);
                BuildConsole.IndentLevel = 3;
                var stepResult = step.Execute();
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
                BuildConsole.WriteLine("Build steps completed in " + buildInfo.BuildDuration.ToString(@"hh\:mm\:ss"));
            }
            else
            {
                BuildConsole.IndentLevel -= 2;
                Console.ForegroundColor = ConsoleColor.Red;
                BuildConsole.WriteLine($"# Build failed on step {stepIndex} ({currentStep.TypeName})");
                Console.ResetColor();
            }

            return buildInfo;
        }

        List<ArchiveInfo> Archive(BuildInfo buildInfo, ProjectPipeline pipeline)
        {
            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteLine("+ Archive");
            BuildConsole.IndentLevel = 1;

            if (buildInfo.Status == BuildCompletionStatus.Faulted)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                BuildConsole.WriteLine("# Skipping, previous build step failed");
                Console.ResetColor();

                // TODO return faulted status
                return null;
            }

            List<ArchiveInfo> archiveInfos = new List<ArchiveInfo>();

            foreach (var step in pipeline.ArchiveSteps)
            {
                BuildConsole.WriteLine("+ " + step.TypeName);
                BuildConsole.IndentLevel = 3;
                var info = step.CreateArchive(buildInfo, pipeline.Workspace);
                archiveInfos.Add(info);
            }

            if (pipeline.ArchiveSteps.Count == 0)
            {
                BuildConsole.IndentLevel = 2;
                BuildConsole.WriteLine("No archive steps");
            }

            return archiveInfos;
        }

        List<DistributeInfo> Distribute(List<ArchiveInfo> archiveInfos, ProjectPipeline pipeline)
        {
            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteLine("+ Distribute");
            BuildConsole.IndentLevel = 1;

            // TODO handle faulted state from the previous step, bail out, forward the error

            List<DistributeInfo> distributeInfos = new List<DistributeInfo>();

            foreach (var step in pipeline.DistributeSteps)
            {
                BuildConsole.WriteLine($"+ {step.TypeName}");
                BuildConsole.IndentLevel = 3;
                step.Distribute(archiveInfos, pipeline.Workspace);
            }

            if (pipeline.DistributeSteps.Count == 0)
            {
                BuildConsole.IndentLevel = 2;
                BuildConsole.WriteLine("No distribute steps");
            }

            return distributeInfos;
        }

        void Notify(List<DistributeInfo> distributeInfos, ProjectPipeline pipeline)
        {
            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteLine("+ Notify");
            BuildConsole.IndentLevel = 1;

            // TODO handle faulted state from the previous step, bail out, forward the error

            foreach (var step in pipeline.NotifySteps)
            {
                BuildConsole.WriteLine("+ " + step.TypeName);
                BuildConsole.IndentLevel = 3;
                step.Notify();
            }

            if (pipeline.NotifySteps.Count == 0)
            {
                BuildConsole.IndentLevel = 2;
                BuildConsole.WriteLine("No notify steps");
            }
        }
    }
}
