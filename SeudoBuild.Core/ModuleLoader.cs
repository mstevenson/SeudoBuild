using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using Newtonsoft.Json;

namespace SeudoBuild
{
    // Based on http://stackoverflow.com/questions/1070787/writing-c-sharp-plugin-system

    public class ModuleLoader
    {
        Dictionary<Type, List<IModule>> stepTypeToModulesMap = new Dictionary<Type, List<IModule>>
        {
            { typeof(ISourceStep), new List<IModule>() },
            { typeof(IBuildStep), new List<IModule>() },
            { typeof(IArchiveStep), new List<IModule>() },
            { typeof(IDistributeStep), new List<IModule>() },
            { typeof(INotifyStep), new List<IModule>() }
        };

        public IEnumerable<ISourceModule> SourceModules
        {
            get { return stepTypeToModulesMap[typeof(ISourceStep)].Cast<ISourceModule>(); }
        }

        public IEnumerable<IBuildModule> BuildModules
        {
            get { return stepTypeToModulesMap[typeof(IBuildStep)].Cast<IBuildModule>(); }
        }

        public IEnumerable<IArchiveModule> ArchiveModules
        {
            get { return stepTypeToModulesMap[typeof(IArchiveStep)].Cast<IArchiveModule>(); }
        }

        public IEnumerable<IDistributeModule> DistributeModules
        {
            get { return stepTypeToModulesMap[typeof(IDistributeStep)].Cast<IDistributeModule>(); }
        }

        public IEnumerable<INotifyModule> NotifyModules
        {
            get { return stepTypeToModulesMap[typeof(INotifyStep)].Cast<INotifyModule>(); }
        }

        public JsonConverter[] GetJsonConverters ()
        {
            List<JsonConverter> converters = new List<JsonConverter>();

            var sourceConverter = new StepConfigConverter<SourceStepConfig>();
            var buildConverter = new StepConfigConverter<BuildStepConfig>();
            var archiveConverter = new StepConfigConverter<ArchiveStepConfig>();
            var distributeConverter = new StepConfigConverter<DistributeStepConfig>();
            var notifyConverter = new StepConfigConverter<NotifyStepConfig>();

            foreach (var kvp in stepTypeToModulesMap)
            {
                // Build a list of json converters, one for each base sep type (source, build, etc)
                // add specific step config types to each one as reported by each module

                foreach (var module in kvp.Value)
                {
                    if (typeof(SourceStepConfig).IsAssignableFrom(module.StepConfigType))
                    {
                        sourceConverter.RegisterConfigType(module.StepConfigName, module.StepConfigType);
                    }
                    if (typeof(BuildStepConfig).IsAssignableFrom(module.StepConfigType))
                    {
                        buildConverter.RegisterConfigType(module.StepConfigName, module.StepConfigType);
                    }
                    if (typeof(ArchiveStepConfig).IsAssignableFrom(module.StepConfigType))
                    {
                        archiveConverter.RegisterConfigType(module.StepConfigName, module.StepConfigType);
                    }
                    if (typeof(DistributeStepConfig).IsAssignableFrom(module.StepConfigType))
                    {
                        distributeConverter.RegisterConfigType(module.StepConfigName, module.StepConfigType);
                    }
                    if (typeof(NotifyStepConfig).IsAssignableFrom(module.StepConfigType))
                    {
                        notifyConverter.RegisterConfigType(module.StepConfigName, module.StepConfigType);
                    }
                }

            }
            converters.Add(sourceConverter);
            converters.Add(buildConverter);
            converters.Add(archiveConverter);
            converters.Add(distributeConverter);
            converters.Add(notifyConverter);

            return converters.ToArray();
        }

        //JsonConverter RegisterConverter<T>(IModule module)
        //    where T : StepConfig
        //{
        //    var converter = new StepConfigConverter<SourceStepConfig>();
        //    if (module.StepConfigType.IsAssignableFrom(typeof(SourceStepConfig)))
        //    {
        //        converter.RegisterConfigType(module.StepConfigName, module.StepConfigType);
        //    }
        //    return converter;
        //}

        public void LoadAssembly(string file)
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

                foreach (Type modType in moduleTypes)
                {
                    object obj = Activator.CreateInstance(modType);
                    if (typeof(ISourceModule).IsAssignableFrom(modType))
                    {
                        ISourceModule moduleInfo = (ISourceModule)obj;
                        stepTypeToModulesMap[typeof(ISourceStep)].Add(moduleInfo);
                    }
                    else if (typeof(IBuildModule).IsAssignableFrom(modType))
                    {
                        IBuildModule moduleInfo = (IBuildModule)obj;
                        stepTypeToModulesMap[typeof(IBuildStep)].Add(moduleInfo);
                    }
                    if (typeof(IArchiveModule).IsAssignableFrom(modType))
                    {
                        IArchiveModule moduleInfo = (IArchiveModule)obj;
                        stepTypeToModulesMap[typeof(IArchiveStep)].Add(moduleInfo);
                    }
                    if (typeof(IDistributeModule).IsAssignableFrom(modType))
                    {
                        IDistributeModule moduleInfo = (IDistributeModule)obj;
                        stepTypeToModulesMap[typeof(IDistributeStep)].Add(moduleInfo);
                    }
                    if (typeof(INotifyModule).IsAssignableFrom(modType))
                    {
                        INotifyModule moduleInfo = (INotifyModule)obj;
                        stepTypeToModulesMap[typeof(INotifyStep)].Add(moduleInfo);
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
                    LoadAssembly(path);
                }
            }
        }

        public T CreatePipelineStep<T>(StepConfig config, Workspace workspace)
            where T : IPipelineStep
        {
            var modulesForStep = stepTypeToModulesMap[typeof(T)];
            foreach (var module in modulesForStep)
            {
                if (config.GetType() == module.StepConfigType)
                {
                    // FIXME fragile, this requires each IModule to implement a constructor with an identical signature.
                    // Constructor args can't be enforced by the interface so this is a runtime error.
                    object obj = Activator.CreateInstance(module.StepType, config, workspace);
                    return (T)obj;
                }
            }
            return default(T);
        }
    }
}
