using System;
using System.Collections.Generic;
using System.Linq;

namespace SeudoBuild
{
    public class ProjectPipeline
    {
        Dictionary<Type, IEnumerable<IPipelineStep>> stepTypeMap = new Dictionary<Type, IEnumerable<IPipelineStep>>();

        public ProjectConfig ProjectConfig { get; private set; }
        public BuildTargetConfig TargetConfig { get; private set; }
        public Workspace Workspace { get; private set; }

        public IReadOnlyCollection<T> GetPipelineSteps<T>()
            where T : IPipelineStep
        {
            IEnumerable<IPipelineStep> steps = null;
            stepTypeMap.TryGetValue(typeof(T), out steps);
            return steps.Cast<T>().ToList();
        }

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
            stepTypeMap[typeof(ISourceStep)] = CreatePipelineSteps<ISourceStep>(loader, Workspace);
            stepTypeMap[typeof(IBuildStep)] = CreatePipelineSteps<IBuildStep>(loader, Workspace);
            stepTypeMap[typeof(IArchiveStep)] = CreatePipelineSteps<IArchiveStep>(loader, Workspace);
            stepTypeMap[typeof(IDistributeStep)] = CreatePipelineSteps<IDistributeStep>(loader, Workspace);
            stepTypeMap[typeof(INotifyStep)] = CreatePipelineSteps<INotifyStep>(loader, Workspace);
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

        IReadOnlyCollection<T> CreatePipelineSteps<T>(ModuleLoader loader, Workspace workspace)
            where T : class, IPipelineStep
        {
            IEnumerable<StepConfig> allStepConfigs = null;

            List<T> pipelineSteps = new List<T>();

            if (typeof(T) == typeof(ISourceStep))
            {
                allStepConfigs = TargetConfig.SourceSteps;
            }
            else if (typeof(T) == typeof(IBuildStep))
            {
                allStepConfigs = TargetConfig.BuildSteps;
            }
            else if (typeof(T) == typeof(IArchiveStep))
            {
                allStepConfigs = TargetConfig.ArchiveSteps;
            }
            else if (typeof(T) == typeof(IDistributeStep))
            {
                allStepConfigs = TargetConfig.DistributeSteps;
            }
            else if (typeof(T) == typeof(INotifyStep))
            {
                allStepConfigs = TargetConfig.NotifySteps;
            }

            foreach (var stepConfig in allStepConfigs)
            {
                T step = loader.CreatePipelineStep<T>(stepConfig, workspace);
                if (step != null)
                {
                    pipelineSteps.Add(step);
                }
            }
            return pipelineSteps.AsReadOnly();
        }
    }
}
