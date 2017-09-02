namespace SeudoBuild.Pipeline.Modules.SteamDistribute
{
    public class SteamDistributeStep : IDistributeStep<SteamDistributeConfig>
    {
        SteamDistributeConfig config;
        ILogger logger;

        public string Type { get; } = "Steam Upload";

        public void Initialize(SteamDistributeConfig config, ITargetWorkspace workspace, ILogger logger)
        {
            this.config = config;
            this.logger = logger;
        }

        public DistributeStepResults ExecuteStep(ArchiveSequenceResults archiveResults, ITargetWorkspace workspace)
        {
            // TODO

            return new DistributeStepResults();
        }
    }
}
