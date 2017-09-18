using SeudoBuild;
using System.Collections.Generic;

namespace SeudoBuild.Pipeline
{
    /// <summary>
    /// Tracks pipeline modules that have been loaded and are available to be
    /// used by build scripts.
    /// </summary>
    public interface IModuleRegistry
    {
        IEnumerable<IModule> GetAllModules();

        IEnumerable<T> GetModules<T>() where T : IModule;

        IEnumerable<IModule> GetModulesForStepType<T>() where T : IPipelineStep;

        void RegisterModule(IModule module);

        IEnumerable<SerializedTypeMap> GetSerializedTypeMaps();
    }
}
