using JetBrains.Annotations;

namespace SeudoCI.Pipeline.Modules.SFTPDistribute;

using Renci.SshNet;
using Renci.SshNet.Common;
using SeudoCI.Core;

public class SFTPDistributeStep : IDistributeStep<SFTPDistributeConfig>
{
    private SFTPDistributeConfig _config = null!;
    private ILogger _logger = null!;

    public string? Type => "SFTP Upload";

    [UsedImplicitly]
    public void Initialize(SFTPDistributeConfig config, ITargetWorkspace workspace, ILogger logger)
    {
        _config = config;
        _logger = logger;
    }

    public DistributeStepResults ExecuteStep(ArchiveSequenceResults archiveResults, ITargetWorkspace workspace)
    {
        var results = new DistributeStepResults();

        try
        {
            // Upload all archived files
            foreach (var archiveInfo in archiveResults.StepResults)
            {
                var stepResult = new DistributeStepResults.FileResult { ArchiveInfo = archiveInfo };
                try
                {
                    Upload(archiveInfo, workspace);
                    stepResult.Success = true;
                    stepResult.Message = "Upload completed successfully";
                    results.FileResults.Add(stepResult);
                }
                catch (Exception e)
                {
                    stepResult.Success = false;
                    stepResult.Message = e.Message;
                    results.FileResults.Add(stepResult);
                    throw new Exception($"File upload failed for {archiveInfo.ArchiveFileName}", e);
                }
            }

            results.IsSuccess = true;
            return results;
        }
        // One or more archived files failed to upload
        catch (Exception e)
        {
            results.IsSuccess = false;
            results.Exception = e;
            return results;
        }
    }

    public void Upload(ArchiveStepResults archiveInfo, ITargetWorkspace workspace)
    {
        // Supply the password via fake keyboard input
        var passwordAuthMethod = new PasswordAuthenticationMethod(_config.Username, _config.Password);
        var keyboardAuthMethod = new KeyboardInteractiveAuthenticationMethod(_config.Username);
        keyboardAuthMethod.AuthenticationPrompt += (_, args) =>
        {
            foreach (var prompt in args.Prompts)
            {
                if (prompt.Request.IndexOf("Password", StringComparison.InvariantCultureIgnoreCase) != -1)
                {
                    prompt.Response = _config.Password;
                }
            }
        };

        var connectionInfo = new ConnectionInfo(_config.Host, _config.Port, _config.Username, passwordAuthMethod, keyboardAuthMethod);

        using var client = new SftpClient(connectionInfo);

        try
        {
            _logger.Write($"Uploading {archiveInfo.ArchiveFileName} to {_config.Host} {_config.WorkingDirectory}", LogType.SmallBullet);

            client.Connect();

            if (!string.IsNullOrWhiteSpace(_config.WorkingDirectory))
            {
                client.ChangeDirectory(_config.WorkingDirectory);
            }

            var archivesDirectory = workspace.GetDirectory(TargetDirectory.Archives);
            var localFilePath = Path.Combine(archivesDirectory, archiveInfo.ArchiveFileName);

            using (var stream = workspace.FileSystem.OpenRead(localFilePath))
            {
                client.BufferSize = 4 * 1024;
                client.UploadFile(stream, archiveInfo.ArchiveFileName);
            }

            _logger.Write("Upload succeeded", LogType.SmallBullet);
        }
        finally
        {
            if (client.IsConnected)
            {
                client.Disconnect();
            }
        }
    }
}
