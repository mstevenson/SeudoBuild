using System;
using System.Collections.Generic;

namespace SeudoBuild.Agent
{
    public class BuildQueue : IBuildQueue
    {
        public BuildRequest ActiveBuild { get; private set; }

        public List<BuildRequest> QueuedBuilds { get; } = new List<BuildRequest>();

        public BuildRequest Build(ProjectConfig config, string target = null)
        {
            var request = new BuildRequest { ProjectConfiguration = config, TargetName = target };
            QueuedBuilds.Add(request);
            return request;
        }

        public BuildRequest GetBuildResult(Guid buildId)
        {
            // TODO
            return new BuildRequest();
        }

        public void CancelBuild(Guid buildId)
        {
        }
    }
}
