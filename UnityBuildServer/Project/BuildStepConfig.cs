using System;
namespace UnityBuildServer
{
    public abstract class BuildStepConfig
    {
        public abstract string Type { get; }
        public string Id { get; }
    }
}
