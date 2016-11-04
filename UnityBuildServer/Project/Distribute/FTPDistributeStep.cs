using System;
using System.IO;
using System.Net;
using System.Text;

namespace UnityBuildServer
{
    public class FTPDistributeStep : DistributeStep
    {
        FTPDistributeConfig config;

        public FTPDistributeStep(FTPDistributeConfig config)
        {
            this.config = config;
        }

        public override void Distribute(string archivePath, Workspace workspace)
        {
            // Get the object used to communicate with the server.
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"{config.URL}:{config.Port}/{config.BasePath}/{config.ArchiveFileName}");
            request.Credentials = new NetworkCredential(config.Username, config.Password);
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.UseBinary = true;
            request.KeepAlive = true;

            var file = new FileInfo($"{workspace.ArchivesDirectory}/{config.ArchiveFileName}");
            request.ContentLength = file.Length;

            int bufferLength = 16 * 1024;
            byte[] buffer = new byte[bufferLength];

            FileStream fileStream = file.OpenRead();

            try
            {
                Stream stream = request.GetRequestStream();
                int length = 0;
                while ((length = fileStream.Read(buffer, 0, bufferLength)) != 0)
                {
                    stream.Write(buffer, 0, length);
                }
                stream.Close();
                fileStream.Close();

                FtpWebResponse response = (FtpWebResponse)request.GetResponse();
                Console.WriteLine("Upload File Complete, status {0}", response.StatusDescription);
                response.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
