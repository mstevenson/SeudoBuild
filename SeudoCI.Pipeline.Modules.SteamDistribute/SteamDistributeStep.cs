namespace SeudoCI.Pipeline.Modules.SteamDistribute;

using Core;

public class SteamDistributeStep : IDistributeStep<SteamDistributeConfig>
{
    private SteamDistributeConfig _config;
    private ILogger _logger;

    public string Type => "Steam Upload";

    public void Initialize(SteamDistributeConfig config, ITargetWorkspace workspace, ILogger logger)
    {
        _config = config;
        _logger = logger;
    }

    public DistributeStepResults ExecuteStep(ArchiveSequenceResults archiveResults, ITargetWorkspace workspace)
    {
        // TODO

        return new DistributeStepResults();
    }
}