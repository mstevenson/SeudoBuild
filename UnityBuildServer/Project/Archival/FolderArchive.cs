using System.IO;

namespace UnityBuildServer
{
    public class FolderArchive : ArchiveConfig
    {
        public override void CreateArchive(BuildInfo buildInfo, Workspace workspace)
        {
            string source = workspace.WorkingDirectory;
            string dest = $"{workspace.ArchivesDirectory}/{buildInfo.GenerateFileName()}";

            CopyDirectory(source, dest);
        }

        void CopyDirectory(string source, string dest)
        {
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
