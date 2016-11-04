using System;
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

            var pipeline = new ProjectPipeline(projectConfig, buildTargetName);
            pipeline.Initialize(config.ProjectsPath);

            UpdateSource(pipeline);
            Build(pipeline);
            Archive(pipeline);
            Distribute(pipeline);
            Notify(pipeline);
        }

        void UpdateSource(ProjectPipeline pipeline)
        {
            Console.WriteLine("Updating source files");
        }

        void Build(ProjectPipeline pipeline)
        {
            Console.WriteLine("Building");
        }

        void Archive(ProjectPipeline pipeline)
        {
            Console.WriteLine("Archiving");
        }

        void Distribute(ProjectPipeline pipeline)
        {
            Console.WriteLine("Distributing");
        }

        void Notify(ProjectPipeline pipeline)
        {
            Console.WriteLine("Notifying");
        }
    }
}
