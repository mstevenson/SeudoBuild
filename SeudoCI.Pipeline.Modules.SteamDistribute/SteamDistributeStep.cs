namespace SeudoCI.Pipeline.Modules.SteamDistribute;

using Core;

public class SteamDistributeStep : IDistributeStep<SteamDistributeConfig>
{
    private SteamDistributeConfig _config = null!;
    private ILogger _logger = null!;

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