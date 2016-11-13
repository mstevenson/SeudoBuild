using System.Collections.Generic;

namespace SeudoBuild.Modules.SteamDistribute
{
    public class SteamDistributeStep : IDistributeStep
    {
        SteamDistributeConfig config;

        public SteamDistributeStep(SteamDistributeConfig config)
        {
            this.config = config;
        }

        public string Type { get; } = "Steam Upload";

        public DistributeStepResults ExecuteStep(ArchiveSequenceResults archiveResults, Workspace workspace)
        {
            // TODO

            return new DistributeStepResults();
        }
    }
}
