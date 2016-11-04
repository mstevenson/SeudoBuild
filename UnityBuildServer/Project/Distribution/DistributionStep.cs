using System;
namespace UnityBuildServer
{
    public abstract class DistributionStep
    {
        public abstract void Distribute(string archivePath, Workspace workspace);
    }
}
