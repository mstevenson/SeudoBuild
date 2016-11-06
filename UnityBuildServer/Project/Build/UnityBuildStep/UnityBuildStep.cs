using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace UnityBuild
{
    public abstract class UnityBuildStep : IBuildStep
    {
        public abstract string TypeName { get; }

        public abstract void Execute();

        protected UnityBuildResults ExecuteUnity(UnityInstallation unityInstallation, string arguments, Workspace workspace)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;

            var results = new UnityBuildResults();

            var startInfo = new ProcessStartInfo
            {
                FileName = unityInstallation.Path,
                Arguments = arguments,
                WorkingDirectory = workspace.WorkingDirectory,
                //RedirectStandardInput = true,
                //RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            var unityProcess = new Process { StartInfo = startInfo };

            //string now = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss");
            //string logPath = $"{workspace.LogsDirectory}/Unity_Build_Log_{now}";

            //using (var writer = File.AppendText(logPath))
            //{
            //unityProcess.OutputDataReceived += (sender, e) =>
            //{
            //    // TODO examine log for errors and other pertinent info
            //};
            
            //unityProcess.ErrorDataReceived += (sender, e) =>
            //{
            //    // TODO return error
            //};

            // Watch for the Unity editor log file to change,
            // and examine its output to determine when the build has completed

            string editorLogPath = GetEditorLogPath(workspace);
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            Task unityEditorLogWatcherTask = Task.Run(() =>
            {
                // Bail if the task was already cancelled
                token.ThrowIfCancellationRequested();

                FileInfo logFile = new FileInfo(editorLogPath);
                bool lastExisted = false;
                DateTime lastModified = DateTime.Now;
                long lastLength = 0;

                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    bool changed = false;
                    int lengthDiff = 0;

                    bool exists = logFile.Exists;
                    if (exists)
                    {
                        if (!lastExisted)
                        {
                            lastExisted = true;
                            lastModified = logFile.LastWriteTimeUtc;
                            lastLength = logFile.Length;
                            changed = true;
                        }
                        else
                        {
                            DateTime modified = logFile.LastWriteTimeUtc;
                            long length = logFile.Length;
                            changed = modified != lastModified || length != lastLength;

                            lastModified = modified;
                            lastLength = length;
                        }
                    }

                    if (changed)
                    {
                        BuildConsole.WriteLine("Unity log changed");

                        // TODO Copy the new log lines (lengthDiff) to our own log file

                        // TODO examine Unity log
                    }

                    // FIXME we may miss the final few lines of the log file

                    Thread.Sleep(100);
                }
            }, token);


            unityProcess.Start();
            unityProcess.WaitForExit();
            tokenSource.Cancel();

            tokenSource.Dispose();
            Console.ResetColor();

            return results;
        }

        string GetEditorLogPath(Workspace workspace)
        {
            switch (workspace.RunningPlatform)
            {
                case Workspace.Platform.Mac:
                    string userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    return userDir + "/Library/Logs/Unity/Editor.log";
                case Workspace.Platform.Windows:
                    string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    return appData + @"\Local\Unity\Editor\Editor.log";
                default:
                    throw new PlatformNotSupportedException($"{workspace.RunningPlatform} platform is not supported");
            }
        }
    }
}
