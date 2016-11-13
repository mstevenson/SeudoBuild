using System;
namespace SeudoBuild.Modules.SteamDistribute
{
    public class SteamDistributeConfig : DistributeStepConfig
    {
        public override string Type { get; } = "Steam Upload";
        public string PublishToBranch { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
