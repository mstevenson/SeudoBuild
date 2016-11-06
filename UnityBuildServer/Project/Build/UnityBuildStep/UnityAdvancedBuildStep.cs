using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using RunProcessAsTask;
using System.IO;

namespace UnityBuild
{
    public class UnityAdvancedBuildStep : BuildStep
    {
        UnityAdvancedBuildConfig config;
        Workspace workspace;

        public UnityAdvancedBuildStep(UnityAdvancedBuildConfig config, Workspace workspace)
        {
            this.config = config;
            this.workspace = workspace;
        }

        public override string TypeName
        {
            get
            {
                return "Unity Advanced Build";
            }
        }

        public override void Execute()
        {
            // TODO
        }

        Task<ProcessResults> Run(UnityInstallation unity, UnityAdvancedBuildConfig config, Workspace workspace)
        {
            if (!File.Exists(unity.Path))
            {
                throw new System.Exception("Unity executable does not exist at path " + unity.Path);
            }

            // FIXME
            string methodName = "";

            // Use System.Environment.GetCommandLineArgs in Unity to get custom arguments
            var args = new List<string>();
            args.Add("-quit");
            args.Add("-batchmode");
            args.Add($"-executeMethod {methodName}");

            if (config.BuildAssetBundles)
            {
                args.Add("-buildAssetBundles");
            }
            if (config.CopyAssetBundlesToStreamingAssets)
            {
                args.Add("-copyAssetBundlesToStreamingAssets");
            }
            if (config.DevelopmentBuild)
            {
                args.Add("-developmentBuild");
            }

            // TODO add more args from config file

            var argString = string.Join(" ", args.ToArray());

            var startInfo = new ProcessStartInfo(unity.Path, argString);

            var task = ProcessEx.RunAsync(startInfo);
            return task;
        }
    }
}
