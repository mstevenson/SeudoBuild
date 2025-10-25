using System;
using System.Collections.Generic;

namespace SeudoCI.Pipeline;

/// <summary>
/// Aggregated results for an entire pipeline run. The object derives from
/// <see cref="PipelineSequenceResults{T}"/> so it can be provided to notify
/// steps while still exposing the individual sequence results that preceded the
/// notification phase.
/// </summary>
public class PipelineRunResults : PipelineSequenceResults<DistributeStepResults>
{
    public PipelineRunResults(
        SourceSequenceResults sourceResults,
        BuildSequenceResults buildResults,
        ArchiveSequenceResults archiveResults,
        DistributeSequenceResults distributeResults)
    {
        SourceResults = sourceResults ?? throw new ArgumentNullException(nameof(sourceResults));
        BuildResults = buildResults ?? throw new ArgumentNullException(nameof(buildResults));
        ArchiveResults = archiveResults ?? throw new ArgumentNullException(nameof(archiveResults));
        DistributeResults = distributeResults ?? throw new ArgumentNullException(nameof(distributeResults));

        // Mirror the distribute results so existing logic that inspects the
        // "previous" sequence continues to work transparently.
        IsSuccess = distributeResults.IsSuccess;
        IsSkipped = distributeResults.IsSkipped;
        IsMandatory = distributeResults.IsMandatory;
        Exception = distributeResults.Exception;
        Duration = distributeResults.Duration;

        StepResults.AddRange(distributeResults.StepResults);
    }

    public SourceSequenceResults SourceResults { get; }

    public BuildSequenceResults BuildResults { get; }

    public ArchiveSequenceResults ArchiveResults { get; }

    public DistributeSequenceResults DistributeResults { get; }

    /// <summary>
    /// Calculates whether every pipeline stage prior to notification succeeded.
    /// </summary>
    public bool IsPipelineSuccessful => SourceResults.IsSuccess && BuildResults.IsSuccess &&
                                        ArchiveResults.IsSuccess && DistributeResults.IsSuccess;

    /// <summary>
    /// Returns an ordered enumeration of all pipeline stages leading up to the
    /// notification stage.
    /// </summary>
    public IReadOnlyList<(string Name, PipelineSequenceResults Results)> EnumerateStages() =>
    [
        ("Source", SourceResults),
        ("Build", BuildResults),
        ("Archive", ArchiveResults),
        ("Distribute", DistributeResults)
    ];
}
