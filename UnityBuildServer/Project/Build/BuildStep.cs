using System;
namespace UnityBuild
{
    public abstract class BuildStep
    {
        public abstract string TypeName { get; }
        public abstract void Execute();
    }
}
