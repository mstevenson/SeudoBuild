# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with the SeudoCI.Core project.

## Project Overview

SeudoCI.Core is the foundational library of the SeudoCI build system, providing essential infrastructure components used across all other projects. It implements cross-platform abstractions for file systems, logging, workspace management, macro expansion, and JSON serialization. This library ensures consistent behavior and platform compatibility throughout the SeudoCI ecosystem.

## Build Commands

- **Build core library**: `dotnet build SeudoCI.Core/SeudoCI.Core.csproj`
- **Clean build artifacts**: `dotnet clean SeudoCI.Core/SeudoCI.Core.csproj`
- **Run tests**: `dotnet test` (when test projects reference this library)

## Architecture Overview

SeudoCI.Core provides six main areas of functionality:

1. **Cross-platform file system abstraction**
2. **Structured console logging with formatting**
3. **Hierarchical workspace management**
4. **Dynamic macro expansion system**
5. **Platform detection utilities**
6. **JSON serialization with custom converters**

## Core Components

### File System Abstraction

**Purpose**: Provides consistent file/directory operations across Windows, macOS, and Linux platforms.

#### IFileSystem Interface (from SeudoCI.Core.Shared)
Defines platform-agnostic file system operations for build processes.

#### WindowsFileSystem
**Location**: `FileSystems/WindowsFileSystem.cs`

**Key Features**:
- Standard Windows file system operations
- Documents path: `Environment.SpecialFolder.MyDocuments`
- Standard output: `"CON"` (Windows console)
- Comprehensive file/directory management
- Stream-based file access for large files

**File Operations**:
- `GetFiles()` / `GetDirectories()` with optional search patterns
- `FileExists()` / `DirectoryExists()` existence checking
- `MoveFile()` / `CopyFile()` / `DeleteFile()` file manipulation
- `ReplaceFile()` atomic file replacement
- `OpenRead()` / `OpenWrite()` stream access

#### MacFileSystem
**Location**: `FileSystems/MacFileSystem.cs`

**Key Features**:
- Inherits from WindowsFileSystem (most operations are identical)
- macOS-specific standard output: `"/dev/stdout"`
- Documents path: `base.DocumentsPath + "/Documents"`
- Handles macOS directory structure conventions

### Logging System

**Location**: `Logger.cs`

**Purpose**: Provides structured, colorized console logging with indentation support for build process visualization.

#### Features
- **Hierarchical indentation**: `IndentLevel` property controls nesting
- **ANSI color support**: Terminal escape sequences for formatting
- **Multiple log types**: Header, Bullet, Success, Failure, Alert, Debug
- **Text styling**: Bold, Dim, Underline, Invert formatting options
- **Queue notifications**: Special formatting for build queue events

#### Log Types and Visual Output
```
Header: Bold text for section headers
Plus: ‣ Project information and metadata
Bullet: • Primary build steps and actions
SmallBullet: ◦ Sub-steps and detailed operations
Success: ✔ Green checkmark for successful operations
Failure: ✘ Red X for failed operations
Alert: ! Yellow warning messages
Debug: Magenta debug information
```

#### Usage Pattern
```csharp
logger.Write("Starting Build", LogType.Header);
logger.IndentLevel++;
logger.Write("Loading project config", LogType.Bullet);
logger.Write("Configuration loaded successfully", LogType.Success);
logger.IndentLevel--;
```

### Workspace Management

**Purpose**: Organizes build artifacts into standardized directory structures for projects and build targets.

#### ProjectWorkspace
**Location**: `ProjectWorkspace.cs`

**Directory Structure**:
```
MyProject/                  // Project workspace root
├── ProjectConfig.json      // Project configuration
├── Logs/                   // Project-level logs
└── Targets/               // Build target workspaces
    ├── Target_A/          // Individual target workspace
    │   ├── Workspace/     // Source files (TargetDirectory.Source)
    │   ├── Output/        // Build products (TargetDirectory.Output)
    │   ├── Archives/      // Packaged builds (TargetDirectory.Archives)
    │   └── Logs/          // Target-specific logs (TargetDirectory.Logs)
    └── Target_B/          // Another target workspace
```

**Key Methods**:
- `InitializeDirectories()` - Creates project directory structure
- `CreateTarget(string targetName)` - Creates and returns target workspace
- `GetDirectory(ProjectDirectory)` - Resolves directory paths
- `CleanDirectory(ProjectDirectory)` - Removes directory contents

#### TargetWorkspace
**Location**: `TargetWorkspace.cs`

