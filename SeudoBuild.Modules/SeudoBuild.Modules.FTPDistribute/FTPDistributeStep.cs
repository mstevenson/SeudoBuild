using System;
using System.IO;
using System.Net;

namespace SeudoBuild.Pipeline.Modules.FTPDistribute
{
    public class FTPDistributeStep : IDistributeStep<FTPDistributeConfig>
    {
        private FTPDistributeConfig _config;
        private ILogger _logger;

        public string Type { get; } = "FTP Upload";

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

        private void Upload (ArchiveStepResults archiveInfo, ITargetWorkspace workspace)
        {
            // Get the object used to communicate with the server.
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create($"{_config.URL}:{_config.Port}/{_config.BasePath}/{archiveInfo.ArchiveFileName}");
            request.Credentials = new NetworkCredential(_config.Username, _config.Password);
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.UseBinary = true;
            request.KeepAlive = true;

            // FIXME abstract IO using IFileSystem

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
            _logger.Write($"Upload File Complete, status {response.StatusDescription}", LogType.SmallBullet);
            response.Close();
        }
    }
}
