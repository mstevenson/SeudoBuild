using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using RunProcessAsTask;
using System.IO;

namespace UnityBuild
{
    public class UnityStandardBuildStep : UnityBuildStep
    {
        UnityStandardBuildConfig config;
        Workspace workspace;

        public UnityStandardBuildStep(UnityStandardBuildConfig config, Workspace workspace)
        {
            this.config = config;
            this.workspace = workspace;
        }

        public override string Type { get; } = "Unity Standard Build";

        public override BuildResult Execute()
        {
            // FIXME don't hard-code

            var version = new VersionNumber { Major = 5, Minor = 4, Patch = 2, Build = "f2" };

            var unityInstallation = new UnityInstallation
            {
                Version = version,
                Path = "/Applications/Unity/Unity.app/Contents/MacOS/Unity"
            };

            var args = GetBuildArgs(config, workspace);
            var buildResult = ExecuteUnity(unityInstallation, args, workspace);

            return buildResult;
        }

        string GetBuildArgs(UnityStandardBuildConfig settings, Workspace workspace)
        {
            var args = new List<string>();
            args.Add("-quit");
            args.Add("-batchmode");
            args.Add("-projectPath");
            args.Add(workspace.WorkingDirectory);

            string executableExtension = "";

            switch (settings.TargetPlatform)
            {
                case UnityPlatform.Mac:
                    args.Add("-buildTarget osx");
                    args.Add("-buildOSX64Player");
                    executableExtension = ".app";
                    break;
                case UnityPlatform.Windows:
                    args.Add("-buildTarget win64");
                    args.Add("-buildWindows64Player");
                    executableExtension = ".exe";
                    break;
                case UnityPlatform.Linux:
                    args.Add("-buildTarget linux64");
                    args.Add("-buildLinux64Player");
                    break;
            }

            string exePath = $"{workspace.BuildOutputDirectory}/{settings.OutputName}{executableExtension}";
            args.Add(exePath);

            return string.Join(" ", args.ToArray());
        }
    }
}
