namespace SeudoCI.Pipeline;

using System;
using System.Collections.Generic;
using System.Linq;
using Core;

public class ProjectPipeline
{
    private readonly Dictionary<Type, IEnumerable<IPipelineStep>> _stepTypeMap = new();

    public ProjectConfig ProjectConfig { get; }
    public BuildTargetConfig TargetConfig { get; }

    public IReadOnlyCollection<T> GetPipelineSteps<T>()
        where T : IPipelineStep
    {
        _stepTypeMap.TryGetValue(typeof(T), out var steps);
        return steps == null ? [] : steps.Cast<T>().ToList();
    }

    public ProjectPipeline(ProjectConfig config, string buildTargetName)
    {
        ProjectConfig = config;
        TargetConfig = ProjectConfig.BuildTargets.FirstOrDefault(t => t.TargetName == buildTargetName) ?? 
                        throw new ArgumentException($"Build target '{buildTargetName}' not found in project '{ProjectConfig.ProjectName}'.");
    }

    public void LoadBuildStepModules(IModuleLoader moduleLoader, ITargetWorkspace workspace, ILogger logger)
    {
        _stepTypeMap[typeof(ISourceStep)] = CreatePipelineSteps<ISourceStep>(moduleLoader, workspace, logger);
        _stepTypeMap[typeof(IBuildStep)] = CreatePipelineSteps<IBuildStep>(moduleLoader, workspace, logger);
        _stepTypeMap[typeof(IArchiveStep)] = CreatePipelineSteps<IArchiveStep>(moduleLoader, workspace, logger);
        _stepTypeMap[typeof(IDistributeStep)] = CreatePipelineSteps<IDistributeStep>(moduleLoader, workspace, logger);
        _stepTypeMap[typeof(INotifyStep)] = CreatePipelineSteps<INotifyStep>(moduleLoader, workspace, logger);
    }

    public IEnumerable<T> CreatePipelineSteps<T>(IModuleLoader loader, ITargetWorkspace workspace, ILogger logger)
        where T : class, IPipelineStep
    {
        var pipelineSteps = new List<T>();

        void RegisterSteps<T2>(IEnumerable<T2> targetStepConfigs) where T2 : StepConfig
        {
            pipelineSteps.AddRange(targetStepConfigs
                .Select(config => loader.CreatePipelineStep<T>(config, workspace, logger)));
        }

        if (typeof(T).IsSubclassOf(typeof(ISourceStep)))
            RegisterSteps(TargetConfig.SourceSteps);
        else if (typeof(T).IsSubclassOf(typeof(IBuildStep)))
            RegisterSteps(TargetConfig.BuildSteps);
        else if (typeof(T).IsSubclassOf(typeof(IArchiveStep)))
            RegisterSteps(TargetConfig.ArchiveSteps);
        else if (typeof(T).IsSubclassOf(typeof(IDistributeStep)))
            RegisterSteps(TargetConfig.DistributeSteps);
        else if (typeof(T).IsSubclassOf(typeof(INotifyStep)))
            RegisterSteps(TargetConfig.NotifySteps);

        return pipelineSteps.AsReadOnly();
    }
}