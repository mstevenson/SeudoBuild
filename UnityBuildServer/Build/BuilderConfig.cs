using System.Collections.Generic;

namespace UnityBuild
{
    public class BuilderConfig
    {
        public string ProjectsPath { get; set; }
        public List<string> UnityInstallations { get; set; } = new List<string>();
    }
}
