using System;
using System.IO;
using System.Collections.Generic;
using RunProcessAsTask;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using System.Diagnostics;

namespace UnityBuild.VCS.Git
{
    public class GitVCS : VersionControlSystem
    {
        Workspace workspace;
        GitVCSConfig config;

        UsernamePasswordCredentials credentials;
        FilterRegistration lfsFilter;
        Signature signature;
        CredentialsHandler credentialsHandler;

        public GitVCS(Workspace workspace, GitVCSConfig config)
        {
            this.workspace = workspace;
            this.config = config;

            credentials = new UsernamePasswordCredentials
            {
                Username = config.Username,
                Password = config.Password
            };
            credentialsHandler = (url, usernameFromUrl, types) => new UsernamePasswordCredentials { Username = config.Username, Password = config.Password };
            signature = new Signature(new Identity("UnityBuildServer", "info@basenjigames.com"), DateTimeOffset.UtcNow);

            // Set up LFS filter
            var filter = new LFSFilter("lfs", workspace.WorkingDirectory, new List<FilterAttributeEntry> { new FilterAttributeEntry("lfs") });
            lfsFilter = GlobalSettings.RegisterFilter(filter);
        }

        public override string TypeName
        {
            get
            {
                return "git";
            }
        }

        public override bool IsWorkingCopyInitialized
        {
            get
            {
                return Repository.IsValid(workspace.WorkingDirectory);
            }
        }

        void StoreCredentials()
        {
            string credentialsPath = $"{workspace.WorkingDirectory}/../git-credentials";

            var uri = new Uri(config.RepositoryURL);
            // Should use UriBuilder, but it doesn't include the password in the resulting uri string
            string urlWithCredentials = $"{uri.Scheme}://{config.Username}:{config.Password}@{uri.Host}{uri.AbsolutePath}";

            File.WriteAllText(credentialsPath, urlWithCredentials);
            credentialsPath = Path.GetFullPath(credentialsPath);
            // FIXME escape spaces in path
            //repo.Config.Set("credential.helper", $"store --file={credentialsPath}", ConfigurationLevel.Local);
        }

        // Clone
        public override void Download()
        {
            if (config.UseLFS)
            {
                BuildConsole.WriteLine($"Cloning LFS repository:  {config.RepositoryURL}");

                Console.ForegroundColor = ConsoleColor.Red;

                workspace.CleanWorkingDirectory();
                string repoUrl = GetUrlWithPassword(config.RepositoryURL);

                var startInfo = new ProcessStartInfo
                {
                    FileName = "git-lfs",
                    Arguments = $"clone {repoUrl} {workspace.WorkingDirectory}",
                    WorkingDirectory = workspace.WorkingDirectory,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                var process = new Process { StartInfo = startInfo };
                process.Start();
                process.WaitForExit();

                Console.ResetColor();
            }
            else
            {
                BuildConsole.WriteLine($"Cloning repository:  {config.RepositoryURL}");

                var cloneOptions = new CloneOptions
                {
                    CredentialsProvider = credentialsHandler,
                    BranchName = config.RepositoryBranchName,
                    Checkout = true,
                    RecurseSubmodules = true
                };

                Repository.Clone(config.RepositoryURL, workspace.WorkingDirectory, cloneOptions);
            }

            // TODO Handle sub-module credentials
        }

        string GetUrlWithPassword(string url)
        {
            // FIXME insecure to include password in the URL, but it's the only way I've
            // found to circumvent the manual password prompt when running git-lfs
            var uri = new Uri(config.RepositoryURL);
            string urlWithCredentials = $"{uri.Scheme}://{config.Username}:{config.Password}@{uri.Host}:{uri.Port}{uri.AbsolutePath}";
            return urlWithCredentials;
        }


        // Pull
        public override void Update()
        {
            BuildConsole.WriteLine("Cleaning working copy");

            // Clean the repo
            using (var repo = new Repository(workspace.WorkingDirectory))
            {
                repo.Reset(ResetMode.Hard);
                repo.RemoveUntrackedFiles();

                // Pull changes
            
                string remoteName = "origin";

                BuildConsole.WriteLine($"Fetching from {remoteName}:  ({config.RepositoryURL})");
                var fetchOptions = new FetchOptions
                {
                    CredentialsProvider = credentialsHandler
                };
                repo.Fetch(remoteName, fetchOptions);

                BuildConsole.WriteLine($"Merging changes into {config.RepositoryBranchName}");
                var mergeOptions = new MergeOptions
                {
                    FastForwardStrategy = FastForwardStrategy.FastForwardOnly,
                    FileConflictStrategy = CheckoutFileConflictStrategy.Theirs,
                    MergeFileFavor = MergeFileFavor.Theirs,
                    FailOnConflict = false
                };
                repo.Merge(config.RepositoryBranchName, signature, mergeOptions);
            }
        }
    }
}
