using System;
using System.IO;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using System.Diagnostics;

namespace SeudoBuild.Pipeline.Modules.GitSource
{
    public class GitSourceStep : ISourceStep<GitSourceConfig>
    {
        GitSourceConfig config;
        ITargetWorkspace workspace;
        ILogger logger;

        public string Type { get; } = "Git";

        public void Initialize(GitSourceConfig config, ITargetWorkspace workspace, ILogger logger)
        {
            this.config = config;
            this.workspace = workspace;
            this.logger = logger;

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

        public SourceStepResults ExecuteStep(ITargetWorkspace workspace)
        {
            logger.IndentLevel++;

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

            results.CommitIdentifier = CurrentCommitShortHash;
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
                return Repository.IsValid(workspace.SourceDirectory);
            }
        }

        public string CurrentCommit
        {
            get
            {
                string result = null;
                using (var repo = new Repository(workspace.SourceDirectory))
                {
                    result = repo.Head.Tip.Sha;
                }
                return result;
            }
        }

        string CurrentCommitShortHash
        {
            get
            {
                string commit = CurrentCommit;
                if (commit.Length == 0)
                {
                    return "";
                }
                return commit.Substring(0, 7);
            }
        }

        void StoreCredentials()
        {
            string credentialsPath = $"{workspace.SourceDirectory}/../git-credentials";

            var uri = new Uri(config.RepositoryURL);
            // Should use UriBuilder, but it doesn't include the password in the resulting uri string
            string urlWithCredentials = $"{uri.Scheme}://{config.Username}:{config.Password}@{uri.Host}{uri.AbsolutePath}";

            // FIXME abstract using IFileSystem

            File.WriteAllText(credentialsPath, urlWithCredentials);
            credentialsPath = Path.GetFullPath(credentialsPath);
            // FIXME escape spaces in path
            //repo.Config.Set("credential.helper", $"store --file={credentialsPath}", ConfigurationLevel.Local);
        }

        // Clone
        public void Download()
        {
            workspace.CleanSourceDirectory();

            if (config.UseLFS)
            {
                logger.Write($"Cloning LFS repository:  {config.RepositoryURL}");

                // FIXME extremely insecure to include password in the URL, but it's the only way I've
                // found to circumvent the manual password prompt when running git-lfs
                var uri = new Uri(config.RepositoryURL);
                string repoUrlWithPassword = $"{uri.Scheme}://{config.Username}:{config.Password}@{uri.Host}:{uri.Port}{uri.AbsolutePath}";

                ExecuteLFSCommand($"clone {repoUrlWithPassword} {workspace.SourceDirectory}");
            }
            else
            {
                logger.Write($"Cloning repository:  {config.RepositoryURL}");

                var cloneOptions = new CloneOptions
                {
                    CredentialsProvider = credentialsHandler,
                    BranchName = string.IsNullOrEmpty(config.RepositoryBranchName) ? "master" : config.RepositoryBranchName,
                    Checkout = true,
                    RecurseSubmodules = true
                };

                Repository.Clone(config.RepositoryURL, workspace.SourceDirectory, cloneOptions);
            }

            // TODO Handle sub-module credentials
        }

        // Pull
        public void Update()
        {
            logger.Write("Cleaning working copy");

            // Clean the repo
            using (var repo = new Repository(workspace.SourceDirectory))
            {
                // Skip the LFS smudge filter when resetting the repo.
                // LFS files will be integrated manually.
                repo.Config.Set("filter.lfs.smudge", "git-lfs smudge --skip %f", ConfigurationLevel.Local);
                repo.Config.Set("filter.lfs.required", false);

                repo.Reset(ResetMode.Hard);
                repo.RemoveUntrackedFiles();

                // Clone a new copy if necessary

                if (!IsWorkingCopyInitialized || repo.Network.Remotes[repo.Head.RemoteName].Url != config.RepositoryURL)
                {
                    logger.Write($"Repository URL has changed, cloning a new copy:  {config.RepositoryURL}");
                    Download();
                    return;
                }

                // Pull changes

                logger.Write($"Pulling changes from {repo.Head.TrackedBranch.FriendlyName}:  {config.RepositoryURL}");

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
                var mergeResult = Commands.Pull(repo, signature, pullOptions);

                if (config.UseLFS)
                {
                    logger.Write("Fetching LFS files");
                    ExecuteLFSCommand("fetch");
                    logger.Write("Checking out LFS files into working copy");
                    ExecuteLFSCommand("checkout");
                }

                if (mergeResult.Status == MergeStatus.UpToDate)
                {
                    logger.Write($"Repository is already up-to-date");
                }
                else
                {
                    logger.Write($"Merged commit {mergeResult.Commit.Sha}: {mergeResult.Commit.MessageShort}");
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
                WorkingDirectory = workspace.SourceDirectory,
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
