namespace SeudoCI.Pipeline.Modules.UnityBuild;

using Core;

public class UnityExecuteMethodBuildStep : UnityBuildStep<UnityExecuteMethodBuildConfig>
{
    public override string Type => "Unity Execute Method";

    protected override string GetBuildArgs(UnityExecuteMethodBuildConfig config, ITargetWorkspace workspace)
    {
        string projectPath = Path.Combine(workspace.GetDirectory(TargetDirectory.Source), config.SubDirectory);
        string stdout = workspace.FileSystem.StandardOutputPath;
        string args = $"-quit -batchmode -executeMethod {config.MethodName} -projectPath {projectPath} -logfile {stdout}";
        return args;
    }
}