using System.IO;

namespace SeudoBuild
{
    public class FolderArchiveStep : IArchiveStep
    {
        FolderArchiveConfig config;

        public FolderArchiveStep(FolderArchiveConfig config, Workspace workspace)
        {
            this.config = config;
        }

        public string Type { get; } = "Folder";

        public ArchiveStepResults ExecuteStep(BuildSequenceResults buildInfo, Workspace workspace)
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

        void CopyDirectory(string source, string dest)
        {
            // FIXME abstract using IFileSystem

            // Get the subdirectories for the specified directory.
            DirectoryInfo sourceDir = new DirectoryInfo(source);

            if (!sourceDir.Exists)
            {
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + source);
            }

            DirectoryInfo[] dirs = sourceDir.GetDirectories();
            if (!Directory.Exists(dest))
            {
                Directory.CreateDirectory(dest);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = sourceDir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(dest, file.Name);
                file.CopyTo(tempPath, false);
            }

            // Copy subdirectories and their contents to new location.
            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(dest, subdir.Name);
                CopyDirectory(subdir.FullName, temppath);
            }
        }
    }
}
