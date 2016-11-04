using System;
namespace UnityBuildServer
{
    public class ShellBuildStep : BuildStepConfig
    {
        public override string Type
        {
            get
            {
                return "Shell";
            }
        }

        public string Text { get; set; }
    }
}
