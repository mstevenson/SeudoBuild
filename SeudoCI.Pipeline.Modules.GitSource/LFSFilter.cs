namespace SeudoCI.Pipeline.Modules.GitSource;

using System.Diagnostics;
using LibGit2Sharp;

public class LFSFilter : Filter
{
    private Process _process = null!;
    private readonly string _repoPath;

    private FilterMode _mode;

    public LFSFilter(string name, string repoPath, IEnumerable<FilterAttributeEntry> attributes) : base(name, attributes)
    {
        _repoPath = repoPath;
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    protected override void Create(string path, string root, FilterMode mode)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;

        _mode = mode;
        string modeArg = mode == FilterMode.Clean ? "clean" : "smudge";

        var startInfo = new ProcessStartInfo
        {
            FileName = "git-lfs",
            Arguments = $"{modeArg} {path}",
            WorkingDirectory = _repoPath,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            UseShellExecute = false
        };
        _process = new Process { StartInfo = startInfo };
        _process.Start();
    }

    protected override void Complete(string path, string root, Stream output)
    {
        // Wait for git-lfs to finish
        _process.StandardInput.Flush();
        _process.StandardInput.Close();

        if (_mode == FilterMode.Clean)
        {
            _process.WaitForExit();
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
            _process.WaitForExit();
        }

        _process.Dispose();

        Console.ResetColor();
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