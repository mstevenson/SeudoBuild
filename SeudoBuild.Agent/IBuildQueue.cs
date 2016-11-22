using System;
using System.Collections.Generic;

namespace SeudoBuild.Agent
{
    public interface IBuildQueue
    {
        BuildRequest ActiveBuild { get; }
        List<BuildRequest> QueuedBuilds { get; }

        BuildRequest Build(ProjectConfig config, string target = null);

        BuildRequest GetBuildResult(Guid buildId);

        void CancelBuild(Guid buildId);
    }
}
