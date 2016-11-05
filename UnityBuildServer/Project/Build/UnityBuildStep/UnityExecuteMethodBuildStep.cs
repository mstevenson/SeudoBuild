using System;
namespace UnityBuild
{
    public class UnityExecuteMethodBuildStep : BuildStep
    {
        UnityExecuteMethodBuildConfig config;
        Workspace workspace;

        public UnityExecuteMethodBuildStep(UnityExecuteMethodBuildConfig config, Workspace workspace)
        {
            this.config = config;
            this.workspace = workspace;
        }

        public override string TypeName
        {
            get
            {
                return "Unity Execute Method";
            }
        }

        public override void Execute()
        {
            // TODO
        }
    }
}
