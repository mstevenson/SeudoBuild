using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using LibGit2Sharp;

namespace SeudoBuild.Pipeline.Modules.GitSource
{
    public class LFSFilter : Filter
    {
        Process process;
        string repoPath;

        FilterMode mode;

        public LFSFilter(string name, string repoPath, IEnumerable<FilterAttributeEntry> attributes) : base(name, attributes)
        {
            this.repoPath = repoPath;
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void Create(string path, string root, FilterMode mode)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;

            this.mode = mode;
            string modeArg = mode == FilterMode.Clean ? "clean" : "smudge";

            var startInfo = new ProcessStartInfo
            {
                FileName = "git-lfs",
                Arguments = $"{modeArg} {path}",
                WorkingDirectory = repoPath,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            process = new Process { StartInfo = startInfo };
            process.Start();
        }

        protected override void Complete(string path, string root, Stream output)
        {
            // Wait for git-lfs to finish
            process.StandardInput.Flush();
            process.StandardInput.Close();

            if (mode == FilterMode.Clean)
            {
                process.WaitForExit();
            }

            // For smudge: save file to working copy
            // For clean: pass git-lfs pointer to git
            process.StandardOutput.BaseStream.CopyTo(output);
            process.StandardOutput.BaseStream.Flush();
            process.StandardOutput.Close();
            output.Flush();
            output.Close();

            if (mode == FilterMode.Smudge)
            {
                process.WaitForExit();
            }

            process.Dispose();

            Console.ResetColor();
        }

        protected override void Smudge(string path, string root, Stream input, Stream output)
        {
            // Write git-lfs pointer to stdin
            input.CopyTo(process.StandardInput.BaseStream);
            input.Flush();
        }

        protected override void Clean(string path, string root, Stream input, Stream output)
        {
            // Write file data to stdin
            input.CopyTo(process.StandardInput.BaseStream);
            input.Flush();
        }
    }
}
