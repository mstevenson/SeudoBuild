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
            SourceSteps = GenerateSourceSteps(loader);
            BuildSteps = GenerateBuildSteps(loader);
            ArchiveSteps = GenerateArchiveSteps(loader);
            DistributeSteps = GenerateDistributeSteps(loader);
            NotifySteps = GenerateNotifySteps(loader);
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

        List<ISourceStep> GenerateSourceSteps(ModuleLoader loader)
        {
            var steps = new List<ISourceStep>();
            foreach (var stepConfig in TargetConfig.SourceSteps)
            {
                ISourceStep step = loader.CreateSourceStep(stepConfig);
                steps.Add(step);
            }
            return steps;
        }

        List<IBuildStep> GenerateBuildSteps(ModuleLoader loader)
        {
            var steps = new List<IBuildStep>();
            foreach (var stepConfig in TargetConfig.BuildSteps)
            {
                IBuildStep step = loader.CreateBuildStep(stepConfig);
                steps.Add(step);
            }
            return steps;
        }

        List<IArchiveStep> GenerateArchiveSteps(ModuleLoader loader)
        {
            var steps = new List<IArchiveStep>();
            foreach (var stepConfig in TargetConfig.ArchiveSteps)
            {
                IArchiveStep archiveStep = loader.CreateArchiveStep(stepConfig);
                steps.Add(archiveStep);
            }
            return steps;
        }

        List<IDistributeStep> GenerateDistributeSteps(ModuleLoader loader)
        {
            var steps = new List<IDistributeStep>();
            foreach (var stepConfig in TargetConfig.DistributeSteps)
            {
                IDistributeStep step = loader.CreateDistributeStep(stepConfig);
                steps.Add(step);
            }
            return steps;
        }

        List<INotifyStep> GenerateNotifySteps(ModuleLoader loader)
        {
            var steps = new List<INotifyStep>();
            foreach (var stepConfig in TargetConfig.NotifySteps)
            {
                INotifyStep step = loader.CreateNotifyStep(stepConfig);
                steps.Add(step);
            }
            return steps;
        }
    }
}
