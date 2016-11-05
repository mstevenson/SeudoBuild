using System;
using System.Collections.Generic;
using System.IO;

namespace UnityBuildServer
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
                if (target.Name == buildTargetName)
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
            BuildConsole.WriteLine("Build completed.");
        }

        void UpdateWorkingCopy(ProjectPipeline pipeline)
        {
            VCS vcs = pipeline.VersionControlSystem;

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

            // Delete all files in the build output directory
            pipeline.Workspace.CleanBuildOutputDirectory();

            BuildInfo buildInfo = new BuildInfo
            {
                BuildDate = DateTime.Now,
                ProjectName = pipeline.ProjectConfig.ProjectName,
                BuildTargetName = pipeline.TargetConfig.Name
            };

            // TODO add commit identifier, app version, and build duration to BuildInfo

            foreach (var step in pipeline.BuildSteps)
            {
                BuildConsole.WriteLine("+ " + step.TypeName);
                BuildConsole.IndentLevel = 3;
                step.Execute();
            }

            return buildInfo;
        }

        List<ArchiveInfo> Archive(BuildInfo buildInfo, ProjectPipeline pipeline)
        {
            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteLine("+ Archive");
            BuildConsole.IndentLevel = 1;

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
