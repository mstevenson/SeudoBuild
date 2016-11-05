using System;
using System.IO;
using System.Collections.Generic;
using Renci.SshNet;

namespace UnityBuildServer
{
    public class SFTPDistributeStep : DistributeStep
    {
        SFTPDistributeConfig config;

        public SFTPDistributeStep(SFTPDistributeConfig config)
        {
            this.config = config;
        }

        public override string TypeName
        {
            get
            {
                return "SFTP Upload";
            }
        }

        public override void Distribute(List<ArchiveInfo> archiveInfos, Workspace workspace)
        {
            foreach (var archiveInfo in archiveInfos)
            {
                Upload(archiveInfo, workspace);
            }
        }

        public void Upload(ArchiveInfo archiveInfo, Workspace workspace)
        {
            var passwordAuthentication = new PasswordAuthenticationMethod(config.Username, config.Password);
            ConnectionInfo connectionInfo = new ConnectionInfo(config.Host, config.Port, config.Username, passwordAuthentication);

            using (var client = new SftpClient(connectionInfo))
            {
                BuildConsole.WriteLine($"Uploading {archiveInfo.ArchiveFileName} to {config.Host}/{config.WorkingDirectory}");

                client.Connect();
                client.ChangeDirectory(config.WorkingDirectory);

                string filename = archiveInfo.ArchiveFileName;
                string filepath = $"{workspace.ArchivesDirectory}/{filename}";

                using (var stream = new FileStream(filepath, FileMode.Open))
                {
                    client.BufferSize = 4 * 1024;
                    client.UploadFile(stream, filename);
                }

                BuildConsole.WriteLine("Upload successful");
            }
        }
    }
}
