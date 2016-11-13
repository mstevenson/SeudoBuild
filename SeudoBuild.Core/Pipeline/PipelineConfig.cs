﻿using System.Collections.Generic;

namespace SeudoBuild
{
    public class PipelineConfig
    {
        public string ProjectsPath { get; set; }
        public List<string> UnityInstallations { get; set; } = new List<string>();
    }
}