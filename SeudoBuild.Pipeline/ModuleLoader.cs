#define DEBUG_ASSEMBLIES

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Diagnostics;

namespace SeudoBuild.Pipeline
{
    public class ModuleLoader : IModuleLoader
    {
        public IModuleRegistry Registry { get; } = new ModuleRegistry();

        const string pipelineNamespace = "SeudoBuild.Pipeline";
        const string pipelineAssemblyName = "SeudoBuild.Pipeline.Shared";

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

            Assembly moduleAssembly = Assembly.LoadFile(file);

            string[] moduleBaseTypeNames = {
                nameof(ISourceModule),
                nameof(IBuildModule),
                nameof(IArchiveModule),
                nameof(IDistributeModule),
                nameof(INotifyModule)
            };

            try
            {
                // Locate all types in the assembly that inherit from IModule
                Assembly coreAssembly = AppDomain.CurrentDomain.GetAssemblies().Single(x => x.GetName().Name.Equals(pipelineAssemblyName));
                Type[] allTypesInAssembly = moduleAssembly.GetTypes();

                var moduleTypesInAssembly = (
                    from baseTypeName in moduleBaseTypeNames
                    from type in allTypesInAssembly
                    where coreAssembly.GetType($"SeudoBuild.Pipeline.{baseTypeName}").IsAssignableFrom(type)
                    select type
                );

                // Instantiate each IModule type
                foreach (Type moduleType in moduleTypesInAssembly)
                {
                    DebugWrite($"Activating type: {moduleType}");
                    object obj = Activator.CreateInstance(moduleType);
                    Registry.RegisterModule((IModule)obj);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Could not load module {file}: {e.Message}");
            }
        }

        public void LoadAllAssemblies(string modulesFolder)
        {
            DebugWrite("Loading assemblies:\n");

            string[] moduleDirs = Directory.GetDirectories(modulesFolder);
            foreach (var dir in moduleDirs)
            {
                string[] files = Directory.GetFiles(dir, "*.dll");
                foreach (var file in files)
                {
                    try
                    {
                        LoadAssembly(file);
                        DebugWrite($"    {file}");
                    }
                    catch (Exception e)
                    {
                        if (e is FileLoadException || e is FileNotFoundException)
                        {
                            throw new ModuleLoadException("Assembly could not be loaded at " + file, e);
                        }
                        throw;
                    }
                }
            }
            DebugWrite("");
        }

        public T CreatePipelineStep<T>(StepConfig config, ITargetWorkspace workspace)
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

                    Console.WriteLine("fails after this step: " + module.StepType);

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

        [Conditional("DEBUG_ASSEMBLIES")]
        void DebugWrite(string line)
        {
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            Console.WriteLine(line);
            Console.ResetColor();
        }
    }
}
