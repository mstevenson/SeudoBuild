//#define DEBUG_ASSEMBLIES

using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Diagnostics;
using SeudoCI.Core;

namespace SeudoCI.Pipeline
{
    public class ModuleLoader : IModuleLoader
    {
        public IModuleRegistry Registry { get; } = new ModuleRegistry();

        private const string PipelineNamespace = "SeudoCI.Pipeline";
        private const string PipelineAssemblyName = "SeudoCI.Pipeline.Shared";

        private readonly ILogger _logger;

        public ModuleLoader (ILogger logger)
        {
            _logger = logger;
        }

        private static Assembly[] PreloadAssemblies(string[] files)
        {
            var assemblies = new Assembly[files.Length];
            for (var i = 0; i < files.Length; i++)
            {
                var file = files[i];
                try
                {
                    var assembly = Assembly.LoadFile(file);
                    assemblies[i] = assembly;
                }
                catch (Exception e)
                {
                    throw new Exception($"Could not load assembly {file}: {e.Message}");
                }
            }
            return assemblies;
        }

        private bool TryLoadModuleAssembly(Assembly assembly)
        {
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
                Type[] allTypesInAssembly = assembly.GetTypes();

                var moduleTypesInAssembly = (
                    from baseTypeName in moduleBaseTypeNames
                    from type in allTypesInAssembly
                    where coreAssembly.GetType($"{PipelineNamespace}.{baseTypeName}").IsAssignableFrom(type)
                    select type
                );

                // Instantiate each IModule type
                bool found = false;
                foreach (Type moduleType in moduleTypesInAssembly)
                {
                    found = true;
                    DebugWrite($"       Activating type:  {moduleType}");
                    object obj = Activator.CreateInstance(moduleType);
                    Registry.RegisterModule((IModule)obj);
                }
                return found;
            }
            catch (Exception e)
            {
                throw new Exception($"Could not load module {assembly.FullName}: {e.Message}");
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
                var assemblies = PreloadAssemblies(files);
                foreach (var assembly in assemblies)
                {
                    try
                    {
                        var loaded = TryLoadModuleAssembly(assembly);
                        if (loaded)
                        {
                            DebugWrite($"    {Path.GetFileName(assembly.Location)}");
                        }
                    }
                    catch (FileLoadException e)
                    {
                        throw new ModuleLoadException($"Assembly for module {Path.GetFileName(moduleDirectory)} could not be loaded at {assembly.Location}", e);
                    }
                    catch (FileNotFoundException e)
                    {
                        throw new ModuleLoadException($"Assembly for module {Path.GetFileName(moduleDirectory)} was not found at {assembly.Location}", e);
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
                // How should this be handled by the SeudoCI plugin loader?


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
