using Path = System.IO.Path;

namespace SeudoBuild.Pipeline.Modules.FolderArchive
{
    public class FolderArchiveStep : IArchiveStep<FolderArchiveConfig>
    {
        FolderArchiveConfig config;
        IWorkspace workspace;

        public string Type { get; } = "Folder";

        public void Initialize(FolderArchiveConfig config, IWorkspace workspace)
        {
            this.config = config;
            this.workspace = workspace;
        }

        public ArchiveStepResults ExecuteStep(BuildSequenceResults buildInfo, IWorkspace workspace)
        {
            try
            {
                string folderName = workspace.Macros.ReplaceVariablesInText(config.FolderName);
                string source = workspace.BuildOutputDirectory;
                string dest = $"{workspace.ArchivesDirectory}/{folderName}";

                // Remove old directory
                if (workspace.FileSystem.DirectoryExists(dest))
                {
                    workspace.FileSystem.DeleteDirectory(dest);
                }

                CopyDirectory(source, dest);

                var results = new ArchiveStepResults { ArchiveFileName = folderName, IsSuccess = true };
                return results;
            }
            catch (System.Exception e)
            {
                return new ArchiveStepResults { IsSuccess = false, Exception = e };
            }
        }

        void CopyDirectory(string sourceDir, string destDir)
        {
            IFileSystem fs = workspace.FileSystem;

            if (!fs.DirectoryExists(sourceDir))
            {
                throw new System.IO.DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDir);
            }

            if (!fs.DirectoryExists(destDir))
            {
                fs.CreateDirectory(destDir);
            }
            else
            {
                throw new System.Exception("Destination path already exists: " + destDir);
            }

            // Get the files in the directory and copy them to the new location.
            var sourceFiles = fs.GetFiles(sourceDir);
            foreach (string sourceFile in sourceFiles)
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(sourceFile));
                fs.CopyFile(sourceFile, destFile);
            }

            // Copy subdirectories and their contents to new location.
            var subDirectories = fs.GetDirectories(sourceDir);
            foreach (string subDirectory in subDirectories)
            {
                string destPath = Path.Combine(destDir, Path.GetFileName(subDirectory));
                CopyDirectory(subDirectory, destPath);
            }
        }
    }
}
