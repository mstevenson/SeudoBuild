namespace SeudoCI.Pipeline;

using SeudoCI.Pipeline.Shared;

/// <summary>
/// Pipeline step that creates a build product from source files that were previously retrieved.
/// </summary>
[StepConfigMapping("BuildSteps")]
public interface IBuildStep : IPipelineStep<SourceSequenceResults, BuildSequenceResults, BuildStepResults>
{
}

public interface IBuildStep<T> : IBuildStep, IPipelineStepWithConfig<SourceSequenceResults, BuildSequenceResults, BuildStepResults, T>
    where T : BuildStepConfig
{
}