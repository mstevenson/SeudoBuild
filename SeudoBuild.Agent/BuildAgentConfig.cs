using System.Collections.Generic;

namespace SeudoBuild.Agent
{
    public class BuildAgentConfig
    {
        public string ListenPort { get; set; }
        public List<DeploymentTarget> DeploymentTargets { get; set; }

        public BuildAgentConfig()
        {
        }
    }
}
