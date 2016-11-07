using System;
namespace UnityBuild
{
    public class SteamDistributeConfig : DistributeConfig
    {
        public override string Type { get; } = "Steam Upload";
        public string PublishToBranch { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
