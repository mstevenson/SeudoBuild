using System;
using SeudoBuild;

namespace SeudoBuild.Agent
{
    public class BuildRequest
    {
        public readonly Guid Id;
        public ProjectConfig ProjectConfiguration { get; set; }
        public string TargetName { get; set; }

        public BuildRequest()
        {
            Id = Guid.NewGuid();
        }
    }
}
