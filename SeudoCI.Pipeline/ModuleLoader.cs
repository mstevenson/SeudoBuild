//#define DEBUG_ASSEMBLIES

namespace SeudoCI.Pipeline;

using System;
using System.Reflection;
using System.Diagnostics;
using Core;
using SeudoCI.Pipeline.Shared;

public class ModuleLoader(ILogger logger) : IModuleLoader
{
    public IModuleRegistry Registry { get; } = new ModuleRegistry();

    private const string PipelineNamespace = "SeudoCI.Pipeline";
    private const string PipelineAssemblyName = "SeudoCI.Pipeline.Shared";

    private bool TryLoadModuleAssembly(Assembly assembly)
    {
        try
        {
            // Get all module interface types using reflection instead of hard-coded names
            Assembly coreAssembly = AppDomain.CurrentDomain
                .GetAssemblies()
                .Single(x => string.Equals(x.GetName().Name, PipelineAssemblyName, StringComparison.Ordinal));
            var moduleInterfaceTypes = coreAssembly.GetTypes()
                .Where(t => t.IsInterface && typeof(IModule).IsAssignableFrom(t) && t != typeof(IModule))
                .Where(t => t.GetCustomAttribute<ModuleCategoryAttribute>() != null);

            Type[] allTypesInAssembly = assembly.GetTypes();

            var moduleTypesInAssembly = (
                from moduleInterfaceType in moduleInterfaceTypes
                from type in allTypesInAssembly
                where moduleInterfaceType.IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract
                select type
            );

            // Instantiate each IModule type
            bool found = false;
            foreach (Type moduleType in moduleTypesInAssembly)
            {
                found = true;
                DebugWrite($"       Activating type:  {moduleType}");
                object obj = Activator.CreateInstance(moduleType) ?? throw new InvalidOperationException();
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

            foreach (var path in files)
            {
                Assembly assembly;
                try
                {
                    assembly = Assembly.LoadFile(path);
                }
                catch (Exception e)
                {
                    throw new Exception($"Could not load assembly {path}: {e.Message}");
                }
                
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

    public T? CreatePipelineStep<T>(StepConfig config, ITargetWorkspace workspace, ILogger logger)
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
                if (pipelineStepObj == null)
                {
                    logger.Write($"Loading module step failed:\n Unable to create instance of {module.StepType}", LogType.Failure);
                    continue;
                }

                // Initialize the pipeline step with the config object
                var method = pipelineStepWithConfigType.GetMethod("Initialize");
                method?.Invoke(pipelineStepObj, [config, workspace, logger]);

                if (pipelineStepObj is T step)
                {
                    return step;
                }

                logger.Write($"Loading module step failed:\n Created instance of {module.StepType} does not implement {typeof(T)}", LogType.Failure);
            }
            catch (TypeLoadException e)
            {
                logger.Write($"Loading module step failed:\n {e.Message}", LogType.Failure);
            }
        }
        return default;
    }

    [Conditional("DEBUG_ASSEMBLIES")]
    private void DebugWrite(string? message)
    {
        logger.Write(message, LogType.Debug);
    }
}