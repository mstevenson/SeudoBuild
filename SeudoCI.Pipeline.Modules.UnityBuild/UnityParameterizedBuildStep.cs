namespace SeudoCI.Pipeline.Modules.UnityBuild;

using Core;

public class UnityParameterizedBuildStep : UnityBuildStep<UnityParameterizedBuildConfig>
{
    public override string? Type => "Unity Parameterized Build";

    protected override IReadOnlyList<string> GetBuildArgs(UnityParameterizedBuildConfig config, ITargetWorkspace workspace)
    {
        // FIXME match this to the Unity build script method name
        const string methodName = "Builder.RemoteBuild";

        // Use System.Environment.GetCommandLineArgs in Unity to get custom arguments
        var args = new List<string>
        {
            "-quit",
            "-batchmode",
            "-executeMethod",
            methodName,
            "-projectPath",
            Path.Combine(workspace.GetDirectory(TargetDirectory.Source), config.SubDirectory),
            "-logfile",
            workspace.FileSystem.StandardOutputPath,

            // Custom args
            "-executableName",
            config.ExecutableName,
            "-targetPlatform",
            Enum.GetName(config.TargetPlatform)!,
            "-outputDirectory",
            workspace.GetDirectory(TargetDirectory.Output)
        };

        if (config.DevelopmentBuild)
        {
            args.Add("-developmentBuild");
        }
        if (config.SceneList.Count > 0)
        {
            args.Add("-sceneList");
            args.Add(string.Join(";", config.SceneList.ToArray()));
        }
        if (config.BuildAssetBundles)
        {
            args.Add("-buildAssetBundles");
        }
        if (config.CopyAssetBundlesToStreamingAssets)
        {
            args.Add("-copyAssetBundlesToStreamingAssets");
        }
        if (config.CustomDefines.Count > 0)
        {
            args.Add("-customDefines");
            args.Add(string.Join(";", config.CustomDefines.ToArray()));
        }
        if (!string.IsNullOrEmpty(config.PreBuildMethod))
        {
            args.Add("-preBuildMethod");
            args.Add(config.PreBuildMethod);
        }
        if (!string.IsNullOrEmpty(config.PreExportMethod))
        {
            args.Add("-preExportMethod");
            args.Add(config.PreExportMethod);
        }
        if (!string.IsNullOrEmpty(config.PostExportMethod))
        {
            args.Add("-postExportMethod");
            args.Add(config.PostExportMethod);
        }

        return args;
    }
}