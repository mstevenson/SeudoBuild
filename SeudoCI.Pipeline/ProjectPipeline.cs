using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SeudoCI.Core;

namespace SeudoCI.Pipeline
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
            return steps == null ? new List<T>() : steps.Cast<T>().ToList();
        }

        public ProjectPipeline (ProjectConfig config, string buildTargetName)
        {
            ProjectConfig = config;
            TargetConfig = ProjectConfig.BuildTargets.FirstOrDefault(t => t.TargetName == buildTargetName);
        }

        public void LoadBuildStepModules(IModuleLoader moduleLoader, ITargetWorkspace workspace, ILogger logger)
        {
            void RegisterSteps<T>() where T : class, IPipelineStep
            {
                _stepTypeMap[typeof(T)] = CreatePipelineSteps<T>(moduleLoader, workspace, logger);
            }
            
            RegisterSteps<ISourceStep>();
            RegisterSteps<IBuildStep>();
            RegisterSteps<IArchiveStep>();
            RegisterSteps<IDistributeStep>();
            RegisterSteps<INotifyStep>();
        }

        public IEnumerable<T> CreatePipelineSteps<T>(IModuleLoader loader, ITargetWorkspace workspace, ILogger logger)
            where T : class, IPipelineStep
        {
            var pipelineSteps = new List<T>();

            void RegisterSteps<T2>(IEnumerable<T2> targetStepConfigs) where T2 : StepConfig
            {
                pipelineSteps.AddRange(targetStepConfigs
                    .Select(config => loader.CreatePipelineStep<T>(config, workspace, logger))
                    .Where(step => step != null));
            }
            
            switch (typeof(T))
            {
                case ISourceStep _:
                    RegisterSteps(TargetConfig.SourceSteps);
                    break;
                case IBuildStep _:
                    RegisterSteps(TargetConfig.BuildSteps);
                    break;
                case IArchiveStep _:
                    RegisterSteps(TargetConfig.ArchiveSteps);
                    break;
                case IDistributeStep _:
                    RegisterSteps(TargetConfig.DistributeSteps);
                    break;
                case INotifyStep _:
                    RegisterSteps(TargetConfig.NotifySteps);
                    break;
            }
            
            return pipelineSteps.AsReadOnly();
        }
    }
}
