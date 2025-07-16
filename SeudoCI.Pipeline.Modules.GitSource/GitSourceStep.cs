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
            ValidateConfiguration();
            
            if (IsWorkingCopyInitialized)
            {
                Update();
            }
            else
            {
                Download();
            }
            
            results.CommitIdentifier = CurrentCommitShortHash;
            results.IsSuccess = true;
        }
        catch (ArgumentException e)
        {
            results.IsSuccess = false;
            results.Exception = e;
            _logger.Write($"Configuration error: {e.Message}", LogType.Failure);
        }
        catch (LibGit2SharpException e)
        {
            results.IsSuccess = false;
            results.Exception = e;
            _logger.Write($"Git operation failed: {e.Message}", LogType.Failure);
        }
        catch (Exception e)
        {
            results.IsSuccess = false;
            results.Exception = e;
            _logger.Write($"Unexpected error during Git operation: {e.Message}", LogType.Failure);
            
            if (e.InnerException != null)
            {
                _logger.Write($"Inner exception: {e.InnerException.Message}", LogType.Failure);
            }
        }

        return results;
    }


    private FilterRegistration? _lfsFilter;
    private Signature _signature = null!;
    private CredentialsHandler _credentialsHandler = null!;

    public bool IsWorkingCopyInitialized => Repository.IsValid(_workspace.GetDirectory(TargetDirectory.Source));

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_config.RepositoryURL))
        {
            throw new ArgumentException("Repository URL cannot be empty");
        }

        if (!Uri.TryCreate(_config.RepositoryURL, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException($"Invalid repository URL: {_config.RepositoryURL}");
        }

        if (!uri.Scheme.Equals("http", StringComparison.OrdinalIgnoreCase) && 
            !uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) &&
            !uri.Scheme.Equals("ssh", StringComparison.OrdinalIgnoreCase) &&
            !uri.Scheme.Equals("git", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException($"Unsupported repository URL scheme: {uri.Scheme}");
        }

        if (_config.UseLFS && !IsLFSAvailable())
        {
            throw new InvalidOperationException("Git LFS is not installed or not available in PATH");
        }
    }

    private bool IsLFSAvailable()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "git-lfs",
                Arguments = "version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            
            using var process = new Process { StartInfo = startInfo };
            process.Start();
            process.WaitForExit(5000); // 5 second timeout
            
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

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
        try
        {
            _logger.Write("Preparing workspace for clone operation", LogType.Debug);
            _workspace.CleanDirectory(TargetDirectory.Source);

            if (_config.UseLFS)
            {
                _logger.Write($"Cloning LFS repository: {_config.RepositoryURL}", LogType.SmallBullet);
                _logger.Write($"Target branch: {_config.RepositoryBranchName ?? "default"}", LogType.Debug);

                // For LFS with authentication, we need to handle credentials differently based on auth type
                string cloneUrl = _config.RepositoryURL;
                
                if (_config.AuthenticationType == GitAuthenticationType.UsernamePassword || 
                    _config.AuthenticationType == GitAuthenticationType.PersonalAccessToken)
                {
                    // Use git credential helper instead of embedding in URL
                    ConfigureGitCredentialHelper();
                }
                
                var branchArg = string.IsNullOrEmpty(_config.RepositoryBranchName) ? "" : $" -b {_config.RepositoryBranchName}";
                var depthArg = _config.ShallowCloneDepth > 0 ? $" --depth {_config.ShallowCloneDepth}" : "";
                ExecuteLFSCommand($"clone{branchArg}{depthArg} {cloneUrl} {_workspace.GetDirectory(TargetDirectory.Source)}");
                _logger.Write("LFS clone completed successfully", LogType.Success);
            }
            else
            {
                _logger.Write($"Cloning repository: {_config.RepositoryURL}", LogType.SmallBullet);
                _logger.Write($"Target branch: {_config.RepositoryBranchName ?? "master"}", LogType.Debug);

                var cloneOptions = new CloneOptions
                {
                    BranchName = string.IsNullOrEmpty(_config.RepositoryBranchName) ? "master" : _config.RepositoryBranchName,
                    Checkout = true,
                    RecurseSubmodules = true
                };
                cloneOptions.FetchOptions.CredentialsProvider = _credentialsHandler;

                // LibGit2Sharp doesn't support shallow clones directly, so we'll use command-line git for shallow clones
                if (_config.ShallowCloneDepth > 0)
                {
                    _logger.Write($"Performing shallow clone with depth {_config.ShallowCloneDepth}", LogType.Debug);
                    PerformShallowClone();
                }
                else if (_config.EnableSparseCheckout && _config.SparseCheckoutPaths.Count > 0)
                {
                    // LibGit2Sharp doesn't support sparse checkout directly, use command-line git
                    _logger.Write("Performing clone with sparse checkout", LogType.Debug);
                    PerformShallowClone(); // This method handles both shallow and regular clones with sparse checkout
                }
                else
                {
                    Repository.Clone(_config.RepositoryURL, _workspace.GetDirectory(TargetDirectory.Source), cloneOptions);
                }
                _logger.Write("Repository cloned successfully", LogType.Success);
                
                // Apply sparse checkout after regular clone if needed
                if (!(_config.ShallowCloneDepth > 0) && _config.EnableSparseCheckout && _config.SparseCheckoutPaths.Count > 0)
                {
                    ConfigureSparseCheckout(_workspace.GetDirectory(TargetDirectory.Source));
                }
            }
        }
        catch (LibGit2SharpException e)
        {
            _logger.Write($"Git clone failed: {e.Message}", LogType.Failure);
            
            // Provide more specific error messages for common issues
            if (e.Message.Contains("401") || e.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase))
            {
                _logger.Write("Authentication failed. Please check your credentials.", LogType.Failure);
            }
            else if (e.Message.Contains("404") || e.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                _logger.Write("Repository not found. Please check the URL and your access permissions.", LogType.Failure);
            }
            else if (e.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            {
                _logger.Write("Network timeout. Please check your internet connection.", LogType.Failure);
            }
            
            throw new InvalidOperationException($"Failed to clone repository from {_config.RepositoryURL}: {e.Message}", e);
        }
        catch (Exception e)
        {
            _logger.Write($"Unexpected error during clone: {e.Message}", LogType.Failure);
            throw;
        }
    }

    // Pull
    public void Update()
    {
        Repository? repo = null;
        try
        {
            _logger.Write("Cleaning working copy", LogType.SmallBullet);

            repo = new Repository(_workspace.GetDirectory(TargetDirectory.Source));
            
            // Validate repository state
            if (!repo.Head.IsTracking)
            {
                throw new InvalidOperationException($"Current branch '{repo.Head.FriendlyName}' is not tracking a remote branch");
            }

            var remoteName = repo.Head.RemoteName;
            if (string.IsNullOrEmpty(remoteName))
            {
                throw new InvalidOperationException("Current branch has no remote configured");
            }

            var currentRemote = repo.Network.Remotes[remoteName];
            if (currentRemote == null)
            {
                throw new InvalidOperationException($"Remote '{remoteName}' not found in repository");
            }

            // Clean the repo
            _logger.Write("Resetting repository to clean state", LogType.Debug);
            repo.Reset(ResetMode.Hard);
            repo.RemoveUntrackedFiles();

            // Clone a new copy if necessary
            if (currentRemote.Url != _config.RepositoryURL)
            {
                _logger.Write($"Repository URL has changed from '{currentRemote.Url}' to '{_config.RepositoryURL}'", LogType.Alert);
                _logger.Write($"Cloning a new copy: {_config.RepositoryURL}", LogType.SmallBullet);
                repo.Dispose();
                repo = null;
                Download();
                return;
            }

            // Pull changes
            _logger.Write($"Pulling changes from {repo.Head.TrackedBranch.FriendlyName}", LogType.SmallBullet);
            _logger.Write($"Repository: {_config.RepositoryURL}", LogType.Debug);

            // Check if this is a shallow repository
            bool isShallow = IsShallowRepository(repo);
            MergeResult? mergeResult = null;
            
            if (isShallow && _config.ShallowCloneDepth > 0)
            {
                _logger.Write("Updating shallow repository with limited depth", LogType.Debug);
                // For shallow repositories, we need to use git fetch with depth
                var branch = repo.Head.FriendlyName;
                repo.Dispose();
                repo = null;
                ExecuteGitCommand($"fetch --depth {_config.ShallowCloneDepth} origin {branch}:{branch}");
                ExecuteGitCommand("reset --hard FETCH_HEAD");
                repo = new Repository(_workspace.GetDirectory(TargetDirectory.Source));
            }
            else
            {
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
                
                mergeResult = Commands.Pull(repo, _signature, pullOptions);
            }

            if (_config.UseLFS)
            {
                _logger.Write("Fetching LFS files", LogType.SmallBullet);
                ExecuteLFSCommand("fetch");
                _logger.Write("Checking out LFS files into working copy", LogType.SmallBullet);
                ExecuteLFSCommand("checkout");
            }

            if (mergeResult != null)
            {
                switch (mergeResult.Status)
                {
                    case MergeStatus.UpToDate:
                        _logger.Write("Repository is already up-to-date", LogType.SmallBullet);
                        break;
                    case MergeStatus.FastForward:
                        _logger.Write($"Fast-forwarded to commit {mergeResult.Commit.Sha[..7]}: {mergeResult.Commit.MessageShort}", LogType.Success);
                        break;
                    case MergeStatus.NonFastForward:
                        _logger.Write($"Merged commit {mergeResult.Commit.Sha[..7]}: {mergeResult.Commit.MessageShort}", LogType.Success);
                        break;
                    case MergeStatus.Conflicts:
                        _logger.Write("Merge completed with conflicts (resolved using remote version)", LogType.Alert);
                        break;
                    default:
                        _logger.Write($"Pull completed with status: {mergeResult.Status}", LogType.SmallBullet);
                        break;
                }
            }
            else if (isShallow)
            {
                _logger.Write("Shallow repository updated successfully", LogType.Success);
            }
        }
        catch (LibGit2SharpException e)
        {
            _logger.Write($"Git update failed: {e.Message}", LogType.Failure);
            throw new InvalidOperationException($"Failed to update repository: {e.Message}", e);
        }
        catch (Exception e)
        {
            _logger.Write($"Unexpected error during update: {e.Message}", LogType.Failure);
            throw;
        }
        finally
        {
            repo?.Dispose();
        }
    }

    private void ExecuteLFSCommand(string arguments)
    {
        _logger.Write($"Executing git-lfs {arguments.Split(' ')[0]}", LogType.Debug);
        
        var startInfo = new ProcessStartInfo
        {
            FileName = "git-lfs",
            Arguments = arguments,
            WorkingDirectory = _workspace.GetDirectory(TargetDirectory.Source),
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            UseShellExecute = false
        };
        
        using var process = new Process { StartInfo = startInfo };
        
        try
        {
            process.Start();
            
            // Read output asynchronously to prevent deadlocks
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            
            if (!process.WaitForExit(300000)) // 5 minute timeout
            {
                process.Kill();
                throw new TimeoutException($"Git LFS command timed out after 5 minutes: git-lfs {arguments}");
            }
            
            var outputText = output.Result;
            var errorText = error.Result;
            
            if (!string.IsNullOrWhiteSpace(outputText))
            {
                _logger.Write(outputText, LogType.Debug);
            }
            
            if (process.ExitCode != 0)
            {
                var errorMessage = $"Git LFS command failed with exit code {process.ExitCode}: git-lfs {arguments}";
                if (!string.IsNullOrWhiteSpace(errorText))
                {
                    errorMessage += $"\nError output: {errorText}";
                    _logger.Write(errorText, LogType.Failure);
                }
                throw new InvalidOperationException(errorMessage);
            }
        }
        catch (Exception e) when (!(e is TimeoutException || e is InvalidOperationException))
        {
            throw new InvalidOperationException($"Failed to execute git-lfs command: {e.Message}", e);
        }
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
    
    private void PerformShallowClone()
    {
        try
        {
            var targetDir = _workspace.GetDirectory(TargetDirectory.Source);
            var branch = string.IsNullOrEmpty(_config.RepositoryBranchName) ? "master" : _config.RepositoryBranchName;
            
            // Build git clone command
            var cloneArgs = "clone";
            
            // Add shallow depth if specified
            if (_config.ShallowCloneDepth > 0)
            {
                cloneArgs += $" --depth {_config.ShallowCloneDepth}";
            }
            
            cloneArgs += $" -b {branch}";
            
            // Add sparse checkout initialization if enabled
            if (_config.EnableSparseCheckout && _config.SparseCheckoutPaths.Count > 0)
            {
                cloneArgs += " --no-checkout --filter=blob:none";
            }
            
            cloneArgs += $" {_config.RepositoryURL} {targetDir}";
            
            // Execute git clone
            ExecuteGitCommand(cloneArgs);
            
            // Configure sparse checkout if enabled
            if (_config.EnableSparseCheckout && _config.SparseCheckoutPaths.Count > 0)
            {
                ConfigureSparseCheckout(targetDir);
            }
            
            if (_config.ShallowCloneDepth > 0)
            {
                _logger.Write($"Shallow clone completed with depth {_config.ShallowCloneDepth}", LogType.Success);
            }
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"Failed to perform clone: {e.Message}", e);
        }
    }
    
    private void ConfigureSparseCheckout(string repoPath)
    {
        try
        {
            _logger.Write("Configuring sparse checkout", LogType.Debug);
            
            // Enable sparse checkout
            ExecuteGitCommand("config core.sparseCheckout true", repoPath);
            
            // Write sparse-checkout file
            var sparseCheckoutPath = Path.Combine(repoPath, ".git", "info", "sparse-checkout");
            var sparseCheckoutDir = Path.GetDirectoryName(sparseCheckoutPath);
            
            if (!Directory.Exists(sparseCheckoutDir))
            {
                Directory.CreateDirectory(sparseCheckoutDir);
            }
            
            // Write paths to sparse-checkout file
            File.WriteAllLines(sparseCheckoutPath, _config.SparseCheckoutPaths);
            
            _logger.Write($"Sparse checkout configured with {_config.SparseCheckoutPaths.Count} paths:", LogType.Debug);
            foreach (var path in _config.SparseCheckoutPaths)
            {
                _logger.Write($"  - {path}", LogType.Debug);
            }
            
            // Perform the checkout
            ExecuteGitCommand("checkout", repoPath);
            
            _logger.Write("Sparse checkout completed successfully", LogType.Success);
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"Failed to configure sparse checkout: {e.Message}", e);
        }
    }
    
    private void ExecuteGitCommand(string arguments, string workingDirectory = null)
    {
        _logger.Write($"Executing git {arguments.Split(' ')[0]}", LogType.Debug);
        
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = arguments,
            WorkingDirectory = workingDirectory ?? _workspace.GetDirectory(TargetDirectory.Source),
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            UseShellExecute = false
        };
        
        // Apply authentication environment variables if needed
        if (_config.AuthenticationType == GitAuthenticationType.UsernamePassword || 
            _config.AuthenticationType == GitAuthenticationType.PersonalAccessToken)
        {
            ConfigureGitCredentialHelper();
        }
        
        using var process = new Process { StartInfo = startInfo };
        
        try
        {
            process.Start();
            
            // Read output asynchronously to prevent deadlocks
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            
            if (!process.WaitForExit(300000)) // 5 minute timeout
            {
                process.Kill();
                throw new TimeoutException($"Git command timed out after 5 minutes: git {arguments}");
            }
            
            var outputText = output.Result;
            var errorText = error.Result;
            
            if (!string.IsNullOrWhiteSpace(outputText))
            {
                _logger.Write(outputText, LogType.Debug);
            }
            
            if (process.ExitCode != 0)
            {
                var errorMessage = $"Git command failed with exit code {process.ExitCode}: git {arguments}";
                if (!string.IsNullOrWhiteSpace(errorText))
                {
                    errorMessage += $"\nError output: {errorText}";
                    _logger.Write(errorText, LogType.Failure);
                }
                throw new InvalidOperationException(errorMessage);
            }
        }
        catch (Exception e) when (!(e is TimeoutException || e is InvalidOperationException))
        {
            throw new InvalidOperationException($"Failed to execute git command: {e.Message}", e);
        }
    }
    
    private bool IsShallowRepository(Repository repo)
    {
        try
        {
            // Check if .git/shallow file exists
            var shallowFile = Path.Combine(repo.Info.Path, "shallow");
            return File.Exists(shallowFile);
        }
        catch
        {
            return false;
        }
    }
}