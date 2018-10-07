using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SeudoBuild.Pipeline
{
    public class ModuleRegistry : IModuleRegistry
    {
        private class ModuleCategory
        {
            public readonly Type ModuleBaseType;
            public readonly Type ModuleStepBaseType;
            public readonly Type StepConfigBaseType;
            public readonly List<IModule> LoadedModules = new List<IModule>();

            protected ModuleCategory(Type moduleBaseType, Type moduleStepBaseType, Type stepConfigBaseType)
            {
                ModuleBaseType = moduleBaseType;
                ModuleStepBaseType = moduleStepBaseType;
                StepConfigBaseType = stepConfigBaseType;
            }
        }

        private class ModuleCategory<T, U, V> : ModuleCategory
            where T : IModule
            where U : IPipelineStep
            where V : StepConfig
        {
            public ModuleCategory() : base(typeof(T), typeof(U), typeof(V)) { }
        }

        private readonly ModuleCategory[] _moduleCategories = {
            new ModuleCategory<ISourceModule, ISourceStep, SourceStepConfig>(),
            new ModuleCategory<IBuildModule, IBuildStep, BuildStepConfig>(),
            new ModuleCategory<IArchiveModule, IArchiveStep, ArchiveStepConfig>(),
            new ModuleCategory<IDistributeModule, IDistributeStep, DistributeStepConfig>(),
            new ModuleCategory<INotifyModule, INotifyStep, NotifyStepConfig>()
        };

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
}
