using System;
namespace UnityBuildServer
{
    public class UnityExecuteMethodStep : BuildStep
    {
        UnityExecuteMethodConfig config;
        Workspace workspace;

        public UnityExecuteMethodStep(UnityExecuteMethodConfig config, Workspace workspace)
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
