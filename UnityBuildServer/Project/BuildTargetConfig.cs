using System.Collections.Generic;

namespace UnityBuildServer
{
    public class BuildTargetConfig
    {
        public string Name { get; set; }
        public IVCSConfiguration VCSConfiguration { get; set; }
        public List<BuildStepConfig> BuildSteps { get; set; } = new List<BuildStepConfig>();
        public List<ArchiveStepConfig> ArchiveSteps { get; set; } = new List<ArchiveStepConfig>();
        public List<DistributionConfig> DistributionSteps { get; set; } = new List<DistributionConfig>();
        public List<NotificationConfig> NotificationSteps { get; set; } = new List<NotificationConfig>();
    }
}
