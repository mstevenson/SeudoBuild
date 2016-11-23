using System;
using Path = System.IO.Path;
using System.Linq;

namespace SeudoBuild
{
    public class ModuleLoaderFactory
    {
        public ModuleLoader Create()
        {
            ModuleLoader loader = new ModuleLoader();
            string modulesDirectory = Path.Combine(Environment.CurrentDirectory, "Modules");
            loader.LoadAllAssemblies(modulesDirectory);

            BuildConsole.WriteLine("Loading Pipeline Modules");
            BuildConsole.IndentLevel++;

            string line = "";

            line = "Source:      " + string.Join(", ", loader.Registry.GetModules<ISourceModule>().Select(m => m.Name).ToArray());
            BuildConsole.WritePlus(line);

            line = "Build:       " + string.Join(", ", loader.Registry.GetModules<IBuildModule>().Select(m => m.Name).ToArray());
            BuildConsole.WritePlus(line);

            line = "Archive:     " + string.Join(", ", loader.Registry.GetModules<IArchiveModule>().Select(m => m.Name).ToArray());
            BuildConsole.WritePlus(line);

            line = "Distribute:  " + string.Join(", ", loader.Registry.GetModules<IDistributeModule>().Select(m => m.Name).ToArray());
            BuildConsole.WritePlus(line);

            line = "Notify:      " + string.Join(", ", loader.Registry.GetModules<INotifyModule>().Select(m => m.Name).ToArray());
            BuildConsole.WritePlus(line);

            BuildConsole.IndentLevel--;

            Console.WriteLine("");

            return loader;
        }
    }
}
