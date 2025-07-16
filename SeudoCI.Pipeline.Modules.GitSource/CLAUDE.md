# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with the SeudoCI.Pipeline.Modules.GitSource project.

## Project Overview

SeudoCI.Pipeline.Modules.GitSource is a source control module for the SeudoCI build system that provides Git repository integration with support for cloning, updating, and managing source code. This module implements the ISourceModule interface and provides comprehensive Git functionality including authentication, branching, submodules, and Git LFS (Large File Storage) support. It serves as the first phase in the build pipeline, downloading and updating source code from Git repositories.

## Build Commands

- **Build Git source module**: `dotnet build SeudoCI.Pipeline.Modules.GitSource/SeudoCI.Pipeline.Modules.GitSource.csproj`
- **Clean build artifacts**: `dotnet clean SeudoCI.Pipeline.Modules.GitSource/SeudoCI.Pipeline.Modules.GitSource.csproj`
- **Run tests**: `dotnet test` (when test projects reference this module)

## Architecture Overview

The GitSource module follows the standard SeudoCI module pattern with four main components:

1. **GitSourceModule**: Module registration and metadata provider
2. **GitSourceStep**: Git operations implementation and repository management
3. **LFSFilter**: Custom Git LFS filter for large file handling
4. **GitSourceConfig**: Configuration schema (in companion Shared project)

## Core Components

### GitSourceModule

**Location**: `GitSourceModule.cs`

**Purpose**: Registers the Git source module with the SeudoCI pipeline system and provides module metadata.

#### Module Registration Properties
```csharp
public string Name => "Git";
public Type StepType { get; } = typeof(GitSourceStep);
public Type StepConfigType { get; } = typeof(GitSourceConfig);
public string StepConfigName => "Git";
```

#### Key Features
- **Module Identity**: Provides "Git" as the module name for discovery
- **Type Mapping**: Links configuration type to implementation type
- **Pipeline Integration**: Enables automatic module loading and registration

### GitSourceStep

**Location**: `GitSourceStep.cs`

**Purpose**: Implements comprehensive Git repository operations including cloning, pulling, authentication, and LFS support using LibGit2Sharp.

#### Key Features
- **Repository Lifecycle Management**: Clone, update, and validate Git repositories
- **Authentication Support**: Username/password authentication with credential handling
- **Branch Management**: Support for specific branch checkout and tracking
- **Submodule Support**: Recursive submodule cloning and updates
- **Git LFS Integration**: Large file storage with custom filter implementation
- **Conflict Resolution**: Automatic conflict resolution favoring remote changes
- **Working Copy Management**: Repository state validation and cleanup

#### Core Methods

**`Initialize(GitSourceConfig config, ITargetWorkspace workspace, ILogger logger)`**
- Stores configuration, workspace, and logger references
- Sets up authentication credentials and handlers
- Configures Git LFS filter if enabled
- Prepares module for Git operations

**`ExecuteStep(ITargetWorkspace workspace)`**
- Main execution method called by pipeline runner
- Determines whether to clone new repository or update existing one
- Handles both initial download and incremental updates
- Returns source step results with commit information

#### Repository State Management

**`IsWorkingCopyInitialized`**
```csharp
public bool IsWorkingCopyInitialized => Repository.IsValid(_workspace.GetDirectory(TargetDirectory.Source));
```
- Validates if source directory contains a valid Git repository
- Used to determine whether to clone or update

**`CurrentCommit` and `CurrentCommitShortHash`**
- Retrieves current HEAD commit SHA from repository
- Provides short hash (7 characters) for build identification
- Used for build artifact tagging and traceability

#### Clone Operations (`Download()`)

**Standard Git Clone**
```csharp
var cloneOptions = new CloneOptions
{
    CredentialsProvider = _credentialsHandler,
    BranchName = string.IsNullOrEmpty(_config.RepositoryBranchName) ? "master" : _config.RepositoryBranchName,
    Checkout = true,
    RecurseSubmodules = true
};
Repository.Clone(_config.RepositoryURL, _workspace.GetDirectory(TargetDirectory.Source), cloneOptions);
```

