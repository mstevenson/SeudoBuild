namespace SeudoBuild.Pipeline.Modules.SteamDistribute
{
    public class SteamDistributeStep : IDistributeStep<SteamDistributeConfig>
    {
        SteamDistributeConfig config;

        public string Type { get; } = "Steam Upload";

        public void Initialize(SteamDistributeConfig config, IWorkspace workspace)
        {
            this.config = config;
        }

        public DistributeStepResults ExecuteStep(ArchiveSequenceResults archiveResults, IWorkspace workspace)
        {
            // TODO

            return new DistributeStepResults();
        }
    }
}
