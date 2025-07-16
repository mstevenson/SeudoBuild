# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with the SeudoCI.Pipeline.Modules.FolderArchive project.

## Project Overview

SeudoCI.Pipeline.Modules.FolderArchive is an archival module for the SeudoCI build system that packages build outputs by copying them to a named folder structure. This module implements the IArchiveModule interface and provides simple folder-based archiving without compression. It serves as the third phase in the build pipeline, organizing build artifacts into a structured directory hierarchy for distribution.

## Build Commands

- **Build folder archive module**: `dotnet build SeudoCI.Pipeline.Modules.FolderArchive/SeudoCI.Pipeline.Modules.FolderArchive.csproj`
- **Clean build artifacts**: `dotnet clean SeudoCI.Pipeline.Modules.FolderArchive/SeudoCI.Pipeline.Modules.FolderArchive.csproj`
- **Run tests**: `dotnet test` (when test projects reference this module)

## Architecture Overview

The FolderArchive module follows the standard SeudoCI module pattern with three main components:

1. **FolderArchiveModule**: Module registration and metadata provider
2. **FolderArchiveStep**: Folder copying implementation and directory management
3. **FolderArchiveConfig**: Configuration schema (in companion Shared project)

## Core Components

### FolderArchiveModule

**Location**: `FolderArchiveModule.cs`

**Purpose**: Registers the folder archive module with the SeudoCI pipeline system and provides module metadata.

#### Module Registration Properties
```csharp
public string Name => "Folder";
public Type StepType { get; } = typeof(FolderArchiveStep);
public Type StepConfigType { get; } = typeof(FolderArchiveConfig);
public string StepConfigName => "Folder";
```

#### Key Features
- **Module Identity**: Provides "Folder" as the module name for discovery
- **Type Mapping**: Links configuration type to implementation type
- **Pipeline Integration**: Enables automatic module loading and registration

### FolderArchiveStep

**Location**: `FolderArchiveStep.cs`

**Purpose**: Implements folder-based archiving by recursively copying build outputs to a named directory structure.

#### Key Features
- **Recursive Directory Copying**: Deep copy of entire directory trees
- **Macro Variable Support**: Expands project and build variables in folder names
- **Cross-Platform File Operations**: Uses IFileSystem abstraction for platform compatibility
- **Error Handling**: Graceful handling of file system operation failures
- **Directory Management**: Automatic cleanup and creation of archive directories

#### Core Methods

**`Initialize(FolderArchiveConfig config, ITargetWorkspace workspace, ILogger logger)`**
- Stores configuration, workspace, and logger references for step execution
- Prepares module for folder archiving operations

**`ExecuteStep(BuildSequenceResults buildInfo, ITargetWorkspace workspace)`**
- Main execution method called by pipeline runner
- Expands macro variables in target folder name
- Removes existing archive directory if present
- Copies entire build output directory to archive location
- Returns success/failure status with exception details

#### Directory Management Process
```csharp
string folderName = workspace.Macros.ReplaceVariablesInText(_config.FolderName);
string source = workspace.GetDirectory(TargetDirectory.Output);
string dest = $"{workspace.GetDirectory(TargetDirectory.Archives)}/{folderName}";
```

**Source Directory**: `Workspace/Output/` (build artifacts)
**Destination Directory**: `Workspace/Archives/{FolderName}/` (organized archive)

#### Recursive Copy Implementation

**`CopyDirectory(string sourceDir, string destDir)`**
- Validates source directory existence
- Creates destination directory (fails if already exists)
- Copies all files in source directory to destination
- Recursively processes all subdirectories
- Maintains original directory structure and file names

#### File System Operations
```csharp
// Directory operations
fileSystem.DirectoryExists(path)
fileSystem.CreateDirectory(path)
fileSystem.DeleteDirectory(path)
fileSystem.GetDirectories(path)

// File operations  
fileSystem.GetFiles(path)
fileSystem.CopyFile(source, dest)
```

