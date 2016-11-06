﻿using System;
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
            // DISABLED - we'll run LFS commands manually
            //var filter = new LFSFilter("lfs", workspace.WorkingDirectory, new List<FilterAttributeEntry> { new FilterAttributeEntry("lfs") });
            //lfsFilter = GlobalSettings.RegisterFilter(filter);
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
                workspace.CleanWorkingDirectory();
                string repoUrl = GetUrlWithPassword(config.RepositoryURL);
                ExecuteLFSCommand($"clone {repoUrl} {workspace.WorkingDirectory}");
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
                // Skip the LFS smudge filter when resetting the repo.
                // LFS files will be integrated manually.
                repo.Config.Set("filter.lfs.smudge", "git-lfs smudge --skip %f", ConfigurationLevel.Local);
                repo.Config.Set("filter.lfs.required", false);

                repo.Reset(ResetMode.Hard);
                repo.RemoveUntrackedFiles();

                // Pull changes

                string remoteName = "origin";
                BuildConsole.WriteLine($"Pulling changes from {remoteName}:  {config.RepositoryURL}");

                var pullOptions = new PullOptions
                {
                    FetchOptions = new FetchOptions
                    {
                        CredentialsProvider = credentialsHandler
                    },
                    MergeOptions = new MergeOptions
                    {
                        FastForwardStrategy = FastForwardStrategy.FastForwardOnly,
                        FileConflictStrategy = CheckoutFileConflictStrategy.Theirs,
                        MergeFileFavor = MergeFileFavor.Theirs,
                        FailOnConflict = false
                    }
                };
                var mergeResult = repo.Network.Pull(signature, pullOptions);

                if (config.UseLFS)
                {
                    BuildConsole.WriteLine("Fetching LFS files");
                    ExecuteLFSCommand("fetch");
                    BuildConsole.WriteLine("Checking out LFS files into working copy");
                    ExecuteLFSCommand("checkout");
                }

                if (mergeResult.Status == MergeStatus.UpToDate)
                {
                    BuildConsole.WriteLine($"Repository is already up-to-date");
                }
                else
                {
                    BuildConsole.WriteLine($"Merged commit {mergeResult.Commit.Sha}: {mergeResult.Commit.MessageShort}");
                }
            }
        }

        void ExecuteLFSCommand(string arguments)
        {
            Console.ForegroundColor = ConsoleColor.Red;

            var startInfo = new ProcessStartInfo
            {
                FileName = "git-lfs",
                Arguments = arguments,
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
    }
}
