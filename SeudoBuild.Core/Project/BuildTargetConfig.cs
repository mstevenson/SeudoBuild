using System.Collections.Generic;
using SeudoBuild.VCS;

namespace SeudoBuild
{
    public class BuildTargetConfig
    {
        public string TargetName { get; set; }
        public VCSConfig VCSConfiguration { get; set; }
        public List<SourceStepConfig> SourceSteps { get; set; } = new List<SourceStepConfig>();
        public List<BuildStepConfig> BuildSteps { get; set; } = new List<BuildStepConfig>();
        public List<ArchiveStepConfig> ArchiveSteps { get; set; } = new List<ArchiveStepConfig>();
        public List<DistributeStepConfig> DistributeSteps { get; set; } = new List<DistributeStepConfig>();
        public List<NotifyStepConfig> NotifySteps { get; set; } = new List<NotifyStepConfig>();
    }
}
