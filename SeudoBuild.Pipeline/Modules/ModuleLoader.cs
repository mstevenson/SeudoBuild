using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using Newtonsoft.Json;
using SeudoBuild.Pipeline;

namespace SeudoBuild.Pipeline
{
    // Based on http://stackoverflow.com/questions/1070787/writing-c-sharp-plugin-system

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

        readonly List<ModuleCategory> moduleCategories = new List<ModuleCategory>
            {
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
                throw new Exception($"Could not find modules of type {typeof(T)}");
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

    public class ModuleLoader : IModuleLoader
    {
        public IModuleRegistry Registry { get; } = new ModuleRegistry();

        public void LoadAssembly(string file)
        {
            if (!File.Exists(file))
            {
                return;
            }
            if (!file.EndsWith(".dll", true, null))
            {
                return;
            }

            Assembly moduleAssembly = null;

            try
            {
                moduleAssembly = Assembly.LoadFile(file);
            }
            catch (Exception e)
            {
                throw e;
            }

            var moduleTypesInAssembly = new List<Type>();

            try
            {
                string[] moduleCategoryTypeNames = {
                    nameof(ISourceModule),
                    nameof(IBuildModule),
                    nameof(IArchiveModule),
                    nameof(IDistributeModule),
                    nameof(INotifyModule)
                };

                // Locate all types in the assembly that inherit from IModule
                Type[] allTypesInAssembly = moduleAssembly.GetTypes();
                Assembly coreAssembly = AppDomain.CurrentDomain.GetAssemblies().Single(x => x.GetName().Name.Equals($"SeudoBuild.Core"));
                foreach (string moduleCategoryTypeName in moduleCategoryTypeNames)
                {
                    Type moduleCategoryType = coreAssembly.GetType($"SeudoBuild.{moduleCategoryTypeName}");
                    foreach (var type in allTypesInAssembly)
                    {
                        if (moduleCategoryType.IsAssignableFrom(type))
                        {
                            moduleTypesInAssembly.Add(type);
                        }
                    }
                }

                // Instantiate each IModule type
                foreach (Type moduleType in moduleTypesInAssembly)
                {
                    object obj = Activator.CreateInstance(moduleType);
                    Registry.RegisterModule((IModule)obj);
                }
            }
            catch (Exception e)
            {
                // TODO
            }
        }

        public void LoadAllAssemblies(string modulesFolder)
        {
            string[] moduleDirs = Directory.GetDirectories(modulesFolder);
            foreach (var dir in moduleDirs)
            {
                string[] files = Directory.GetFiles(dir, "*.dll");
                foreach (var file in files)
                {
                    LoadAssembly(file);
                }
            }
        }

        public T CreatePipelineStep<T>(StepConfig config, Workspace workspace)
            where T : IPipelineStep
        {
            foreach (var module in Registry.GetModulesForStepType<T>())
            {
                Type configType = config.GetType();
                
                if (configType == module.StepConfigType)
                {
                    // Construct a pipeline step type with a generic parameter for our config type
                    // Example:  IPipelineStepWithConfig<ZipArchiveConfig>
                    Type pipelineStepWithConfigType = typeof(IInitializable<>).MakeGenericType(configType);

                    // Instantiate a IPipelineStep object
                    object pipelineStepObj = Activator.CreateInstance(module.StepType);

                    // Initialize the pipeline step with the config object
                    var method = pipelineStepWithConfigType.GetMethod("Initialize");
                    method.Invoke(pipelineStepObj, new object[] { config, workspace });

                    T step = (T)pipelineStepObj;
                    return step;
                }
            }
            return default(T);
        }
    }
}
