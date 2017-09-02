using System.IO;

namespace SeudoBuild.Pipeline.Modules.UnityBuild
{
    public class UnityExecuteMethodBuildStep : UnityBuildStep<UnityExecuteMethodBuildConfig>
    {
        public override string Type { get; } = "Unity Execute Method";

        protected override string GetBuildArgs(UnityExecuteMethodBuildConfig config, ITargetWorkspace workspace)
        {
            string projectPath = Path.Combine(workspace.SourceDirectory, config.SubDirectory);
            string args = $"-quit -batchmode -executeMethod {config.MethodName} -projectPath {projectPath} -logfile {TargetWorkspace.StandardOutputPath}";
            return args;
        }
    }
}
