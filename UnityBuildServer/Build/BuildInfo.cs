using System;
using System.Collections.Generic;

namespace UnityBuildServer
{
    public class BuildInfo
    {
        public string ProjectName { get; set; }
        public string BuildTargetName { get; set; }
        public DateTime BuildDate { get; set; } = DateTime.Now;
        public string CommitIdentifier { get; set; }
        public VersionNumber AppVersion { get; set; } = new VersionNumber();
    }
}
