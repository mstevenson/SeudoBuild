using System.Collections.Generic;

namespace SeudoBuild
{
    public interface IModuleLoader
    {
        ModuleRegistry Registry { get; }
    }
}
