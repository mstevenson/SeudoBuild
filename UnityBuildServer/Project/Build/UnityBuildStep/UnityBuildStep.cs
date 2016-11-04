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

        public override string TypeName
        {
            get
            {
                return "Unity Build";
            }
        }

        public override BuildInfo Execute()
        {
            // TODO

            return new BuildInfo();
        }
    }
}
