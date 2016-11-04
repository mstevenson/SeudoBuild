using System;
namespace UnityBuildServer
{
    public abstract class BuildStep
    {
        public abstract string TypeName { get; }
        public abstract BuildInfo Execute();
    }
}
