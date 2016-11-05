namespace UnityBuild
{
    public abstract class ArchiveStep
    {
        public abstract string TypeName { get; }
        public abstract ArchiveInfo CreateArchive(BuildInfo buildInfo, Workspace workspace);
    }
}
