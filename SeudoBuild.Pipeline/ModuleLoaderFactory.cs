using System;
using Path = System.IO.Path;
using System.Linq;

namespace SeudoBuild.Pipeline
{
    public class ModuleLoaderFactory
    {
        public ModuleLoader Create(ILogger logger)
        {
            ModuleLoader loader = new ModuleLoader();
            string modulesDirectory = Path.Combine(Environment.CurrentDirectory, "Modules");
            loader.LoadAllAssemblies(modulesDirectory);

            logger.WriteLine("Loading Pipeline Modules");
            logger.IndentLevel++;

            string line = "";

            line = "Source:      " + string.Join(", ", loader.Registry.GetModules<ISourceModule>().Select(m => m.Name).ToArray());
            logger.WritePlus(line);

            line = "Build:       " + string.Join(", ", loader.Registry.GetModules<IBuildModule>().Select(m => m.Name).ToArray());
            logger.WritePlus(line);

            line = "Archive:     " + string.Join(", ", loader.Registry.GetModules<IArchiveModule>().Select(m => m.Name).ToArray());
            logger.WritePlus(line);

            line = "Distribute:  " + string.Join(", ", loader.Registry.GetModules<IDistributeModule>().Select(m => m.Name).ToArray());
            logger.WritePlus(line);

            line = "Notify:      " + string.Join(", ", loader.Registry.GetModules<INotifyModule>().Select(m => m.Name).ToArray());
            logger.WritePlus(line);

            logger.IndentLevel--;

            Console.WriteLine("");

            return loader;
        }
    }
}
