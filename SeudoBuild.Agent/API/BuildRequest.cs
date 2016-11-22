using System;
using SeudoBuild;

namespace SeudoBuild.Agent
{
    public class BuildRequest
    {
        public readonly Guid Guid;
        public ProjectConfig ProjectConfiguration { get; set; }
        public string TargetName { get; set; }

        public BuildRequest()
        {
            Guid = Guid.NewGuid();
        }
    }
}
