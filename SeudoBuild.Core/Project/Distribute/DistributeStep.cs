using System.Collections.Generic;

namespace SeudoBuild
{
    public abstract class DistributeStep
    {
        public abstract string Type { get; }
        public abstract DistributeInfo Distribute(ArchiveStepResults archiveResults, Workspace workspace);
    }
}
