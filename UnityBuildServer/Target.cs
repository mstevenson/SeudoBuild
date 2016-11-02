using System.Collections.Generic;

namespace UnityBuildServer
{
    public class Target
    {
        public string Name { get; set; }
        public string RepositoryBranch { get; set; }
        public bool AutoBuild { get; set; }
        public UnityBuildSettings UnityBuildStep { get; set; }
        public List<BuildStep> BuildSteps { get; set; } = new List<BuildStep>();
    }
}
