namespace SeudoCI.Pipeline.Modules.UnityBuild;

using Core;

public class UnityExecuteMethodBuildStep : UnityBuildStep<UnityExecuteMethodBuildConfig>
{
    public override string? Type => "Unity Execute Method";

    protected override IReadOnlyList<string> GetBuildArgs(UnityExecuteMethodBuildConfig config, ITargetWorkspace workspace)
    {
        return new List<string>
        {
            "-quit",
            "-batchmode",
            "-executeMethod",
            config.MethodName,
            "-projectPath",
            Path.Combine(workspace.GetDirectory(TargetDirectory.Source), config.SubDirectory),
            "-logfile",
            workspace.FileSystem.StandardOutputPath
        };
    }
}