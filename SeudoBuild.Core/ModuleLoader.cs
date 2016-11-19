using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using Newtonsoft.Json;

namespace SeudoBuild
{
    // Based on http://stackoverflow.com/questions/1070787/writing-c-sharp-plugin-system

    public class ModuleLoader
    {
        class ModuleCategory
        {
            public readonly Type moduleType;
            public readonly Type moduleStepType;
            public readonly Type stepConfigType;
            public readonly List<IModule> loadedModules = new List<IModule>();

            public ModuleCategory(Type moduleType, Type moduleStepType, Type stepConfigType)
            {
                this.moduleType = moduleType;
                this.moduleStepType = moduleStepType;
                this.stepConfigType = stepConfigType;
            }
        }

        class ModuleCategory<T, U, V> : ModuleCategory
            where T : IModule
            where U : IPipelineStep
            where V : StepConfig
        {
            public ModuleCategory() : base(typeof(T), typeof(U), typeof(V)) {}
        }

        public class ModuleRegistry
        {
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
                    var category = moduleCategories.First(cat => cat.moduleType == typeof(T));
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
                var category = moduleCategories.First(cat => cat.moduleStepType == typeof(T));
                return category.loadedModules;
            }

            public void RegisterModule(IModule module)
            {
                foreach (var category in moduleCategories)
                {
                    if (category.moduleType.IsAssignableFrom(module.GetType()))
                    {
                        category.loadedModules.Add(module);
                    }
                }
            }
        }

        public ModuleRegistry Registry { get; } = new ModuleRegistry();

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
                foreach (var s in files)
                {
                    string path = Path.Combine(Environment.CurrentDirectory, s);
                    LoadAssembly(path);
                }
            }
        }

        public T CreatePipelineStep<T>(StepConfig config, Workspace workspace)
            where T : IPipelineStep
        {
            foreach (var module in Registry.GetModulesForStepType<T>())
            {
                if (config.GetType() == module.StepConfigType)
                {
                    // FIXME fragile, this requires each IModule to implement a constructor with an identical signature.
                    // Constructor args can't be enforced by the interface so this is a runtime error.
                    object obj = Activator.CreateInstance(module.StepType, config, workspace);
                    return (T)obj;
                }
            }
            return default(T);
        }

        public JsonConverter[] GetJsonConverters()
        {
            Dictionary<Type, StepConfigConverter> converters = new Dictionary<Type, StepConfigConverter>()
            {
                { typeof(SourceStepConfig), new StepConfigConverter<SourceStepConfig>() },
                { typeof(BuildStepConfig), new StepConfigConverter<BuildStepConfig>() },
                { typeof(ArchiveStepConfig), new StepConfigConverter<ArchiveStepConfig>() },
                { typeof(DistributeStepConfig), new StepConfigConverter<DistributeStepConfig>() },
                { typeof(NotifyStepConfig), new StepConfigConverter<NotifyStepConfig>() }
            };

            foreach (var kvp in converters)
            {
                foreach (var module in Registry.GetAllModules())
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
