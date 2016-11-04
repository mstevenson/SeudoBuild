using System.Collections.Generic;
using UnityBuildServer.VersionControl;

namespace UnityBuildServer
{
    public class BuildTargetConfig
    {
        public string Id { get; set; }
        public IVCSConfiguration VCSConfiguration { get; set; }
        public List<BuildStepConfig> BuildSteps { get; set; } = new List<BuildStepConfig>();
        public List<ArchiveConfig> Archives { get; set; } = new List<ArchiveConfig>();
        public List<DistributionConfig> Distributions { get; set; } = new List<DistributionConfig>();
        public List<NotificationConfig> Notifications { get; set; } = new List<NotificationConfig>();
    }
}
