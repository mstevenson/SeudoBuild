using System;
using System.IO;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace SeudoBuild.Modules.SFTPDistribute
{
    public class SFTPDistributeStep : IDistributeStep<SFTPDistributeConfig>
    {
        SFTPDistributeConfig config;

        public string Type { get; } = "SFTP Upload";

        public void Initialize(SFTPDistributeConfig config, Workspace workspace)
        {
            this.config = config;
        }

        public DistributeStepResults ExecuteStep(ArchiveSequenceResults archiveResults, Workspace workspace)
        {
            try
            {
                foreach (var archiveInfo in archiveResults.StepResults)
                {
                    Upload(archiveInfo, workspace);
                }
                return new DistributeStepResults { IsSuccess = true };
            }
            catch (Exception e)
            {
                return new DistributeStepResults { IsSuccess = false, Exception = e };
            }
        }

        public void Upload(ArchiveStepResults archiveInfo, Workspace workspace)
        {
            // Supply the password via fake keyboard input
            var keyboardAuthMethod = new KeyboardInteractiveAuthenticationMethod(config.Username);
            keyboardAuthMethod.AuthenticationPrompt += new EventHandler<AuthenticationPromptEventArgs>((sender, args) => {
                foreach (AuthenticationPrompt prompt in args.Prompts)
                {
                    if (prompt.Request.IndexOf("Password:", StringComparison.InvariantCultureIgnoreCase) != -1)
                    {
                        prompt.Response = config.Password;
                    }
                }
            });
                                                                                           
            ConnectionInfo connectionInfo = new ConnectionInfo(config.Host, config.Port, config.Username, keyboardAuthMethod);

            using (var client = new SftpClient(connectionInfo))
            {
                BuildConsole.WriteLine($"Uploading {archiveInfo.ArchiveFileName} to {config.Host} {config.WorkingDirectory}");

                client.Connect();
                client.ChangeDirectory(config.WorkingDirectory);

                string filename = archiveInfo.ArchiveFileName;
                string filepath = $"{workspace.ArchivesDirectory}/{filename}";

                using (var stream = new FileStream(filepath, FileMode.Open))
                {
                    client.BufferSize = 4 * 1024;
                    client.UploadFile(stream, filename);
                }

                BuildConsole.WriteLine("Upload succeeded");
            }
        }
    }
}
