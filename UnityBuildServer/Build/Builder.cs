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
        }

        void UpdateSource(ProjectPipeline pipeline)
        {
            Console.WriteLine("Updating source files");
        }

        List<BuildInfo> Build(ProjectPipeline pipeline)
        {
            Console.WriteLine("+ Build");

            List<BuildInfo> buildInfos = new List<BuildInfo>();

            foreach (var step in pipeline.BuildSteps)
            {
                Console.WriteLine("  + " + step.TypeName);
                var info = step.Execute();
                buildInfos.Add(info);
            }

            return buildInfos;
        }

        List<ArchiveInfo> Archive(List<BuildInfo> buildInfos, ProjectPipeline pipeline)
        {
            Console.WriteLine("+ Archive");

            List<ArchiveInfo> archiveInfos = new List<ArchiveInfo>();

            foreach (var step in pipeline.ArchiveSteps)
            {
                Console.WriteLine("  + " + step.TypeName);
                //step.CreateArchive(buildInfo);
            }

            return archiveInfos;
        }

        List<DistributeInfo> Distribute(List<ArchiveInfo> archiveInfos, ProjectPipeline pipeline)
        {
            Console.WriteLine("+ Distribute");

            List<DistributeInfo> distributeInfos = new List<DistributeInfo>();

            foreach (var step in pipeline.DistributeSteps)
            {
                Console.WriteLine($"  + {step.TypeName}");
                //step.Distribute (
            }

            return distributeInfos;
        }

        void Notify(List<DistributeInfo> distributeInfos, ProjectPipeline pipeline)
        {
            Console.WriteLine("+ Notify");

            foreach (var step in pipeline.NotifySteps)
            {
                Console.WriteLine("  + " + step.TypeName);
                step.Notify();
            }
        }
    }
}
