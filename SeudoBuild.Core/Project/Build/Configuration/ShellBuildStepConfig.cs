using System;
namespace SeudoBuild
{
    public class ShellBuildStepConfig : BuildStepConfig
    {
        public override string Type { get; } = "Shell Script";

        public string Command { get; set; }
    }
}
