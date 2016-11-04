using System;
namespace UnityBuildServer
{
    public class GitVCSConfig : VCSConfig
    {
        public string RepositoryURL { get; set; }
        public string RepositoryBranchName { get; set; }
        public bool IsLFS { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
