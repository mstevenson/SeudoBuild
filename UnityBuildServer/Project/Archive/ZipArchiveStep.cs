using System.IO;
using Ionic.Zip;

namespace UnityBuildServer
{
    public class ZipArchiveStep : ArchiveStep
    {
        ZipArchiveConfig config;

        public ZipArchiveStep(ZipArchiveConfig config)
        {
            this.config = config;
        }

        public override void CreateArchive(BuildInfo buildInfo, Workspace workspace)
        {
            using (var zipFile = new ZipFile())
            {
                string outputName = buildInfo.GenerateFileName();
                zipFile.AddDirectory(workspace.WorkingDirectory);
                zipFile.Save($"{workspace.ArchivesDirectory}/{outputName}.zip");
            }
        }
    }
}
