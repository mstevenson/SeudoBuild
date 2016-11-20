using System;
using System.Diagnostics;

namespace SeudoBuild.Modules.ShellBuild
{
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
        ShellBuildStepConfig config;
        Workspace workspace;

        public string Type { get; } = "Shell Script";

        public void Initialize(ShellBuildStepConfig config, Workspace workspace)
        {
            this.config = config;
            this.workspace = workspace;
        }

        public BuildStepResults ExecuteStep(SourceSequenceResults vcsResults, Workspace workspace)
        {
            try
            {
                // Replace variables in string that begin and end with the % character
                var command = workspace.Macros.ReplaceVariablesInText(config.Command);
                // Escape quotes
                command = command.Replace(@"""", @"\""");

                var startInfo = new ProcessStartInfo
                {
                    FileName = "bash",
                    Arguments = $"-c \"{command}\"",
                    WorkingDirectory = workspace.WorkingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                var process = new Process { StartInfo = startInfo };
                process.Start();

                System.Console.ForegroundColor = System.ConsoleColor.Cyan;

                while (!process.StandardOutput.EndOfStream)
                {
                    string line = process.StandardOutput.ReadLine();
                    BuildConsole.WriteLine(line);
                }

                process.WaitForExit();
                System.Console.ResetColor();

                // FIXME fill in more build step result properties?

                var results = new BuildStepResults();
                int exitCode = process.ExitCode;
                if (exitCode == 0)
                {
                    results.IsSuccess = true;
                    results.Exception = new Exception("Build process exited abnoramlly");
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
}
