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
            
        }


        Task<ProcessResults> Run(UnityInstallation unity, UnityStandardBuildConfig unityBuildSettings, Workspace workspace)
        {
            if (!File.Exists(unity.Path))
            {
                throw new System.Exception("Unity executable does not exist at path " + unity.Path);
            }

            var args = GetBuildArgs(unityBuildSettings, workspace);
            var startInfo = new ProcessStartInfo(unity.Path, args);

            var task = ProcessEx.RunAsync(startInfo);
            return task;
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
