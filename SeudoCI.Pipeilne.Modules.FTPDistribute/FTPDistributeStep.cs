namespace SeudoCI.Pipeline.Modules.FTPDistribute;

using FluentFTP;
using SeudoCI.Core;

public class FTPDistributeStep : IDistributeStep<FTPDistributeConfig>
{
    private FTPDistributeConfig _config = null!;
    private ILogger _logger = null!;

    public string? Type => "FTP Upload";

    public void Initialize(FTPDistributeConfig config, ITargetWorkspace workspace, ILogger logger)
    {
        _config = config;
        _logger = logger;
    }

    public DistributeStepResults ExecuteStep(ArchiveSequenceResults archiveResults, ITargetWorkspace workspace)
    {
        var results = new DistributeStepResults();

        try
        {
            // Upload all archived files using modern async FTP client
            var uploadTask = UploadFilesAsync(archiveResults, workspace, results);
            uploadTask.GetAwaiter().GetResult();
            
            return new DistributeStepResults { IsSuccess = true };
        }
        catch (Exception e)
        {
            results.IsSuccess = false;
            results.Exception = e;
            return results;
        }
    }

    private async Task UploadFilesAsync(ArchiveSequenceResults archiveResults, ITargetWorkspace workspace, DistributeStepResults results)
    {
        using var ftpClient = new AsyncFtpClient(_config.URL, _config.Username, _config.Password, _config.Port);
        
        try
        {
            // Configure FTP client settings
            ftpClient.Config.ConnectTimeout = 30000; // 30 second timeout
            ftpClient.Config.DataConnectionType = FtpDataConnectionType.AutoPassive;
            ftpClient.Config.EncryptionMode = FtpEncryptionMode.None; // Plain FTP for compatibility
            
            _logger.Write($"Connecting to FTP server {_config.URL}:{_config.Port}", LogType.SmallBullet);
            await ftpClient.Connect();
            
            _logger.Write("FTP connection established", LogType.SmallBullet);

            // Ensure remote directory exists
            if (!string.IsNullOrEmpty(_config.BasePath))
            {
                await ftpClient.CreateDirectory(_config.BasePath);
            }

            // Upload all archived files
            foreach (var archiveInfo in archiveResults.StepResults)
            {
                var stepResult = new DistributeStepResults.FileResult { ArchiveInfo = archiveInfo };
                try
                {
                    await UploadFileAsync(ftpClient, archiveInfo, workspace);
                    stepResult.Success = true;
                    stepResult.Message = "Upload completed successfully";
                    results.FileResults.Add(stepResult);
                }
                catch (Exception e)
                {
                    stepResult.Success = false;
                    stepResult.Message = e.Message;
                    results.FileResults.Add(stepResult);
                    throw new Exception($"File upload failed for {archiveInfo.ArchiveFileName}: {e.Message}");
                }
            }
        }
        finally
        {
            if (ftpClient.IsConnected)
            {
                await ftpClient.Disconnect();
                _logger.Write("FTP connection closed", LogType.SmallBullet);
            }
        }
    }

    private async Task UploadFileAsync(AsyncFtpClient ftpClient, ArchiveStepResults archiveInfo, ITargetWorkspace workspace)
    {
        var localFilePath = Path.Combine(workspace.GetDirectory(TargetDirectory.Archives), archiveInfo.ArchiveFileName);
        var remoteFilePath = string.IsNullOrEmpty(_config.BasePath) 
            ? archiveInfo.ArchiveFileName 
            : $"{_config.BasePath.TrimEnd('/')}/{archiveInfo.ArchiveFileName}";

        _logger.Write($"Uploading {archiveInfo.ArchiveFileName} to {remoteFilePath}", LogType.SmallBullet);

        // Use workspace file system abstraction
        using var fileStream = workspace.FileSystem.OpenRead(localFilePath);
        
        var uploadResult = await ftpClient.UploadStream(fileStream, remoteFilePath, FtpRemoteExists.Overwrite, true);
        
        if (uploadResult == FtpStatus.Success)
        {
            _logger.Write($"Successfully uploaded {archiveInfo.ArchiveFileName}", LogType.SmallBullet);
        }
        else
        {
            throw new Exception($"FTP upload failed with status: {uploadResult}");
        }
    }
}