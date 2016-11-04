using System;
namespace UnityBuildServer
{
    public abstract class DistributeStep
    {
        public abstract string TypeName { get; }
        public abstract void Distribute(string archivePath, Workspace workspace);
    }
}
