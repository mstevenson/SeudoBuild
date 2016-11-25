using System.IO;

namespace SeudoBuild.Modules.UnityBuild
{
    public class UnityExecuteMethodBuildStep : UnityBuildStep<UnityExecuteMethodBuildConfig>
    {
        public override string Type { get; } = "Unity Execute Method";

        protected override string GetBuildArgs(UnityExecuteMethodBuildConfig config, Workspace workspace)
        {
            string projectPath = Path.Combine(workspace.WorkingDirectory, config.SubDirectory);
            string args = $"-quit -batchmode -executeMethod {config.MethodName} -projectPath {projectPath} -logfile {Workspace.StandardOutputPath}";
            return args;
        }
    }
}
