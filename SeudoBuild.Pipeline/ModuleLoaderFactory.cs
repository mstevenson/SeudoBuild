using System;
using Path = System.IO.Path;
using System.Linq;

namespace SeudoBuild.Pipeline
{
    /// <summary>
    /// Creates a ModuleLoader object.
    /// </summary>
    public class ModuleLoaderFactory
    {
        public IModuleLoader Create(ILogger logger)
        {
            ModuleLoader loader = new ModuleLoader();
            string modulesDirectory = Path.Combine(Environment.CurrentDirectory, "Modules");
            loader.LoadAllAssemblies(modulesDirectory);

            logger.Write("Loading Pipeline Modules");
            logger.IndentLevel++;

            string line = "";

            line = "Source:      " + string.Join(", ", loader.Registry.GetModules<ISourceModule>().Select(m => m.Name).ToArray());
            logger.Write(line, LogType.Plus);

            line = "Build:       " + string.Join(", ", loader.Registry.GetModules<IBuildModule>().Select(m => m.Name).ToArray());
            logger.Write(line, LogType.Plus);

            line = "Archive:     " + string.Join(", ", loader.Registry.GetModules<IArchiveModule>().Select(m => m.Name).ToArray());
            logger.Write(line, LogType.Plus);

            line = "Distribute:  " + string.Join(", ", loader.Registry.GetModules<IDistributeModule>().Select(m => m.Name).ToArray());
            logger.Write(line, LogType.Plus);

            line = "Notify:      " + string.Join(", ", loader.Registry.GetModules<INotifyModule>().Select(m => m.Name).ToArray());
            logger.Write(line, LogType.Plus);

            logger.IndentLevel--;

            Console.WriteLine("");

            return loader;
        }
    }
}
