using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;

namespace SeudoBuild.Pipeline
{
    public class ModuleLoader : IModuleLoader
    {
        public IModuleRegistry Registry { get; } = new ModuleRegistry();

        void LoadAssembly(string file)
        {
            // Absolute path
            file = Path.GetFullPath(file);

            if (!File.Exists(file))
            {
                return;
            }
            if (!file.EndsWith(".dll", true, null))
            {
                return;
            }

            Assembly moduleAssembly = null;

            moduleAssembly = Assembly.LoadFile(file);

            var moduleTypesInAssembly = new List<Type>();

            string[] moduleCategoryTypeNames = {
                nameof(ISourceModule),
                nameof(IBuildModule),
                nameof(IArchiveModule),
                nameof(IDistributeModule),
                nameof(INotifyModule)
            };

            try
            {
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
                    //try
                    //{
                        LoadAssembly(file);
                    //}
                    //catch (Exception e)
                    //{
                    //    if (e is FileLoadException || e is FileNotFoundException)
                    //    {
                    //        throw new ModuleLoadException("Assembly could not be loaded at " + file, e);
                    //    }
                    //    throw;
                    //}
                }
            }
        }

        public T CreatePipelineStep<T>(StepConfig config, IWorkspace workspace)
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
