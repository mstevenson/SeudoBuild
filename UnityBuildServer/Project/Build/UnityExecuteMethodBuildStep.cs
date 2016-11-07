
namespace UnityBuild
{
    public class UnityExecuteMethodBuildStep : UnityBuildStep
    {
        UnityExecuteMethodBuildConfig config;
        Workspace workspace;

        public UnityExecuteMethodBuildStep(UnityExecuteMethodBuildConfig config, Workspace workspace)
        {
            this.config = config;
            this.workspace = workspace;
        }

        public override string Type { get; } = "Unity Execute Method";

        public override BuildResult Execute()
        {
            // FIXME don't hard-code

            var version = new VersionNumber { Major = 5, Minor = 4, Patch = 2, Build = "f2" };

            var unityInstallation = new UnityInstallation
            {
                Version = version,
                ExePath = "/Applications/Unity/Unity.app/Contents/MacOS/Unity"
            };

            var args = $"-quit -batchmode -executeMethod {config.MethodName} -projectPath {workspace.WorkingDirectory}";
            var buildResult = ExecuteUnity(unityInstallation, args, workspace);

            return buildResult;
        }
    }
}
