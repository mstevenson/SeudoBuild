using System;
using System.IO;
using System.Net;
using System.Collections.Generic;

namespace SeudoBuild
{
    public class FTPDistributeStep : IDistributeStep
    {
        FTPDistributeConfig config;

        public FTPDistributeStep(FTPDistributeConfig config)
        {
            this.config = config;
        }

        public string Type { get; } = "FTP Upload";

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

        void Upload (ArchiveStepResults archiveInfo, Workspace workspace)
        {
            // Get the object used to communicate with the server.
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"{config.URL}:{config.Port}/{config.BasePath}/{archiveInfo.ArchiveFileName}");
            request.Credentials = new NetworkCredential(config.Username, config.Password);
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.UseBinary = true;
            request.KeepAlive = true;

            var file = new FileInfo($"{workspace.ArchivesDirectory}/{archiveInfo.ArchiveFileName}");
            request.ContentLength = file.Length;

            int bufferLength = 16 * 1024;
            byte[] buffer = new byte[bufferLength];

            FileStream fileStream = file.OpenRead();

            Stream stream = request.GetRequestStream();
            int length = 0;
            while ((length = fileStream.Read(buffer, 0, bufferLength)) != 0)
            {
                stream.Write(buffer, 0, length);
            }
            stream.Close();
            fileStream.Close();

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            BuildConsole.WriteLine($"Upload File Complete, status {response.StatusDescription}");
            response.Close();
        }
    }
}
