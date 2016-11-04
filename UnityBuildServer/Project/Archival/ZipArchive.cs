using System;
namespace UnityBuildServer
{
    public class ZipArchive : ArchiveConfig
    {
        public string Filename { get; set; }

        public override void CreateArchive(string workingDirectory, string archivesDirectory)
        {
            // TODO create a ZIP file from the contents of the working directory,
            // save it into the archives directory
        }
    }
}
