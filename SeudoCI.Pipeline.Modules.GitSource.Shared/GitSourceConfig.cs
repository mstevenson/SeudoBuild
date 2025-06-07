namespace SeudoCI.Pipeline.Modules.GitSource;

/// <summary>
/// Configuration values for a source pipeline step that clones a git repository.
/// </summary>
public class GitSourceConfig : SourceStepConfig
{
    public override string Name => "Git";
    public string RepositoryURL { get; set; } = string.Empty;
    public string RepositoryBranchName { get; set; } = string.Empty;
    public bool UseLFS { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}