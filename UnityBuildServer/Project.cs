using System.Collections.Generic;

namespace UnityBuildServer
{
    public class Project
    {
        public string Name { get; set; }
        public string RepositoryURL { get; set; }
        public List<Target> Targets { get; set; } = new List<Target>();
    }
}
