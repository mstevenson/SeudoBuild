namespace SeudoCI.Pipeline.Modules.ZipArchive;

using Ionic.Zip;
using SeudoCI.Core;
using Path = System.IO.Path;

public class ZipArchiveStep : IArchiveStep<ZipArchiveConfig>
{
    private ZipArchiveConfig _config;
    private ILogger _logger;
        
    public string Type => "Zip File";

    public void Initialize(ZipArchiveConfig config, ITargetWorkspace workspace, ILogger logger)
    {
        _config = config;
        _logger = logger;
    }

    public ArchiveStepResults ExecuteStep(BuildSequenceResults buildInfo, ITargetWorkspace workspace)
    {
        try
        {
            var fs = workspace.FileSystem;

            // Remove file extension in case it was accidentally included in the config data
            string filename = Path.GetFileNameWithoutExtension(_config.Filename);
            // Replace in-line variables
            filename = workspace.Macros.ReplaceVariablesInText(filename);
            // Sanitize
            filename = filename.SanitizeFilename();
            filename = filename + ".zip";
            string filepath = $"{workspace.GetDirectory(TargetDirectory.Archives)}/{filename}";

            // Remove old file
            if (fs.FileExists(filepath)) {
                fs.DeleteFile(filepath);
            }

            _logger.Write($"Creating zip file {filename}", LogType.SmallBullet);

            // Save zip file
            using (var zipFile = new ZipFile())
            using (var stream = fs.OpenWrite(filepath))
            {
                zipFile.AddDirectory(workspace.GetDirectory(TargetDirectory.Output));
                zipFile.Save(stream);
            }

            _logger.Write("Zip file saved", LogType.SmallBullet);

            var results = new ArchiveStepResults { ArchiveFileName = filename, IsSuccess = true };
            return results;
        }
        catch (Exception e)
        {
            return new ArchiveStepResults { IsSuccess = false, Exception = e };
        }
    }
}