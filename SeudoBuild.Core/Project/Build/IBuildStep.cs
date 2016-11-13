using System;
namespace SeudoBuild
{
    public interface IBuildStep : IPipelineStep<SourceSequenceResults, BuildSequenceResults, BuildStepResults>
    {
    }
}
