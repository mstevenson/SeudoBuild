namespace SeudoCI.Agent.API.Configuration;

/// <summary>
/// Configuration options for the SeudoCI Agent.
/// </summary>
public class AgentOptions
{
    public const string SectionName = "Agent";

    /// <summary>
    /// Default port for the HTTP server when not specified via command line.
    /// </summary>
    public int DefaultPort { get; set; } = 5511;

    /// <summary>
    /// Base directory where pipeline modules are located.
    /// </summary>
    public string ModulesBaseDirectory { get; set; } = "Modules";

    /// <summary>
    /// Timeout for HTTP requests.
    /// </summary>
    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
}