using System.IO;
using SeudoCI.Core;

namespace SeudoCI.Pipeline.Modules.UnityBuild
{
    public class UnityExecuteMethodBuildStep : UnityBuildStep<UnityExecuteMethodBuildConfig>
    {
        public override string Type { get; } = "Unity Execute Method";

        protected override string GetBuildArgs(UnityExecuteMethodBuildConfig config, ITargetWorkspace workspace)
        {
            string projectPath = Path.Combine(workspace.GetDirectory(TargetDirectory.Source), config.SubDirectory);
            string stdout = workspace.FileSystem.StandardOutputPath;
            string args = $"-quit -batchmode -executeMethod {config.MethodName} -projectPath {projectPath} -logfile {stdout}";
            return args;
        }
    }
}
