using System;
using System.Collections.Generic;

namespace SeudoBuild
{
    public class ProjectPipeline
    {
        //public VersionControlSystem VersionControlSystem { get; private set; }
        public List<ISourceStep> SourceSteps { get; private set; }
        public List<IBuildStep> BuildSteps { get; private set; }
        public List<IArchiveStep> ArchiveSteps { get; private set; }
        public List<IDistributeStep> DistributeSteps { get; private set; }
        public List<INotifyStep> NotifySteps { get; private set; }

        public ProjectConfig ProjectConfig { get; private set; }
        public BuildTargetConfig TargetConfig { get; private set; }

        public Workspace Workspace { get; private set; }

        public static ProjectPipeline Create(string baseDirectory, ProjectConfig config, string buildTargetName, ModuleLoader loader)
        {
            var pipeline = new ProjectPipeline(config, buildTargetName);
            pipeline.Initialize(baseDirectory, buildTargetName, loader);
            return pipeline;
        }

        ProjectPipeline (ProjectConfig config, string buildTargetName)
        {
            this.ProjectConfig = config;
        }

        void Initialize(string projectsBaseDirectory, string buildTargetName, ModuleLoader loader)
        {
            string projectNameSanitized = ProjectConfig.ProjectName.Replace(' ', '_');
            string projectDirectory = $"{projectsBaseDirectory}/{projectNameSanitized}";

            Workspace = new Workspace(projectDirectory);
            BuildConsole.WriteLine("Saving to " + projectDirectory);
            Console.WriteLine("");
            Workspace.CreateSubDirectories();

            TargetConfig = GetBuildTargetConfig(buildTargetName);

            //VersionControlSystem = InitializeVersionControlSystem();
            SourceSteps = GetPipelineSteps<ISourceStep>(loader);
            BuildSteps = GetPipelineSteps<IBuildStep>(loader);
            ArchiveSteps = GetPipelineSteps<IArchiveStep>(loader);
            DistributeSteps = GetPipelineSteps<IDistributeStep>(loader);
            NotifySteps = GetPipelineSteps<INotifyStep>(loader);
        }

        BuildTargetConfig GetBuildTargetConfig(string targetName)
        {
            foreach (var t in ProjectConfig.BuildTargets)
            {
                if (t.TargetName == targetName)
                {
                    return t;
                }
            }
            return null;
        }

        List<T> GetPipelineSteps<T>(ModuleLoader loader)
            where T : IPipelineStep
        {
            var steps = new List<T>();
            foreach (var stepConfig in TargetConfig.SourceSteps)
            {
                T step = loader.CreatePipelineStep<T>(stepConfig);
                steps.Add(step);
            }
            return steps;
        }
    }
}
