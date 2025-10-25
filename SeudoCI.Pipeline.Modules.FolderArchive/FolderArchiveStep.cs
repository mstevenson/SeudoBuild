using JetBrains.Annotations;

namespace SeudoCI.Pipeline.Modules.FolderArchive;

using Core;

public class FolderArchiveStep : IArchiveStep<FolderArchiveConfig>
{
    private FolderArchiveConfig _config = null!;
    private ILogger _logger = null!;

    public string? Type => "Folder";

    [UsedImplicitly]
    public void Initialize(FolderArchiveConfig config, ITargetWorkspace workspace, ILogger logger)
    {
        _config = config;
        _logger = logger;
    }

    public ArchiveStepResults ExecuteStep(BuildSequenceResults buildInfo, ITargetWorkspace workspace)
    {
        try
        {
            var fileSystem = workspace.FileSystem;

            string folderName = workspace.Macros.ReplaceVariablesInText(_config.FolderName);

            if (string.IsNullOrWhiteSpace(folderName))
            {
                throw new InvalidOperationException("Folder archive requires a non-empty folder name.");
            }

            folderName = folderName.SanitizeFilename();

            if (string.IsNullOrWhiteSpace(folderName))
            {
                throw new InvalidOperationException("Folder archive name resolves to an empty value after sanitization.");
            }

            string source = workspace.GetDirectory(TargetDirectory.Output);
            string archivesDirectory = workspace.GetDirectory(TargetDirectory.Archives);
            string dest = Path.Combine(archivesDirectory, folderName);

            // Remove old directory
            if (fileSystem.DirectoryExists(dest))
            {
                fileSystem.DeleteDirectory(dest);
            }

            _logger.Write($"Copying build output to folder archive '{folderName}'", LogType.SmallBullet);

            CopyDirectory(fileSystem, source, dest);

            _logger.Write("Folder archive created", LogType.SmallBullet);

            var results = new ArchiveStepResults { ArchiveFileName = folderName, IsSuccess = true };
            return results;
        }
        catch (Exception e)
        {
            return new ArchiveStepResults { IsSuccess = false, Exception = e };
        }
    }

    private static void CopyDirectory(IFileSystem fileSystem, string sourceDir, string destDir)
    {
        if (!fileSystem.DirectoryExists(sourceDir))
        {
            throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDir);
        }

        if (!fileSystem.DirectoryExists(destDir))
        {
            fileSystem.CreateDirectory(destDir);
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
            CopyDirectory(fileSystem, subDirectory, destPath);
        }
    }
}
