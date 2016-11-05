using System;
namespace UnityBuildServer
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
