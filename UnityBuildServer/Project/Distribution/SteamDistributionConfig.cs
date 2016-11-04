using System;
namespace UnityBuildServer
{
    public class SteamDistributionConfig : DistributionConfig
    {
        public string PublishToBranch { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
