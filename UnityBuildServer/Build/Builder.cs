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
            var buildInfo = Build(pipeline);
            var archiveInfo = Archive(buildInfo, pipeline);
            var distributeInfo = Distribute(archiveInfo, pipeline);
            Notify(distributeInfo, pipeline);
        }

        void UpdateSource(ProjectPipeline pipeline)
        {
            Console.WriteLine("Updating source files");
        }

        BuildInfo Build(ProjectPipeline pipeline)
        {
            Console.WriteLine("Building");

            foreach (var step in pipeline.BuildSteps)
            {
                
            }

            return new BuildInfo();
        }

        List<ArchiveInfo> Archive(BuildInfo buildInfo, ProjectPipeline pipeline)
        {
            Console.WriteLine("Archiving");

            List<ArchiveInfo> archiveInfos = new List<ArchiveInfo>();

            foreach (var step in pipeline.ArchiveSteps)
            {
                //step.CreateArchive(buildInfo);
            }

            return archiveInfos;
        }

        List<DistributeInfo> Distribute(List<ArchiveInfo> archiveInfos, ProjectPipeline pipeline)
        {
            Console.WriteLine("Distributing");

            List<DistributeInfo> distributeInfos = new List<DistributeInfo>();

            foreach (var step in pipeline.DistributeSteps)
            {
                //step.Distribute (
            }

            return distributeInfos;
        }

        void Notify(List<DistributeInfo> distributeInfos, ProjectPipeline pipeline)
        {
            foreach (var step in pipeline.NotifySteps)
            {
                step.Notify();
            }

            Console.WriteLine("Notifying");
        }
    }
}
