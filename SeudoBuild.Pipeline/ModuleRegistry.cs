using System;
using System.Collections.Generic;
using System.Linq;

namespace SeudoBuild.Pipeline
{
    public class ModuleRegistry : IModuleRegistry
    {
        // Contains refrences to all modules belonging to a specific base type: Source, Build, Archive, Distribute, or Notify
        class ModuleCategory
        {
            public readonly Type moduleBaseType; // inherits from IModule
            public readonly Type moduleStepBaseType; // inherits from IPipelineStep
            public readonly Type stepConfigBaseType; // inherits from StepConfig

            public readonly List<IModule> loadedModules = new List<IModule>();

            public ModuleCategory(Type moduleBaseType, Type moduleStepBaseType, Type stepConfigBaseType)
            {
                this.moduleBaseType = moduleBaseType;
                this.moduleStepBaseType = moduleStepBaseType;
                this.stepConfigBaseType = stepConfigBaseType;
            }
        }

        readonly ModuleCategory[] moduleCategories = {
            new ModuleCategory(typeof(ISourceModule), typeof(ISourceStep), typeof(SourceStepConfig)),
            new ModuleCategory(typeof(IBuildModule), typeof(IBuildStep), typeof(BuildStepConfig)),
            new ModuleCategory(typeof(IArchiveModule), typeof(IArchiveStep), typeof(ArchiveStepConfig)),
            new ModuleCategory(typeof(IDistributeModule), typeof(IDistributeStep), typeof(DistributeStepConfig)),
            new ModuleCategory(typeof(INotifyModule), typeof(INotifyStep), typeof(NotifyStepConfig))
        };

        public IEnumerable<IModule> GetAllModules()
        {
            return moduleCategories.Select(category => category.loadedModules).SelectMany(module => module);
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

        public IEnumerable<SerializedTypeMap> GetSerializedTypeMaps()
        {
            throw new NotImplementedException();

            var typeMaps = new List<SerializedTypeMap>();




            foreach (var category in moduleCategories)
            {
                typeMaps.Add(new SerializedTypeMap("TEMP", category.stepConfigBaseType));


                //converters.Add(category.stepConfigBaseType, new StepConfigConverter(category.stepConfigBaseType));
            }

            //var allModules = GetAllModules();

            //foreach (var kvp in converters)
            //{
            //    foreach (var module in allModules)
            //    {
            //        Type configBaseType = kvp.Key;
            //        if (configBaseType.IsAssignableFrom(module.StepConfigType))
            //        {
            //            converters[configBaseType].RegisterConfigType(module.StepConfigName, module.StepConfigType);
            //        }
            //    }
            //}

            //return converters.Values.ToArray();
        }
    }
}
