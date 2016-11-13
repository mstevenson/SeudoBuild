using System;

namespace SeudoBuild.Modules.FTPDistribute
{
    public class FTPDistributeConfig : DistributeStepConfig
    {
        public override string Type { get; } = "FTP Upload";
        public string URL { get; set; }
        public int Port { get; set; } = 21;
        public string BasePath { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