**LFS-Enabled Clone**
- Uses external `git-lfs` process for LFS repository cloning
- Embeds credentials directly in URL (security concern noted in code)
- Bypasses LibGit2Sharp for LFS operations due to complexity

#### Update Operations (`Update()`)

**Repository Cleanup**
```csharp
repo.Reset(ResetMode.Hard);
repo.RemoveUntrackedFiles();
```
- Hard reset to remove local changes
- Removes untracked files to ensure clean state

**URL Change Detection**
```csharp
if (repo.Network.Remotes[repo.Head.RemoteName].Url != _config.RepositoryURL)
{
    Download(); // Clone new repository
    return;
}
```
- Detects when repository URL has changed in configuration
- Automatically re-clones from new URL

**Pull and Merge Strategy**
```csharp
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
```
- **Fast-Forward Only**: Prevents complex merge scenarios
- **Favor Remote**: Always accept remote changes in conflicts
- **No Conflict Failures**: Continues build even with merge conflicts

#### Authentication System

**Credential Setup**
```csharp
_credentials = new UsernamePasswordCredentials
{
    Username = config.Username,
    Password = config.Password
};
_credentialsHandler = (url, usernameFromUrl, types) => 
    new UsernamePasswordCredentials { Username = config.Username, Password = config.Password };
```
- Uses basic username/password authentication
- Supports credential callbacks for LibGit2Sharp operations
- No OAuth or SSH key support

**Credential Security Issues**
```csharp
// FIXME extremely insecure to include password in the URL
string repoUrlWithPassword = $"{uri.Scheme}://{username}:{password}@{uri.Host}:{uri.Port}{uri.AbsolutePath}";
```
- Embeds passwords in URLs for LFS operations
- Creates temporary credential files on disk
- No secure credential storage mechanism

### LFSFilter

**Location**: `LFSFilter.cs`

**Purpose**: Custom LibGit2Sharp filter that integrates Git LFS operations for large file handling.

#### Key Features
- **Process Integration**: Spawns `git-lfs` processes for clean/smudge operations
- **Stream Processing**: Handles file data streaming between Git and LFS
- **Mode Support**: Supports both clean (upload) and smudge (download) operations
- **Working Directory Management**: Operates within repository context

#### Filter Operations

**Filter Creation**
```csharp
protected override void Create(string path, string root, FilterMode mode)
{
    string modeArg = mode == FilterMode.Clean ? "clean" : "smudge";
    var startInfo = new ProcessStartInfo
    {
        FileName = "git-lfs",
        Arguments = $"{modeArg} {path}",
        WorkingDirectory = _repoPath,
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        CreateNoWindow = true,
        UseShellExecute = false
    };
    _process = new Process { StartInfo = startInfo };
    _process.Start();
}
```

**Clean Operation (Upload to LFS)**
```csharp
protected override void Clean(string path, string root, Stream input, Stream output)
{
    // Write file data to stdin
    input.CopyTo(_process.StandardInput.BaseStream);
    input.Flush();
}
```
- Converts large files to LFS pointer files
- Uploads actual content to LFS storage

**Smudge Operation (Download from LFS)**
```csharp
protected override void Smudge(string path, string root, Stream input, Stream output)
{
    // Write git-lfs pointer to stdin
    input.CopyTo(_process.StandardInput.BaseStream);
    input.Flush();
}
```
- Converts LFS pointer files to actual file content
- Downloads content from LFS storage

#### LFS Command Execution

**`ExecuteLFSCommand(string arguments)`**
- Executes git-lfs commands within repository context
- Handles fetch and checkout operations for LFS files
- Provides visual feedback with console color changes

### GitSourceConfig

**Location**: `SeudoCI.Pipeline.Modules.GitSource.Shared/GitSourceConfig.cs`

**Purpose**: Defines configuration schema for Git repository settings.

#### Configuration Properties

