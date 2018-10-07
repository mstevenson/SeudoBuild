using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SeudoBuild.Pipeline.Modules.UnityBuild
{
    public abstract class UnityBuildStep<T> : IBuildStep<T>
        where T : UnityBuildConfig
    {
        private T _config;
        private ITargetWorkspace _workspace;
        ILogger _logger;

        public void Initialize(T config, ITargetWorkspace workspace, ILogger logger)
        {
            _config = config;
            _workspace = workspace;
            _logger = logger;
        }

        public abstract string Type { get; }

        protected abstract string GetBuildArgs(T config, ITargetWorkspace workspace);

        public BuildStepResults ExecuteStep(SourceSequenceResults vcsResults, ITargetWorkspace workspace)
        {
            var unityVersion = _config.UnityVersionNumber;
            string unityDirName = "Unity";
            if (unityVersion != null && unityVersion.IsValid)
            {
                unityDirName = $"{unityDirName} {unityVersion.ToString()}";
            }

            var fileSystem = new FileSystem();
            var unityInstallation = UnityInstallation.FindUnityInstallation(unityVersion, fileSystem);

            var args = GetBuildArgs(_config, workspace);
            var buildResult = ExecuteUnity(unityInstallation, args, workspace, _config.SubDirectory);

            return buildResult;
        }

        private BuildStepResults ExecuteUnity(UnityInstallation unityInstallation, string arguments,
            ITargetWorkspace workspace, string relativeUnityProjectFolder)
        {
            if (!workspace.FileSystem.FileExists(unityInstallation.ExePath))
            {
                throw new Exception("Unity executable does not exist at path " + unityInstallation.ExePath);
            }

            _logger.Write($"Building with Unity {unityInstallation.Version}", LogType.SmallBullet);

            string projectFolderPath = Path.Combine(workspace.SourceDirectory, relativeUnityProjectFolder);

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
                WorkingDirectory = workspace.SourceDirectory,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };

            using (var unityProcess = new Process { StartInfo = startInfo })
            {
                var logParser = new UnityLogParser();
                
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
                            _logger.Write(logOutput, LogType.SmallBullet);
                        }
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
        }

        private string GetBuildLogPath(ITargetWorkspace workspace)
        {
            string now = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss");
            return $"{workspace.LogsDirectory}/Unity_Build_Log_{now}.txt";
        }
    }
}
