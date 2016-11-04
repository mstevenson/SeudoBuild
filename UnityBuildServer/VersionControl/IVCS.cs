namespace UnityBuildServer
{
    public interface IVCS
    {
        bool IsWorkingCopyInitialized { get; }
        void Download(string url);
        void Update(string url);
        void ChangeBranch(string branchName);
    }
}
