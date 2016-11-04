using System;
namespace UnityBuildServer.VersionControl
{
    public class GitConfiguration : IVCSConfiguration
    {
        public string RepositoryURL { get; set; }
        public string RepositoryBranchName { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
    }
}
