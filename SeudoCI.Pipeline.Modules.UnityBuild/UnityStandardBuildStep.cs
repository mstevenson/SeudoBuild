namespace SeudoCI.Pipeline.Modules.UnityBuild;

using SeudoCI.Core;
using Path = System.IO.Path;

public class UnityStandardBuildStep : UnityBuildStep<UnityStandardBuildConfig>
{
    public override string? Type => "Unity Standard Build";

    protected override IReadOnlyList<string> GetBuildArgs(UnityStandardBuildConfig config, ITargetWorkspace workspace)
    {
        var args = new List<string>
        {
            "-quit",
            "-batchmode",
            "-projectPath",
            Path.Combine(workspace.GetDirectory(TargetDirectory.Source), config.SubDirectory),
            "-logfile",
            workspace.FileSystem.StandardOutputPath
        };

        string executableExtension = "";

        switch (config.TargetPlatform)
        {
            case UnityPlatform.Mac:
                args.Add("-buildTarget");
                args.Add("StandaloneOSX");
                args.Add("-buildOSX64Player");
                executableExtension = ".app";
                break;
            case UnityPlatform.Windows:
                args.Add("-buildTarget");
                args.Add("StandaloneWindows64");
                args.Add("-buildWindows64Player");
                executableExtension = ".exe";
                break;
            case UnityPlatform.Linux:
                args.Add("-buildTarget");
                args.Add("StandaloneLinux64");
                args.Add("-buildLinux64Player");
                break;
        }

        var executableName = config.OutputName;
        if (!string.IsNullOrWhiteSpace(executableExtension))
        {
            executableName += executableExtension;
        }

        var outputPath = Path.Combine(workspace.GetDirectory(TargetDirectory.Output), executableName);
        args.Add(outputPath);

        return args;
    }
}