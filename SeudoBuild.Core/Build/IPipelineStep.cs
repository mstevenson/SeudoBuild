using System;
namespace SeudoBuild
{
    public interface IPipelineStep<TInSeq, TOutSeq, TOutStep>// : IPipelineStep<TInSeq, TOutStep>
        where TInSeq : PipelineSequenceResults // previous sequence results
        where TOutSeq : PipelineSequenceResults<TOutStep> // current sequence results
        where TOutStep : PipelineStepResults, new() // current step results
    {
        string Type { get; }
        TOutStep ExecuteStep(TInSeq previousSequence, Workspace workspace);
    }

    // The extra generic type parameter allows for type inference
    public interface IPipelineStep<TInSeq, TInStep, TOutSeq, TOutStep> : IPipelineStep<TInSeq, TOutSeq, TOutStep>
        where TInSeq : PipelineSequenceResults<TInStep> // previous sequence results
        where TInStep : PipelineStepResults, new() // previous step results
        where TOutSeq : PipelineSequenceResults<TOutStep>, new() // current sequence results
        where TOutStep : PipelineStepResults, new() // current step results
    {
    }
}
