using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;

namespace SeudoBuild
{
    // Based on http://stackoverflow.com/questions/1070787/writing-c-sharp-plugin-system

    public class ModuleLoader
    {
        public List<ISourceModule> sourceModules = new List<ISourceModule>();
        public List<IBuildModule> buildModules = new List<IBuildModule>();
        public List<IArchiveModule> archiveModules = new List<IArchiveModule>();
        public List<IDistributeModule> distributeModules = new List<IDistributeModule>();
        public List<INotifyModule> notifyModules = new List<INotifyModule>();

        public void Load(string file)
        {
            if (!File.Exists(file))
            {
                return;
            }
            if (!file.EndsWith(".dll", true, null))
            {
                return;
            }

            Assembly moduleAssembly = null;

            try
            {
                moduleAssembly = Assembly.LoadFile(file);
            }
            catch (Exception e)
            {
                throw e;
            }

            var moduleTypes = new List<Type>();

            try
            {
                foreach (string moduleInterfaceName in new string[] { nameof(ISourceModule), nameof(IBuildModule), nameof(IArchiveModule), nameof(IDistributeModule), nameof(INotifyModule) })
                {
                    Type[] types = moduleAssembly.GetTypes();
                    Assembly core = AppDomain.CurrentDomain.GetAssemblies().Single(x => x.GetName().Name.Equals($"SeudoBuild.Core"));

                    Type targetType = core.GetType($"SeudoBuild.{moduleInterfaceName}");
                    foreach (var type in types)
                    {
                        if (targetType.IsAssignableFrom(type))
                        {
                            moduleTypes.Add(type);
                        }
                    }
                }

                foreach (Type type in moduleTypes)
                {
                    object obj = Activator.CreateInstance(type);
                    if (typeof(ISourceModule).IsAssignableFrom(type))
                    {
                        ISourceModule moduleInfo = (ISourceModule)obj;
                        sourceModules.Add(moduleInfo);
                    }
                    else if (typeof(IBuildModule).IsAssignableFrom(type))
                    {
                        IBuildModule moduleInfo = (IBuildModule)obj;
                        buildModules.Add(moduleInfo);
                    }
                    if (typeof(IArchiveModule).IsAssignableFrom(type))
                    {
                        IArchiveModule moduleInfo = (IArchiveModule)obj;
                        archiveModules.Add(moduleInfo);
                    }
                    if (typeof(IDistributeModule).IsAssignableFrom(type))
                    {
                        IDistributeModule moduleInfo = (IDistributeModule)obj;
                        distributeModules.Add(moduleInfo);
                    }
                    if (typeof(INotifyModule).IsAssignableFrom(type))
                    {
                        INotifyModule moduleInfo = (INotifyModule)obj;
                        notifyModules.Add(moduleInfo);
                    }
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
                foreach (var s in files)
                {
                    string path = Path.Combine(Environment.CurrentDirectory, s);
                    Load(path);
                }
            }
        }

        public T CreatePipelineStep<T>(StepConfig config)
            where T : IPipelineStep
        {
            foreach (var module in archiveModules)
            {
                if (module.MatchesConfigType(config))
                {
                    Activator.CreateInstance(module.StepType, config);
                }
            }
            return default(T);
        }
    }
}
