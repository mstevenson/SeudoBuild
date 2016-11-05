using System.Diagnostics;

namespace UnityBuildServer
{
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

            while (!process.StandardOutput.EndOfStream)
            {
                string line = process.StandardOutput.ReadLine();

                // TODO do something with line
                BuildConsole.WriteLine(line);
            }
        }
    }
}
