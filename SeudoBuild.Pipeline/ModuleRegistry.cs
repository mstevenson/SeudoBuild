using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace SeudoBuild.Pipeline
{
    public class ModuleRegistry : IModuleRegistry
    {
        class ModuleCategory
        {
            public readonly Type moduleBaseType;
            public readonly Type moduleStepBaseType;
            public readonly Type stepConfigBaseType;
            public readonly List<IModule> loadedModules = new List<IModule>();

            public ModuleCategory(Type moduleBaseType, Type moduleStepBaseType, Type stepConfigBaseType)
            {
                this.moduleBaseType = moduleBaseType;
                this.moduleStepBaseType = moduleStepBaseType;
                this.stepConfigBaseType = stepConfigBaseType;
            }
        }

        class ModuleCategory<T, U, V> : ModuleCategory
            where T : IModule
            where U : IPipelineStep
            where V : StepConfig
        {
            public ModuleCategory() : base(typeof(T), typeof(U), typeof(V)) { }
        }

        readonly ModuleCategory[] moduleCategories = {
            new ModuleCategory<ISourceModule, ISourceStep, SourceStepConfig>(),
            new ModuleCategory<IBuildModule, IBuildStep, BuildStepConfig>(),
            new ModuleCategory<IArchiveModule, IArchiveStep, ArchiveStepConfig>(),
            new ModuleCategory<IDistributeModule, IDistributeStep, DistributeStepConfig>(),
            new ModuleCategory<INotifyModule, INotifyStep, NotifyStepConfig>()
        };

        public IEnumerable<IModule> GetAllModules()
        {
            return moduleCategories.Select(cat => cat.loadedModules).SelectMany(m => m);
        }

        public IEnumerable<T> GetModules<T>()
            where T : IModule
        {
            try
            {
                var category = moduleCategories.First(cat => cat.moduleBaseType == typeof(T));
                return category.loadedModules.Cast<T>();
            }
            catch
            {
                throw new ModuleLoadException($"Could not find modules of type {typeof(T)}");
            }
        }

        public IEnumerable<IModule> GetModulesForStepType<T>()
            where T : IPipelineStep
        {
            var category = moduleCategories.First(cat => cat.moduleStepBaseType == typeof(T));
            return category.loadedModules;
        }

        public void RegisterModule(IModule module)
        {
            foreach (var category in moduleCategories)
            {
                if (category.moduleBaseType.IsAssignableFrom(module.GetType()))
                {
                    category.loadedModules.Add(module);
                }
            }
        }

        public JsonConverter[] GetJsonConverters()
        {
            var converters = new Dictionary<Type, StepConfigConverter>();
            foreach (var category in moduleCategories)
            {
                converters.Add(category.stepConfigBaseType, new StepConfigConverter(category.stepConfigBaseType));
            }

            var allModules = GetAllModules();

            foreach (var kvp in converters)
            {
                foreach (var module in allModules)
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
