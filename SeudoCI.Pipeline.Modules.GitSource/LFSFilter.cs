namespace SeudoCI.Pipeline.Modules.GitSource;

using System.Diagnostics;
using LibGit2Sharp;

public class LFSFilter(string name, string repoPath, IEnumerable<FilterAttributeEntry> attributes)
    : Filter(name, attributes)
{
    private Process _process = null!;

    private FilterMode _mode;

    protected override void Create(string path, string root, FilterMode mode)
    {
        _mode = mode;
        string modeArg = mode == FilterMode.Clean ? "clean" : "smudge";

        var startInfo = new ProcessStartInfo
        {
            FileName = "git-lfs",
            Arguments = $"{modeArg} {path}",
            WorkingDirectory = repoPath,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            UseShellExecute = false
        };
        
        try
        {
            _process = new Process { StartInfo = startInfo };
            _process.Start();
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"Failed to start git-lfs {modeArg} process: {e.Message}", e);
        }
    }

    protected override void Complete(string path, string root, Stream output)
    {
        try
        {
            // Wait for git-lfs to finish
            _process.StandardInput.Flush();
            _process.StandardInput.Close();

            if (_mode == FilterMode.Clean)
            {
                if (!_process.WaitForExit(60000)) // 1 minute timeout
                {
                    _process.Kill();
                    throw new TimeoutException($"Git LFS {_mode} operation timed out for {path}");
                }
            }

            // For smudge: save file to working copy
            // For clean: pass git-lfs pointer to git
            _process.StandardOutput.BaseStream.CopyTo(output);
            _process.StandardOutput.BaseStream.Flush();
            _process.StandardOutput.Close();
            output.Flush();
            output.Close();

            if (_mode == FilterMode.Smudge)
            {
                if (!_process.WaitForExit(60000)) // 1 minute timeout
                {
                    _process.Kill();
                    throw new TimeoutException($"Git LFS {_mode} operation timed out for {path}");
                }
            }

            if (_process.ExitCode != 0)
            {
                var errorOutput = _process.StandardError.ReadToEnd();
                throw new InvalidOperationException($"Git LFS {_mode} failed with exit code {_process.ExitCode}. Error: {errorOutput}");
            }
        }
        finally
        {
            _process?.Dispose();
        }
    }

    protected override void Smudge(string path, string root, Stream input, Stream output)
    {
        // Write git-lfs pointer to stdin
        input.CopyTo(_process.StandardInput.BaseStream);
        input.Flush();
    }

    protected override void Clean(string path, string root, Stream input, Stream output)
    {
        // Write file data to stdin
        input.CopyTo(_process.StandardInput.BaseStream);
        input.Flush();
    }
}