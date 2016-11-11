using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SeudoBuild
{
    public abstract class UnityBuildStep : IBuildStep
    {
        public abstract string Type { get; }

        public abstract BuildResult Execute();

        protected BuildResult ExecuteUnity(UnityInstallation unityInstallation, string arguments, Workspace workspace)
        {
            BuildConsole.WriteLine($"Building with Unity {unityInstallation.Version}");

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

            var buildResult = new BuildResult();
            if (unityProcess.ExitCode == 0)
            {
                buildResult.Status = BuildCompletionStatus.Completed;
            }
            else
            {
                buildResult.Status = BuildCompletionStatus.Faulted;
            }

            return buildResult;
        }

        protected string GetBuildLogPath(Workspace workspace)
        {
            string now = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss");
            return $"{workspace.LogsDirectory}/Unity_Build_Log_{now}.txt";
        }
    }
}
