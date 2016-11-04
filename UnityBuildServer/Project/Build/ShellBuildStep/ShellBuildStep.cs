using System.Diagnostics;

namespace UnityBuildServer
{
    public class ShellBuildStep : BuildStep
    {
        ShellBuildStepConfig config;

        public ShellBuildStep(ShellBuildStepConfig config)
        {
            this.config = config;
        }

        public override string TypeName
        {
            get
            {
                return "Shell Script";
            }
        }

        public override BuildInfo Execute()
        {
            string command = config.Text.Replace(@"""", @"\""");

            var startInfo = new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = $"-c \"{command}\"",
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
                System.Console.WriteLine("    " + line);
            }

            return new BuildInfo();
        }
    }
}
