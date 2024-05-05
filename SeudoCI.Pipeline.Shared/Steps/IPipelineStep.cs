using SeudoCI.Core;

namespace SeudoCI.Pipeline
{
    public interface IInitializable<in T>
        where T : StepConfig
    {
        void Initialize(T config, ITargetWorkspace workspace, ILogger logger);
    }

    public interface IPipelineStep
    {
        string Type { get; }
    }

    public interface IPipelineStepWithConfig<in T> : IPipelineStep
        where T : StepConfig
    {
        void Initialize(T config, ITargetWorkspace workspace, ILogger logger);
    }

    public interface IPipelineStep<TOutSeq, out TOutStep> : IPipelineStep
        where TOutSeq : PipelineSequenceResults<TOutStep>, new() // current sequence results
        where TOutStep : PipelineStepResults, new() // current step results
    {
        TOutStep ExecuteStep(ITargetWorkspace workspace);
    }

    public interface IPipelineStepWithConfig<TOutSeq, out TOutStep, TConfig> : IInitializable<TConfig>, IPipelineStep<TOutSeq, TOutStep>
        where TOutSeq : PipelineSequenceResults<TOutStep>, new() // current sequence results
        where TOutStep : PipelineStepResults, new() // current step results
        where TConfig : StepConfig
    {
    }

    public interface IPipelineStep<in TInSeq, TOutSeq, out TOutStep> : IPipelineStep
        where TInSeq : PipelineSequenceResults // previous sequence results
        where TOutSeq : PipelineSequenceResults<TOutStep>, new() // current sequence results
        where TOutStep : PipelineStepResults, new() // current step results
    {
        TOutStep ExecuteStep(TInSeq previousSequence, ITargetWorkspace workspace);
    }

    public interface IPipelineStepWithConfig<in TInSeq, TOutSeq, out TOutStep, TConfig> : IInitializable<TConfig>, IPipelineStep<TInSeq, TOutSeq, TOutStep>
        where TInSeq : PipelineSequenceResults // previous sequence results
        where TOutSeq : PipelineSequenceResults<TOutStep>, new() // current sequence results
        where TOutStep : PipelineStepResults, new() // current step results
        where TConfig : StepConfig
    {
    }

    // The extra generic type parameter allows for type inference
    public interface IPipelineStep<in TInSeq, TInStep, TOutSeq, out TOutStep> : IPipelineStep<TInSeq, TOutSeq, TOutStep>
        where TInSeq : PipelineSequenceResults<TInStep> // previous sequence results
        where TInStep : PipelineStepResults, new() // previous step results
        where TOutSeq : PipelineSequenceResults<TOutStep>, new() // current sequence results
        where TOutStep : PipelineStepResults, new() // current step results
    {
    }

    // The extra generic type parameter allows for type inference
    public interface IPipelineStepWithConfig<in TInSeq, TInStep, TOutSeq, out TOutStep, TConfig> : IPipelineStepWithConfig<TInSeq, TOutSeq, TOutStep, TConfig>
        where TInSeq : PipelineSequenceResults<TInStep> // previous sequence results
        where TInStep : PipelineStepResults, new() // previous step results
        where TOutSeq : PipelineSequenceResults<TOutStep>, new() // current sequence results
        where TOutStep : PipelineStepResults, new() // current step results
        where TConfig : StepConfig
    {
    }
}
