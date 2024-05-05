namespace SeudoCI.Pipeline;

/// <summary>
/// Pipeline step that creates a build product from source files that were previously retrieved.
/// </summary>
public interface IBuildStep : IPipelineStep<SourceSequenceResults, BuildSequenceResults, BuildStepResults>
{
}

public interface IBuildStep<T> : IBuildStep, IPipelineStepWithConfig<SourceSequenceResults, BuildSequenceResults, BuildStepResults, T>
    where T : BuildStepConfig
{
}