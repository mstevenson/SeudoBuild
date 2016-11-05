using System;
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
            // TODO
        }


        //static Task<ProcessResults> Run(UnityInstallation unity, UnityStandardBuildConfig unityBuildSettings, Workspace workspace)
        //{
        //    if (!File.Exists(unity.Path))
        //    {
        //        throw new System.Exception("Unity executable does not exist at path " + unity.Path);
        //    }

        //    var args = BuildArgs(unityBuildSettings, workspace);
        //    var startInfo = new ProcessStartInfo(unity.Path, args);

        //    var task = ProcessEx.RunAsync(startInfo);
        //    return task;
        //}

        //string BuildArgs(UnityStandardBuildConfig settings, Workspace workspace)
        //{
        //    var args = new List<string>();
        //    args.Add("-quit");
        //    args.Add("-batchmode");
        //    args.Add("-silent-crashes");

        //    bool hasExecuteMethod = !string.IsNullOrEmpty(settings.MethodName);
        //    string executableExtension = "";

        //    switch (settings.TargetPlatform)
        //    {
        //        case UnityStandardBuildConfig.Platform.Mac:
        //            args.Add("-buildTarget osx");
        //            if (!hasExecuteMethod)
        //            {
        //                args.Add("-buildOSX64Player");
        //                executableExtension = ".app";
        //            }
        //            break;
        //        case UnityStandardBuildConfig.Platform.Windows:
        //            args.Add("-buildTarget win64");
        //            if (!hasExecuteMethod)
        //            {
        //                args.Add("-buildWindows64Player");
        //                executableExtension = ".exe";
        //            }
        //            break;
        //        case UnityStandardBuildConfig.Platform.Linux:
        //            args.Add("-buildTarget linux64");
        //            if (!hasExecuteMethod)
        //            {
        //                args.Add("-buildLinux64Player");
        //            }
        //            break;
        //    }

        //    if (!hasExecuteMethod)
        //    {
        //        string exePath = $"{workspace.WorkingDirectory}/Output/{settings.ExecutableName}.{executableExtension}";
        //        args.Add(exePath);
        //    }

        //    args.Add($"-executeMethod {settings.MethodName}");

        //    return string.Join(" ", args.ToArray());
        //}
    }
}
