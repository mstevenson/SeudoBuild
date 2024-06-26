﻿namespace SeudoCI.Pipeline.Modules.UnityBuild;

using Core;

public class UnityParameterizedBuildStep : UnityBuildStep<UnityParameterizedBuildConfig>
{
    public override string Type => "Unity Parameterized Build";

    protected override string GetBuildArgs(UnityParameterizedBuildConfig config, ITargetWorkspace workspace)
    {
        // FIXME match this to the Unity build script method name
        const string methodName = "Builder.RemoteBuild";

        // Use System.Environment.GetCommandLineArgs in Unity to get custom arguments
        var args = new List<string>();
        args.Add("-quit");
        args.Add("-batchmode");
        args.Add("-executeMethod");
        args.Add(methodName);
        args.Add("-projectPath");
        args.Add(Path.Combine(workspace.GetDirectory(TargetDirectory.Source), config.SubDirectory));
        args.Add("-logfile");
        args.Add(workspace.FileSystem.StandardOutputPath);

        // Custom args

        args.Add("-executableName");
        args.Add(config.ExecutableName);

        args.Add("-targetPlatform");
        args.Add(System.Enum.GetName(typeof(UnityPlatform), config.TargetPlatform));

        args.Add("-outputDirectory");
        args.Add(workspace.GetDirectory(TargetDirectory.Output));

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

        var argString = string.Join(" ", args.ToArray());

        return argString;
    }
}