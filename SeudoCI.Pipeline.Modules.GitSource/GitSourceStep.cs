using System;
using System.Collections.Generic;
using System.IO;
using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using System.Diagnostics;
using SeudoCI.Core;

namespace SeudoCI.Pipeline.Modules.GitSource
{
    public class GitSourceStep : ISourceStep<GitSourceConfig>
    {
        private GitSourceConfig _config;
        private ITargetWorkspace _workspace;
        private ILogger _logger;

        public string Type { get; } = "Git";

        public void Initialize(GitSourceConfig config, ITargetWorkspace workspace, ILogger logger)
        {
            _config = config;
            _workspace = workspace;
            _logger = logger;

            _credentials = new UsernamePasswordCredentials
            {
                Username = config.Username,
                Password = config.Password
            };
            _credentialsHandler = (url, usernameFromUrl, types) => new UsernamePasswordCredentials { Username = config.Username, Password = config.Password };
            _signature = new Signature(new Identity("SeudoCI", "info@basenjigames.com"), DateTimeOffset.UtcNow);

            // Set up LFS filter
            if (config.UseLFS)
            {
                //if (IsLFSAvailable())
                //{
                    var filter = new LFSFilter("lfs", workspace.GetDirectory(TargetDirectory.Source), new List<FilterAttributeEntry> { new FilterAttributeEntry("lfs") });
                    _lfsFilter = GlobalSettings.RegisterFilter(filter);
                //}
                //else
                //{
                //    // TODO fail with specific exception
                //    throw new Exception();
                //}
            }
        }

        //bool IsLFSAvailable()
        //{
        //    // TODO validate that LFS is installed on Mac and Windows
        //    return true;
        //}

        public SourceStepResults ExecuteStep(ITargetWorkspace workspace)
        {
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


        private UsernamePasswordCredentials _credentials;
        private FilterRegistration _lfsFilter;
        private Signature _signature;
        private CredentialsHandler _credentialsHandler;

        public bool IsWorkingCopyInitialized => Repository.IsValid(_workspace.GetDirectory(TargetDirectory.Source));

        public string CurrentCommit
        {
            get
            {
                string result;
                using (var repo = new Repository(_workspace.GetDirectory(TargetDirectory.Source)))
                {
                    result = repo.Head.Tip.Sha;
                }
                return result;
            }
        }

        private string CurrentCommitShortHash
        {
            get
            {
                string commit = CurrentCommit;
                return commit.Length == 0 ? "" : commit.Substring(0, 7);
            }
        }

        private void StoreCredentials()
        {
            string credentialsPath = $"{_workspace.GetDirectory(TargetDirectory.Source)}/../git-credentials";

            var uri = new Uri(_config.RepositoryURL);
            // Should use UriBuilder, but it doesn't include the password in the resulting uri string
            string urlWithCredentials = $"{uri.Scheme}://{_config.Username}:{_config.Password}@{uri.Host}{uri.AbsolutePath}";

            // FIXME abstract using IFileSystem

            File.WriteAllText(credentialsPath, urlWithCredentials);
            credentialsPath = Path.GetFullPath(credentialsPath);
            // FIXME escape spaces in path
            //repo.Config.Set("credential.helper", $"store --file={credentialsPath}", ConfigurationLevel.Local);
        }

        // Clone
        public void Download()
        {
            _workspace.CleanDirectory(TargetDirectory.Source);

            if (_config.UseLFS)
            {
                _logger.Write($"Cloning LFS repository:  {_config.RepositoryURL}", LogType.SmallBullet);

                // FIXME extremely insecure to include password in the URL, but it's the only way I've
                // found to circumvent the manual password prompt when running git-lfs
                // TODO investigate git credential managers on Mac and Windows
                var uri = new Uri(_config.RepositoryURL);
                string repoUrlWithPassword = $"{uri.Scheme}://{_config.Username}:{_config.Password}@{uri.Host}:{uri.Port}{uri.AbsolutePath}";

                ExecuteLFSCommand($"clone {repoUrlWithPassword} {_workspace.GetDirectory(TargetDirectory.Source)}");
            }
            else
            {
                _logger.Write($"Cloning repository:  {_config.RepositoryURL}", LogType.SmallBullet);

                var cloneOptions = new CloneOptions
                {
                    CredentialsProvider = _credentialsHandler,
                    BranchName = string.IsNullOrEmpty(_config.RepositoryBranchName) ? "master" : _config.RepositoryBranchName,
                    Checkout = true,
                    RecurseSubmodules = true
                };

                Repository.Clone(_config.RepositoryURL, _workspace.GetDirectory(TargetDirectory.Source), cloneOptions);
            }

            // TODO Handle sub-module credentials
        }

        // Pull
        public void Update()
        {
            _logger.Write("Cleaning working copy", LogType.SmallBullet);

            // Clean the repo
            using (var repo = new Repository(_workspace.GetDirectory(TargetDirectory.Source)))
            {
                //// Skip the LFS smudge filter when resetting the repo.
                //// LFS files will be integrated manually.
                //repo.Config.Set("filter.lfs.smudge", "git-lfs smudge --skip %f", ConfigurationLevel.Local);
                //repo.Config.Set("filter.lfs.required", false);

                repo.Reset(ResetMode.Hard);
                repo.RemoveUntrackedFiles();

                // Clone a new copy if necessary

                if (!IsWorkingCopyInitialized || repo.Network.Remotes[repo.Head.RemoteName].Url != _config.RepositoryURL)
                {
                    _logger.Write($"Repository URL has changed, cloning a new copy:  {_config.RepositoryURL}", LogType.SmallBullet);
                    Download();
                    return;
                }

                // Pull changes

                _logger.Write($"Pulling changes from {repo.Head.TrackedBranch.FriendlyName}:  {_config.RepositoryURL}", LogType.SmallBullet);

                var pullOptions = new PullOptions
                {
                    FetchOptions = new FetchOptions
                    {
                        CredentialsProvider = _credentialsHandler
                    },
                    MergeOptions = new MergeOptions
                    {
                        FastForwardStrategy = FastForwardStrategy.FastForwardOnly,
                        FileConflictStrategy = CheckoutFileConflictStrategy.Theirs,
                        MergeFileFavor = MergeFileFavor.Theirs,
                        FailOnConflict = false
                    }
                };
                var mergeResult = Commands.Pull(repo, _signature, pullOptions);

                if (_config.UseLFS)
                {
                    _logger.Write("Fetching LFS files", LogType.SmallBullet);
                    ExecuteLFSCommand("fetch");
                    _logger.Write("Checking out LFS files into working copy", LogType.SmallBullet);
                    ExecuteLFSCommand("checkout");
                }

                if (mergeResult.Status == MergeStatus.UpToDate)
                {
                    _logger.Write("Repository is already up-to-date", LogType.SmallBullet);
                }
                else
                {
                    _logger.Write($"Merged commit {mergeResult.Commit.Sha}: {mergeResult.Commit.MessageShort}", LogType.SmallBullet);
                }
            }
        }

        private void ExecuteLFSCommand(string arguments)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;

            var startInfo = new ProcessStartInfo
            {
                FileName = "git-lfs",
                Arguments = arguments,
                WorkingDirectory = _workspace.GetDirectory(TargetDirectory.Source),
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
