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
    
    /// <summary>
    /// The authentication method to use when connecting to the repository.
    /// Defaults to UsernamePassword for backward compatibility.
    /// </summary>
    public GitAuthenticationType AuthenticationType { get; set; } = GitAuthenticationType.UsernamePassword;
    
    /// <summary>
    /// Username for authentication. Required for UsernamePassword and PersonalAccessToken types.
    /// For SSH authentication, this is typically "git" for GitHub/GitLab.
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// Password for UsernamePassword authentication. 
    /// DEPRECATED: Use PersonalAccessToken or SSH keys for better security.
    /// </summary>
    public string Password { get; set; } = string.Empty;
    
    /// <summary>
    /// Personal access token for token-based authentication.
    /// Used when AuthenticationType is PersonalAccessToken.
    /// </summary>
    public string PersonalAccessToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Path to the SSH private key file.
    /// Required when AuthenticationType is SSHKey.
    /// </summary>
    public string PrivateKeyPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Path to the SSH public key file.
    /// Required when AuthenticationType is SSHKey.
    /// </summary>
    public string PublicKeyPath { get; set; } = string.Empty;
    
    /// <summary>
    /// Passphrase for encrypted SSH private keys.
    /// Leave empty if the private key is not encrypted.
    /// </summary>
    public string Passphrase { get; set; } = string.Empty;
}