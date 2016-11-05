using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using LibGit2Sharp;

namespace UnityBuildServer
{
    public class LFSFilter : Filter
    {
        Process process;
        string repoPath;

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
            var startInfo = new ProcessStartInfo
            {
                FileName = "git-lfs",
                Arguments = mode == FilterMode.Clean ? "clean" : "smudge",
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
            process.WaitForExit();

            // For smudge: save file to working copy
            // For clean: pass git-lfs pointer to git
            process.StandardOutput.BaseStream.CopyTo(output);
            output.Flush();
            output.Close();

            process.Dispose();
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
