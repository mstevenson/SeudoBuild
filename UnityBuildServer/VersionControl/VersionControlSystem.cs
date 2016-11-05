namespace UnityBuild.VCS
{
    // FIXME this should be an interface and not an abstract class, but the linker
    // strips out all inheriting classes for some unknown reason

    public abstract class VersionControlSystem
    {
        public abstract string TypeName { get; }
        public abstract bool IsWorkingCopyInitialized { get; }
        public abstract void Download();
        public abstract void Update();
    }
}