#### Error Handling Strategy
- **Source Directory Missing**: Throws DirectoryNotFoundException with descriptive message
- **Destination Already Exists**: Throws Exception to prevent accidental overwrites
- **File Copy Failures**: Propagated to caller with full exception details
- **General Exceptions**: Wrapped in ArchiveStepResults with failure status

### FolderArchiveConfig

**Location**: `SeudoCI.Pipeline.Modules.FolderArchive.Shared/FolderArchiveConfig.cs`

**Purpose**: Defines configuration schema for folder archiving settings.

#### Configuration Properties

**Archive Settings**
- `FolderName`: Target folder name for archive (supports macro variables)

#### Configuration Schema
```csharp
public class FolderArchiveConfig : ArchiveStepConfig
{
    public override string Name => "Folder";
    public string FolderName { get; set; }
}
```

#### Example Configuration
```json
{
  "Type": "Folder",
  "FolderName": "%project_name%_%build_target_name%_%build_date%"
}
```

## Pipeline Integration

### Execution Context
FolderArchive executes as the third phase of the SeudoCI pipeline:
```
Source → Build → **Archive** → Distribute → Notify
```

### Input Requirements
- **BuildSequenceResults**: Results from build phase containing:
  - Build completion status
  - Build artifact information
  - Success/failure details

### Output Generation
- **ArchiveStepResults**: Archive execution results containing:
  - `ArchiveFileName`: Name of created archive folder
  - `IsSuccess`: Success/failure status
  - `Exception`: Error details (if failed)

### Workspace Directory Structure

#### Before Archive Execution
```
Workspace/
├── Output/           # Build artifacts (source)
│   ├── game.exe
│   ├── data/
│   └── config/
└── Archives/         # Archive destination (empty)
```

#### After Archive Execution
```
Workspace/
├── Output/           # Build artifacts (unchanged)
│   ├── game.exe
│   ├── data/
│   └── config/
└── Archives/
    └── MyGame_Release_2024-01-15--14-30-22/  # Created folder archive
        ├── game.exe
        ├── data/
        └── config/
```

### Macro Variable Expansion
The module automatically expands standard SeudoCI macro variables in folder names:
- `%project_name%` - Project identifier
- `%build_target_name%` - Build target name
- `%build_date%` - Build timestamp
- `%app_version%` - Application version
- `%commit_id%` - Source control commit hash

## Dependencies

### External Dependencies
**None** - This module uses only .NET framework functionality for maximum compatibility and minimal deployment complexity.

### Internal Dependencies
- **SeudoCI.Pipeline.Shared**: Pipeline interfaces and base classes
- **SeudoCI.Pipeline.Modules.FolderArchive.Shared**: Configuration types
- **SeudoCI.Core**: File system abstractions and workspace management

### Framework Dependencies
- **.NET 8.0**: Target framework
- **System.IO**: File and directory operations
- **System namespaces**: Basic .NET functionality

## Configuration Examples

### Basic Folder Archive
```json
{
  "Type": "Folder",
  "FolderName": "Release_Build"
}
```

### Timestamped Archive
```json
{
  "Type": "Folder", 
  "FolderName": "%project_name%_%build_date%"
}
```

### Version-Based Archive
```json
{
  "Type": "Folder",
  "FolderName": "%project_name%_v%app_version%"
}
```

### Complete Archive Name
```json
{
  "Type": "Folder",
  "FolderName": "%project_name%_%build_target_name%_%app_version%_%commit_id%"
}
```

### Multiple Archive Steps
```json
{
  "ArchiveSteps": [
    {
      "Type": "Folder",
      "FolderName": "Latest_Build"
    },
    {
      "Type": "Folder", 
      "FolderName": "Backup_%build_date%"
    }
  ]
}
```

## Common Issues and Solutions

### Directory Already Exists
- **Issue**: "Destination path already exists" errors
- **Cause**: Previous archive with same name not cleaned up
- **Solution**: Module automatically deletes existing directory before copying
- **Prevention**: Use timestamp or version variables to ensure unique names

