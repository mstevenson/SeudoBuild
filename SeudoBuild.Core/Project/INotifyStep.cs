using System;
namespace SeudoBuild
{
    public interface INotifyStep : IPipelineStep<DistributeSequenceResults, DistributeStepResults, NotifySequenceResults, NotifyStepResults>
    {
    }
}
