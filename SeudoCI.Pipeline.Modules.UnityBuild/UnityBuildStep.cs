using JetBrains.Annotations;

namespace SeudoCI.Pipeline.Modules.UnityBuild;

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Core;

public abstract class UnityBuildStep<T> : IBuildStep<T>
    where T : UnityBuildConfig
{
    private T _config = null!;
    private ILogger _logger = null!;

    [UsedImplicitly]
    public void Initialize(T config, ITargetWorkspace workspace, ILogger logger)
    {
        _config = config;
        _logger = logger;
    }

    public abstract string? Type { get; }

    protected abstract IReadOnlyList<string> GetBuildArgs(T config, ITargetWorkspace workspace);

    public BuildStepResults ExecuteStep(SourceSequenceResults vcsResults, ITargetWorkspace workspace)
    {
        var unityVersion = _config.UnityVersionNumber;
        string unityDirName = "Unity";
        if (unityVersion.IsValid)
        {
            unityDirName = $"{unityDirName} {unityVersion}";
        }

        var platform = PlatformUtils.RunningPlatform;
        var unityInstallation = UnityInstallation.FindUnityInstallation(unityVersion, platform);

        var args = GetBuildArgs(_config, workspace);
        var buildResult = ExecuteUnity(unityInstallation, args, workspace, _config.SubDirectory);

        return buildResult;
    }

    private BuildStepResults ExecuteUnity(UnityInstallation unityInstallation, IReadOnlyList<string> arguments,
        ITargetWorkspace workspace, string relativeUnityProjectFolder)
    {
        var exePath = unityInstallation.ExePath;
        if (string.IsNullOrWhiteSpace(exePath))
        {
            throw new Exception("Unity executable path was not resolved.");
        }

        if (!workspace.FileSystem.FileExists(exePath))
        {
            throw new Exception("Unity executable does not exist at path " + exePath);
        }

        _logger.Write($"Building with Unity {unityInstallation.Version}", LogType.SmallBullet);

        string projectFolderPath = Path.Combine(workspace.GetDirectory(TargetDirectory.Source), relativeUnityProjectFolder);

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
            FileName = exePath,
            WorkingDirectory = workspace.GetDirectory(TargetDirectory.Source),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            UseShellExecute = false
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var unityProcess = new Process();
        unityProcess.StartInfo = startInfo;
        var logParser = new UnityLogParser();
                
        string now = DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss");
        string logPath = $"{workspace.GetDirectory(TargetDirectory.Logs)}/Unity_Build_Log_{now}.txt";
                
        using var writer = new StreamWriter(logPath);
        var stderr = new StringBuilder();
        var writerLock = new object();

        unityProcess.OutputDataReceived += (sender, e) =>
        {
            if (e.Data == null)
            {
                return;
            }

            lock (writerLock)
            {
                writer.WriteLine(e.Data);
                writer.Flush();
            }

            string? logOutput = logParser.ProcessLogLine(e.Data);
            if (logOutput != null)
            {
                _logger.Write(logOutput, LogType.SmallBullet);
            }
        };

        var errorDetected = false;

        unityProcess.ErrorDataReceived += (_, e) =>
        {
            if (string.IsNullOrEmpty(e.Data))
            {
                return;
            }

            lock (writerLock)
            {
                writer.WriteLine(e.Data);
                writer.Flush();
            }

            stderr.AppendLine(e.Data);
            errorDetected = true;
            _logger.Write(e.Data, LogType.Failure);
        };

        unityProcess.Start();
        unityProcess.BeginOutputReadLine();
        unityProcess.BeginErrorReadLine();
        unityProcess.WaitForExit();

        var results = new BuildStepResults();

        if (unityProcess.ExitCode == 0 && !errorDetected)
        {
            results.IsSuccess = true;
        }
        else
        {
            results.IsSuccess = false;
            var errorMessage = new StringBuilder();

            if (unityProcess.ExitCode != 0)
            {
                errorMessage.Append($"Build process exited with code {unityProcess.ExitCode}.");
            }

            if (stderr.Length > 0)
            {
                if (errorMessage.Length > 0)
                {
                    errorMessage.Append(' ');
                }

                errorMessage.Append(stderr.ToString().Trim());
            }

            if (errorMessage.Length == 0)
            {
                errorMessage.Append("Build process exited abnormally");
            }

            results.Exception = new Exception(errorMessage.ToString());
        }

        return results;
    }
}