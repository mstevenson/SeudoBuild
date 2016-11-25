using System;
using System.Collections.Generic;

namespace SeudoBuild
{
    [Serializable]
    public class ProjectConfig
    {
        public string ProjectName { get; set; }
        public List<BuildTargetConfig> BuildTargets { get; set; } = new List<BuildTargetConfig>();
    }
}
