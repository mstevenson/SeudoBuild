namespace SeudoCI.Agent;

using Pipeline;

/// <summary>
/// Describes a queued build.
/// </summary>
public class BuildResult
{
    public enum Status
    {
        Queued,
        Complete,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Unique build process identifier.
    /// </summary>
    public int Id { get; init; }

    /// <summary>
    /// Configuration settings for the build process.
    /// </summary>
    public ProjectConfig ProjectConfiguration { get; init; }

    /// <summary>
    /// The active target for the build process.
    /// </summary>
    public string TargetName { get; init; }

    /// <summary>
    /// The status of the build process for the current project and target.
    /// </summary>
    public Status BuildStatus { get; set; }
}