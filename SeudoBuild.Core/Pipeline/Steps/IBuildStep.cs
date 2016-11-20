using System;
namespace SeudoBuild
{
    public interface IBuildStep : IPipelineStep<SourceSequenceResults, BuildSequenceResults, BuildStepResults>
    {
    }

    public interface IBuildStep<T> : IBuildStep, IPipelineStepWithConfig<SourceSequenceResults, BuildSequenceResults, BuildStepResults, T>
        where T : BuildStepConfig
    {
    }
}
