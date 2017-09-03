using System;
using Ionic.Zip;
using Path = System.IO.Path;

namespace SeudoBuild.Pipeline.Modules.ZipArchive
{
    public class ZipArchiveStep : IArchiveStep<ZipArchiveConfig>
    {
        ZipArchiveConfig config;
        ILogger logger;
        
        public string Type { get; } = "Zip File";

        public void Initialize(ZipArchiveConfig config, ITargetWorkspace workspace, ILogger logger)
        {
            this.config = config;
            this.logger = logger;
        }

        public ArchiveStepResults ExecuteStep(BuildSequenceResults buildInfo, ITargetWorkspace workspace)
        {
            try
            {
                var fs = workspace.FileSystem;

                // Remove file extension in case it was accidentally included in the config data
                string filename = Path.GetFileNameWithoutExtension(config.Filename);
                // Replace in-line variables
                filename = workspace.Macros.ReplaceVariablesInText(config.Filename);
                // Sanitize
                filename = filename.SanitizeFilename();
                filename = filename + ".zip";
                string filepath = $"{workspace.ArchivesDirectory}/{filename}";

                // Remove old file
                if (fs.FileExists(filepath)) {
                    fs.DeleteFile(filepath);
                }

                logger.Write($"Creating zip file {filename}", LogType.SmallBullet);

                // Save zip file
                using (var zipFile = new ZipFile())
                using (var stream = fs.OpenWrite(filepath))
                {
                    zipFile.AddDirectory(workspace.OutputDirectory);
                    zipFile.Save(stream);
                }

                logger.Write("Zip file saved", LogType.SmallBullet);

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
