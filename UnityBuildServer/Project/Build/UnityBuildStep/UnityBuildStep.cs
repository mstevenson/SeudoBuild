using System;
using System.Diagnostics;
using System.IO;

namespace UnityBuild
{
    public abstract class UnityBuildStep : IBuildStep
    {
        public abstract string TypeName { get; }

        public abstract void Execute();

        public UnityBuildResults ExecuteUnity(string arguments, Workspace workspace)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;

            var results = new UnityBuildResults();

            var startInfo = new ProcessStartInfo
            {
                FileName = "git-lfs",
                Arguments = arguments,
                WorkingDirectory = workspace.WorkingDirectory,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            var process = new Process { StartInfo = startInfo };

            //string now = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss");
            //string logPath = $"{workspace.LogsDirectory}/Unity_Build_Log_{now}";

            //using (var writer = File.AppendText(logPath))
            //{
            process.OutputDataReceived += (sender, e) =>
            {
                // TODO examine log for errors and other pertinent info
            };
            
            process.ErrorDataReceived += (sender, e) =>
            {
                // TODO return error
            };

            // TODO watch the Unity editor log file for changes, examine it
            string editorLogPath = GetEditorLogPath(workspace);

            process.Start();
            process.WaitForExit();
            //}

            Console.ResetColor();

            return results;
        }

        string GetEditorLogPath(Workspace workspace)
        {
            switch (workspace.Platform)
            {
                case Workspace.PlatformType.Mac:
                    string userDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    return userDir + "/Library/Logs/Unity/Editor.log";
                case Workspace.PlatformType.Windows:
                    string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    return appData + @"\Local\Unity\Editor\Editor.log";
                default:
                    throw new PlatformNotSupportedException($"{workspace.Platform} platform is not supported");
            }
        }
    }
}
