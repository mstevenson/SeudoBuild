using System.IO;

namespace SeudoBuild.Pipeline.Modules.UnityBuild
{
    public class UnityExecuteMethodBuildStep : UnityBuildStep<UnityExecuteMethodBuildConfig>
    {
        public override string Type { get; } = "Unity Execute Method";

        protected override string GetBuildArgs(UnityExecuteMethodBuildConfig config, IWorkspace workspace)
        {
            string projectPath = Path.Combine(workspace.WorkingDirectory, config.SubDirectory);
            string args = $"-quit -batchmode -executeMethod {config.MethodName} -projectPath {projectPath} -logfile {Workspace.StandardOutputPath}";
            return args;
        }
    }
}
