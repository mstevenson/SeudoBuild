using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SeudoBuild.Modules.UnityBuild
{
    public abstract class UnityBuildStep<T> : IBuildStep<T>
        where T : UnityBuildConfig
    {
        protected T config;
        protected Workspace workspace;

        public void Initialize(T config, Workspace workspace)
        {
            this.config = config;
            this.workspace = workspace;
        }

        public abstract string Type { get; }

        protected abstract string GetBuildArgs(T config, Workspace workspace);

        public BuildStepResults ExecuteStep(SourceSequenceResults vcsResults, Workspace workspace)
        {
            var unityVersion = config.UnityVersionNumber;
            string unityDirName = "Unity";
            if (unityVersion != null && unityVersion.IsValid)
            {
                unityDirName = $"{unityDirName} {unityVersion.ToString()}";
            }

            var fileSystem = new FileSystem();
            var unityInstallation = UnityInstallation.FindUnityInstallation(unityVersion, fileSystem);

            var args = GetBuildArgs(config, workspace);
            var buildResult = ExecuteUnity(unityInstallation, args, workspace, config.SubDirectory);

            return buildResult;
        }

        protected BuildStepResults ExecuteUnity(UnityInstallation unityInstallation, string arguments, Workspace workspace, string relativeUnityProjectFolder)
        {
            if (!workspace.FileSystem.FileExists(unityInstallation.ExePath))
            {
                throw new Exception("Unity executable does not exist at path " + unityInstallation.ExePath);
            }

            BuildConsole.WriteLine($"Building with Unity {unityInstallation.Version}");

            string projectFolderPath = Path.Combine(workspace.WorkingDirectory, relativeUnityProjectFolder);

            // Validate Unity project folder contents
            var dirs = Directory.GetDirectories(projectFolderPath);
            if (!dirs.Any(path => Path.GetFileName(path) == "Library" || Path.GetFileName(path) == "ProjectSettings"))
            {
                return new BuildStepResults
                {
                    IsSuccess = false,
                    Exception = new Exception("Working directory does not appear to contain a Unity project.")
                };
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = unityInstallation.ExePath,
                Arguments = arguments,
                WorkingDirectory = workspace.WorkingDirectory,
                //RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            var unityProcess = new Process { StartInfo = startInfo };

            var logParser = new UnityLogParser();

            // FIXME is this all being disposed correctly?

            string logPath = GetBuildLogPath(workspace);
            var writer = new StreamWriter(logPath);
            try
            {
                unityProcess.OutputDataReceived += (sender, e) =>
                {
                    //Console.WriteLine(e.Data);
                    writer.WriteLine(e.Data);
                    writer.Flush();
                    string logOutput = logParser.ProcessLogLine(e.Data);
                    if (logOutput != null)
                    {
                        BuildConsole.WriteLine(logOutput);
                    }
                    // TODO examine log for errors and other pertinent info
                };

                //unityProcess.ErrorDataReceived += (sender, e) =>
                //{
                //    // TODO return error
                //};

                unityProcess.Start();
                unityProcess.BeginOutputReadLine();
                unityProcess.WaitForExit();
            }
            finally
            {
                writer.Close();
            }

            var results = new BuildStepResults();
            if (unityProcess.ExitCode == 0)
            {
                results.IsSuccess = true;
            }
            else
            {
                results.IsSuccess = false;
                results.Exception = new Exception("Build process exited abnormally");
            }

            return results;
        }

        protected string GetBuildLogPath(Workspace workspace)
        {
            string now = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss");
            return $"{workspace.LogsDirectory}/Unity_Build_Log_{now}.txt";
        }
    }
}
