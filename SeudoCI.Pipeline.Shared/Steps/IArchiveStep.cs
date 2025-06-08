namespace SeudoCI.Pipeline;

using SeudoCI.Pipeline.Shared;

/// <summary>
/// Pipeline step that takes a completed build product and archives it.
/// </summary>
[StepConfigMapping("ArchiveSteps")]
public interface IArchiveStep : IPipelineStep<BuildSequenceResults, BuildStepResults, ArchiveSequenceResults, ArchiveStepResults>
{
}

public interface IArchiveStep<T> : IArchiveStep, IPipelineStepWithConfig<BuildSequenceResults, BuildStepResults, ArchiveSequenceResults, ArchiveStepResults, T>
    where T : ArchiveStepConfig
{
}