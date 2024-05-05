namespace SeudoCI.Pipeline.Modules.ShellBuild;

using System.Diagnostics;
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
    private ShellBuildStepConfig _config;
    private ITargetWorkspace _workspace;
    private ILogger _logger;

    public string Type => "Shell Script";

    public void Initialize(ShellBuildStepConfig config, ITargetWorkspace workspace, ILogger logger)
    {
        _config = config;
        _workspace = workspace;
        _logger = logger;
    }

    public BuildStepResults ExecuteStep(SourceSequenceResults vcsResults, ITargetWorkspace workspace)
    {
        try
        {
            // Replace variables in string that begin and end with the % character
            var command = workspace.Macros.ReplaceVariablesInText(_config.Command);
            // Escape quotes
            command = command.Replace(@"""", @"\""");

            var startInfo = new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = $"-c \"{command}\"",
                WorkingDirectory = workspace.GetDirectory(TargetDirectory.Source),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            var process = new Process { StartInfo = startInfo };
            process.Start();

            Console.ForegroundColor = ConsoleColor.Cyan;

            while (!process.StandardOutput.EndOfStream)
            {
                string line = process.StandardOutput.ReadLine();
                _logger.Write(line);
            }

            process.WaitForExit();
            Console.ResetColor();

            // FIXME fill in more build step result properties?

            var results = new BuildStepResults();
            int exitCode = process.ExitCode;
            if (exitCode == 0)
            {
                results.IsSuccess = true;
                results.Exception = new Exception("Build process exited abnormally");
            }
            else
            {
                results.IsSuccess = false;
            }

            return results;
        }
        catch (Exception e)
        {
            return new BuildStepResults { IsSuccess = false, Exception = e };
        }
    }
}