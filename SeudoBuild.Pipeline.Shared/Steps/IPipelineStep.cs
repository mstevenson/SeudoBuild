namespace SeudoBuild.Pipeline
{
    public interface IInitializable<T>
        where T : StepConfig
    {
        void Initialize(T config, IWorkspace workspace, ILogger logger);
    }

    public interface IPipelineStep
    {
        string Type { get; }
    }

    public interface IPipelineStepWithConfig<T> : IPipelineStep
    where T : StepConfig
    {
        void Initialize(T config, IWorkspace workspace, ILogger logger);
    }

    public interface IPipelineStep<TOutSeq, TOutStep> : IPipelineStep
        where TOutSeq : PipelineSequenceResults<TOutStep>, new() // current sequence results
        where TOutStep : PipelineStepResults, new() // current step results
    {
        TOutStep ExecuteStep(IWorkspace workspace);
    }

    public interface IPipelineStepWithConfig<TOutSeq, TOutStep, TConfig> : IInitializable<TConfig>, IPipelineStep<TOutSeq, TOutStep>
        where TOutSeq : PipelineSequenceResults<TOutStep>, new() // current sequence results
        where TOutStep : PipelineStepResults, new() // current step results
        where TConfig : StepConfig
    {
    }

    public interface IPipelineStep<TInSeq, TOutSeq, TOutStep> : IPipelineStep
        where TInSeq : PipelineSequenceResults // previous sequence results
        where TOutSeq : PipelineSequenceResults<TOutStep>, new() // current sequence results
        where TOutStep : PipelineStepResults, new() // current step results
    {
        TOutStep ExecuteStep(TInSeq previousSequence, IWorkspace workspace);
    }

    public interface IPipelineStepWithConfig<TInSeq, TOutSeq, TOutStep, TConfig> : IInitializable<TConfig>, IPipelineStep<TInSeq, TOutSeq, TOutStep>
        where TInSeq : PipelineSequenceResults // previous sequence results
        where TOutSeq : PipelineSequenceResults<TOutStep>, new() // current sequence results
        where TOutStep : PipelineStepResults, new() // current step results
        where TConfig : StepConfig
    {
    }

    // The extra generic type parameter allows for type inference
    public interface IPipelineStep<TInSeq, TInStep, TOutSeq, TOutStep> : IPipelineStep<TInSeq, TOutSeq, TOutStep>
        where TInSeq : PipelineSequenceResults<TInStep> // previous sequence results
        where TInStep : PipelineStepResults, new() // previous step results
        where TOutSeq : PipelineSequenceResults<TOutStep>, new() // current sequence results
        where TOutStep : PipelineStepResults, new() // current step results
    {
    }

    // The extra generic type parameter allows for type inference
    public interface IPipelineStepWithConfig<TInSeq, TInStep, TOutSeq, TOutStep, TConfig> : IPipelineStepWithConfig<TInSeq, TOutSeq, TOutStep, TConfig>
        where TInSeq : PipelineSequenceResults<TInStep> // previous sequence results
        where TInStep : PipelineStepResults, new() // previous step results
        where TOutSeq : PipelineSequenceResults<TOutStep>, new() // current sequence results
        where TOutStep : PipelineStepResults, new() // current step results
        where TConfig : StepConfig
    {
    }
}
