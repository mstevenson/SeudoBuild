namespace SeudoCI.Pipeline.Modules.UnityBuild;

/// <inheritdoc />
/// <summary>
/// Configuration values for a build pipeline step for a Unity project.
/// </summary>
public abstract class UnityBuildConfig : BuildStepConfig
{
    /// <summary>
    /// The installed Unity executable to build with.
    /// </summary>
    public VersionNumber UnityVersionNumber { get; set; } = new();

    /// <summary>
    /// A path relative to the working directory that contains a Unity project.
    /// </summary>
    public string SubDirectory { get; set; } = "";
}