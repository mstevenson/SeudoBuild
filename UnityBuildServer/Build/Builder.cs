using System;
using System.Collections.Generic;
using System.IO;

namespace UnityBuildServer
{
    public class Builder
    {
        BuilderConfig config;

        public Builder(BuilderConfig config)
        {
            this.config = config;
        }

        public void ExecuteBuild(ProjectConfig projectConfig, string buildTargetName)
        {
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

            var pipeline = ProjectPipeline.Create(config.ProjectsPath, projectConfig, buildTargetName);

            UpdateWorkingCopy(pipeline);
            var buildInfo = Build(pipeline);
            var archiveInfos = Archive(buildInfo, pipeline);
            var distributeInfos = Distribute(archiveInfos, pipeline);
            Notify(distributeInfos, pipeline);

            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteLine("Build completed.");
        }

        void UpdateWorkingCopy(ProjectPipeline pipeline)
        {
            VCS vcs = pipeline.VersionControlSystem;

            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteLine($"+ Update working copy ({vcs.TypeName})");
            BuildConsole.IndentLevel = 1;

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
                ProjectName = pipeline.ProjectConfig.Name,
                BuildTargetName = pipeline.TargetConfig.Name
            };

            // TODO add commit identifier, app version, and build duration to BuildInfo

            foreach (var step in pipeline.BuildSteps)
            {
                BuildConsole.WriteLine("+ " + step.TypeName);
                BuildConsole.IndentLevel = 2;
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
                BuildConsole.IndentLevel = 2;
                // TODO pass BuildInfos
                step.CreateArchive(buildInfo, pipeline.Workspace);
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
                BuildConsole.IndentLevel = 2;
                step.Distribute(archiveInfos, pipeline.Workspace);
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
                BuildConsole.IndentLevel = 2;
                step.Notify();
            }
        }
    }
}