**Purpose**: Manages build artifacts for individual build targets with automatic macro registration.

**Built-in Macros**:
- `%working_directory%` - Source directory path
- `%build_output_directory%` - Output directory path
- `%archives_directory%` - Archives directory path
- `%logs_directory%` - Logs directory path

**Standard Macros** (populated by pipeline):
- `%project_name%` - Project identifier
- `%build_target_name%` - Target identifier
- `%app_version%` - Version as major.minor.patch
- `%build_date%` - Build completion timestamp
- `%commit_identifier%` - Source control commit hash

### Macro System

**Location**: `Macros.cs`

**Purpose**: Dynamic text replacement for build configurations, supporting parameterized builds and environment-specific values.

#### Implementation
- Extends `Dictionary<string, string>` for key-value storage
- Macro syntax: `%variable_name%` delimited by percent signs
- **Replacement logic**: Known macros replaced with values, unknown macros removed
- **Regex cleanup**: Removes unmatched macro patterns to prevent configuration errors

#### Usage Pattern
```csharp
macros["project_name"] = "MyGame";
macros["commit_id"] = "abc123f";
string result = macros.ReplaceVariablesInText("Build %project_name% from %commit_id%");
// Result: "Build MyGame from abc123f"
```

### Platform Detection

**Location**: `PlatformUtils.cs`

**Purpose**: Reliable cross-platform detection for file system and build tool selection.

#### Detection Logic
- **Windows**: Default case (most common)
- **macOS**: Checks for macOS-specific directories (`/Applications`, `/System`, `/Users`, `/Volumes`)
- **Linux**: Unix platform without macOS directories
- **Handles**: .NET's incorrect macOS detection as Unix

#### Usage
```csharp
IFileSystem fileSystem = PlatformUtils.RunningPlatform == Platform.Windows 
    ? new WindowsFileSystem() 
    : new MacFileSystem();
```

### JSON Serialization

**Location**: `Serializer.cs`

**Purpose**: Standardized JSON serialization for configuration files with custom converter support.

#### Features
- **Newtonsoft.Json integration** with indented formatting
- **Custom converter support** for complex module configurations
- **File system abstraction** for cross-platform file access
- **Stream-based I/O** for large configuration files
- **Safe file operations** with automatic cleanup

#### Methods
- `Serialize<T>(T obj)` - Object to JSON string
- `SerializeToFile<T>(T obj, string path)` - Object to JSON file
- `Deserialize<T>(string json, JsonConverter[] converters)` - JSON string to object
- `DeserializeFromFile<T>(string path, JsonConverter[] converters)` - JSON file to object

## Dependencies

### External Packages
- **Newtonsoft.Json 13.0.3**: JSON serialization and deserialization

### Internal Dependencies
- **SeudoCI.Core.Shared**: Interfaces and enums (IFileSystem, ILogger, Platform, etc.)

## Development Patterns

### Cross-Platform File Operations
```csharp
public void ProcessFiles(IFileSystem fileSystem)
{
    if (fileSystem.DirectoryExists(sourcePath))
    {
        var files = fileSystem.GetFiles(sourcePath, "*.txt");
        foreach (var file in files)
        {
            using var stream = fileSystem.OpenRead(file);
            // Process file content
        }
    }
}
```

### Workspace Management
```csharp
public void SetupBuild(IProjectWorkspace project, string targetName)
{
    project.InitializeDirectories();
    var target = project.CreateTarget(targetName);
    target.CleanDirectory(TargetDirectory.Output);
    target.InitializeDirectories();
}
```

### Structured Logging
```csharp
public void LogBuildProcess(ILogger logger)
{
    logger.Write("Build Process", LogType.Header);
    logger.IndentLevel++;
    try
    {
        logger.Write("Compiling source", LogType.Bullet);
        // Build logic here
        logger.Write("Build completed", LogType.Success);
    }
    catch (Exception e)
    {
        logger.Write($"Build failed: {e.Message}", LogType.Failure);
    }
    finally
    {
        logger.IndentLevel--;
    }
}
```

## Design Principles

### Abstraction
All platform-specific operations are abstracted through interfaces, enabling testability and cross-platform support.

### Immutability
File paths and directory structures are determined at workspace creation, ensuring consistent build environments.

### Separation of Concerns
Each component has a single responsibility: logging, file operations, workspace management, or serialization.

### Error Handling
Graceful handling of missing directories, failed file operations, and platform detection edge cases.

### Performance
Stream-based file operations and efficient directory traversal for large projects and build artifacts.