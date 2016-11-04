using System;
namespace UnityBuildServer
{
    public class ShellBuildStep : BuildStep
    {
        ShellBuildStepConfig config;

        public ShellBuildStep(ShellBuildStepConfig config)
        {
            this.config = config;
        }
    }
}
