namespace SeudoCI.Pipeline;

/// <summary>
/// Pipeline step that takes a completed build product and archives it.
/// </summary>
public interface IArchiveStep : IPipelineStep<BuildSequenceResults, BuildStepResults, ArchiveSequenceResults, ArchiveStepResults>
{
}

public interface IArchiveStep<T> : IArchiveStep, IPipelineStepWithConfig<BuildSequenceResults, BuildStepResults, ArchiveSequenceResults, ArchiveStepResults, T>
    where T : ArchiveStepConfig
{
}