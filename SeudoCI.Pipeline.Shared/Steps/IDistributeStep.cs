namespace SeudoCI.Pipeline;

/// <summary>
/// Pipeline step that distributes a completed build archive.
/// </summary>
public interface IDistributeStep : IPipelineStep<ArchiveSequenceResults, ArchiveStepResults, DistributeSequenceResults, DistributeStepResults>
{
}

public interface IDistributeStep<T> : IDistributeStep, IPipelineStepWithConfig<ArchiveSequenceResults, ArchiveStepResults, DistributeSequenceResults, DistributeStepResults, T>
    where T : DistributeStepConfig
{
}