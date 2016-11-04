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
                throw new System.Exception("Could not find target named " + buildTargetName);
            }

            var pipeline = ProjectPipeline.Create(config.ProjectsPath, projectConfig, buildTargetName);

            UpdateSource(pipeline);
            var buildInfos = Build(pipeline);
            var archiveInfos = Archive(buildInfos, pipeline);
            var distributeInfos = Distribute(archiveInfos, pipeline);
            Notify(distributeInfos, pipeline);

            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteLine("Build completed.");
        }

        void UpdateSource(ProjectPipeline pipeline)
        {
            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteLine("Updating source files");
        }

        List<BuildInfo> Build(ProjectPipeline pipeline)
        {
            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteLine("+ Build");
            BuildConsole.IndentLevel = 1;

            List<BuildInfo> buildInfos = new List<BuildInfo>();

            foreach (var step in pipeline.BuildSteps)
            {
                BuildConsole.WriteLine("+ " + step.TypeName);
                BuildConsole.IndentLevel = 2;
                var info = step.Execute();
                buildInfos.Add(info);
            }

            return buildInfos;
        }

        List<ArchiveInfo> Archive(List<BuildInfo> buildInfos, ProjectPipeline pipeline)
        {
            BuildConsole.IndentLevel = 0;
            BuildConsole.WriteLine("+ Archive");
            BuildConsole.IndentLevel = 1;

            List<ArchiveInfo> archiveInfos = new List<ArchiveInfo>();

            foreach (var step in pipeline.ArchiveSteps)
            {
                BuildConsole.WriteLine("+ " + step.TypeName);
                BuildConsole.IndentLevel = 2;
                //step.CreateArchive(buildInfo);
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
                //step.Distribute (
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
