using System;

namespace SeudoBuild
{
    [Serializable]
    public abstract class StepConfig
    {
        public abstract string Type { get; }
    }
}
