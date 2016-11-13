using System;
namespace SeudoBuild
{
    public interface IBuildStep : IPipelineStep<VCSResults, BuildSequenceResults, BuildStepResults>
    {
    }
}
