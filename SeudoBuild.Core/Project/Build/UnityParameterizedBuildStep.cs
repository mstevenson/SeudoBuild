using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using RunProcessAsTask;
using System.IO;

namespace SeudoBuild
{
    public class UnityParameterizedBuildStep : UnityBuildStep
    {
        UnityParameterizedBuildConfig config;
        Workspace workspace;

        public UnityParameterizedBuildStep(UnityParameterizedBuildConfig config, Workspace workspace)
        {
            this.config = config;
            this.workspace = workspace;
        }

        public override string Type { get; } = "Unity Parameterized Build";

        public override BuildStepResults ExecuteStep(VCSResults vcsResults, Workspace workspace)
        {
            // TODO
            return null;
        }

        Task<ProcessResults> Run(UnityInstallation unity, UnityParameterizedBuildConfig config, Workspace workspace)
        {
            if (!File.Exists(unity.ExePath))
            {
                throw new System.Exception("Unity executable does not exist at path " + unity.ExePath);
            }

            // FIXME match this to the Unity build script method name
            string methodName = "Builder.RemoteBuild";

            // Use System.Environment.GetCommandLineArgs in Unity to get custom arguments
            var args = new List<string>();
            args.Add("-quit");
            args.Add("-batchmode");
            args.Add("-executeMethod");
            args.Add(methodName);
            args.Add("-projectPath");
            args.Add(workspace.WorkingDirectory);
            args.Add("-logfile");
            args.Add(Workspace.StandardOutputPath);

            // Custom args

            args.Add("-executableName");
            args.Add(config.ExecutableName);

            args.Add("-targetPlatform");
            args.Add(System.Enum.GetName(typeof(UnityPlatform), config.TargetPlatform));

            args.Add("-outputDirectory");
            args.Add(workspace.BuildOutputDirectory);

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

            var startInfo = new ProcessStartInfo(unity.ExePath, argString);

            var task = ProcessEx.RunAsync(startInfo);
            return task;
        }
    }
}
