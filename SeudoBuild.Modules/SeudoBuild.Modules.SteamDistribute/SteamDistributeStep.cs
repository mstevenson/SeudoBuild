namespace SeudoBuild.Pipeline.Modules.SteamDistribute
{
    public class SteamDistributeStep : IDistributeStep<SteamDistributeConfig>
    {
        SteamDistributeConfig config;
        ILogger logger;

        public string Type { get; } = "Steam Upload";

        public void Initialize(SteamDistributeConfig config, IWorkspace workspace, ILogger logger)
        {
            this.config = config;
            this.logger = logger;
        }

        public DistributeStepResults ExecuteStep(ArchiveSequenceResults archiveResults, IWorkspace workspace)
        {
            // TODO

            return new DistributeStepResults();
        }
    }
}
