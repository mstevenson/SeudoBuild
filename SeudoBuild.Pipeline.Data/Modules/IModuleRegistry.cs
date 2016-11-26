using System.Collections.Generic;
using Newtonsoft.Json;

namespace SeudoBuild.Pipeline
{
    public interface IModuleRegistry
    {
        IEnumerable<IModule> GetAllModules();

        IEnumerable<T> GetModules<T>() where T : IModule;

        IEnumerable<IModule> GetModulesForStepType<T>() where T : IPipelineStep;

        void RegisterModule(IModule module);

        JsonConverter[] GetJsonConverters();
    }
}
