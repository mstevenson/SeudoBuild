using System;
namespace UnityBuildServer
{
    public class UnityBuildStep : BuildStep
    {
        UnityBuildConfig config;

        public UnityBuildStep(UnityBuildConfig config)
        {
            this.config = config;
        }
    }
}
