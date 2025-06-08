namespace SeudoCI.Pipeline;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core;
using SeudoCI.Pipeline.Shared;

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
        // Use reflection to discover all step interfaces with StepConfigMapping attributes
        var stepInterfaces = Assembly.GetAssembly(typeof(IPipelineStep))!
            .GetTypes()
            .Where(t => t.IsInterface && typeof(IPipelineStep).IsAssignableFrom(t) && t != typeof(IPipelineStep))
            .Where(t => t.GetCustomAttribute<StepConfigMappingAttribute>() != null);

        foreach (var stepInterface in stepInterfaces)
        {
            var steps = CreatePipelineSteps(stepInterface, moduleLoader, workspace, logger);
            _stepTypeMap[stepInterface] = steps;
        }
    }

    private IEnumerable<IPipelineStep> CreatePipelineSteps(Type stepInterfaceType, IModuleLoader loader, ITargetWorkspace workspace, ILogger logger)
    {
        var attribute = stepInterfaceType.GetCustomAttribute<StepConfigMappingAttribute>();
        if (attribute == null)
            return [];

        // Get the configuration collection using reflection
        var configProperty = typeof(BuildTargetConfig).GetProperty(attribute.ConfigPropertyName);
        if (configProperty == null)
            return [];

        var configCollection = configProperty.GetValue(TargetConfig) as System.Collections.IEnumerable;
        if (configCollection == null)
            return [];

        var pipelineSteps = new List<IPipelineStep>();
        
        // Use reflection to call CreatePipelineStep for each configuration
        var createStepMethod = typeof(IModuleLoader).GetMethod(nameof(IModuleLoader.CreatePipelineStep));
        if (createStepMethod == null)
            return [];

        foreach (StepConfig config in configCollection)
        {
            var genericMethod = createStepMethod.MakeGenericMethod(stepInterfaceType);
            var step = genericMethod.Invoke(loader, [config, workspace, logger]) as IPipelineStep;
            if (step != null)
                pipelineSteps.Add(step);
        }

        return pipelineSteps.AsReadOnly();
    }

    public IEnumerable<T> CreatePipelineSteps<T>(IModuleLoader loader, ITargetWorkspace workspace, ILogger logger)
        where T : class, IPipelineStep
    {
        return CreatePipelineSteps(typeof(T), loader, workspace, logger).Cast<T>();
    }
}