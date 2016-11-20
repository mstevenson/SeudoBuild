using System.IO;

namespace SeudoBuild.Modules.UnityBuild
{
    public class UnityExecuteMethodBuildStep : UnityBuildStep<UnityExecuteMethodBuildConfig>
    {
        public override string Type { get; } = "Unity Execute Method";

        public override BuildStepResults ExecuteStep(SourceSequenceResults vcsResults, Workspace workspace)
        {
            // FIXME don't hard-code

            var version = new VersionNumber { Major = 5, Minor = 4, Patch = 2, Build = "f2" };

            var unityInstallation = new UnityInstallation
            {
                Version = version,
                ExePath = "/Applications/Unity/Unity.app/Contents/MacOS/Unity"
            };

            string projectPath = Path.Combine(workspace.WorkingDirectory, config.SubDirectory);

            var args = $"-quit -batchmode -executeMethod {config.MethodName} -projectPath {projectPath} -logfile {Workspace.StandardOutputPath}";
            var results = ExecuteUnity(unityInstallation, args, workspace, config.SubDirectory);

            return results;
        }
    }
}
