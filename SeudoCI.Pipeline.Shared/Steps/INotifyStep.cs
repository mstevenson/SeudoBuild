namespace SeudoCI.Pipeline;

using SeudoCI.Pipeline.Shared;

/// <summary>
/// Pipeline step that publishes a notification after the pipeline has completed all of its other steps.
/// </summary>
[StepConfigMapping("NotifySteps")]
public interface INotifyStep : IPipelineStep<DistributeSequenceResults, DistributeStepResults, NotifySequenceResults, NotifyStepResults>
{
}

public interface INotifyStep<T> : INotifyStep, IPipelineStepWithConfig<DistributeSequenceResults, DistributeStepResults, NotifySequenceResults, NotifyStepResults, T>
    where T : NotifyStepConfig
{
}