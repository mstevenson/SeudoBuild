using System.Collections.Generic;
using SeudoBuild.Pipeline;

namespace SeudoBuild.Agent
{
    public interface IBuildQueue
    {
        BuildRequest ActiveBuild { get; }

        BuildRequest Build(ProjectConfig config, string target = null);

        List<BuildResult> GetAllBuildResults();

        BuildResult GetBuildResult(int buildId);

        void CancelBuild(int buildId);
    }
}
