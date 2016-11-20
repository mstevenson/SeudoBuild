using System.Collections.Generic;
using Path = System.IO.Path;

namespace SeudoBuild.Modules.UnityBuild
{
    public class UnityStandardBuildStep : UnityBuildStep<UnityStandardBuildConfig>
    {
        public override string Type { get; } = "Unity Standard Build";

        protected override string GetBuildArgs(UnityStandardBuildConfig config, Workspace workspace)
        {
            var args = new List<string>();
            args.Add("-quit");
            args.Add("-batchmode");
            args.Add("-projectPath");
            args.Add(Path.Combine(workspace.WorkingDirectory, config.SubDirectory));
            args.Add("-logfile");
            args.Add(Workspace.StandardOutputPath);

            string executableExtension = "";

            switch (config.TargetPlatform)
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

            string exePath = $"{workspace.BuildOutputDirectory}/{config.OutputName}{executableExtension}";
            args.Add(exePath);

            return string.Join(" ", args.ToArray());
        }
    }
}
