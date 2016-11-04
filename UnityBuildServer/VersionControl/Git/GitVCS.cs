using System;
using System.IO;
using System.Collections.Generic;
using RunProcessAsTask;
using LibGit2Sharp;

namespace UnityBuildServer
{
    public class GitVCS : VCS
    {
        string workingDirectory;
        bool isLFS;

        public static UsernamePasswordCredentials credentials;
        //public static Signature signature;
        FilterRegistration lfsFilter;

        public GitVCS(string workingDirectory, string username, string password, bool isLFS)
        {
            // Register git-lfs Smudge and Clean filters here
            // https://github.com/libgit2/libgit2sharp/issues/1236
            this.workingDirectory = workingDirectory;
            this.isLFS = isLFS;

            credentials = new UsernamePasswordCredentials
            {
                Username = username,
                Password = password
            };
            //Identity identity = new Identity("UnityBuildServer", "");
            //signature = new Signature(identity, DateTimeOffset.UtcNow);

            // Set up LFS filter
            var filter = new LFSFilter("lfs", workingDirectory, new List<FilterAttributeEntry> { new FilterAttributeEntry("lfs") });
            lfsFilter = GlobalSettings.RegisterFilter(filter);
        }

        public override bool IsWorkingCopyInitialized
        {
            get
            {
                return Directory.Exists($"{workingDirectory}/.git");
            }
        }

        // Clone
        public override void Download(string url)
        {
            // Clone repo

            // Pull LFS files

            // Handle sub-module credentials
        }

        // Pull
        public override void Update(string url)
        {
            // run 'git clean'

            // optionally 'git reset' to remove files created during build process?
        }

        // Checkout
        public override void ChangeBranch(string branchName)
        {
        }
    }
}
