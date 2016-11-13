using System;
using System.IO;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using System.Diagnostics;


namespace SeudoBuild.Modules.GitSource
{
    public class GitSourceStep : ISourceStep
    {
        GitSourceConfig config;
        Workspace workspace;

        public string Type { get; } = "Git";

        public GitSourceStep(GitSourceConfig config, Workspace workspace)
        {
            this.config = config;
            this.workspace = workspace;

            credentials = new UsernamePasswordCredentials
            {
                Username = config.Username,
                Password = config.Password
            };
            credentialsHandler = (url, usernameFromUrl, types) => new UsernamePasswordCredentials { Username = config.Username, Password = config.Password };
            signature = new Signature(new Identity("SeudoBuild", "info@basenjigames.com"), DateTimeOffset.UtcNow);

            // Set up LFS filter
            // DISABLED - we'll run LFS commands manually
            //var filter = new LFSFilter("lfs", workspace.WorkingDirectory, new List<FilterAttributeEntry> { new FilterAttributeEntry("lfs") });
            //lfsFilter = GlobalSettings.RegisterFilter(filter);
        }

        public SourceStepResults ExecuteStep(Workspace workspace)
        {
            // FIXME
            //replacements["commit_identifier"] = vcsResults.CurrentCommitIdentifier;

            BuildConsole.IndentLevel++;

            var results = new SourceStepResults();

            try
            {
                if (IsWorkingCopyInitialized)
                {
                    Update();
                }
                else
                {
                    Download();
                }
            }
            catch (Exception e)
            {
                results.IsSuccess = false;
                results.Exception = e;
            }

            results.CommitIdentifier = CurrentCommit;
            results.IsSuccess = true;
            return results;
        }


        UsernamePasswordCredentials credentials;
        FilterRegistration lfsFilter;
        Signature signature;
        CredentialsHandler credentialsHandler;

        public bool IsWorkingCopyInitialized
        {
            get
            {
                return Repository.IsValid(workspace.WorkingDirectory);
            }
        }

        public string CurrentCommit
        {
            get
            {
                string result = null;
                using (var repo = new Repository(workspace.WorkingDirectory))
                {
                    result = repo.Head.Tip.Sha;
                }
                return result;
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
        public void Download()
        {
            workspace.CleanWorkingDirectory();

            if (config.UseLFS)
            {
                BuildConsole.WriteLine($"Cloning LFS repository:  {config.RepositoryURL}");

                // FIXME extremely insecure to include password in the URL, but it's the only way I've
                // found to circumvent the manual password prompt when running git-lfs
                var uri = new Uri(config.RepositoryURL);
                string repoUrlWithPassword = $"{uri.Scheme}://{config.Username}:{config.Password}@{uri.Host}:{uri.Port}{uri.AbsolutePath}";

                ExecuteLFSCommand($"clone {repoUrlWithPassword} {workspace.WorkingDirectory}");
            }
            else
            {
                BuildConsole.WriteLine($"Cloning repository:  {config.RepositoryURL}");

                var cloneOptions = new CloneOptions
                {
                    CredentialsProvider = credentialsHandler,
                    BranchName = string.IsNullOrEmpty(config.RepositoryBranchName) ? "master" : config.RepositoryBranchName,
                    Checkout = true,
                    RecurseSubmodules = true
                };

                Repository.Clone(config.RepositoryURL, workspace.WorkingDirectory, cloneOptions);
            }

            // TODO Handle sub-module credentials
        }

        // Pull
        public void Update()
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

                // Clone a new copy if necessary

                if (!IsWorkingCopyInitialized || repo.Head.Remote.Url != config.RepositoryURL)
                {
                    BuildConsole.WriteLine($"Repository URL has changed, cloning a new copy:  {config.RepositoryURL}");
                    Download();
                    return;
                }

                // Pull changes

                BuildConsole.WriteLine($"Pulling changes from {repo.Head.TrackedBranch.FriendlyName}:  {config.RepositoryURL}");

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
            Console.ForegroundColor = ConsoleColor.Cyan;

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
