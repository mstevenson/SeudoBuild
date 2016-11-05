using System.Collections.Generic;

namespace UnityBuildServer
{
    public abstract class DistributeStep
    {
        public abstract string TypeName { get; }
        public abstract void Distribute(List<ArchiveInfo> archiveInfos, Workspace workspace);
    }
}
