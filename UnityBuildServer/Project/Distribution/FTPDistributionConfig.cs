using System;
namespace UnityBuildServer
{
    public class FTPDistributionConfig : DistributionConfig
    {
        public string URL { get; set; }
        public int Port { get; set; } = 21;
        public string RemotePath { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
