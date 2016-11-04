namespace UnityBuildServer
{
    public abstract class ArchiveStep
    {
        public abstract void CreateArchive(BuildInfo buildInfo, Workspace workspace);
    }
}
