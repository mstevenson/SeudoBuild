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

        public void ExecuteBuildPipeline(ProjectConfig projectConfig, string buildTargetName)
        {
            if (projectConfig == null)
            {
                throw new ArgumentNullException("projectConfig", "A project configuration definition must be specified.");
            }
            if (string.IsNullOrEmpty (buildTargetName))
            {
                throw new ArgumentNullException("buildTargetName", "A project configuration definition must be specified.");
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

            // Setup
            var pipeline = ProjectPipeline.Create(builderConfig.ProjectsPath, projectConfig, buildTargetName);
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
                BuildConsole.WriteBullet($"{step.Type} (step {stepIndex}/{pipeline.BuildSteps.Count})");
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
                BuildConsole.WriteFailure($"Build failed on step {stepIndex} ({currentStep.Type})");
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
                BuildConsole.WriteBullet(step.Type);
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
                BuildConsole.WriteBullet(step.Type);
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
                BuildConsole.WriteBullet(step.Type);
                step.Notify();
            }

            BuildConsole.IndentLevel--;
        }
    }
}