**Repository Settings**
- `RepositoryURL`: Git repository URL (HTTP/HTTPS/SSH)
- `RepositoryBranchName`: Target branch (defaults to "master")

**Authentication**
- `Username`: Git authentication username
- `Password`: Git authentication password

**Features**
- `UseLFS`: Enable Git LFS support for large files

#### Configuration Schema
```csharp
public class GitSourceConfig : SourceStepConfig
{
    public override string Name => "Git";
    public string RepositoryURL { get; set; }
    public string RepositoryBranchName { get; set; }
    public bool UseLFS { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}
```

## Pipeline Integration

### Execution Context
GitSource executes as the first phase of the SeudoCI pipeline:
```
**Source** → Build → Archive → Distribute → Notify
```

### Input Requirements
- **No Previous Results**: Source phase initiates the pipeline
- **Clean Workspace**: Operates on empty or existing source directory

### Output Generation
- **SourceStepResults**: Source execution results containing:
  - `CommitIdentifier`: Short SHA hash of current commit
  - `IsSuccess`: Success/failure status
  - `Exception`: Error details (if failed)

### Workspace Directory Structure

#### After Source Execution
```
Workspace/
├── Source/           # Git repository (created/updated)
│   ├── .git/
│   ├── src/
│   ├── assets/
│   └── project files
├── Output/           # Empty (for build phase)
├── Archives/         # Empty (for archive phase)
└── Logs/            # Build logs
```

## Dependencies

### External Dependencies
- **LibGit2Sharp-SSH 1.0.22**: Git operations library with SSH support
  - Repository cloning and updating
  - Authentication and credential management
  - Branch and submodule operations
  - Custom filter support for LFS

### System Dependencies
- **git-lfs**: External Git LFS binary required for LFS operations
  - Must be installed and available in system PATH
  - Used for LFS clone, fetch, and checkout operations

### Internal Dependencies
- **SeudoCI.Pipeline.Shared**: Pipeline interfaces and base classes
- **SeudoCI.Pipeline.Modules.GitSource.Shared**: Configuration types
- **SeudoCI.Core**: Logging and workspace management

### Framework Dependencies
- **.NET 8.0**: Target framework
- **System.Diagnostics**: Process execution for LFS commands
- **System.IO**: File and stream operations

## Configuration Examples

### SSH Key Authentication (Not Currently Supported)
SSH authentication requires LibGit2Sharp to be compiled with SSH support, which is not included in the standard NuGet package.
For SSH authentication, configure your system Git with SSH keys and use the command-line git directly.

### Personal Access Token (Recommended for HTTPS)
```json
{
  "Type": "Git",
  "RepositoryURL": "https://github.com/company/project.git",
  "AuthenticationType": "PersonalAccessToken",
  "Username": "your-username",
  "PersonalAccessToken": "${GIT_TOKEN}"
}
```

### SSH Agent Authentication (Not Currently Supported)
SSH agent authentication requires LibGit2Sharp to be compiled with SSH support, which is not included in the standard NuGet package.

### Basic Authentication (Deprecated)
```json
{
  "Type": "Git",
  "RepositoryURL": "https://github.com/company/project.git",
  "AuthenticationType": "UsernamePassword",
  "Username": "build-user",
  "Password": "password"
}
```

### Personal Access Token with Specific Branch
```json
{
  "Type": "Git",
  "RepositoryURL": "https://github.com/company/project.git",
  "RepositoryBranchName": "development",
  "AuthenticationType": "PersonalAccessToken",
  "Username": "your-username",
  "PersonalAccessToken": "${GIT_TOKEN}"
}
```

### Git LFS with Personal Access Token
```json
{
  "Type": "Git",
  "RepositoryURL": "https://github.com/company/game-project.git",
  "UseLFS": true,
  "AuthenticationType": "PersonalAccessToken",
  "Username": "your-username",
  "PersonalAccessToken": "${GIT_TOKEN}"
}
```

