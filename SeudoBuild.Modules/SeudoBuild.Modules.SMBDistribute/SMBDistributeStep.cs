using System;
using Path = System.IO.Path;
using System.Diagnostics;

namespace SeudoBuild.Pipeline.Modules.SMBDistribute
{
    public class SMBDistributeStep : IDistributeStep<SMBDistributeConfig>
    {
        private SMBDistributeConfig _config;
        private ILogger _logger;

        public string Type { get; } = "SMB Transfer";

        public void Initialize(SMBDistributeConfig config, ITargetWorkspace workspace, ILogger logger)
        {
            _config = config;
            _logger = logger;
        }

        public DistributeStepResults ExecuteStep(ArchiveSequenceResults archiveResults, ITargetWorkspace workspace)
        {
            try
            {
                foreach (var archiveInfo in archiveResults.StepResults)
                {
                    //Upload(archiveInfo, workspace);
                }
                return new DistributeStepResults { IsSuccess = true };
            }
            catch (Exception e)
            {
                return new DistributeStepResults { IsSuccess = false, Exception = e };
            }
        }

        private void Mount(bool mount, SMBDistributeConfig config, Platform platform)
        {
            if (platform == Platform.Mac)
            {
                const string mountDir = "SMBDistribute";
                string serverPath = Path.Combine(config.Host, config.Directory);
                string command = mount ? "mount" : "umount";
                var startInfo = new ProcessStartInfo($"/sbin/{command}", $"mount -t -smbfs //{config.Username}:{config.Password}@{serverPath} /Volumes/{mountDir}");
                var process = Process.Start(startInfo);
                process?.WaitForExit(10000);
            }
            else
            {
                throw new Exception("SMB Distribute does not support platform " + platform);
            }
        }
    }
}
