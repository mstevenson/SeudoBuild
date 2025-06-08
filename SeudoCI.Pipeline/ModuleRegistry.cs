namespace SeudoCI.Pipeline;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SeudoCI.Pipeline.Shared;

public class ModuleRegistry : IModuleRegistry
{
    private record ModuleCategory(
        Type ModuleBaseType,
        Type ModuleStepBaseType,
        Type StepConfigBaseType,
        string CategoryName)
    {
        public readonly List<IModule> LoadedModules = [];
    }

    private readonly ModuleCategory[] _moduleCategories = DiscoverModuleCategories();

    private static ModuleCategory[] DiscoverModuleCategories()
    {
        var categories = new List<ModuleCategory>();
        
        // Get all types that implement IModule and have the ModuleCategoryAttribute
        var moduleTypes = Assembly.GetAssembly(typeof(IModule))!
            .GetTypes()
            .Where(t => t.IsInterface && typeof(IModule).IsAssignableFrom(t) && t != typeof(IModule))
            .Where(t => t.GetCustomAttribute<ModuleCategoryAttribute>() != null);

        foreach (var moduleType in moduleTypes)
        {
            var attribute = moduleType.GetCustomAttribute<ModuleCategoryAttribute>()!;
            categories.Add(new ModuleCategory(
                moduleType,
                attribute.StepInterfaceType,
                attribute.ConfigBaseType,
                attribute.CategoryName));
        }

        return categories.ToArray();
    }

    public IEnumerable<IModule> GetAllModules()
    {
        return _moduleCategories.Select(cat => cat.LoadedModules).SelectMany(m => m);
    }

    public IEnumerable<T> GetModules<T>()
        where T : IModule
    {
        try
        {
            var category = _moduleCategories.First(cat => cat.ModuleBaseType == typeof(T));
            return category.LoadedModules.Cast<T>();
        }
        catch
        {
            throw new ModuleLoadException($"Could not find modules of type {typeof(T)}");
        }
    }

    public IEnumerable<IModule> GetModulesForStepType<T>()
        where T : IPipelineStep
    {
        var category = _moduleCategories.First(cat => cat.ModuleStepBaseType == typeof(T));
        return category.LoadedModules;
    }

    public void RegisterModule(IModule module)
    {
        foreach (var category in _moduleCategories)
        {
            if (category.ModuleBaseType.IsInstanceOfType(module))
            {
                category.LoadedModules.Add(module);
            }
        }
    }

    public StepConfigConverter[] GetJsonConverters()
    {
        var converters = new Dictionary<Type, StepConfigConverter>();
        foreach (var category in _moduleCategories)
        {
            converters.Add(category.StepConfigBaseType, new StepConfigConverter(category.StepConfigBaseType));
        }

        foreach (var kvp in converters)
        {
            foreach (var module in GetAllModules())
            {
                Type configBaseType = kvp.Key;
                if (configBaseType.IsAssignableFrom(module.StepConfigType))
                {
                    converters[configBaseType].RegisterConfigType(module.StepConfigName, module.StepConfigType);
                }
            }
        }

        return converters.Values.ToArray();
    }
}