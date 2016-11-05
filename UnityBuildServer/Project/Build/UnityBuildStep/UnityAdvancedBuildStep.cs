using System;
namespace UnityBuildServer
{
    public class UnityAdvancedBuildStep : BuildStep
    {
        UnityAdvancedBuildConfig config;
        Workspace workspace;

        public UnityAdvancedBuildStep(UnityAdvancedBuildConfig config, Workspace workspace)
        {
            this.config = config;
            this.workspace = workspace;
        }

        public override string TypeName
        {
            get
            {
                return "Unity Advanced Build";
            }
        }

        public override void Execute()
        {
            // TODO
        }
    }
}
