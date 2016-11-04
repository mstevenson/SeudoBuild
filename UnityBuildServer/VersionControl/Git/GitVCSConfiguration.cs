using System;
namespace UnityBuildServer
{
    public class GitVCSConfiguration : IVCSConfiguration
    {
        public string RepositoryURL { get; set; }
        public string RepositoryBranchName { get; set; }
        public bool IsLFS { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
    }
}
