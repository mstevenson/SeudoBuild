using System.Collections.Generic;

namespace SeudoBuild
{
    public interface IDistributeStep : IPipelineStep<ArchiveSequenceResults, ArchiveStepResults, DistributeSequenceResults, DistributeStepResults>
    {
    }
}
