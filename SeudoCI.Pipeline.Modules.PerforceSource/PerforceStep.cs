namespace SeudoCI.Pipeline.Modules.PerforceSource;

using Core;
using Shared;
using Perforce.P4;

public class PerforceStep : ISourceStep<PerforceConfig>
{
    private PerforceConfig _config = null!;
    private ITargetWorkspace _workspace = null!;
    private ILogger _logger = null!;
    private P4Server _server = null!;
    
    public SourceStepResults ExecuteStep(ITargetWorkspace workspace)
    {
        throw new NotImplementedException();
    }

    public string Type => "Perforce";
    
    public bool IsWorkingCopyInitialized { get; }
    
    public string CurrentCommit { get; } = string.Empty;
    
    public void Initialize(PerforceConfig config, ITargetWorkspace workspace, ILogger logger)
    {
        _config = config;
        _workspace = workspace;
        _logger = logger;
        _server = new P4Server(_config.Server, _config.User, _config.Pass, _config.Client);
    }
    
    public void Download()
    {
        // var cmd = new P4Command(_server,
        throw new NotImplementedException();
    }

    public void Update()
    {
        throw new NotImplementedException();
    }
}