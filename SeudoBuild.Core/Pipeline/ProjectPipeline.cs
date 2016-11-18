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

        public ProjectPipeline (ProjectConfig config, string buildTargetName)
        {
            this.ProjectConfig = config;
            TargetConfig = ProjectConfig.BuildTargets.FirstOrDefault(t => t.TargetName == buildTargetName);
        }

        public void InitializeWorkspace(string projectsBaseDirectory, IFileSystem fileSystem)
        {
            string projectNameSanitized = ProjectConfig.ProjectName.Replace(' ', '_');
            string projectDirectory = $"{projectsBaseDirectory}/{projectNameSanitized}";
            Workspace = new Workspace(projectDirectory, fileSystem);
            Workspace.CreateSubDirectories();
        }

        public void LoadBuildStepModules(ModuleLoader loader)
        {
            stepTypeMap[typeof(ISourceStep)] = CreatePipelineSteps<ISourceStep>(loader, Workspace);
            stepTypeMap[typeof(IBuildStep)] = CreatePipelineSteps<IBuildStep>(loader, Workspace);
            stepTypeMap[typeof(IArchiveStep)] = CreatePipelineSteps<IArchiveStep>(loader, Workspace);
            stepTypeMap[typeof(IDistributeStep)] = CreatePipelineSteps<IDistributeStep>(loader, Workspace);
            stepTypeMap[typeof(INotifyStep)] = CreatePipelineSteps<INotifyStep>(loader, Workspace);
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
