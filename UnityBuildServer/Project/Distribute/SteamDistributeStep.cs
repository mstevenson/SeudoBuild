using System;
namespace UnityBuildServer
{
    public class SteamDistributeStep : DistributeStep
    {
        SteamDistributeConfig config;

        public SteamDistributeStep(SteamDistributeConfig config)
        {
            this.config = config;
        }

        public override string TypeName
        {
            get
            {
                return "Steam Upload";
            }
        }

        public override void Distribute(string archivePath, Workspace workspace)
        {
            // TODO
        }
    }
}
