using System;
using Path = System.IO.Path;
using System.Diagnostics;

namespace SeudoBuild.Modules.SMBDistribute
{
    public class SMBDistributeStep : IDistributeStep<SMBDistributeConfig>
    {
        SMBDistributeConfig config;

        public void Initialize(SMBDistributeConfig config, Workspace workspace)
        {
            this.config = config;
        }

        public string Type { get; } = "SMB Transfer";

        public DistributeStepResults ExecuteStep(ArchiveSequenceResults archiveResults, Workspace workspace)
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

        void Mount(bool mount, SMBDistributeConfig config)
        {
            if (Workspace.RunningPlatform == Workspace.Platform.Mac)
            {
                const string mountDir = "SMBDistribute";
                string serverPath = Path.Combine(config.Host, config.Directory);
                string command = mount ? "mount" : "umount";
                var startInfo = new ProcessStartInfo($"/sbin/{command}", $"mount -t -smbfs //{config.Username}:{config.Password}@{serverPath} /Volumes/{mountDir}");
                var process = Process.Start(startInfo);
                process.WaitForExit(10000);
            }
            else
            {
                throw new Exception("SMB Distribute does not support platform " + Workspace.RunningPlatform);
            }
        }
    }
}
