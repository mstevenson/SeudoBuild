# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with the SeudoCI.Agent.Shared project.

## Project Overview

SeudoCI.Agent.Shared is a shared library that defines the core interfaces, contracts, and data models used by the SeudoCI agent system. This project provides the foundational abstractions that enable communication and coordination between different components of the distributed build system, including agents, queues, and build execution engines.

## Build Commands

- **Build shared library**: `dotnet build SeudoCI.Agent.Shared/SeudoCI.Agent.Shared.csproj`
- **Clean build artifacts**: `dotnet clean SeudoCI.Agent.Shared/SeudoCI.Agent.Shared.csproj`
- **Run tests**: `dotnet test` (when tests are added)

## Project Structure

This is a minimal shared library containing only essential interfaces and models:

- **IBuilder.cs**: Build execution contract
- **IBuildQueue.cs**: Build queue management contract  
- **BuildResult.cs**: Build status and result data model
- **IReceiver.cs**: Message/event subscription contract

## Core Interfaces

### IBuilder
Defines the contract for build execution engines that process pipeline configurations.

**Key Members:**
- `bool IsRunning` - Indicates if a build is currently executing
- `bool Build(IPipelineRunner pipeline, ProjectConfig projectConfig, string target)` - Executes a build for specified project and target

**Usage Pattern:**
```csharp
public class MyBuilder : IBuilder
{
    public bool IsRunning { get; private set; }
    
    public bool Build(IPipelineRunner pipeline, ProjectConfig projectConfig, string target)
    {
        IsRunning = true;
        try
        {
            pipeline.ExecutePipeline(projectConfig, target, moduleLoader);
            return true;
        }
        finally
        {
            IsRunning = false;
        }
    }
}
```

### IBuildQueue
Defines the contract for managing build job queues and tracking build results.

**Key Members:**
- `BuildResult ActiveBuild` - Currently executing build (read-only)
- `BuildResult EnqueueBuild(ProjectConfig config, string target = null)` - Adds build to queue
- `IEnumerable<BuildResult> GetAllBuildResults()` - Retrieves all build history
- `BuildResult GetBuildResult(int buildId)` - Gets specific build status
- `BuildResult CancelBuild(int buildId)` - Cancels queued or running build

**Usage Pattern:**
```csharp
var queue = container.Resolve<IBuildQueue>();
var buildResult = queue.EnqueueBuild(projectConfig, "Release");
Console.WriteLine($"Build {buildResult.Id} queued for project {buildResult.ProjectConfiguration.ProjectName}");
```

### IReceiver
Minimal contract for subscription-based message handling (likely for future extensibility).

**Key Members:**
- `void Subscribe()` - Establishes subscription to events or messages

**Purpose:**
- Provides foundation for event-driven communication
- Enables loose coupling between system components
- Currently minimal implementation suggesting future expansion

## Data Models

### BuildResult
Represents the state and metadata of a build job throughout its lifecycle.

**Properties:**
- `int Id` - Unique build identifier (immutable)
- `ProjectConfig ProjectConfiguration` - Build configuration (immutable)
- `string TargetName` - Specific build target name (immutable)
- `Status BuildStatus` - Current build state (mutable)

**Status Enumeration:**
- `Queued` - Build submitted but not started
- `Complete` - Build finished successfully
- `Failed` - Build completed with errors
- `Cancelled` - Build stopped by user request

**Design Notes:**
- Uses `init` setters for immutable properties
- Status is the only mutable property after creation
- Integrates with SeudoCI.Pipeline.Shared for ProjectConfig

## Dependencies

- **SeudoCI.Pipeline.Shared**: Provides ProjectConfig and IPipelineRunner types
- **.NET 8.0**: Target framework with modern C# language features
- **No external packages**: Keeps shared contracts lightweight

## Usage in SeudoCI System

### Agent Implementation
The main SeudoCI.Agent project implements these interfaces:
- `Builder` class implements `IBuilder`
- `BuildQueue` class implements `IBuildQueue`
- REST API uses `BuildResult` for JSON serialization

### Client Integration
Client applications reference this shared library to:
- Submit builds using `ProjectConfig` from REST API
- Monitor build status via `BuildResult` polling
- Implement custom queue management logic

### Testing and Mocking
These interfaces enable unit testing with mock implementations:
```csharp
var mockBuilder = new Mock<IBuilder>();
mockBuilder.Setup(b => b.Build(It.IsAny<IPipelineRunner>(), It.IsAny<ProjectConfig>(), It.IsAny<string>()))
          .Returns(true);
```

## Design Principles

### Single Responsibility
Each interface has a focused, well-defined responsibility:
- `IBuilder` - Build execution only
- `IBuildQueue` - Queue management only
- `IReceiver` - Message subscription only

### Immutability
`BuildResult` emphasizes immutable design with init-only properties for core data, allowing only status updates after creation.

### Minimal Dependencies
The shared library has minimal external dependencies to avoid version conflicts and maintain compatibility across different consuming projects.

### Future Extensibility
The interfaces are designed to support future enhancements without breaking existing implementations:
- `IReceiver` provides foundation for event-driven features
- `BuildResult` can be extended with additional status tracking
- Queue interface can support batch operations