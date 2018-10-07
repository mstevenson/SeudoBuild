//#define DEBUG_ASSEMBLIES

using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Diagnostics;

namespace SeudoBuild.Pipeline
{
    public class ModuleLoader : IModuleLoader
    {
        public IModuleRegistry Registry { get; } = new ModuleRegistry();

        private const string PipelineNamespace = "SeudoBuild.Pipeline";
        private const string PipelineAssemblyName = "SeudoBuild.Pipeline.Shared";

        private readonly ILogger _logger;

        public ModuleLoader (ILogger logger)
        {
            _logger = logger;
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
                Assembly coreAssembly = AppDomain.CurrentDomain.GetAssemblies().Single(x => x.GetName().Name.Equals(PipelineAssemblyName));
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
                    DebugWrite($"       Activating type:  {moduleType}");
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
                DebugWrite($"[Started module {Path.GetFileName(moduleDirectory)}]");
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
                DebugWrite($"[Finished module {Path.GetFileName(moduleDirectory)}]");
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

                if (configType != module.StepConfigType)
                {
                    continue;
                }
                
                // Construct a pipeline step type with a generic parameter for our config type
                // Example:  IPipelineStepWithConfig<ZipArchiveConfig>
                Type pipelineStepWithConfigType = typeof(IInitializable<>).MakeGenericType(configType);




                // FIXME The GitSource module fails to find types belonging to the LibGit2Sharp assembly
                // LibGit2Sharp uses dllmap to point to platform-specific DLLs.
                // How should this be handled by the SeudoBuild plugin loader?


                // Instantiate a IPipelineStep object
                try
                {
                    var pipelineStepObj = Activator.CreateInstance(module.StepType);

                    // Initialize the pipeline step with the config object
                    var method = pipelineStepWithConfigType.GetMethod("Initialize");
                    method?.Invoke(pipelineStepObj, new object[] { config, workspace, logger });

                    var step = (T)pipelineStepObj;
                    return step;
                }
                catch (TypeLoadException e)
                {
                    logger.Write($"Loading module step failed:\n {e.Message}", LogType.Failure);
                }
            }
            return default(T);
        }

        [Conditional("DEBUG_ASSEMBLIES")]
        private void DebugWrite(string message)
        {
            _logger.Write(message, LogType.Debug);
        }
    }
}
