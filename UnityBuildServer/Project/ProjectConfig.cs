﻿using System.Collections.Generic;

namespace UnityBuildServer
{
    public class ProjectConfig
    {
        public string ProjectName { get; set; }
        public List<BuildTargetConfig> BuildTargets { get; set; } = new List<BuildTargetConfig>();
    }
}
