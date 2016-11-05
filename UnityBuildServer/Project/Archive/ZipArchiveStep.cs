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
            string outputName = workspace.Replacements.ReplaceVariablesInText(config.Filename);
            // Sanitize
            outputName = outputName.Replace(' ', '_');

            // Save zip file
            using (var zipFile = new ZipFile())
            {
                zipFile.AddDirectory(workspace.WorkingDirectory);
                zipFile.Save($"{workspace.ArchivesDirectory}/{outputName}.zip");
            }

            var archiveInfo = new ArchiveInfo { ArchiveFileName = outputName };
            return archiveInfo;
        }
    }
}
