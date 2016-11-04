using System.IO;
using Ionic.Zip;

namespace UnityBuildServer
{
    public class ZipArchive : ArchiveConfig
    {
        public string Filename { get; set; }

        public override void CreateArchive(string workingDirectory, string archivesDirectory)
        {
            using (var zipFile = new ZipFile())
            {
                zipFile.AddDirectory(workingDirectory);
                string filename = "test.zip";
                zipFile.Save($"{archivesDirectory}/{filename}");
            }
        }
    }
}
