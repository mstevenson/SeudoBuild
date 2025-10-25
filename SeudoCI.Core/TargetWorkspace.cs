namespace SeudoCI.Core;

using System;

// Macros:
// 
// %project_name% -- the name for the entire project
// %build_target_name% -- the specific target that was built
// %app_version% -- version number as major.minor.patch
// %build_date% -- the date that the build was completed
// %commit_identifier% -- the current commit number or hash

public class TargetWorkspace : ITargetWorkspace
{
    private readonly string _baseDirectory;
        
    public IMacros Macros { get; }

    public IFileSystem FileSystem { get; }
    
    public IProjectWorkspace ProjectWorkspace { get; set; } = null!;
        
    public TargetWorkspace(string baseDirectory, IFileSystem fileSystem, IMacros? parentMacros = null)
    {
        _baseDirectory = baseDirectory;
        FileSystem = fileSystem;
        Macros = new ChainedMacros(parentMacros);
        
        Macros["working_directory"] = GetDirectory(TargetDirectory.Source);
        Macros["build_output_directory"] = GetDirectory(TargetDirectory.Output);
        Macros["archives_directory"] = GetDirectory(TargetDirectory.Archives);
        Macros["logs_directory"] = GetDirectory(TargetDirectory.Logs);
    }
        
    public string GetDirectory(TargetDirectory directory)
    {
        return directory switch
        {
            TargetDirectory.Source => $"{_baseDirectory}/Workspace",
            TargetDirectory.Output => $"{_baseDirectory}/Output",
            TargetDirectory.Archives => $"{_baseDirectory}/Archives",
            TargetDirectory.Logs => $"{_baseDirectory}/Logs",
            _ => throw new ArgumentOutOfRangeException(nameof(directory), directory, null)
        };
    }

    public void CleanDirectory(TargetDirectory directory)
    {
        var dir = GetDirectory(directory);
            
        if (!FileSystem.DirectoryExists(dir))
        {
            return;
        }

        FileSystem.DeleteDirectory(dir);
    }

    public void InitializeDirectories()
    {
        FileSystem.CreateDirectory(GetDirectory(TargetDirectory.Source));
        FileSystem.CreateDirectory(GetDirectory(TargetDirectory.Output));
        FileSystem.CreateDirectory(GetDirectory(TargetDirectory.Archives));
        FileSystem.CreateDirectory(GetDirectory(TargetDirectory.Logs));
    }
}