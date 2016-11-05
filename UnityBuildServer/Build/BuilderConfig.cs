using System.Collections.Generic;

namespace UnityBuildServer
{
    public class BuilderConfig
    {
        public string ProjectsPath { get; set; }
        public List<string> UnityInstallations { get; set; } = new List<string>();
    }
}
