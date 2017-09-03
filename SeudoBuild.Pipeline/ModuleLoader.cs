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

        readonly ILogger logger;

        public ModuleLoader (ILogger logger)
        {
            this.logger = logger;
        }

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
                    DebugWrite($"       Activating module type:  {moduleType}");
                    object obj = Activator.CreateInstance(moduleType);
                    Registry.RegisterModule((IModule)obj);
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Could not load module {file}: {e.Message}");
            }
        }

        public void LoadAllModules(string modulesBaseDirectory)
        {
            DebugWrite("Loading assemblies for all modules:\n");

            string[] moduleDirectories = Directory.GetDirectories(modulesBaseDirectory);
            foreach (var moduleDirectory in moduleDirectories)
            {
                string[] files = Directory.GetFiles(moduleDirectory, "*.dll");
                foreach (var file in files)
                {
                    try
                    {
                        LoadAssembly(file);
                        DebugWrite($"    {Path.GetFileName(Path.GetDirectoryName(file))}/{Path.GetFileName(file)}");
                    }
                    catch (FileLoadException e)
                    {
                        throw new ModuleLoadException($"Assembly for module {Path.GetFileName(moduleDirectory)} could not be loaded at {file}", e);
                    }
                    catch (FileNotFoundException e)
                    {
                        throw new ModuleLoadException($"Assembly for module {Path.GetFileName(moduleDirectory)} was not found at {file}", e);
                    }
                    catch
                    {
                        throw;
                    }
                }
                DebugWrite("");
            }
            DebugWrite("");
        }

        public T CreatePipelineStep<T>(StepConfig config, ITargetWorkspace workspace, ILogger logger)
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




                    // FIXME The GitSource module fails to find types belonging to the LibGit2Sharp assembly
                    // 


                    // FIXME Fails need to load LibGit2Sharp. How?
                    // Need to load all assemblies required by each module ahead of time?

                    // Instantiate a IPipelineStep object
                    object pipelineStepObj = null;
                    try
                    {
                        pipelineStepObj = Activator.CreateInstance(module.StepType);

                        // Initialize the pipeline step with the config object
                        var method = pipelineStepWithConfigType.GetMethod("Initialize");
                        method.Invoke(pipelineStepObj, new object[] { config, workspace, logger });

                        T step = (T)pipelineStepObj;
                        return step;
                    }
                    catch (TypeLoadException e)
                    {
                        logger.Write($"Loading module step failed:\n {e.Message}", LogType.Failure);
                    }
                }
            }
            return default(T);
        }

        [Conditional("DEBUG_ASSEMBLIES")]
        void DebugWrite(string message)
        {
            logger.Write(message, LogType.Debug);
        }
    }
}
