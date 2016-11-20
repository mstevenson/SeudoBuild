using System.Collections.Generic;

namespace SeudoBuild
{
    public interface IDistributeStep : IPipelineStep<ArchiveSequenceResults, ArchiveStepResults, DistributeSequenceResults, DistributeStepResults>
    {
    }

    public interface IDistributeStep<T> : IDistributeStep, IPipelineStepWithConfig<ArchiveSequenceResults, ArchiveStepResults, DistributeSequenceResults, DistributeStepResults, T>
        where T : DistributeStepConfig
    {
    }
}
