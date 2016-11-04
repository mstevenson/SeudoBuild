namespace UnityBuildServer
{
    // FIXME this should be an interface and not an abstract class, but the linker
    // strips out all inheriting classes for some unknown reason

    public abstract class VCS
    {
        public abstract bool IsWorkingCopyInitialized { get; }
        public abstract void Download(string url);
        public abstract void Update(string url);
        public abstract void ChangeBranch(string branchName);
    }
}
