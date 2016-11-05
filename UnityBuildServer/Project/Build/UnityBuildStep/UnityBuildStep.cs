using System;
namespace UnityBuildServer
{
    public class UnityBuildStep : BuildStep
    {
        UnityBuildConfig config;
        Workspace workspace;

        public UnityBuildStep(UnityBuildConfig config, Workspace workspace)
        {
            this.config = config;
            this.workspace = workspace;
        }

        public override string TypeName
        {
            get
            {
                return "Unity Build";
            }
        }

        public override void Execute()
        {
            // TODO
        }
    }
}
