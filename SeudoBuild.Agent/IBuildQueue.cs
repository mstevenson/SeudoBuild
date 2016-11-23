using System;
using System.Collections.Generic;

namespace SeudoBuild.Agent
{
    public interface IBuildQueue
    {
        BuildRequest ActiveBuild { get; }

        BuildRequest Build(ProjectConfig config, string target = null);

        BuildResult GetBuildResult(Guid buildId);

        void CancelBuild(Guid buildId);
    }
}
