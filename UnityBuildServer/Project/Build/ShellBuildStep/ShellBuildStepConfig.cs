using System;
namespace UnityBuild
{
    public class ShellBuildStepConfig : BuildStepConfig
    {
        public override string Type
        {
            get
            {
                return "Shell";
            }
        }

        public string Command { get; set; }
    }
}
