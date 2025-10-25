namespace SeudoCI.Agent;

using System.Threading;
using Pipeline;
using Core;

/// <summary>
/// Executes a build pipeline for a given project and target.
/// </summary>
public class Builder(IModuleLoader moduleLoader, ILogger logger)
{
    /// <summary>
    /// Indicates whether a build is current in-progress.
    /// </summary>
    public bool IsRunning { get; private set; }

    private readonly ILogger _logger = logger;

    /// <summary>
    /// Execute a build for the given project and target.
    /// </summary>
    public bool Build(IPipelineRunner pipeline, ProjectConfig projectConfig, string target,
        CancellationToken cancellationToken = default)
    {
        if (projectConfig == null)
        {
            throw new ArgumentException("Could not execute build, projectConfig is null");
        }

        // Find a valid target
        if (string.IsNullOrEmpty(target))
        {
            try
            {
                target = projectConfig.BuildTargets[0].TargetName;
            }
            catch (IndexOutOfRangeException)
            {
                throw new InvalidProjectConfigException("ProjectConfig does not contain a build target.");
            }
        }

        // Execute build
        IsRunning = true;
        try
        {
            pipeline.ExecutePipeline(projectConfig, target, moduleLoader, cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        finally
        {
            IsRunning = false;
        }
    }
}