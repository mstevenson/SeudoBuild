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
        List<IArchiveModule> archiveModules = new List<IArchiveModule>();

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

            Type moduleInfo = null;
            try
            {
                Type[] types = assembly.GetTypes();
                Assembly core = AppDomain.CurrentDomain.GetAssemblies().Single(x => x.GetName().Name.Equals("SeudoBuild"));
                Type type = core.GetType("SeudoBuild.IBuildModule");
                foreach (var t in types)
                    if (type.IsAssignableFrom((Type)t))
                    {
                        moduleInfo = t;
                        break;
                    }

                if (moduleInfo != null)
                {
                    var obj = Activator.CreateInstance(moduleInfo);
                    IArchiveModule module = (IArchiveModule)obj;
                    archiveModules.Add(module);
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

        public ISourceStep CreateSourceStep(SourceStepConfig config)
        {
            // TODO
            return default(ISourceStep);
        }

        public IBuildStep CreateBuildStep(BuildStepConfig config)
        {
            // TODO
            return default(IBuildStep);
        }

        public IArchiveStep CreateArchiveStep(ArchiveStepConfig config)
        {
            foreach (var module in archiveModules)
            {
                if (module.MatchesConfigType(config))
                {
                    Activator.CreateInstance(module.ArchiveStepType, config);
                }
            }

            // TODO use Activator to create an archive step
            return default(IArchiveStep);
        }

        public IDistributeStep CreateDistributeStep(DistributeStepConfig config)
        {
            // TODO
            return default(IDistributeStep);
        }

        public INotifyStep CreateNotifyStep(NotifyStepConfig config)
        {
            // TODO
            return default(INotifyStep);
        }
    }
}
