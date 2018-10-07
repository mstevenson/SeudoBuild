using System;
using System.Collections.Generic;
using System.Linq;

namespace SeudoBuild.Pipeline
{
    public class ProjectPipeline
    {
        private readonly Dictionary<Type, IEnumerable<IPipelineStep>> _stepTypeMap = new Dictionary<Type, IEnumerable<IPipelineStep>>();

        public ProjectConfig ProjectConfig { get; }
        public BuildTargetConfig TargetConfig { get; }

        public IReadOnlyCollection<T> GetPipelineSteps<T>()
            where T : IPipelineStep
        {
            _stepTypeMap.TryGetValue(typeof(T), out var steps);
            return steps.Cast<T>().ToList();
        }

        public ProjectPipeline (ProjectConfig config, string buildTargetName)
        {
            ProjectConfig = config;
            TargetConfig = ProjectConfig.BuildTargets.FirstOrDefault(t => t.TargetName == buildTargetName);
        }

        public void LoadBuildStepModules(IModuleLoader moduleLoader, ITargetWorkspace workspace, ILogger logger)
        {
            _stepTypeMap[typeof(ISourceStep)] = CreatePipelineSteps<ISourceStep>(moduleLoader, workspace, logger);
            _stepTypeMap[typeof(IBuildStep)] = CreatePipelineSteps<IBuildStep>(moduleLoader, workspace, logger);
            _stepTypeMap[typeof(IArchiveStep)] = CreatePipelineSteps<IArchiveStep>(moduleLoader, workspace, logger);
            _stepTypeMap[typeof(IDistributeStep)] = CreatePipelineSteps<IDistributeStep>(moduleLoader, workspace, logger);
            _stepTypeMap[typeof(INotifyStep)] = CreatePipelineSteps<INotifyStep>(moduleLoader, workspace, logger);
        }

        private IEnumerable<T> CreatePipelineSteps<T>(IModuleLoader loader, ITargetWorkspace workspace, ILogger logger)
            where T : class, IPipelineStep
        {
            var pipelineSteps = new List<T>();

            if (typeof(T) == typeof(ISourceStep))
            {
                foreach (var stepConfig in TargetConfig.SourceSteps)
                {
                    T step = loader.CreatePipelineStep<T>(stepConfig, workspace, logger);
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
                    var step = loader.CreatePipelineStep<T>(stepConfig, workspace, logger);
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
                    var step = loader.CreatePipelineStep<T>(stepConfig, workspace, logger);
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
                    var step = loader.CreatePipelineStep<T>(stepConfig, workspace, logger);
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
                    var step = loader.CreatePipelineStep<T>(stepConfig, workspace, logger);
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
