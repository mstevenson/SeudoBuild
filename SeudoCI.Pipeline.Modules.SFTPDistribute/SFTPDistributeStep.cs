using JetBrains.Annotations;

namespace SeudoCI.Pipeline.Modules.SFTPDistribute;

using Renci.SshNet;
using Renci.SshNet.Common;
using SeudoCI.Core;

public class SFTPDistributeStep : IDistributeStep<SFTPDistributeConfig>
{
    private SFTPDistributeConfig _config;
    private ILogger _logger;

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
                    results.FileResults.Add(stepResult);
                }
                catch (Exception e)
                {
                    stepResult.Success = false;
                    stepResult.Message = e.Message;
                    results.FileResults.Add(stepResult);
                    throw new Exception("File upload failed");
                }
            }
            return new DistributeStepResults { IsSuccess = true };
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
        var keyboardAuthMethod = new KeyboardInteractiveAuthenticationMethod(_config.Username);
        keyboardAuthMethod.AuthenticationPrompt += (sender, args) => {
            foreach (AuthenticationPrompt prompt in args.Prompts)
            {
                if (prompt.Request.IndexOf("Password:", StringComparison.InvariantCultureIgnoreCase) != -1)
                {
                    prompt.Response = _config.Password;
                }
            }
        };
                                                                                           
        var connectionInfo = new ConnectionInfo(_config.Host, _config.Port, _config.Username, keyboardAuthMethod);

        using (var client = new SftpClient(connectionInfo))
        {
            _logger.Write($"Uploading {archiveInfo.ArchiveFileName} to {_config.Host} {_config.WorkingDirectory}", LogType.SmallBullet);

            client.Connect();
            client.ChangeDirectory(_config.WorkingDirectory);

            string filename = archiveInfo.ArchiveFileName;
            string filepath = $"{workspace.GetDirectory(TargetDirectory.Archives)}/{filename}";

            using (var stream = new FileStream(filepath, FileMode.Open))
            {
                client.BufferSize = 4 * 1024;
                client.UploadFile(stream, filename);
            }

            _logger.Write("Upload succeeded", LogType.SmallBullet);
        }
    }
}