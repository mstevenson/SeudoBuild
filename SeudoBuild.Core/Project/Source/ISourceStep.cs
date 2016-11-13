namespace SeudoBuild
{
    public interface ISourceStep : IPipelineStep<SourceSequenceResults, SourceStepResults>
    {
        bool IsWorkingCopyInitialized { get; }
        string CurrentCommit { get; }
        void Download();
        void Update();
    }
}
