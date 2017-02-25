using System.Collections.Generic;
using SeudoBuild.Pipeline;

namespace SeudoBuild.Agent
{
    /// <summary>
    /// Queues projects and feeds them sequentially to a builder.
    /// </summary>
    public interface IBuildQueue
    {
        BuildResult ActiveBuild { get; }

        BuildResult EnqueueBuild(ProjectConfig config, string target = null);

        IEnumerable<BuildResult> GetAllBuildResults();

        BuildResult GetBuildResult(int buildId);

        BuildResult CancelBuild(int buildId);
    }
}
