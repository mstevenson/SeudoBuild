using JetBrains.Annotations;

namespace SeudoCI.Pipeline.Modules.GitSource;

using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using System.Diagnostics;
using Core;

public class GitSourceStep : ISourceStep<GitSourceConfig>
{
    private GitSourceConfig _config = null!;
    private ITargetWorkspace _workspace = null!;
    private ILogger _logger = null!;

    public string? Type => "Git";

    [UsedImplicitly]
    public void Initialize(GitSourceConfig config, ITargetWorkspace workspace, ILogger logger)
    {
        _config = config;
        _workspace = workspace;
        _logger = logger;

        _credentialsHandler = CreateCredentialsHandler(config);
        _signature = new Signature(new Identity("SeudoCI", "noreply@seudoci.local"), DateTimeOffset.UtcNow);

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
            _logger.Write($"Git operation failed: {e.Message}", LogType.Failure);
            return results;
        }

        results.CommitIdentifier = CurrentCommitShortHash;
        results.IsSuccess = true;
        return results;
    }


    private FilterRegistration? _lfsFilter;
    private Signature _signature = null!;
    private CredentialsHandler _credentialsHandler = null!;

    public bool IsWorkingCopyInitialized => Repository.IsValid(_workspace.GetDirectory(TargetDirectory.Source));

    public string CurrentCommit
    {
        get
        {
            using var repo = new Repository(_workspace.GetDirectory(TargetDirectory.Source));
            return repo.Head.Tip.Sha;
        }
    }

    private string CurrentCommitShortHash
    {
        get
        {
            string commit = CurrentCommit;
            return commit.Length == 0 ? "" : commit[..7];
        }
    }
    
    private CredentialsHandler CreateCredentialsHandler(GitSourceConfig config)
    {
        return (url, usernameFromUrl, types) =>
        {
            switch (config.AuthenticationType)
            {
                case GitAuthenticationType.UsernamePassword:
                    if (string.IsNullOrEmpty(config.Username) || string.IsNullOrEmpty(config.Password))
                    {
                        throw new InvalidOperationException("Username and Password are required for UsernamePassword authentication.");
                    }
                    return new UsernamePasswordCredentials 
                    { 
                        Username = config.Username, 
                        Password = config.Password 
                    };
                    
                case GitAuthenticationType.PersonalAccessToken:
                    if (string.IsNullOrEmpty(config.PersonalAccessToken))
                    {
                        throw new InvalidOperationException("PersonalAccessToken is required for PersonalAccessToken authentication.");
                    }
                    // For token auth, username can be anything (often 'x-access-token' or the actual username)
                    return new UsernamePasswordCredentials 
                    { 
                        Username = string.IsNullOrEmpty(config.Username) ? "x-access-token" : config.Username,
                        Password = config.PersonalAccessToken 
                    };
                    
                case GitAuthenticationType.SSHKey:
                    // SSH support is not available in LibGit2Sharp 0.30.0 standard package
                    // For SSH authentication, users should:
                    // 1. Use SSH agent with proper SSH keys loaded
                    // 2. Configure git to use SSH through system git config
                    // 3. Use git:// or ssh:// URLs which will use system SSH
                    throw new NotSupportedException(
                        "SSH key authentication requires system Git with SSH configured. " +
                        "Please ensure your SSH keys are properly configured in ~/.ssh/ " +
                        "and that you can clone the repository using command-line git.");
                    
                case GitAuthenticationType.SSHAgent:
                    // SSH agent support is not available in LibGit2Sharp 0.30.0 standard package
                    throw new NotSupportedException(
                        "SSH agent authentication requires system Git with SSH agent running. " +
                        "Please ensure ssh-agent is running and your keys are loaded.");
                    
                default:
                    throw new NotSupportedException($"Authentication type {config.AuthenticationType} is not supported.");
            }
        };
    }


    // Clone
    public void Download()
    {
        _workspace.CleanDirectory(TargetDirectory.Source);

        if (_config.UseLFS)
        {
            _logger.Write($"Cloning LFS repository:  {_config.RepositoryURL}", LogType.SmallBullet);

            // For LFS with authentication, we need to handle credentials differently based on auth type
            string cloneUrl = _config.RepositoryURL;
            
            if (_config.AuthenticationType == GitAuthenticationType.UsernamePassword || 
                _config.AuthenticationType == GitAuthenticationType.PersonalAccessToken)
            {
                // Use git credential helper instead of embedding in URL
                ConfigureGitCredentialHelper();
            }
            
            ExecuteLFSCommand($"clone {cloneUrl} {_workspace.GetDirectory(TargetDirectory.Source)}");
        }
        else
        {
            _logger.Write($"Cloning repository:  {_config.RepositoryURL}", LogType.SmallBullet);

            var cloneOptions = new CloneOptions
            {
                BranchName = string.IsNullOrEmpty(_config.RepositoryBranchName) ? "master" : _config.RepositoryBranchName,
                Checkout = true,
                RecurseSubmodules = true
            };
            cloneOptions.FetchOptions.CredentialsProvider = _credentialsHandler;

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
    
    private void ConfigureGitCredentialHelper()
    {
        // Configure git to use a temporary credential helper for this operation
        // This avoids embedding passwords in URLs
        var gitConfigCommands = new List<string>();
        
        if (_config.AuthenticationType == GitAuthenticationType.PersonalAccessToken)
        {
            // For tokens, configure git to use the token as password
            var token = _config.PersonalAccessToken;
            var username = string.IsNullOrEmpty(_config.Username) ? "x-access-token" : _config.Username;
            
            // Set up a temporary askpass script that returns the token
            var askPassScript = Path.Combine(Path.GetTempPath(), $"git-askpass-{Guid.NewGuid()}.sh");
            File.WriteAllText(askPassScript, $"#!/bin/sh\necho '{token}'");
            
            if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                // Make script executable on Unix-like systems
                var chmod = new ProcessStartInfo("chmod", $"+x {askPassScript}")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(chmod)?.WaitForExit();
            }
            
            Environment.SetEnvironmentVariable("GIT_ASKPASS", askPassScript);
            
            // Clean up after operation
            AppDomain.CurrentDomain.ProcessExit += (s, e) => 
            {
                if (File.Exists(askPassScript))
                    File.Delete(askPassScript);
            };
        }
        else if (_config.AuthenticationType == GitAuthenticationType.UsernamePassword)
        {
            _logger.Write("Warning: Using username/password authentication is deprecated. Consider using SSH keys or personal access tokens.", LogType.Alert);
            
            // For backward compatibility, set up basic auth
            // Note: This is still not ideal but better than URL embedding
            var authString = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{_config.Username}:{_config.Password}"));
            Environment.SetEnvironmentVariable("GIT_AUTH_HEADER", $"Authorization: Basic {authString}");
        }
    }
}