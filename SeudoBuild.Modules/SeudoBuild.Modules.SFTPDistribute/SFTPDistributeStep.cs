using System;
using System.IO;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace SeudoBuild.Pipeline.Modules.SFTPDistribute
{
    public class SFTPDistributeStep : IDistributeStep<SFTPDistributeConfig>
    {
        SFTPDistributeConfig config;
        ILogger logger;

        public string Type { get; } = "SFTP Upload";

        public void Initialize(SFTPDistributeConfig config, IWorkspace workspace, ILogger logger)
        {
            this.config = config;
            this.logger = logger;
        }

        public DistributeStepResults ExecuteStep(ArchiveSequenceResults archiveResults, IWorkspace workspace)
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

        public void Upload(ArchiveStepResults archiveInfo, IWorkspace workspace)
        {
            // Supply the password via fake keyboard input
            var keyboardAuthMethod = new KeyboardInteractiveAuthenticationMethod(config.Username);
            keyboardAuthMethod.AuthenticationPrompt += (sender, args) => {
                foreach (AuthenticationPrompt prompt in args.Prompts)
                {
                    if (prompt.Request.IndexOf("Password:", StringComparison.InvariantCultureIgnoreCase) != -1)
                    {
                        prompt.Response = config.Password;
                    }
                }
            };
                                                                                           
            ConnectionInfo connectionInfo = new ConnectionInfo(config.Host, config.Port, config.Username, keyboardAuthMethod);

            using (var client = new SftpClient(connectionInfo))
            {
                logger.WriteLine($"Uploading {archiveInfo.ArchiveFileName} to {config.Host} {config.WorkingDirectory}");

                client.Connect();
                client.ChangeDirectory(config.WorkingDirectory);

                string filename = archiveInfo.ArchiveFileName;
                string filepath = $"{workspace.ArchivesDirectory}/{filename}";

                using (var stream = new FileStream(filepath, FileMode.Open))
                {
                    client.BufferSize = 4 * 1024;
                    client.UploadFile(stream, filename);
                }

                logger.WriteLine("Upload succeeded");
            }
        }
    }
}
