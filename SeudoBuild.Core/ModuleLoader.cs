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
        List<ISourceModule> sourceModules = new List<ISourceModule>();
        List<IBuildModule> buildModules = new List<IBuildModule>();
        List<IArchiveModule> archiveModules = new List<IArchiveModule>();
        List<IDistributeModule> distributeModules = new List<IDistributeModule>();
        List<INotifyModule> notifyModules = new List<INotifyModule>();

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

            Assembly assembly = null;

            try
            {
                assembly = Assembly.LoadFile(file);
            }
            catch (Exception e)
            {
                throw e;
            }

            var moduleTypes = new List<Type>();
            try
            {
                Type[] types = assembly.GetTypes();
                Assembly coreAssembly = AppDomain.CurrentDomain.GetAssemblies().Single(x => x.GetName().Name.Equals("SeudoBuild"));
                foreach (string moduleInterfaceName in new string[] { nameof(ISourceModule), nameof(IBuildModule), nameof(IArchiveModule), nameof(IDistributeModule), nameof(INotifyModule) })
                {
                    Type targetType = coreAssembly.GetType($"SeudoBuild.{moduleInterfaceName}");
                    foreach (var type in types)
                    {
                        if (targetType.IsAssignableFrom(type))
                        {
                            moduleTypes.Add(type);
                            break;
                        }
                    }

                    foreach (Type type in moduleTypes)
                    {
                        object obj = Activator.CreateInstance(type);
                        if (type is ISourceModule)
                        {
                            ISourceModule moduleInfo = (ISourceModule)obj;
                            sourceModules.Add(moduleInfo);
                        }
                        else if (type is IBuildModule)
                        {
                            IBuildModule moduleInfo = (IBuildModule)obj;
                            buildModules.Add(moduleInfo);
                        }
                        if (type is IArchiveModule)
                        {
                            IArchiveModule moduleInfo = (IArchiveModule)obj;
                            archiveModules.Add(moduleInfo);
                        }
                        if (type is IDistributeModule)
                        {
                            IDistributeModule moduleInfo = (IDistributeModule)obj;
                            distributeModules.Add(moduleInfo);
                        }
                        if (type is INotifyModule)
                        {
                            INotifyModule moduleInfo = (INotifyModule)obj;
                            notifyModules.Add(moduleInfo);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                // TODO
            }
        }

        public void LoadAll()
        {
            string[] files = Directory.GetFiles("./Modules/", "*.dll");
            foreach (var s in files)
            {
                string path = Path.Combine(Environment.CurrentDirectory, s);
                Load(path);
            }

            //for (int i = 0; i < modules.Count; i++)
            //{
            //    IModule p = modules.ElementAt(i);
            //    try
            //    {
            //        if (!p.OnAllLoaded())
            //        {
            //            modules.RemoveAt(i);
            //            --i;
            //        }
            //    }
            //    catch (Exception)
            //    {
            //        modules.RemoveAt(i);
            //        --i;
            //    }
            //}
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
