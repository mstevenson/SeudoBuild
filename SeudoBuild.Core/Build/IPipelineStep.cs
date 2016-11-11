using System;
namespace SeudoBuild
{
    public interface IPipelineStep<InSeq, InStep, OutStep>
        where InSeq : PipelineSequenceResults<InStep> // previous sequence's sequence result type
        where InStep : PipelineStepResults, new() // previous sequence's step result type
        where OutStep : PipelineStepResults // this sequence's step result type
    {
        string Type { get; }
        OutStep ExecuteStep(InSeq previousSequence, Workspace workspace);
    }
}
