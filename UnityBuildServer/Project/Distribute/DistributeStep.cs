using System.Collections.Generic;

namespace UnityBuild
{
    public abstract class DistributeStep
    {
        public abstract string Type { get; }
        public abstract void Distribute(List<ArchiveInfo> archiveInfos, Workspace workspace);
    }
}
