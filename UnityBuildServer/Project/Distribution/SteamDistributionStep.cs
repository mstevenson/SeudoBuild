using System;
namespace UnityBuildServer
{
    public class SteamDistributionStep : DistributionStep
    {
        SteamDistributionConfig config;

        public SteamDistributionStep(SteamDistributionConfig config)
        {
            this.config = config;
        }

        public override void Distribute(string archivePath, Workspace workspace)
        {
            // TODO
        }
    }
}
