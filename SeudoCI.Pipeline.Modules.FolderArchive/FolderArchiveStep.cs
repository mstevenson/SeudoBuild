using JetBrains.Annotations;

namespace SeudoCI.Pipeline.Modules.FolderArchive;

using Core;

public class FolderArchiveStep : IArchiveStep<FolderArchiveConfig>
{
    private FolderArchiveConfig _config = null!;
    private ITargetWorkspace _workspace = null!;
    private ILogger _logger = null!;

    public string? Type => "Folder";

    [UsedImplicitly]
    public void Initialize(FolderArchiveConfig config, ITargetWorkspace workspace, ILogger logger)
    {
        _config = config;
        _workspace = workspace;
        _logger = logger;
    }

    public ArchiveStepResults ExecuteStep(BuildSequenceResults buildInfo, ITargetWorkspace workspace)
    {
        try
        {
            string folderName = workspace.Macros.ReplaceVariablesInText(_config.FolderName);
            string source = workspace.GetDirectory(TargetDirectory.Output);
            string dest = $"{workspace.GetDirectory(TargetDirectory.Archives)}/{folderName}";

            // Remove old directory
            if (workspace.FileSystem.DirectoryExists(dest))
            {
                workspace.FileSystem.DeleteDirectory(dest);
            }

            CopyDirectory(source, dest);

            var results = new ArchiveStepResults { ArchiveFileName = folderName, IsSuccess = true };
            return results;
        }
        catch (Exception e)
        {
            return new ArchiveStepResults { IsSuccess = false, Exception = e };
        }
    }

    private void CopyDirectory(string sourceDir, string destDir)
    {
        var fileSystem = _workspace.FileSystem;

        if (!fileSystem.DirectoryExists(sourceDir))
        {
            throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDir);
        }

        if (!fileSystem.DirectoryExists(destDir))
        {
            fileSystem.CreateDirectory(destDir);
        }
        else
        {
            throw new Exception("Destination path already exists: " + destDir);
        }

        // Get the files in the directory and copy them to the new location.
        var sourceFiles = fileSystem.GetFiles(sourceDir);
        foreach (string sourceFile in sourceFiles)
        {
            string destFile = Path.Combine(destDir, Path.GetFileName(sourceFile));
            fileSystem.CopyFile(sourceFile, destFile);
        }

        // Copy subdirectories and their contents to new location.
        var subDirectories = fileSystem.GetDirectories(sourceDir);
        foreach (string subDirectory in subDirectories)
        {
            string destPath = Path.Combine(destDir, Path.GetFileName(subDirectory));
            CopyDirectory(subDirectory, destPath);
        }
    }
}