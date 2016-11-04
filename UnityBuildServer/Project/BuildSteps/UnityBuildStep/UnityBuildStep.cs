using System;
namespace UnityBuildServer
{
    public class UnityBuildStep : BuildStep
    {
        UnityBuildStepConfig config;

        public UnityBuildStep(UnityBuildStepConfig config)
        {
            this.config = config;
        }
    }
}
