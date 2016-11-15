using System;
using System.IO;
using Ionic.Zip;

namespace SeudoBuild.Modules.ZipArchive
{
    public class ZipArchiveStep : IArchiveStep
    {
        ZipArchiveConfig config;
        Workspace workspace;

        public string Type { get; } = "Zip File";

        public ZipArchiveStep(ZipArchiveConfig config, Workspace workspace)
        {
            this.config = config;
            this.workspace = workspace;
        }

        public ArchiveStepResults ExecuteStep(BuildSequenceResults buildInfo, Workspace workspace)
        {
            try
            {
                // Remove file extension in case it was accidentally included in the config data
                string filename = Path.GetFileNameWithoutExtension(config.Filename);
                // Replace in-line variables
                filename = workspace.Macros.ReplaceVariablesInText(config.Filename);
                // Sanitize
                filename = filename.Replace(' ', '_');
                filename = filename + ".zip";
                string filepath = $"{workspace.ArchivesDirectory}/{filename}";

                // Remove old file
                if (File.Exists(filepath)) {
                    File.Delete(filepath);
                }

                BuildConsole.WriteLine($"Creating zip file {filename}");

                // Save zip file
                using (var zipFile = new ZipFile())
                {
                    zipFile.AddDirectory(workspace.BuildOutputDirectory);
                    zipFile.Save(filepath);
                }

                BuildConsole.WriteLine("Zip file saved");

                var results = new ArchiveStepResults { ArchiveFileName = filename, IsSuccess = true };
                return results;
            }
            catch (Exception e)
            {
                return new ArchiveStepResults { IsSuccess = false, Exception = e };
            }
        }
    }
}
