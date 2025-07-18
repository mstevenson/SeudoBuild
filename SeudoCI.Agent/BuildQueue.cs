using System.Collections.Concurrent;
using SeudoCI.Core;
using SeudoCI.Pipeline;

namespace SeudoCI.Agent;

/// <summary>
/// Queues projects and feeds them sequentially to a builder.
/// </summary>
public class BuildQueue(Builder builder, IModuleLoader moduleLoader, ILogger logger)
    : IBuildQueue
{
    private const string OutputFolderName = "SeudoCI";

    private CancellationTokenSource? _tokenSource;
    private int _buildIndex;
    private bool _isQueueRunning;

    public BuildResult? ActiveBuild { get; private set; }
    private ConcurrentQueue<BuildResult> QueuedBuilds { get; } = new();
    private ConcurrentDictionary<int, BuildResult> Builds { get; } = new();

    /// <summary>
    /// Begin executing builds in the queue. Builds will continue until the queue has been exhausted.
    /// </summary>
    public void StartQueue(IFileSystem fileSystem)
    {
        if (_isQueueRunning)
        {
            return;
        }
        _isQueueRunning = true;

        _tokenSource = new CancellationTokenSource();

        // Create output folder in user's documents folder
        var outputPath = fileSystem.DocumentsPath;
        if (!fileSystem.DirectoryExists(outputPath))
        {
            throw new DirectoryNotFoundException("User documents folder not found");
        }
        outputPath = Path.Combine(outputPath, OutputFolderName);
        if (!fileSystem.DirectoryExists(outputPath))
        {
            fileSystem.CreateDirectory(outputPath);
        }

        Task.Factory.StartNew(() => TaskQueuePump(outputPath, moduleLoader), _tokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    private void TaskQueuePump(string outputPath, IModuleLoader moduleLoader)
    {
        while (true)
        {
            // Clean up and bail out
            if (_tokenSource?.IsCancellationRequested == true)
            {
                ActiveBuild = null;
                return;
            }

            if (!builder.IsRunning && QueuedBuilds.Count > 0)
            {
                if (QueuedBuilds.TryDequeue(out var build))
                {
                    // Ignore builds that have been cancelled
                    if (build.BuildStatus != BuildResult.Status.Queued)
                    {
                        continue;
                    }

                    string printableTarget = string.IsNullOrEmpty(build.TargetName) ? "default target" : $"target '{build.TargetName}'";
                    logger.QueueNotification($"Building project '{build.ProjectConfiguration.ProjectName}', {printableTarget}");

                    ActiveBuild = build;
                    var pipeline = new PipelineRunner(new PipelineConfig { BaseDirectory = outputPath }, logger);
                    builder.Build(pipeline, ActiveBuild.ProjectConfiguration, ActiveBuild.TargetName ?? string.Empty);
                }
            }
            Thread.Sleep(200);
        }
    }

    /// <summary>
    /// Queues a project for building.
    /// If target is null, the default target in the given ProjectConfig will be used.
    /// </summary>
    public BuildResult EnqueueBuild(ProjectConfig config, string? target = null)
    {
        _buildIndex++;
        var result = new BuildResult
        {
            Id = _buildIndex,
            BuildStatus = BuildResult.Status.Queued,
            ProjectConfiguration = config,
            TargetName = target
        };

        QueuedBuilds.Enqueue(result);
        Builds.TryAdd(result.Id, result);
        return result;
    }

    /// <summary>
    /// Returns results for all known builds, including successful, failed, in-progress, and queued builds.
    /// </summary>
    public IEnumerable<BuildResult> GetAllBuildResults()
    {
        return Builds.Values;
    }

    /// <summary>
    /// Return the result for a specific build.
    /// </summary>
    public BuildResult? GetBuildResult(int buildId)
    {
        Builds.TryGetValue(buildId, out var result);
        return result;
    }

    /// <summary>
    /// Stop a given build. If the build is in progress it will be halted, otherwise it will be removed from the queue.
    /// </summary>
    public BuildResult? CancelBuild(int buildId)
    {
        if (ActiveBuild != null && ActiveBuild.Id == buildId)
        {
            ActiveBuild = null;
            // TODO signal build process to stop
        }

        if (Builds.TryGetValue(buildId, out var result))
        {
            result.BuildStatus = BuildResult.Status.Cancelled;
        }
        return result;
    }
}