### Source Directory Missing
- **Issue**: "Source directory does not exist" errors
- **Cause**: Build phase failed or produced no output
- **Solution**: Verify build steps completed successfully and produced artifacts

### Permission Errors
- **Issue**: File system permission denied errors
- **Cause**: Insufficient rights to create/delete directories
- **Solution**: Ensure build agent has write permissions to workspace directory

### Large Directory Trees
- **Issue**: Long execution times for large build outputs
- **Cause**: Recursive copying of many files/directories
- **Consideration**: Monitor performance for very large builds

## Security Considerations

### File System Access
- **Directory Traversal**: Uses workspace-relative paths to prevent escape
- **Permission Requirements**: Requires read access to Output directory, write access to Archives directory
- **Path Validation**: Relies on workspace abstraction for path security

### Archive Integrity
- **Overwrite Protection**: Prevents accidental overwrites by failing if destination exists
- **Atomic Operations**: Copy operation is not atomic; interruption may leave partial archives
- **No Compression**: Files copied as-is without modification or validation

## Development Patterns

### Custom Folder Naming
```csharp
// Enhanced folder name generation
private string GenerateFolderName()
{
    var template = _config.FolderName ?? DefaultTemplate;
    var expanded = _workspace.Macros.ReplaceVariablesInText(template);
    return SanitizeFolderName(expanded);
}
```

### Incremental Copy Support
```csharp
// Skip unchanged files optimization
private void CopyFileIfNewer(string source, string dest)
{
    if (!File.Exists(dest) || File.GetLastWriteTime(source) > File.GetLastWriteTime(dest))
    {
        fileSystem.CopyFile(source, dest);
    }
}
```

### Progress Reporting
```csharp
// File copy progress tracking
private void CopyDirectoryWithProgress(string sourceDir, string destDir)
{
    var totalFiles = CountFiles(sourceDir);
    var copiedFiles = 0;
    
    // Copy with progress updates
    foreach (var file in files)
    {
        CopyFile(file);
        copiedFiles++;
        ReportProgress(copiedFiles, totalFiles);
    }
}
```

### Error Handling Best Practices
- Always return ArchiveStepResults with appropriate success/failure status
- Log file system errors at appropriate levels for debugging
- Provide actionable error messages for configuration and runtime issues
- Handle partial copy scenarios gracefully

## Performance Considerations

### Copy Operation Optimization
- **Single-Threaded**: Current implementation copies files sequentially
- **Memory Usage**: Copies files individually (no bulk memory operations)
- **Disk I/O**: Direct file system copies without buffering optimization

### Large Archive Handling
- **Directory Scanning**: Recursive directory enumeration may be slow for deep trees
- **File Count**: No optimization for directories with many small files
- **Progress Feedback**: No progress reporting for long-running operations

### Memory Management
- **File Handles**: Proper cleanup of file system resources
- **Path Strings**: Efficient path manipulation using Path.Combine
- **Directory Structures**: No caching of directory information

## Comparison with Other Archive Modules

### FolderArchive vs ZipArchive
- **Compression**: FolderArchive provides no compression, ZipArchive compresses files
- **Access Speed**: Folder structure allows immediate file access, ZIP requires extraction
- **Size**: Folder archives larger, ZIP archives more compact
- **Compatibility**: Folders universally accessible, ZIP requires extraction tools

### Use Case Recommendations
- **FolderArchive**: Best for local builds, development testing, immediate file access
- **ZipArchive**: Better for distribution, network transfer, long-term storage

## Future Enhancement Opportunities

1. **Progress Reporting**: Add file copy progress callbacks for long operations
2. **Incremental Copy**: Skip unchanged files to improve performance
3. **Symbolic Link Support**: Handle symbolic links appropriately
4. **Archive Validation**: Verify copy completeness and integrity
5. **Custom Filters**: Support file/directory exclusion patterns
6. **Parallel Copy**: Multi-threaded copying for performance improvement
7. **Compression Options**: Optional compression within folder structure
8. **Archive Metadata**: Include build information and manifest files