using System;
namespace UnityBuildServer
{
    public class FolderArchive : ArchiveConfig
    {
        public override void CreateArchive(string workingDirectory, string archivesDirectory)
        {
            // TODO Move contents of working directory to archives directory
            // under a new folder matching the project name
        }
    }
}
