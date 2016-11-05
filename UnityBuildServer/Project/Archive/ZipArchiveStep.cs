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

        public override string TypeName
        {
            get
            {
                return "Zip File";
            }
        }

        public override ArchiveInfo CreateArchive(BuildInfo buildInfo, Workspace workspace)
        {
            // Replace in-line variables
            string filename = workspace.Replacements.ReplaceVariablesInText(config.Filename);
            // Sanitize
            filename = filename.Replace(' ', '_');
            string filepath = $"{workspace.ArchivesDirectory}/{filename}.zip";

            // Remove old file
            if (File.Exists(filepath)) {
                File.Delete(filepath);
            }

            // Save zip file
            using (var zipFile = new ZipFile())
            {
                zipFile.AddDirectory(workspace.WorkingDirectory);
                zipFile.Save(filepath);
            }

            var archiveInfo = new ArchiveInfo { ArchiveFileName = filename };
            return archiveInfo;
        }
    }
}
