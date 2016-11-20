namespace SeudoBuild
{
    public interface ISourceStep : IPipelineStep<SourceSequenceResults, SourceStepResults>
    {
        bool IsWorkingCopyInitialized { get; }
        string CurrentCommit { get; }
        void Download();
        void Update();
    }

    public interface ISourceStep<T> : ISourceStep, IPipelineStepWithConfig<SourceSequenceResults, SourceStepResults, T>
        where T : SourceStepConfig
    {
    }
}
