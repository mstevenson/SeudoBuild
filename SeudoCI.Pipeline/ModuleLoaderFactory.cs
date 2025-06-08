namespace SeudoCI.Pipeline;

using System;
using System.Linq;
using Core;

/// <summary>
/// Creates a ModuleLoader object.
/// </summary>
public class ModuleLoaderFactory
{
    public static IModuleLoader Create(ILogger logger)
    {
        logger.Write("Loading Modules\n", LogType.Header);
        var loader = new ModuleLoader(logger);

        try
        {
            string modulesDirectory = Path.Combine(Environment.CurrentDirectory, "Modules");
            loader.LoadAllModules(modulesDirectory);
        }
        catch (ModuleLoadException e)
        {
            logger.Write(e.Message, LogType.Failure);
        }

        logger.IndentLevel++;

        string line;

        line = "    Source:  " + string.Join(", ", loader.Registry.GetModules<ISourceModule>().Select(m => m.Name).ToArray());
        logger.Write(line);

        line = "     Build:  " + string.Join(", ", loader.Registry.GetModules<IBuildModule>().Select(m => m.Name).ToArray());
        logger.Write(line);

        line = "   Archive:  " + string.Join(", ", loader.Registry.GetModules<IArchiveModule>().Select(m => m.Name).ToArray());
        logger.Write(line);

        line = "Distribute:  " + string.Join(", ", loader.Registry.GetModules<IDistributeModule>().Select(m => m.Name).ToArray());
        logger.Write(line);

        line = "    Notify:  " + string.Join(", ", loader.Registry.GetModules<INotifyModule>().Select(m => m.Name).ToArray());
        logger.Write(line);

        logger.IndentLevel--;

        return loader;
    }
}