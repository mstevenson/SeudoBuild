namespace SeudoBuild
{
    public abstract class ArchiveStep
    {
        public abstract string Type { get; }
        public abstract ArchiveInfo CreateArchive(BuildStepResults buildInfo, Workspace workspace);
    }
}
