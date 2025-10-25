using JetBrains.Annotations;

namespace SeudoCI.Pipeline.Modules.SteamDistribute;

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Core;
using Path = System.IO.Path;

public class SteamDistributeStep : IDistributeStep<SteamDistributeConfig>
{
    private SteamDistributeConfig _config = null!;
    private ILogger _logger = null!;

    public string? Type => "Steam Upload";

    [UsedImplicitly]
    public void Initialize(SteamDistributeConfig config, ITargetWorkspace workspace, ILogger logger)
    {
        _config = config;
        _logger = logger;
    }

    public DistributeStepResults ExecuteStep(ArchiveSequenceResults archiveResults, ITargetWorkspace workspace)
    {
        var results = new DistributeStepResults();

        try
        {
            ValidateConfiguration();

            var archivesToUpload = ResolveTargetArchives(archiveResults);

            foreach (var archiveInfo in archivesToUpload)
            {
                var fileResult = new DistributeStepResults.FileResult { ArchiveInfo = archiveInfo };

                try
                {
                    UploadArchiveToSteam(archiveInfo, workspace);
                    fileResult.Success = true;
                    fileResult.Message = BuildSuccessMessage();
                    results.FileResults.Add(fileResult);
                }
                catch (Exception ex)
                {
                    fileResult.Success = false;
                    fileResult.Message = ex.Message;
                    results.FileResults.Add(fileResult);

                    throw new Exception($"Steam upload failed for archive '{archiveInfo.ArchiveFileName}'.", ex);
                }
            }

            results.IsSuccess = true;
        }
        catch (Exception ex)
        {
            results.IsSuccess = false;
            results.Exception = ex;
        }

        return results;
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_config.Username))
        {
            throw new InvalidOperationException("Steam upload requires a username to authenticate with steamcmd.");
        }

        if (string.IsNullOrWhiteSpace(_config.Password))
        {
            throw new InvalidOperationException("Steam upload requires a password to authenticate with steamcmd.");
        }
    }

    private IReadOnlyList<ArchiveStepResults> ResolveTargetArchives(ArchiveSequenceResults archiveResults)
    {
        if (archiveResults?.StepResults == null || archiveResults.StepResults.Count == 0)
        {
            throw new InvalidOperationException("Steam upload requires at least one archived build to be available.");
        }

        if (string.IsNullOrWhiteSpace(_config.ArchiveFileName))
        {
            return archiveResults.StepResults;
        }

        var matches = archiveResults.StepResults
            .Where(result => string.Equals(result.ArchiveFileName, _config.ArchiveFileName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            throw new InvalidOperationException($"Archive '{_config.ArchiveFileName}' was not produced earlier in the pipeline.");
        }

        return matches;
    }

    private string BuildSuccessMessage()
    {
        if (string.IsNullOrWhiteSpace(_config.PublishToBranch))
        {
            return "Steam upload completed successfully.";
        }

        return $"Steam upload completed successfully (branch '{_config.PublishToBranch}').";
    }

    private void UploadArchiveToSteam(ArchiveStepResults archiveInfo, ITargetWorkspace workspace)
    {
        var fileSystem = workspace.FileSystem;
        var archivesDirectory = workspace.GetDirectory(TargetDirectory.Archives);
        var archivePath = Path.Combine(archivesDirectory, archiveInfo.ArchiveFileName);

        string stagingDirectory;
        bool cleanupRequired = false;

        if (fileSystem.FileExists(archivePath))
        {
            stagingDirectory = Path.Combine(
                archivesDirectory,
                Path.GetFileNameWithoutExtension(archiveInfo.ArchiveFileName) + "_steamcmd");

            if (fileSystem.DirectoryExists(stagingDirectory))
            {
                fileSystem.DeleteDirectory(stagingDirectory);
            }

            fileSystem.CreateDirectory(stagingDirectory);

            _logger.Write($"Extracting '{archiveInfo.ArchiveFileName}' for Steam upload.", LogType.SmallBullet);
            ZipFile.ExtractToDirectory(archivePath, stagingDirectory, overwriteFiles: true);
            cleanupRequired = true;
        }
        else if (fileSystem.DirectoryExists(archivePath))
        {
            stagingDirectory = archivePath;
        }
        else
        {
            throw new FileNotFoundException(
                $"Archive '{archiveInfo.ArchiveFileName}' could not be found in the archives directory.",
                archivePath);
        }

        try
        {
            var scriptPath = FindAppBuildScript(stagingDirectory);

            var branchName = _config.PublishToBranch?.Trim() ?? string.Empty;
            if (!string.IsNullOrEmpty(branchName))
            {
                ApplySetLiveBranch(scriptPath, branchName);
            }

            var builderDirectory = FindBuilderDirectory(stagingDirectory, scriptPath)
                                   ?? Path.GetDirectoryName(scriptPath)
                                   ?? stagingDirectory;

            var steamCmdExecutable = FindSteamCmdExecutable(builderDirectory, stagingDirectory);

            ExecuteSteamCmd(steamCmdExecutable, builderDirectory, scriptPath);
        }
        finally
        {
            if (cleanupRequired && fileSystem.DirectoryExists(stagingDirectory))
            {
                fileSystem.DeleteDirectory(stagingDirectory);
            }
        }
    }

    private static string FindAppBuildScript(string rootDirectory)
    {
        var scripts = Directory.GetFiles(rootDirectory, "app_build*.vdf", SearchOption.AllDirectories);

        if (scripts.Length == 0)
        {
            throw new InvalidOperationException("Could not locate a SteamPipe app_build script (app_build*.vdf) in the archive contents.");
        }

        if (scripts.Length == 1)
        {
            return scripts[0];
        }

        var preferred = scripts.FirstOrDefault(path =>
            path.Contains($"{Path.DirectorySeparatorChar}scripts{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));

        return preferred ?? scripts[0];
    }

    private void ApplySetLiveBranch(string scriptPath, string branchName)
    {
        if (string.Equals(branchName, "default", StringComparison.OrdinalIgnoreCase))
        {
            _logger.Write(
                "Steam's default branch cannot be published automatically. Please publish it manually from Steamworks.",
                LogType.Alert);
            return;
        }

        _logger.Write($"Configuring Steam build script '{Path.GetFileName(scriptPath)}' to publish branch '{branchName}'.", LogType.SmallBullet);

        var lines = File.ReadAllLines(scriptPath).ToList();
        var setLiveIndex = lines.FindIndex(line =>
            line.TrimStart().StartsWith("\"SetLive\"", StringComparison.OrdinalIgnoreCase));

        var indent = "        ";
        if (setLiveIndex >= 0)
        {
            var tokenIndex = lines[setLiveIndex].IndexOf("\"SetLive\"", StringComparison.Ordinal);
            indent = tokenIndex > 0 ? lines[setLiveIndex][..tokenIndex] : string.Empty;
        }

        var newLine = $"{indent}\"SetLive\"	\"{branchName}\"";

        if (setLiveIndex >= 0)
        {
            lines[setLiveIndex] = newLine;
        }
        else
        {
            var insertIndex = lines.FindLastIndex(line => line.Trim() == "}");
            if (insertIndex < 0)
            {
                insertIndex = lines.Count;
            }

            lines.Insert(insertIndex, newLine);
        }

        File.WriteAllLines(scriptPath, lines);
    }

    private static string? FindBuilderDirectory(string stagingDirectory, string scriptPath)
    {
        var scriptDirectory = Path.GetDirectoryName(scriptPath);
        if (scriptDirectory == null)
        {
            return null;
        }

        var stagingFullPath = Path.GetFullPath(stagingDirectory);
        var current = new DirectoryInfo(scriptDirectory);

        while (current != null && Path.GetFullPath(current.FullName).StartsWith(stagingFullPath, StringComparison.OrdinalIgnoreCase))
        {
            var candidate = Path.Combine(current.FullName, "builder");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        var searchResult = Directory.GetDirectories(stagingDirectory, "builder", SearchOption.AllDirectories);
        return searchResult.FirstOrDefault();
    }

    private static string? FindSteamCmdExecutable(string builderDirectory, string stagingDirectory)
    {
        var candidates = new List<string>();

        if (Directory.Exists(builderDirectory))
        {
            candidates.AddRange(new[]
            {
                Path.Combine(builderDirectory, "steamcmd.exe"),
                Path.Combine(builderDirectory, "steamcmd.sh"),
                Path.Combine(builderDirectory, "steamcmd")
            });
        }

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        var search = Directory.GetFiles(stagingDirectory, "steamcmd*", SearchOption.AllDirectories)
            .FirstOrDefault(path => Path.GetFileName(path).StartsWith("steamcmd", StringComparison.OrdinalIgnoreCase));

        return search;
    }

    private void ExecuteSteamCmd(string? executablePath, string workingDirectory, string scriptPath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            _logger.Write("No steamcmd executable found in the archive. Falling back to system-installed steamcmd.", LogType.Alert);
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath ?? "steamcmd",
            WorkingDirectory = Directory.Exists(workingDirectory)
                ? workingDirectory
                : Path.GetDirectoryName(scriptPath) ?? Directory.GetCurrentDirectory(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("+login");
        startInfo.ArgumentList.Add(_config.Username);
        startInfo.ArgumentList.Add(_config.Password);
        startInfo.ArgumentList.Add("+run_app_build");
        startInfo.ArgumentList.Add(Path.GetFullPath(scriptPath));
        startInfo.ArgumentList.Add("+quit");

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = false };

        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                _logger.Write(args.Data);
            }
        };

        process.ErrorDataReceived += (_, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                stderr.AppendLine(args.Data);
                _logger.Write(args.Data, LogType.Failure);
            }
        };

        _logger.Write("Starting SteamPipe upload via steamcmd.", LogType.SmallBullet);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            var error = stderr.ToString().Trim();
            if (!string.IsNullOrEmpty(error))
            {
                throw new InvalidOperationException($"steamcmd exited with code {process.ExitCode}: {error}");
            }

            throw new InvalidOperationException($"steamcmd exited with code {process.ExitCode}.");
        }

        _logger.Write("Steam upload completed successfully.", LogType.Success);
    }
}
