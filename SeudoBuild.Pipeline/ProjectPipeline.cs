using System;
using System.Collections.Generic;
using System.Linq;

namespace SeudoBuild.Pipeline
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

        public void LoadBuildStepModules(IModuleLoader moduleLoader)
        {
            stepTypeMap[typeof(ISourceStep)] = CreatePipelineSteps<ISourceStep>(moduleLoader, Workspace);
            stepTypeMap[typeof(IBuildStep)] = CreatePipelineSteps<IBuildStep>(moduleLoader, Workspace);
            stepTypeMap[typeof(IArchiveStep)] = CreatePipelineSteps<IArchiveStep>(moduleLoader, Workspace);
            stepTypeMap[typeof(IDistributeStep)] = CreatePipelineSteps<IDistributeStep>(moduleLoader, Workspace);
            stepTypeMap[typeof(INotifyStep)] = CreatePipelineSteps<INotifyStep>(moduleLoader, Workspace);
        }

        IReadOnlyCollection<T> CreatePipelineSteps<T>(IModuleLoader loader, Workspace workspace)
            where T : class, IPipelineStep
        {
            List<T> pipelineSteps = new List<T>();

            if (typeof(T) == typeof(ISourceStep))
            {
                foreach (var stepConfig in TargetConfig.SourceSteps)
                {
                    T step = loader.CreatePipelineStep<T>(stepConfig, workspace);
                    if (step != null)
                    {
                        pipelineSteps.Add(step);
                    }
                }
            }
            else if (typeof(T) == typeof(IBuildStep))
            {
                foreach (var stepConfig in TargetConfig.BuildSteps)
                {
                    T step = loader.CreatePipelineStep<T>(stepConfig, workspace);
                    if (step != null)
                    {
                        pipelineSteps.Add(step);
                    }
                }
            }
            else if (typeof(T) == typeof(IArchiveStep))
            {
                foreach (var stepConfig in TargetConfig.ArchiveSteps)
                {
                    T step = loader.CreatePipelineStep<T>(stepConfig, workspace);
                    if (step != null)
                    {
                        pipelineSteps.Add(step);
                    }
                }
            }
            else if (typeof(T) == typeof(IDistributeStep))
            {
                foreach (var stepConfig in TargetConfig.DistributeSteps)
                {
                    T step = loader.CreatePipelineStep<T>(stepConfig, workspace);
                    if (step != null)
                    {
                        pipelineSteps.Add(step);
                    }
                }
            }
            else if (typeof(T) == typeof(INotifyStep))
            {
                foreach (var stepConfig in TargetConfig.NotifySteps)
                {
                    T step = loader.CreatePipelineStep<T>(stepConfig, workspace);
                    if (step != null)
                    {
                        pipelineSteps.Add(step);
                    }
                }
            }

            return pipelineSteps.AsReadOnly();
        }
    }
}
