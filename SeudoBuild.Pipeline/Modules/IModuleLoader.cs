using System.Collections.Generic;

namespace SeudoBuild.Pipeline
{
    public interface IModuleLoader
    {
        ModuleRegistry Registry { get; }
    }
}
