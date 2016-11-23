using System;
using SeudoBuild;

namespace SeudoBuild.Agent
{
    public class BuildRequest
    {
        public readonly int Id;
        public ProjectConfig ProjectConfiguration { get; set; }
        public string TargetName { get; set; }

        public BuildRequest(int id)
        {
            this.Id = id;
        }
    }
}
