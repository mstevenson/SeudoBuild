﻿using System.Diagnostics;

namespace UnityBuildServer
{
    /// <summary>
    /// Executes an arbitrary shell script as part of a build process.
    /// 
    /// Variables:
    /// %working_directory% -- the full path of the working directory in which the un-built project files are stored
    /// %build_output_directory% -- the directory containing build products
    /// %archives_directory% -- the directory in which build products will be archived during a later step
    /// </summary>
    public class ShellBuildStep : BuildStep
    {
        ShellBuildStepConfig config;
        Workspace workspace;

        public ShellBuildStep(ShellBuildStepConfig config, Workspace workspace)
        {
            this.config = config;
            this.workspace = workspace;
        }

        public override string TypeName
        {
            get
            {
                return "Shell Script";
            }
        }

        public override void Execute()
        {
            // Replace variables in string that begin and end with the % character
            var command = workspace.Replacements.ReplaceVariablesInText(config.Command);
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

            System.Console.ForegroundColor = System.ConsoleColor.DarkGreen;

            while (!process.StandardOutput.EndOfStream)
            {
                string line = process.StandardOutput.ReadLine();
                BuildConsole.WriteLine(line);
            }

            System.Console.ResetColor();
        }
    }
}
