using System;
namespace SeudoBuild
{
    public interface INotifyStep : IPipelineStep<DistributeSequenceResults, DistributeStepResults, NotifySequenceResults, NotifyStepResults>
    {
    }

    public interface INotifyStep<T> : INotifyStep, IPipelineStepWithConfig<DistributeSequenceResults, DistributeStepResults, NotifySequenceResults, NotifyStepResults, T>
        where T : NotifyStepConfig
    {
    }
}
