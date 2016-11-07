namespace UnityBuild
{
    public abstract class ArchiveStep
    {
        public abstract string Type { get; }
        public abstract ArchiveInfo CreateArchive(BuildInfo buildInfo, Workspace workspace);
    }
}
