namespace SeudoCI.Pipeline;

using SeudoCI.Pipeline.Shared;

/// <summary>
/// Pipeline step that retrieves project files from a repository so that they can be built.
/// </summary>
[StepConfigMapping("SourceSteps")]
public interface ISourceStep : IPipelineStep<SourceSequenceResults, SourceStepResults>
{
    bool IsWorkingCopyInitialized { get; }
    string CurrentCommit { get; }
    void Download();
    void Update();
}

public interface ISourceStep<T> : ISourceStep, IPipelineStepWithConfig<SourceSequenceResults, SourceStepResults, T>
    where T : SourceStepConfig
{
}