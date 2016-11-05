using System.Collections.Generic;
using UnityBuild.VCS;

namespace UnityBuild
{
    public class BuildTargetConfig
    {
        public string Name { get; set; }
        public VCSConfig VCSConfiguration { get; set; }
        public List<BuildStepConfig> BuildSteps { get; set; } = new List<BuildStepConfig>();
        public List<ArchiveStepConfig> ArchiveSteps { get; set; } = new List<ArchiveStepConfig>();
        public List<DistributeConfig> DistributeSteps { get; set; } = new List<DistributeConfig>();
        public List<NotifyConfig> NotifySteps { get; set; } = new List<NotifyConfig>();
    }
}