### Private GitLab with Token
```json
{
  "Type": "Git",
  "RepositoryURL": "https://gitlab.company.com/internal/project.git",
  "RepositoryBranchName": "release/v2.0",
  "AuthenticationType": "PersonalAccessToken",
  "Username": "ci-bot",
  "PersonalAccessToken": "${GITLAB_TOKEN}"
}
```

## Known Issues and Limitations

### LibGit2Sharp Integration
- **Assembly Loading**: ModuleLoader has issues loading LibGit2Sharp dependencies
- **Platform Dependencies**: LibGit2Sharp requires platform-specific native libraries
- **DLL Resolution**: Complex dependency resolution for Git operations

### Security Concerns
- **Password in URLs**: LFS operations embed passwords in command-line URLs
- **Credential Storage**: Temporary credential files written to disk
- **No SSH Support**: Limited to username/password authentication
- **Plain Text Passwords**: Configuration stores passwords in clear text

### LFS Limitations
- **External Dependency**: Requires git-lfs binary installation
- **Process Spawning**: Relies on external processes for LFS operations
- **Error Handling**: Limited error reporting from LFS processes
- **Performance**: Additional overhead for LFS file operations

### Authentication Issues
- **OAuth Not Supported**: No support for modern OAuth authentication
- **SSH Keys Not Supported**: No SSH key-based authentication
- **Token Expiration**: No automatic token refresh mechanisms

## Development Patterns

### Enhanced Authentication
```csharp
// Support for SSH keys
public class GitSSHCredentials
{
    public string PrivateKeyPath { get; set; }
    public string PublicKeyPath { get; set; }
    public string Passphrase { get; set; }
}
```

### Secure Credential Management
```csharp
// Environment variable support
private string GetSecurePassword()
{
    return Environment.GetEnvironmentVariable("GIT_PASSWORD") ?? _config.Password;
}
```

### OAuth Integration
```csharp
// GitHub/GitLab OAuth support
public class GitOAuthCredentials
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
}
```

### Error Handling Best Practices
- Always return SourceStepResults with appropriate success/failure status
- Log Git errors at appropriate levels for debugging
- Provide actionable error messages for authentication and network issues
- Handle repository corruption gracefully with automatic re-cloning

## Performance Considerations

### Repository Size Optimization
- **Shallow Clones**: Consider `--depth=1` for large repositories
- **Sparse Checkout**: Support for partial repository checkout
- **LFS Optimization**: Selective LFS file downloading

### Network Efficiency
- **Incremental Updates**: Pull only changes since last build
- **Compression**: Git's built-in compression for network operations
- **Parallel Operations**: Concurrent submodule and LFS operations

### Disk Space Management
- **Repository Cleanup**: Automatic cleanup of old repositories
- **LFS Pruning**: Remove old LFS files to save space
- **Submodule Optimization**: Selective submodule updates

## Security Best Practices

### Credential Security
1. **Environment Variables**: Store credentials in environment variables
2. **Credential Managers**: Integrate with system credential managers
3. **Token-Based Auth**: Use personal access tokens instead of passwords
4. **SSH Keys**: Implement SSH key-based authentication

### Repository Security
1. **URL Validation**: Validate repository URLs to prevent injection
2. **Branch Restrictions**: Limit allowed branches for security
3. **Submodule Security**: Validate submodule sources
4. **LFS Security**: Validate LFS server authenticity

## Future Enhancement Opportunities

1. **SSH Key Support**: Implement SSH key-based authentication
2. **OAuth Integration**: Support for GitHub/GitLab OAuth
3. **Shallow Clone Support**: Reduce clone time and disk usage
4. **Sparse Checkout**: Partial repository checkout for large repos
5. **Credential Manager Integration**: System credential storage
6. **Repository Caching**: Shared repository cache across builds
7. **Advanced Merge Strategies**: Configurable conflict resolution
8. **Submodule Management**: Enhanced submodule configuration
9. **Performance Monitoring**: Track clone/update performance metrics
10. **Security Scanning**: Integration with security scanning tools