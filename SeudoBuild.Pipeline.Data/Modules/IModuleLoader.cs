using System.Collections.Generic;

namespace SeudoBuild.Pipeline
{
    public interface IModuleLoader
    {
        IModuleRegistry Registry { get; }
    }
}
