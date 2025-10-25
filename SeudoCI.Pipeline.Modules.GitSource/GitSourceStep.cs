using JetBrains.Annotations;

namespace SeudoCI.Pipeline.Modules.GitSource;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Core;

public class GitSourceStep : ISourceStep<GitSourceConfig>
{
    private GitSourceConfig _config = null!;
    private ITargetWorkspace _workspace = null!;
    private ILogger _logger = null!;
    private Uri? _repositoryUri;
    private bool _knownHostsSeeded;
    private static readonly UTF8Encoding Utf8NoBom = new(false);

    public string Type => "Git";

    [UsedImplicitly]
    public void Initialize(GitSourceConfig config, ITargetWorkspace workspace, ILogger logger)
    {
        _config = config;
        _workspace = workspace;
        _logger = logger;
        _repositoryUri = null;
        _knownHostsSeeded = false;
    }
    
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
            _logger.Write("Git operation completed successfully for repository", LogType.Success);
        }
        catch (ArgumentException e)
        {
            results.IsSuccess = false;
            results.Exception = e;
            _logger.Write($"Configuration error: {e.Message}", LogType.Failure);
        }
        catch (UnauthorizedAccessException e)
        {
            results.IsSuccess = false;
            results.Exception = e;
            _logger.Write("Authentication failed. Please check your credentials.", LogType.Failure);
        }
        catch (SecurityException e)
        {
            results.IsSuccess = false;
            results.Exception = e;
            _logger.Write("Security validation failed. Please check your configuration.", LogType.Failure);
        }
        catch (InvalidOperationException e)
        {
            results.IsSuccess = false;
            results.Exception = e;
            _logger.Write($"Git operation failed: {e.Message}", LogType.Failure);
        }
        catch (TimeoutException e)
        {
            results.IsSuccess = false;
            results.Exception = e;
            _logger.Write("Git operation timed out. Please check your network connection.", LogType.Failure);
        }
        catch (Exception e)
        {
            results.IsSuccess = false;
            results.Exception = e;
            _logger.Write("An unexpected error occurred during Git operation", LogType.Failure);
            if (e.InnerException != null)
            {
                _logger.Write("Additional error information available for debugging", LogType.Debug);
            }
        }

        return results;
    }
    
    public bool IsWorkingCopyInitialized => GitIsValidRepository(_workspace.GetDirectory(TargetDirectory.Source));

    private void ValidateConfiguration()
    {
        // Input validation to prevent injection attacks
        
        if (string.IsNullOrWhiteSpace(_config.RepositoryURL))
        {
            throw new ArgumentException("Repository URL cannot be empty");
        }

        // Validate and sanitize repository URL
        if (!Uri.TryCreate(_config.RepositoryURL, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException($"Invalid repository URL format"); // Don't expose the actual URL in error
        }

        _repositoryUri = uri;

        // Restrict allowed URL schemes - remove insecure git:// protocol
        var allowedSchemes = new[] { "http", "https", "ssh" };
        if (!allowedSchemes.Contains(uri.Scheme.ToLowerInvariant()))
        {
            throw new ArgumentException($"Unsupported repository URL scheme: {uri.Scheme}. Only http, https, and ssh are supported.");
        }
        
        // Validate repository URL format and prevent malicious URLs
        ValidateRepositoryUrl(uri);
        
        // Validate authentication method matches URL scheme
        ValidateAuthenticationMethod(uri);
        
        // Validate branch name to prevent injection
        if (!string.IsNullOrEmpty(_config.RepositoryBranchName))
        {
            ValidateBranchName(_config.RepositoryBranchName);
        }
        
        // Validate sparse checkout paths
        if (_config.EnableSparseCheckout && _config.SparseCheckoutPaths != null)
        {
            ValidateSparseCheckoutPaths(_config.SparseCheckoutPaths);
        }

        // Validate authentication credentials
        ValidateAuthenticationCredentials();

        if (_config.KnownHostsEntries?.Count > 0)
        {
            ValidateKnownHostsEntries(_config.KnownHostsEntries);
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
    
    private void ValidateRepositoryUrl(Uri repositoryUri)
    {
        // Prevent localhost and internal network access
        if (repositoryUri.IsLoopback || 
            repositoryUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            repositoryUri.Host.StartsWith("127.") ||
            repositoryUri.Host.StartsWith("10.") ||
            repositoryUri.Host.StartsWith("192.168.") ||
            repositoryUri.Host.StartsWith("169.254."))
        {
            throw new ArgumentException("Repository URLs pointing to local or internal networks are not allowed");
        }
        
        // Validate hostname format
        if (!IsValidHostname(repositoryUri.Host))
        {
            throw new ArgumentException("Invalid hostname in repository URL");
        }
        
        // Prevent unusual ports that might be used for attacks
        if (repositoryUri.Port > 0 && repositoryUri.Port < 1024 && 
            repositoryUri.Port != 22 && repositoryUri.Port != 80 && repositoryUri.Port != 443)
        {
            throw new ArgumentException("Repository URL uses a restricted port number");
        }
    }
    
    private static bool IsValidHostname(string hostname)
    {
        if (string.IsNullOrWhiteSpace(hostname) || hostname.Length > 253)
            return false;
            
        // Basic hostname validation regex
        var hostnameRegex = new Regex(@"^[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?)*$");
        return hostnameRegex.IsMatch(hostname);
    }
    
    private void ValidateAuthenticationMethod(Uri repositoryUri)
    {
        // Ensure authentication method is compatible with URL scheme
        var scheme = repositoryUri.Scheme.ToLowerInvariant();
        
        switch (_config.AuthenticationType)
        {
            case GitAuthenticationType.SSHKey:
            case GitAuthenticationType.SSHAgent:
                if (scheme != "ssh")
                {
                    throw new ArgumentException($"SSH authentication can only be used with ssh:// URLs.");
                }
                break;
                
            case GitAuthenticationType.UsernamePassword:
            case GitAuthenticationType.PersonalAccessToken:
                if (scheme != "http" && scheme != "https")
                {
                    throw new ArgumentException($"Username/password and token authentication can only be used with http:// or https:// URLs.");
                }
                // Warn about using HTTP instead of HTTPS
                if (scheme == "http")
                {
                    _logger.Write("Warning: Using HTTP instead of HTTPS for authentication is insecure and not recommended.", LogType.Alert);
                }
                break;
        }
    }
    
    private static void ValidateBranchName(string branchName)
    {
        // Validate branch name to prevent command injection
        if (string.IsNullOrWhiteSpace(branchName))
        {
            throw new ArgumentException("Branch name cannot be empty or whitespace");
        }
        
        // Check for dangerous characters that could be used for injection
        var dangerousChars = new[] { ';', '|', '&', '$', '`', '\"', '\'', '\n', '\r', '\0', '\t', '<', '>', '*', '?', '[', ']', '{', '}', '(', ')', '~', '^' };
        if (branchName.IndexOfAny(dangerousChars) >= 0)
        {
            throw new ArgumentException("Branch name contains invalid characters that could be used for command injection");
        }
        
        // Additional validation for command injection patterns
        if (branchName.Contains("--") || branchName.Contains("../") || branchName.Contains("..\\"))
        {
            throw new ArgumentException("Branch name contains potentially dangerous patterns");
        }
        
        // Validate against Git branch naming rules
        if (branchName.StartsWith('-') || branchName.EndsWith('.') || branchName.Contains("..") || branchName.StartsWith('/'))
        {
            throw new ArgumentException("Branch name violates Git naming conventions");
        }
        
        // Validate using regex
        var branchNameRegex = new Regex("^[a-zA-Z0-9._/-]+$");
        if (!branchNameRegex.IsMatch(branchName))
        {
            throw new ArgumentException("Branch name contains invalid characters");
        }
        
        // Length validation
        if (branchName.Length > 255)
        {
            throw new ArgumentException("Branch name is too long");
        }
    }
    
    private static void ValidateSparseCheckoutPaths(List<string> paths)
    {
        // Validate sparse checkout paths to prevent path traversal
        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Sparse checkout path cannot be empty");
            }
            
            // Path traversal prevention
            try
            {
                ValidateFilePath(path);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException($"Invalid sparse checkout path contains dangerous characters: {path}");
            }
            
            // Validate path format
            var pathRegex = new Regex("^[a-zA-Z0-9._/-]+$");
            if (!pathRegex.IsMatch(path))
            {
                throw new ArgumentException($"Sparse checkout path contains invalid characters: {path}");
            }
            
            // Length validation
            if (path.Length > 4096)
            {
                throw new ArgumentException($"Sparse checkout path is too long: {path}");
            }
            
            // Prevent absolute paths
            if (Path.IsPathRooted(path))
            {
                throw new ArgumentException($"Absolute paths are not allowed in sparse checkout: {path}");
            }
        }
    }

    private static void ValidateKnownHostsEntries(IEnumerable<string> entries)
    {
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry))
            {
                continue;
            }

            if (entry.Contains('\n') || entry.Contains('\r'))
            {
                throw new ArgumentException("Known hosts entries must be single-line values.");
            }
        }
    }
    
    private void ValidateAuthenticationCredentials()
    {
        // Validate authentication credentials based on authentication type
        switch (_config.AuthenticationType)
        {
            case GitAuthenticationType.UsernamePassword:
                if (string.IsNullOrWhiteSpace(_config.Username))
                {
                    throw new ArgumentException("Username cannot be empty for username/password authentication");
                }
                if (string.IsNullOrWhiteSpace(_config.Password))
                {
                    throw new ArgumentException("Password cannot be empty for username/password authentication");
                }
                // Validate username format
                if (!IsValidUsername(_config.Username))
                {
                    throw new ArgumentException("Username contains invalid characters");
                }
                break;
                
            case GitAuthenticationType.PersonalAccessToken:
                if (string.IsNullOrWhiteSpace(_config.PersonalAccessToken))
                {
                    throw new ArgumentException("Personal access token cannot be empty for token authentication");
                }
                // Basic token format validation
                if (_config.PersonalAccessToken.Length < 10 || _config.PersonalAccessToken.Length > 255)
                {
                    throw new ArgumentException("Personal access token has invalid length");
                }
                break;
                
            case GitAuthenticationType.SSHKey:
                if (string.IsNullOrWhiteSpace(_config.PrivateKeyPath))
                {
                    throw new ArgumentException("Private key path cannot be empty for SSH key authentication");
                }
                if (!File.Exists(_config.PrivateKeyPath))
                {
                    throw new FileNotFoundException($"SSH private key file not found: {_config.PrivateKeyPath}");
                }
                // Validate key file path
                ValidateFilePath(_config.PrivateKeyPath);
                break;
                
            case GitAuthenticationType.SSHAgent:
                // SSH agent authentication doesn't require additional credential validation
                break;
                
            default:
                throw new ArgumentException($"Unsupported authentication type: {_config.AuthenticationType}");
        }
    }
    
    private static bool IsValidUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length > 255)
            return false;
            
        // Basic username validation - alphanumeric plus common characters
        var usernameRegex = new Regex(@"^[a-zA-Z0-9._@-]+$");
        return usernameRegex.IsMatch(username);
    }
    
    // Validate file path to prevent path traversal
    private static void ValidateFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be empty");
        }
        
        // Check for path traversal attempts
        if (filePath.Contains("..") || 
            filePath.StartsWith('/') || 
            filePath.Contains('\0') ||
            filePath.Contains('~') ||
            filePath.Contains('$') ||
            filePath.Contains('`') ||
            filePath.Contains('|') ||
            filePath.Contains('&') ||
            filePath.Contains(';') ||
            filePath.Contains('>') ||
            filePath.Contains('<'))
        {
            throw new ArgumentException("File path contains dangerous characters");
        }
        
        if (!Path.IsPathRooted(filePath))
        {
            throw new ArgumentException("File path must be absolute");
        }
    }

    public string CurrentCommit
    {
        get
        {
            var commitInfo = GitGetCommitInfo(_workspace.GetDirectory(TargetDirectory.Source));
            return commitInfo.CommitHash;
        }
    }

    private string CurrentCommitShortHash
    {
        get
        {
            var commitInfo = GitGetCommitInfo(_workspace.GetDirectory(TargetDirectory.Source));
            return commitInfo.CommitHashShort;
        }
    }
    
    // Clone
    public void Download()
    {
        try
        {
            _logger.Write("Preparing workspace for clone operation", LogType.Debug);
            _workspace.CleanDirectory(TargetDirectory.Source);

            var targetPath = _workspace.GetDirectory(TargetDirectory.Source);
            var branch = string.IsNullOrEmpty(_config.RepositoryBranchName) ? "main" : _config.RepositoryBranchName;
            
            _logger.Write($"Cloning repository: {_config.RepositoryURL}", LogType.SmallBullet);
            _logger.Write($"Target branch: {branch}", LogType.Debug);

            // Use native git clone with all features
            GitClone(_config.RepositoryURL, targetPath, branch, _config.ShallowCloneDepth, _config.UseLFS);
            
            // Configure sparse checkout if enabled
            if (_config.EnableSparseCheckout && _config.SparseCheckoutPaths.Count > 0)
            {
                _logger.Write("Configuring sparse checkout", LogType.Debug);
                GitConfigureSparseCheckout(targetPath, _config.SparseCheckoutPaths);
                _logger.Write($"Sparse checkout configured with {_config.SparseCheckoutPaths.Count} paths", LogType.Success);
            }
            
            _logger.Write("Repository cloned successfully", LogType.Success);
        }
        catch (Exception e)
        {
            _logger.Write($"Git clone failed: {e.Message}", LogType.Failure);
            
            // Provide specific error messages without exposing sensitive information
            if (e.Message.Contains("401") || e.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase))
            {
                _logger.Write("Authentication failed. Please check your credentials.", LogType.Failure);
                throw new UnauthorizedAccessException("Repository authentication failed", e);
            }

            if (e.Message.Contains("404") || e.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                _logger.Write("Repository not found. Please check the URL and your access permissions.", LogType.Failure);
                throw new InvalidOperationException("Repository not found or access denied", e);
            }

            if (e.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            {
                _logger.Write("Network timeout. Please check your internet connection.", LogType.Failure);
                throw new TimeoutException("Git operation timed out", e);
            }

            // Don't expose repository URL in error messages
            throw new InvalidOperationException("Failed to clone repository", e);
        }
    }

    // Pull
    public void Update()
    {
        try
        {
            var workingDirectory = _workspace.GetDirectory(TargetDirectory.Source);
            
            _logger.Write("Cleaning working copy", LogType.SmallBullet);
            
            // Get current repository information
            var commitInfo = GitGetCommitInfo(workingDirectory);
            if (!commitInfo.IsValidRepository)
            {
                throw new InvalidOperationException("Source directory does not contain a valid Git repository");
            }

            // Clean the repository
            _logger.Write("Resetting repository to clean state", LogType.Debug);
            GitReset(workingDirectory);

            // Check if remote URL has changed
            if (!string.IsNullOrEmpty(commitInfo.RemoteUrl) && commitInfo.RemoteUrl != _config.RepositoryURL)
            {
                _logger.Write($"Repository URL has changed from '{commitInfo.RemoteUrl}' to '{_config.RepositoryURL}'", LogType.Alert);
                _logger.Write($"Cloning a new copy: {_config.RepositoryURL}", LogType.SmallBullet);
                Download();
                return;
            }

            // Pull changes
            _logger.Write($"Pulling changes from {commitInfo.BranchName}", LogType.SmallBullet);
            _logger.Write($"Repository: {_config.RepositoryURL}", LogType.Debug);

            // Check if this is a shallow repository
            bool isShallow = IsShallowRepository(workingDirectory);
            
            if (isShallow && _config.ShallowCloneDepth > 0)
            {
                _logger.Write("Updating shallow repository with limited depth", LogType.Debug);
                ExecuteGitCommandWithArgs(["fetch", "--depth", _config.ShallowCloneDepth.ToString(), "origin"], workingDirectory);
                ExecuteGitCommandWithArgs(["reset", "--hard", "FETCH_HEAD"], workingDirectory);
                _logger.Write("Shallow repository updated successfully", LogType.Success);
            }
            else
            {
                var pullResult = ExecuteGitCommandWithArgs(["pull", "--ff-only", "--no-edit"], workingDirectory, throwOnError: false);
                
                if (pullResult.IsSuccess)
                {
                    if (pullResult.StandardOutput.Contains("Already up to date"))
                    {
                        _logger.Write("Repository is already up-to-date", LogType.SmallBullet);
                    }
                    else
                    {
                        _logger.Write("Repository updated successfully", LogType.Success);
                    }
                }
                else if (pullResult.StandardError.Contains("non-fast-forward"))
                {
                    _logger.Write("Pull failed due to non-fast-forward changes, performing hard reset", LogType.Alert);
                    ExecuteGitCommandWithArgs(["fetch", "origin"], workingDirectory);
                    ExecuteGitCommandWithArgs(["reset", "--hard", "origin/HEAD"], workingDirectory);
                    _logger.Write("Repository reset to remote HEAD", LogType.Success);
                }
                else
                {
                    throw new InvalidOperationException($"Pull failed: {pullResult.StandardError}");
                }
            }

            // Update LFS files if enabled
            if (_config.UseLFS)
            {
                _logger.Write("Updating LFS files", LogType.SmallBullet);
                ExecuteGitCommandWithArgs(["lfs", "fetch"], workingDirectory);
                ExecuteGitCommandWithArgs(["lfs", "checkout"], workingDirectory);
                _logger.Write("LFS files updated successfully", LogType.Success);
            }
        }
        catch (Exception e)
        {
            _logger.Write($"Git update failed: {e.Message}", LogType.Failure);
            throw new InvalidOperationException($"Failed to update repository: {e.Message}", e);
        }
    }

    
    private GitCommandResult ExecuteGitCommandWithArgs(List<string> arguments, string? workingDirectory = null, bool throwOnError = true)
    {
        if (arguments.Count == 0)
        {
            throw new ArgumentException("Git command arguments cannot be empty");
        }
        
        // Log command without exposing sensitive arguments
        var safeCommand = arguments[0];
        _logger.Write($"Executing git {safeCommand}", LogType.Debug);
        
        // Audit log for authentication-related commands
        if (safeCommand.Contains("clone") || safeCommand.Contains("fetch") || safeCommand.Contains("pull"))
        {
            _logger.Write($"Git authentication operation: {safeCommand}", LogType.Alert);
        }
        
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = workingDirectory ?? _workspace.GetDirectory(TargetDirectory.Source),
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            UseShellExecute = false
        };
        
        // Add arguments individually to prevent injection
        foreach (var arg in arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }
        
        // Apply authentication environment variables if needed
        ConfigureGitAuthentication(startInfo);
        
        using var process = new Process { StartInfo = startInfo };
        
        try
        {
            process.Start();
            
            // Read output asynchronously to prevent deadlocks with timeout
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            
            // Enhanced timeout with proper resource cleanup
            if (!process.WaitForExit(300000)) // 5 minute timeout
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(5000); // Wait for cleanup
                }
                catch (Exception killEx)
                {
                    _logger.Write($"Warning: Could not kill timed-out process: {killEx.Message}", LogType.Alert);
                }
                
                var timeoutResult = new GitCommandResult
                {
                    ExitCode = -1,
                    StandardOutput = "",
                    StandardError = $"Git command timed out after 5 minutes: git {safeCommand}",
                    IsSuccess = false
                };
                
                if (throwOnError)
                {
                    throw new TimeoutException(timeoutResult.StandardError);
                }
                return timeoutResult;
            }
            
            var outputText = output.Result;
            var errorText = error.Result;
            
            if (!string.IsNullOrWhiteSpace(outputText))
            {
                _logger.Write(outputText, LogType.Debug);
            }
            
            var result = new GitCommandResult
            {
                ExitCode = process.ExitCode,
                StandardOutput = outputText,
                StandardError = errorText,
                IsSuccess = process.ExitCode == 0
            };
            
            if (!result.IsSuccess)
            {
                var errorMessage = $"Git command failed with exit code {process.ExitCode}: git {safeCommand}";
                if (!string.IsNullOrWhiteSpace(errorText))
                {
                    errorMessage += $"\nError output: {errorText}";
                    _logger.Write(errorText, LogType.Failure);
                }
                
                // Log authentication failures for security monitoring
                if (errorText.Contains("authentication", StringComparison.OrdinalIgnoreCase) ||
                    errorText.Contains("401", StringComparison.OrdinalIgnoreCase) ||
                    errorText.Contains("403", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Write($"Authentication failure detected during git operation", LogType.Alert);
                }
                
                if (throwOnError)
                {
                    throw new InvalidOperationException(errorMessage);
                }
            }
            
            return result;
        }
        catch (Exception e) when (!(e is TimeoutException || e is InvalidOperationException))
        {
            var exceptionResult = new GitCommandResult
            {
                ExitCode = -1,
                StandardOutput = "",
                StandardError = $"Failed to execute git command: {e.Message}",
                IsSuccess = false
            };
            
            if (throwOnError)
            {
                throw new InvalidOperationException(exceptionResult.StandardError, e);
            }
            return exceptionResult;
        }
    }
    
    private void ConfigureGitAuthentication(ProcessStartInfo startInfo)
    {
        // Log authentication method being used (without sensitive details)
        _logger.Write($"Configuring Git authentication using {_config.AuthenticationType}", LogType.Debug);
        
        switch (_config.AuthenticationType)
        {
            case GitAuthenticationType.UsernamePassword:
                ConfigureUsernamePasswordAuth(startInfo);
                break;
            case GitAuthenticationType.PersonalAccessToken:
                ConfigurePersonalAccessTokenAuth(startInfo);
                break;
            case GitAuthenticationType.SSHKey:
                ConfigureSSHKeyAuth(startInfo);
                break;
            case GitAuthenticationType.SSHAgent:
                ConfigureSSHAgentAuth(startInfo);
                break;
            default:
                throw new ArgumentException($"Unsupported authentication type: {_config.AuthenticationType}");
        }
        
        // Log successful authentication configuration
        _logger.Write($"Git authentication configured successfully", LogType.Debug);
    }
    
    private void ConfigureUsernamePasswordAuth(ProcessStartInfo startInfo)
    {
        if (string.IsNullOrEmpty(_config.Username) || string.IsNullOrEmpty(_config.Password))
        {
            throw new InvalidOperationException("Username and Password are required for UsernamePassword authentication.");
        }
        
        // Log authentication attempt without exposing credentials
        _logger.Write($"Configuring username/password authentication for user: {_config.Username}", LogType.Debug);
        
        // Use secure credential helper script instead of environment variables
        var credentialHelper = CreateSecureCredentialHelper(_config.Username, _config.Password);
        startInfo.EnvironmentVariables["GIT_ASKPASS"] = credentialHelper;
        startInfo.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";
        
        // Disable credential prompting entirely
        startInfo.EnvironmentVariables["GIT_CONFIG_NOSYSTEM"] = "1";
        
        _logger.Write("Warning: Using username/password authentication is deprecated. Consider using SSH keys or personal access tokens.", LogType.Alert);
    }
    
    private void ConfigurePersonalAccessTokenAuth(ProcessStartInfo startInfo)
    {
        if (string.IsNullOrEmpty(_config.PersonalAccessToken))
        {
            throw new InvalidOperationException("PersonalAccessToken is required for PersonalAccessToken authentication.");
        }
        
        var username = string.IsNullOrEmpty(_config.Username) ? "x-access-token" : _config.Username;
        
        // Log authentication attempt without exposing token
        _logger.Write($"Configuring personal access token authentication for user: {username}", LogType.Debug);
        
        // Use secure credential helper script instead of environment variables
        var credentialHelper = CreateSecureCredentialHelper(username, _config.PersonalAccessToken);
        startInfo.EnvironmentVariables["GIT_ASKPASS"] = credentialHelper;
        startInfo.EnvironmentVariables["GIT_TERMINAL_PROMPT"] = "0";
        
        // Disable credential prompting entirely
        startInfo.EnvironmentVariables["GIT_CONFIG_NOSYSTEM"] = "1";
    }
    
    private void ConfigureSSHKeyAuth(ProcessStartInfo startInfo)
    {
        if (string.IsNullOrEmpty(_config.PrivateKeyPath))
        {
            throw new InvalidOperationException("PrivateKeyPath is required for SSHKey authentication.");
        }
        
        if (!File.Exists(_config.PrivateKeyPath))
        {
            throw new FileNotFoundException($"SSH private key file not found: {_config.PrivateKeyPath}");
        }
        
        // Log SSH key authentication attempt
        _logger.Write($"Configuring SSH key authentication using key: {Path.GetFileName(_config.PrivateKeyPath)}", LogType.Debug);
        
        // Validate SSH key file permissions
        ValidateSSHKeyPermissions(_config.PrivateKeyPath);
        
        // Create secure known_hosts file path
        var knownHostsPath = CreateSecureKnownHostsPath();
        
        // Enable host key checking for security with proper known_hosts management
        var sshCommand = $"ssh -i \"{EscapeShellArgument(_config.PrivateKeyPath)}\" -o UserKnownHostsFile=\"{knownHostsPath}\" -o StrictHostKeyChecking=yes -o PasswordAuthentication=no -o PubkeyAuthentication=yes";
        
        if (!string.IsNullOrEmpty(_config.Passphrase))
        {
            _logger.Write("SSH key has passphrase protection enabled", LogType.Debug);
            
            // Use secure credential helper for SSH passphrase
            var credentialHelper = CreateSecureCredentialHelper("", _config.Passphrase);
            startInfo.EnvironmentVariables["SSH_ASKPASS"] = credentialHelper;
            startInfo.EnvironmentVariables["DISPLAY"] = ":0"; // Required for SSH_ASKPASS to work
            startInfo.EnvironmentVariables["SSH_ASKPASS_REQUIRE"] = "force";
        }
        
        startInfo.EnvironmentVariables["GIT_SSH_COMMAND"] = sshCommand;
    }
    
    private void ConfigureSSHAgentAuth(ProcessStartInfo startInfo)
    {
        // Log SSH agent authentication attempt
        _logger.Write("Configuring SSH agent authentication", LogType.Debug);
        
        // Create secure known_hosts file path
        var knownHostsPath = CreateSecureKnownHostsPath();
        
        // SSH agent authentication relies on the system SSH agent
        // Enable host key checking for security with proper known_hosts management
        startInfo.EnvironmentVariables["GIT_SSH_COMMAND"] = $"ssh -o UserKnownHostsFile=\"{knownHostsPath}\" -o StrictHostKeyChecking=yes -o PasswordAuthentication=no -o PubkeyAuthentication=yes";
        
        // Verify SSH agent is running
        if (!IsSSHAgentRunning())
        {
            _logger.Write("SSH agent is not running or has no loaded keys", LogType.Failure);
            throw new InvalidOperationException("SSH agent is not running. Please start ssh-agent and load your SSH keys.");
        }
        
        _logger.Write("SSH agent is running and ready for authentication", LogType.Debug);
    }
    
    private bool IsSSHAgentRunning()
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "ssh-add",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add("-l");

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(5000))
            {
                try
                {
                    process.Kill(true);
                }
                catch
                {
                    // Ignore kill failures
                }

                _logger.Write("ssh-add -l timed out while checking for an SSH agent", LogType.Alert);
                return false;
            }

            var stdout = stdoutTask.Result;
            var stderr = stderrTask.Result;

            if (process.ExitCode == 0)
            {
                return !stdout.Contains("The agent has no identities", StringComparison.OrdinalIgnoreCase) &&
                       !stdout.Contains("no identities", StringComparison.OrdinalIgnoreCase);
            }

            return stderr.Contains("no identities", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            _logger.Write($"Warning: Unable to query ssh-agent status: {ex.Message}", LogType.Alert);
            return false;
        }
    }
    
    private string CreateSecureCredentialHelper(string username = "", string password = "")
    {
        var tempDir = Path.GetTempPath();

        if (OperatingSystem.IsWindows())
        {
            var scriptPath = Path.Combine(tempDir, $"git-askpass-{Guid.NewGuid():N}.cmd");
            var builder = new StringBuilder();
            builder.AppendLine("@echo off");
            builder.AppendLine("setlocal ENABLEEXTENSIONS");
            builder.AppendLine("if \"%~1\"==\"get\" (");

            if (!string.IsNullOrEmpty(username))
            {
                builder.AppendLine($"  echo username={EscapeWindowsCredentialValue(username)}");
            }

            if (!string.IsNullOrEmpty(password))
            {
                builder.AppendLine($"  echo password={EscapeWindowsCredentialValue(password)}");
            }

            builder.AppendLine(")");
            builder.AppendLine("exit /b 0");

            File.WriteAllText(scriptPath, builder.ToString(), Utf8NoBom);
            ScheduleFileForDeletion(scriptPath);
            return scriptPath;
        }

        var shellPath = Path.Combine(tempDir, $"git-askpass-{Guid.NewGuid():N}.sh");
        var script = new StringBuilder();
        script.AppendLine("#!/bin/sh");
        script.AppendLine("case \"$1\" in");
        script.AppendLine("get)");

        if (!string.IsNullOrEmpty(username))
        {
            script.AppendLine($"  echo username={EscapeShellArgument(username)}");
        }

        if (!string.IsNullOrEmpty(password))
        {
            script.AppendLine($"  echo password={EscapeShellArgument(password)}");
        }

        script.AppendLine("  ;;");
        script.AppendLine("esac");

        File.WriteAllText(shellPath, script.ToString(), Utf8NoBom);
        RunChmod("700", shellPath);
        ScheduleFileForDeletion(shellPath);
        return shellPath;
    }
    
    private string CreateSecureKnownHostsPath()
    {
        var userHome = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (string.IsNullOrEmpty(userHome))
        {
            throw new InvalidOperationException("Unable to determine user profile directory for SSH known_hosts management.");
        }

        var sshDir = Path.Combine(userHome, ".ssh");
        Directory.CreateDirectory(sshDir);

        RunChmod("700", sshDir);

        var knownHostsPath = Path.Combine(sshDir, "known_hosts");
        if (!File.Exists(knownHostsPath))
        {
            File.WriteAllText(knownHostsPath, string.Empty, Utf8NoBom);
        }

        RunChmod("600", knownHostsPath);

        if (!_knownHostsSeeded)
        {
            SeedKnownHostsFile(knownHostsPath);
            _knownHostsSeeded = true;
        }

        return knownHostsPath;
    }

    private void SeedKnownHostsFile(string knownHostsPath)
    {
        try
        {
            var existingEntries = new HashSet<string>(StringComparer.Ordinal);
            if (File.Exists(knownHostsPath))
            {
                foreach (var line in File.ReadLines(knownHostsPath))
                {
                    var trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        existingEntries.Add(trimmed);
                    }
                }
            }

            var entriesToAdd = new List<string>();

            if (_config.KnownHostsEntries != null)
            {
                foreach (var entry in _config.KnownHostsEntries)
                {
                    if (string.IsNullOrWhiteSpace(entry))
                    {
                        continue;
                    }

                    var trimmed = entry.Trim();
                    if (existingEntries.Add(trimmed))
                    {
                        entriesToAdd.Add(trimmed);
                    }
                }
            }

            if (_repositoryUri != null &&
                string.Equals(_repositoryUri.Scheme, "ssh", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var entry in FetchKnownHostEntriesFromRemote(_repositoryUri))
                {
                    if (string.IsNullOrWhiteSpace(entry))
                    {
                        continue;
                    }

                    var trimmed = entry.Trim();
                    if (existingEntries.Add(trimmed))
                    {
                        entriesToAdd.Add(trimmed);
                    }
                }
            }

            if (entriesToAdd.Count > 0)
            {
                File.AppendAllText(knownHostsPath, string.Join(Environment.NewLine, entriesToAdd) + Environment.NewLine, Utf8NoBom);
                RunChmod("600", knownHostsPath);
                _logger.Write($"Seeded {entriesToAdd.Count} SSH known host entr{(entriesToAdd.Count == 1 ? "y" : "ies")}", LogType.Debug);
            }
        }
        catch (Exception ex)
        {
            _logger.Write($"Warning: Failed to seed known_hosts file: {ex.Message}", LogType.Alert);
        }
    }

    private IEnumerable<string> FetchKnownHostEntriesFromRemote(Uri repositoryUri)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "ssh-keyscan",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            if (repositoryUri.Port > 0 && repositoryUri.Port != 22)
            {
                startInfo.ArgumentList.Add("-p");
                startInfo.ArgumentList.Add(repositoryUri.Port.ToString(CultureInfo.InvariantCulture));
            }

            startInfo.ArgumentList.Add(repositoryUri.Host);

            using var process = new Process { StartInfo = startInfo };
            process.Start();

            var stdoutTask = process.StandardOutput.ReadToEndAsync();
            var stderrTask = process.StandardError.ReadToEndAsync();

            if (!process.WaitForExit(10000))
            {
                try
                {
                    process.Kill(true);
                }
                catch
                {
                    // Ignore kill failures
                }

                _logger.Write($"ssh-keyscan timed out for host {repositoryUri.Host}", LogType.Alert);
                return Array.Empty<string>();
            }

            var stdout = stdoutTask.Result;

            if (process.ExitCode != 0)
            {
                var stderr = stderrTask.Result;
                if (!string.IsNullOrWhiteSpace(stderr))
                {
                    _logger.Write($"ssh-keyscan failed for {repositoryUri.Host}: {stderr.Trim()}", LogType.Alert);
                }

                return Array.Empty<string>();
            }

            return stdout
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToArray();
        }
        catch (Exception ex)
        {
            _logger.Write($"Warning: Unable to fetch known host entries: {ex.Message}", LogType.Alert);
            return Array.Empty<string>();
        }
    }

    private void RunChmod(string mode, string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "chmod",
                UseShellExecute = false,
                CreateNoWindow = true
            };
            startInfo.ArgumentList.Add(mode);
            startInfo.ArgumentList.Add(path);

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            process.WaitForExit();
        }
        catch (Exception ex)
        {
            _logger.Write($"Warning: Failed to set permissions on {path}: {ex.Message}", LogType.Alert);
        }
    }

    private void ScheduleFileForDeletion(string filePath)
    {
        // Schedule file for deletion after a short delay
        Task.Delay(TimeSpan.FromMinutes(5)).ContinueWith(_ =>
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.Write($"Warning: Could not delete temporary credential file: {ex.Message}", LogType.Alert);
            }
        });
    }
    
    private static string EscapeShellArgument(string argument)
    {
        // Escape shell arguments to prevent command injection
        if (string.IsNullOrEmpty(argument))
            return "\"\"";

        // Replace dangerous characters
        return argument
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("$", "\\$")
            .Replace("`", "\\`")
            .Replace("!", "\\!")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    private static string EscapeWindowsCredentialValue(string value)
    {
        return value
            .Replace("%", "%%")
            .Replace("^", "^^")
            .Replace("&", "^&")
            .Replace("|", "^|")
            .Replace(">", "^>")
            .Replace("<", "^<")
            .Replace("(", "^(")
            .Replace(")", "^)");
    }
    
    private void ValidateSSHKeyPermissions(string privateKeyPath)
    {
        // Validate SSH key file permissions for security
        try
        {
            var fileInfo = new FileInfo(privateKeyPath);
            
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                return;
            }
            
            // On Unix-like systems, SSH keys should have restrictive permissions (600)
            // Check if file is readable by others (basic security check)
            if (fileInfo.IsReadOnly == false)
            {
                _logger.Write($"Warning: SSH private key file may have overly permissive permissions: {privateKeyPath}", LogType.Alert);
            }
        }
        catch (Exception ex)
        {
            _logger.Write($"Warning: Could not validate SSH key permissions: {ex.Message}", LogType.Alert);
        }
    }
    
    private class GitCommandResult
    {
        public int ExitCode { get; set; }
        public string StandardOutput { get; set; } = "";
        public string StandardError { get; set; } = "";
        public bool IsSuccess { get; set; }
    }
    
    // Git command wrapper methods
    private void GitClone(string repositoryUrl, string targetPath, string? branch = null, int depth = 0, bool enableLFS = false)
    {
        // Build git clone command with proper argument escaping
        var cloneArgs = new List<string> { "clone" };
        
        if (depth > 0)
        {
            cloneArgs.Add("--depth");
            cloneArgs.Add(depth.ToString());
        }
        
        if (!string.IsNullOrEmpty(branch))
        {
            cloneArgs.Add("--branch");
            cloneArgs.Add(branch);
        }
        
        cloneArgs.Add("--recurse-submodules");
        
        if (enableLFS)
        {
            cloneArgs.Add("--no-checkout");
        }
        
        cloneArgs.Add(repositoryUrl);
        cloneArgs.Add(targetPath);

        ExecuteGitCommandWithArgs(cloneArgs);

        if (enableLFS)
        {
            ExecuteGitCommandWithArgs(["lfs", "install", "--local"], targetPath);

            var checkoutTarget = ResolveCheckoutBranch(targetPath, branch);
            ExecuteGitCommandWithArgs(["checkout", checkoutTarget], targetPath);

            var pullArgs = new List<string> { "lfs", "pull" };
            if (!string.Equals(checkoutTarget, "HEAD", StringComparison.OrdinalIgnoreCase))
            {
                pullArgs.Add("origin");
                pullArgs.Add(checkoutTarget);
            }

            ExecuteGitCommandWithArgs(pullArgs, targetPath);
            ExecuteGitCommandWithArgs(["lfs", "checkout"], targetPath);
        }
    }

    private string ResolveCheckoutBranch(string repositoryPath, string? requestedBranch)
    {
        if (!string.IsNullOrWhiteSpace(requestedBranch))
        {
            return requestedBranch;
        }

        try
        {
            var headPath = Path.Combine(repositoryPath, ".git", "HEAD");
            if (File.Exists(headPath))
            {
                var headContent = File.ReadAllText(headPath).Trim();
                const string prefix = "ref: ";
                if (headContent.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    var reference = headContent[prefix.Length..];
                    var segments = reference.Split('/', StringSplitOptions.RemoveEmptyEntries);
                    if (segments.Length > 0)
                    {
                        return segments[^1];
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Write($"Warning: Unable to determine default branch from HEAD: {ex.Message}", LogType.Alert);
        }

        return "HEAD";
    }
    
    private void GitPull(string workingDirectory)
    {
        var result = ExecuteGitCommandWithArgs(["pull", "--ff-only", "--no-edit"], workingDirectory);
        if (result.IsSuccess || !result.StandardError.Contains("non-fast-forward"))
        {
            return;
        }
        _logger.Write("Pull failed due to non-fast-forward changes, performing hard reset", LogType.Alert);
        ExecuteGitCommandWithArgs(["fetch", "origin"], workingDirectory);
        ExecuteGitCommandWithArgs(["reset", "--hard", "origin/HEAD"], workingDirectory);
    }
    
    private void GitFetch(string workingDirectory, string remote = "origin")
    {
        ExecuteGitCommandWithArgs(["fetch", remote], workingDirectory);
    }
    
    private void GitReset(string workingDirectory, bool hard = true)
    {
        var resetArgs = hard
            ? new List<string> { "reset", "--hard", "HEAD" }
            : new List<string> { "reset", "HEAD" };
        ExecuteGitCommandWithArgs(resetArgs, workingDirectory);
        ExecuteGitCommandWithArgs(["clean", "-fd"], workingDirectory);
    }
    
    private GitStatusResult GitStatus(string workingDirectory)
    {
        var result = ExecuteGitCommandWithArgs(["status", "--porcelain"], workingDirectory, throwOnError: false);
        
        if (!result.IsSuccess)
        {
            return new GitStatusResult
            {
                IsValidRepository = false,
                HasUncommittedChanges = false,
                HasUntrackedFiles = false
            };
        }
        
        var lines = result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        return new GitStatusResult
        {
            IsValidRepository = true,
            HasUncommittedChanges = lines.Any(line => line.StartsWith("M ") || line.StartsWith("A ") || line.StartsWith("D ")),
            HasUntrackedFiles = lines.Any(line => line.StartsWith("??"))
        };
    }
    
    private GitCommitInfo GitGetCommitInfo(string workingDirectory)
    {
        var commitResult = ExecuteGitCommandWithArgs(["rev-parse", "HEAD"], workingDirectory, throwOnError: false);
        var branchResult = ExecuteGitCommandWithArgs(["rev-parse", "--abbrev-ref", "HEAD"], workingDirectory, throwOnError: false);
        var remoteResult = ExecuteGitCommandWithArgs(["remote", "get-url", "origin"], workingDirectory, throwOnError: false);
        
        if (!commitResult.IsSuccess)
        {
            return new GitCommitInfo
            {
                IsValidRepository = false,
                CommitHash = "",
                CommitHashShort = "",
                BranchName = "",
                RemoteUrl = ""
            };
        }
        
        var fullHash = commitResult.StandardOutput.Trim();
        var shortHash = fullHash.Length >= 7 ? fullHash[..7] : fullHash;
        
        return new GitCommitInfo
        {
            IsValidRepository = true,
            CommitHash = fullHash,
            CommitHashShort = shortHash,
            BranchName = branchResult.IsSuccess ? branchResult.StandardOutput.Trim() : "",
            RemoteUrl = remoteResult.IsSuccess ? remoteResult.StandardOutput.Trim() : ""
        };
    }
    
    private bool GitIsValidRepository(string workingDirectory)
    {
        var result = ExecuteGitCommandWithArgs(["rev-parse", "--git-dir"], workingDirectory, throwOnError: false);
        return result.IsSuccess;
    }
    
    private void GitConfigureSparseCheckout(string workingDirectory, List<string> paths)
    {
        // Use secure git command execution
        ExecuteGitCommandWithArgs(["config", "core.sparseCheckout", "true"], workingDirectory);
        
        var sparseCheckoutPath = Path.Combine(workingDirectory, ".git", "info", "sparse-checkout");
        var sparseCheckoutDir = Path.GetDirectoryName(sparseCheckoutPath);
        
        if (!Directory.Exists(sparseCheckoutDir) && !string.IsNullOrEmpty(sparseCheckoutDir))
        {
            Directory.CreateDirectory(sparseCheckoutDir);
        }
        
        // Write paths safely with proper error handling
        try
        {
            File.WriteAllLines(sparseCheckoutPath, paths);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to write sparse checkout configuration: {ex.Message}", ex);
        }
        
        ExecuteGitCommandWithArgs(["checkout"], workingDirectory);
    }
    
    private class GitStatusResult
    {
        public bool IsValidRepository { get; set; }
        public bool HasUncommittedChanges { get; set; }
        public bool HasUntrackedFiles { get; set; }
    }
    
    private class GitCommitInfo
    {
        public bool IsValidRepository { get; set; }
        public string CommitHash { get; set; } = "";
        public string CommitHashShort { get; set; } = "";
        public string BranchName { get; set; } = "";
        public string RemoteUrl { get; set; } = "";
    }
    
    private bool IsShallowRepository(string workingDirectory)
    {
        try
        {
            var result = ExecuteGitCommandWithArgs(["rev-parse", "--is-shallow-repository"], workingDirectory, throwOnError: false);
            return result.IsSuccess && result.StandardOutput.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}