namespace SeudoCI.Pipeline.Modules.GitSource;

/// <summary>
/// Specifies the authentication method to use when connecting to a Git repository.
/// </summary>
public enum GitAuthenticationType
{
    /// <summary>
    /// Traditional username and password authentication.
    /// </summary>
    UsernamePassword,
    
    /// <summary>
    /// SSH key-based authentication using public/private key pairs.
    /// </summary>
    SSHKey,
    
    /// <summary>
    /// SSH agent authentication (uses keys managed by ssh-agent).
    /// </summary>
    SSHAgent,
    
    /// <summary>
    /// Personal access token or OAuth token authentication.
    /// </summary>
    PersonalAccessToken
}