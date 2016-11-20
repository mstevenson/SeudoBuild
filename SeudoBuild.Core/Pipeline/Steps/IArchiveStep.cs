namespace SeudoBuild
{
    public interface IArchiveStep : IPipelineStep<BuildSequenceResults, BuildStepResults, ArchiveSequenceResults, ArchiveStepResults>
    {
    }

    public interface IArchiveStep<T> : IArchiveStep, IPipelineStepWithConfig<BuildSequenceResults, BuildStepResults, ArchiveSequenceResults, ArchiveStepResults, T>
        where T : ArchiveStepConfig
    {
    }
}
