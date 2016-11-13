using System;
namespace SeudoBuild
{
    public interface IPipelineStep<TInSeq, TOutSeq, TOutStep>
        where TInSeq : PipelineSequenceResults // previous sequence results
        where TOutSeq : PipelineSequenceResults<TOutStep> // current sequence results
        where TOutStep : PipelineStepResults, new() // current step results
    {
        string Type { get; }
        TOutStep ExecuteStep(TInSeq previousSequence, Workspace workspace);
    }

    public interface IPipelineStep<TInSeq, TInStep, TOutSeq, TOutStep>
        where TInSeq : PipelineSequenceResults<TInStep> // previous sequence results
        where TInStep : PipelineStepResults, new() // previous step results
        where TOutSeq : PipelineSequenceResults<TOutStep>, new() // current sequence results
        where TOutStep : PipelineStepResults, new() // current step results
    {
        string Type { get; }
        TOutStep ExecuteStep(TInSeq previousSequence, Workspace workspace);
    }
}
