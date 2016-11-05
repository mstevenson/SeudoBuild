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
            // Remove file extension in case it was accidentally included in the config data
            string filename = Path.GetFileNameWithoutExtension(config.Filename);
            // Replace in-line variables
            filename = workspace.Replacements.ReplaceVariablesInText(config.Filename);
            // Sanitize
            filename = filename.Replace(' ', '_');
            filename = filename + ".zip";
            string filepath = $"{workspace.ArchivesDirectory}/{filename}";

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
