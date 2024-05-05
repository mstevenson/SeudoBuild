namespace SeudoCI.Agent;

using Pipeline;
using Core;

/// <inheritdoc />
/// <summary>
/// Executes a build pipeline for a given project and target.
/// </summary>
public class Builder(IModuleLoader moduleLoader, ILogger logger) : IBuilder
{
    /// <summary>
    /// Indicates whether a build is current in-progress.
    /// </summary>
    public bool IsRunning { get; private set; }

    private readonly ILogger _logger = logger;

    /// <summary>
    /// Execute a build for the given project and target.
    /// </summary>
    public bool Build(IPipelineRunner pipeline, ProjectConfig projectConfig, string target)
    {
        if (projectConfig == null)
        {
            throw new System.ArgumentException("Could not execute build, projectConfig is null");
        }

        // Find a valid target
        if (string.IsNullOrEmpty(target))
        {
            try
            {
                target = projectConfig.BuildTargets[0].TargetName;
            }
            catch (System.IndexOutOfRangeException)
            {
                throw new InvalidProjectConfigException("ProjectConfig does not contain a build target.");
            }
        }

        // Execute build
        IsRunning = true;
        pipeline.ExecutePipeline(projectConfig, target, moduleLoader);
        IsRunning = false;
            
        return true;
    }
}