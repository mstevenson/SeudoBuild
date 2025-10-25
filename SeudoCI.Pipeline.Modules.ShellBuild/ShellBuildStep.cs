using JetBrains.Annotations;

namespace SeudoCI.Pipeline.Modules.ShellBuild;

using System.Diagnostics;
using System.Text;
using Core;

/// <summary>
/// Executes an arbitrary shell script as part of a build process.
/// 
/// Macros:
/// %working_directory% -- the full path of the working directory in which the un-built project files are stored
/// %build_output_directory% -- the directory containing build products
/// %archives_directory% -- the directory in which build products will be archived during a later step
/// </summary>
public class ShellBuildStep : IBuildStep<ShellBuildStepConfig>
{
    private ShellBuildStepConfig _config = null!;
    private ITargetWorkspace _workspace = null!;
    private ILogger _logger = null!;

    public string? Type => "Shell Script";

    [UsedImplicitly]
    public void Initialize(ShellBuildStepConfig config, ITargetWorkspace workspace, ILogger logger)
    {
        _config = config;
        _workspace = workspace;
        _logger = logger;
    }

    public BuildStepResults ExecuteStep(SourceSequenceResults vcsResults, ITargetWorkspace workspace)
    {
        var results = new BuildStepResults();

        try
        {
            if (string.IsNullOrWhiteSpace(_config.Command))
            {
                throw new InvalidOperationException("Shell command cannot be empty.");
            }

            // Replace variables in string that begin and end with the % character
            var command = workspace.Macros.ReplaceVariablesInText(_config.Command);

            var startInfo = new ProcessStartInfo("bash")
            {
                WorkingDirectory = workspace.GetDirectory(TargetDirectory.Source),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            startInfo.ArgumentList.Add("-c");
            startInfo.ArgumentList.Add(command);

            using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };

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

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                results.IsSuccess = true;
                _logger.Write("Shell command completed successfully.", LogType.Success);
            }
            else
            {
                results.IsSuccess = false;
                var errorMessage = new StringBuilder($"Shell command exited with code {process.ExitCode}.");
                if (stderr.Length > 0)
                {
                    errorMessage.Append(' ').Append(stderr.ToString().Trim());
                }

                results.Exception = new InvalidOperationException(errorMessage.ToString());
            }
        }
        catch (Exception e)
        {
            results.IsSuccess = false;
            results.Exception = e;
            _logger.Write($"Shell command failed: {e.Message}", LogType.Failure);
        }

        return results;
    }
}
