using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using RunProcessAsTask;
using System.IO;

namespace UnityBuild
{
    public class UnityStandardBuildStep : BuildStep
    {
        UnityStandardBuildConfig config;
        Workspace workspace;

        public UnityStandardBuildStep(UnityStandardBuildConfig config, Workspace workspace)
        {
            this.config = config;
            this.workspace = workspace;
        }

        public override string TypeName
        {
            get
            {
                return "Unity Standard Build";
            }
        }

        public override void Execute()
        {
            // FIXME don't hard-code
            var version = new VersionNumber { Major = 5, Minor = 4, Patch = 2, Build = "f2" };

            BuildConsole.WriteLine($"Building with Unity {version}");

            Run(new UnityInstallation {
                Version = version,
                Path = "/Applications/Unity/Unity.app/Contents/MacOS/Unity"
            }, config, workspace);
        }


        void Run(UnityInstallation unityInstallation, UnityStandardBuildConfig unityBuildSettings, Workspace workspace)
        {
            
            var args = GetBuildArgs(unityBuildSettings, workspace);

            Console.ForegroundColor = ConsoleColor.Cyan;

            var startInfo = new ProcessStartInfo
            {
                FileName = unityInstallation.Path,
                Arguments = args,
                WorkingDirectory = workspace.WorkingDirectory,
                //RedirectStandardInput = true,
                //RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            var process = new Process { StartInfo = startInfo };
            process.Start();
            process.WaitForExit();

            Console.ResetColor();
        }

        string GetBuildArgs(UnityStandardBuildConfig settings, Workspace workspace)
        {
            var args = new List<string>();
            args.Add("-quit");
            args.Add("-batchmode");

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

            string exePath = $"{workspace.BuildOutputDirectory}/{settings.ExecutableName}{executableExtension}";
            args.Add(exePath);

            return string.Join(" ", args.ToArray());
        }
    }
}
