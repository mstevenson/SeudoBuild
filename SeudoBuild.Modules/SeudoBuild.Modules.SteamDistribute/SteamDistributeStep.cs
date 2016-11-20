using System.Collections.Generic;

namespace SeudoBuild.Modules.SteamDistribute
{
    public class SteamDistributeStep : IDistributeStep<SteamDistributeConfig>
    {
        SteamDistributeConfig config;

        public string Type { get; } = "Steam Upload";

        public void Initialize(SteamDistributeConfig config, Workspace workspace)
        {
            this.config = config;
        }

        public DistributeStepResults ExecuteStep(ArchiveSequenceResults archiveResults, Workspace workspace)
        {
            // TODO

            return new DistributeStepResults();
        }
    }
}
