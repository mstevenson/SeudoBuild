# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with the SeudoCI.Pipeline project.

## Project Overview

SeudoCI.Pipeline is the core execution engine of the SeudoCI build system. It orchestrates the complete build pipeline by dynamically loading modules, executing build steps in sequence, managing workspaces, and coordinating the flow of data between pipeline stages. This project serves as the central coordinator that transforms project configurations into executed build pipelines.

## Build Commands

- **Build pipeline engine**: `dotnet build SeudoCI.Pipeline/SeudoCI.Pipeline.csproj`
- **Clean build artifacts**: `dotnet clean SeudoCI.Pipeline/SeudoCI.Pipeline.csproj`
- **Run tests**: `dotnet test` (when test projects reference this library)

## Architecture Overview

SeudoCI.Pipeline implements a modular, extensible pipeline execution system with five core components:

1. **PipelineRunner**: Main execution engine and workspace orchestrator
2. **ProjectPipeline**: Step instantiation and type mapping coordinator  
3. **ModuleLoader**: Dynamic assembly loading and step instantiation
4. **ModuleRegistry**: Type-safe module registration and retrieval
5. **ModuleLoaderFactory**: Module discovery and initialization

## Core Components

### PipelineRunner

**Location**: `PipelineRunner.cs`

**Purpose**: Primary execution engine that orchestrates complete build pipelines from start to finish.

#### Key Responsibilities
- **Workspace Management**: Creates and initializes project and target workspaces
- **Pipeline Orchestration**: Executes the five-phase pipeline sequence
- **Macro Population**: Sets up dynamic variable replacement
- **Configuration Persistence**: Saves project configuration for debugging
- **Error Handling**: Manages step failures and pipeline short-circuiting
- **Logging Coordination**: Provides structured, indented build output

#### Pipeline Execution Sequence
```
Source → Build → Archive → Distribute → Notify
```

Each phase processes results from the previous phase, enabling data flow through the pipeline.

#### Core Methods

**`ExecutePipeline(ProjectConfig projectConfig, string buildTargetName, IModuleLoader moduleLoader)`**
- Main entry point for pipeline execution
- Validates configuration and target existence
- Sets up workspace structure and macros
- Orchestrates all five pipeline phases
- Handles overall error reporting

**Execution Flow**:
1. **Configuration Validation**: Verifies project config and target existence
2. **Workspace Setup**: Creates project/target directories with proper structure
3. **Configuration Serialization**: Saves config copy for debugging/audit
4. **Pipeline Creation**: Instantiates ProjectPipeline with loaded modules
5. **Macro Population**: Sets standard variables (project_name, build_date, etc.)
6. **Sequential Execution**: Runs each pipeline phase in order
7. **Result Aggregation**: Combines results and reports final status

#### Workspace Structure Created
```
BaseDirectory/
└── ProjectName/
    ├── ProjectConfig.json      // Serialized configuration
    ├── Logs/                   // Project-level logs
    └── Targets/
        └── TargetName/
            ├── Workspace/      // Source files
            ├── Output/         // Build products
            ├── Archives/       // Packaged builds
            └── Logs/           // Target-specific logs
```

#### Standard Macro Variables
- `%project_name%` - Project identifier
- `%build_target_name%` - Build target name
- `%build_date%` - Timestamp (yyyy-dd-M--HH-mm-ss format)
- `%app_version%` - Target version number
- `%commit_id%` - Source control commit hash (from first source step)

### ProjectPipeline

**Location**: `ProjectPipeline.cs`

**Purpose**: Manages step instantiation and provides type-safe access to pipeline steps by category.

#### Key Features
- **Type-Safe Step Retrieval**: Generic methods for accessing steps by interface type
- **Configuration Mapping**: Converts configuration objects to executable step instances
- **Module Integration**: Coordinates with ModuleLoader for step instantiation
- **Step Filtering**: Handles null/failed step creation gracefully

#### Core Architecture
```csharp
private readonly Dictionary<Type, IEnumerable<IPipelineStep>> _stepTypeMap;
```

Maps pipeline step interface types to their instantiated implementations.

#### Key Methods

**`LoadBuildStepModules(IModuleLoader moduleLoader, ITargetWorkspace workspace, ILogger logger)`**
- Registers all five step types with the module loader
- Creates step instances for each configured step
- Populates internal type mapping for retrieval

**`GetPipelineSteps<T>() where T : IPipelineStep`**
- Type-safe retrieval of steps by interface type
- Returns empty collection if no steps of requested type
- Used by PipelineRunner to get steps for each phase

**`CreatePipelineSteps<T>(IModuleLoader loader, ITargetWorkspace workspace, ILogger logger)`**
- Maps step interface types to configuration step collections
- Filters out failed step instantiations
- Returns read-only collection of successfully created steps

### ModuleLoader

**Location**: `ModuleLoader.cs`

**Purpose**: Dynamic assembly loading system that discovers and instantiates pipeline modules from DLL files.

#### Key Features
- **Dynamic Assembly Loading**: Scans directories for module DLLs
- **Type Discovery**: Uses reflection to find module implementation types
- **Step Instantiation**: Creates configured step instances with proper initialization
- **Error Handling**: Graceful handling of assembly load failures
- **Debug Support**: Conditional logging for module loading diagnostics

#### Module Discovery Process
1. **Directory Scanning**: Searches `Modules/` directory for subdirectories
2. **DLL Enumeration**: Finds all .dll files in each module directory
3. **Assembly Loading**: Loads assemblies using `Assembly.LoadFile()`
4. **Type Filtering**: Identifies types implementing module interfaces
5. **Instance Creation**: Instantiates and registers discovered modules

#### Supported Module Types
```csharp
string[] moduleBaseTypeNames = {
    nameof(ISourceModule),
    nameof(IBuildModule), 
    nameof(IArchiveModule),
    nameof(IDistributeModule),
    nameof(INotifyModule)
};
```

#### Step Creation Process
```csharp
public T CreatePipelineStep<T>(StepConfig config, ITargetWorkspace workspace, ILogger logger)
```

1. **Module Matching**: Finds modules that support the requested step type
2. **Configuration Matching**: Matches step config type to module config type
3. **Instance Creation**: Uses `Activator.CreateInstance()` for step instantiation
4. **Initialization**: Calls `Initialize()` method with config, workspace, and logger
5. **Error Recovery**: Returns null for failed instantiations

#### Known Issues
- **LibGit2Sharp Dependency**: Git module has issues with platform-specific DLL loading
- **Assembly Resolution**: Complex dependencies may fail to load properly

### ModuleRegistry

**Location**: `ModuleRegistry.cs`

**Purpose**: Type-safe registry that organizes and provides access to loaded pipeline modules.

#### Architecture
Uses strongly-typed module categories to organize different types of pipeline modules:

```csharp
private readonly ModuleCategory[] _moduleCategories = {
    new ModuleCategory<ISourceModule, ISourceStep, SourceStepConfig>(),
    new ModuleCategory<IBuildModule, IBuildStep, BuildStepConfig>(),
    new ModuleCategory<IArchiveModule, IArchiveStep, ArchiveStepConfig>(),
    new ModuleCategory<IDistributeModule, IDistributeStep, DistributeStepConfig>(),
    new ModuleCategory<INotifyModule, INotifyStep, NotifyStepConfig>()
};
```

#### Key Methods

**`RegisterModule(IModule module)`**
- Automatically categorizes modules based on implemented interfaces
- Single module can implement multiple interfaces
- Thread-safe registration process

**`GetModules<T>() where T : IModule`**
- Type-safe retrieval of modules by interface type
- Returns strongly-typed enumerable of matching modules
- Throws exception if module type not found

**`GetModulesForStepType<T>() where T : IPipelineStep`**
- Retrieves modules that can create specific step types
- Used by ModuleLoader for step instantiation
- Maps step interfaces to module implementations

**`GetJsonConverters()`**
- Creates custom JSON converters for configuration deserialization
- Enables polymorphic deserialization of step configurations
- Registers all known step configuration types with converters

### ModuleLoaderFactory

**Location**: `ModuleLoaderFactory.cs`

**Purpose**: Factory class that creates and initializes ModuleLoader instances with comprehensive module discovery.

#### Key Features
- **Automatic Module Discovery**: Scans default `Modules/` directory
- **Error Handling**: Graceful handling of module load failures
- **Diagnostic Logging**: Reports discovered modules by category
- **Initialization Coordination**: Sets up complete module ecosystem

#### Module Discovery Output
```
Loading Modules

    Source:  Git
     Build:  Unity Standard Build, Unity Execute Method Build, Shell Command
   Archive:  Folder, Zip
Distribute:  FTP, SFTP, SMB, Steam Upload
    Notify:  Email Notification
```

#### Directory Structure Expected
```
ExecutableDirectory/
└── Modules/
    ├── ModuleName1/
    │   ├── ModuleName1.dll
    │   └── Dependencies.dll
    └── ModuleName2/
        ├── ModuleName2.dll
        └── Dependencies.dll
```

## Pipeline Execution Flow

### High-Level Workflow
1. **Agent Request**: Build agent receives project configuration and target name
2. **Module Loading**: ModuleLoaderFactory discovers and loads all available modules
3. **Pipeline Creation**: PipelineRunner creates execution context
4. **Workspace Setup**: Project and target workspaces initialized
5. **Step Instantiation**: ProjectPipeline creates step instances from configuration
6. **Sequential Execution**: Five pipeline phases executed in order
7. **Result Aggregation**: Success/failure status reported

### Phase-by-Phase Execution

#### 1. Source Phase
- **Input**: Target workspace (empty)
- **Processing**: Downloads/updates source code
- **Output**: `SourceSequenceResults` with commit information
- **Typical Steps**: Git clone/pull, Perforce sync

#### 2. Build Phase  
- **Input**: `SourceSequenceResults` + workspace with source code
- **Processing**: Compiles source into build artifacts
- **Output**: `BuildSequenceResults` with build status
- **Typical Steps**: Unity builds, shell script execution

#### 3. Archive Phase
- **Input**: `BuildSequenceResults` + workspace with build outputs
- **Processing**: Packages build outputs for distribution
- **Output**: `ArchiveSequenceResults` with archive file information
- **Typical Steps**: ZIP compression, folder copying

#### 4. Distribute Phase
- **Input**: `ArchiveSequenceResults` + workspace with archived files
- **Processing**: Uploads/deploys archives to target destinations
- **Output**: `DistributeSequenceResults` with deployment status
- **Typical Steps**: FTP upload, Steam distribution, network copying

#### 5. Notify Phase
- **Input**: `DistributeSequenceResults` + complete build status
- **Processing**: Sends notifications about build completion
- **Output**: `NotifySequenceResults` with notification status
- **Typical Steps**: Email alerts, webhook calls

### Error Handling Strategy

#### Step-Level Failures
- Individual step failures captured in step results
- Step exceptions logged with detailed error messages
- Step failure stops sequence execution immediately

#### Sequence-Level Failures
- Failed sequence prevents subsequent sequences from running
- Previous sequence results marked as failed
- Overall pipeline marked as failed with aggregated error information

#### Pipeline-Level Failures
- Configuration validation failures prevent pipeline start
- Module loading failures prevent step instantiation
- Workspace creation failures prevent pipeline execution

## Dependencies

### Internal Dependencies
- **SeudoCI.Core**: Logging, workspace management, file system abstractions
- **SeudoCI.Pipeline.Shared**: Interfaces, base classes, and configuration types

### Framework Dependencies
- **.NET 8.0**: Target framework with reflection and assembly loading
- **System.Reflection**: Dynamic assembly loading and type discovery
- **System.Diagnostics**: Performance timing and conditional compilation

### No External Packages
This project relies only on framework capabilities, ensuring minimal deployment complexity and maximum compatibility.

## Configuration Integration

### Project Configuration Structure
```json
{
  "ProjectName": "MyProject",
  "BuildTargets": [
    {
      "TargetName": "Release",
      "Version": { "Major": 1, "Minor": 0, "Patch": 0 },
      "SourceSteps": [ { "Type": "GitSourceConfig", ... } ],
      "BuildSteps": [ { "Type": "UnityBuildConfig", ... } ],
      "ArchiveSteps": [ { "Type": "ZipArchiveConfig", ... } ],
      "DistributeSteps": [ { "Type": "FTPDistributeConfig", ... } ],
      "NotifySteps": [ { "Type": "EmailNotifyConfig", ... } ]
    }
  ]
}
```

### Configuration Processing
1. **Deserialization**: JSON converted to strongly-typed configuration objects
2. **Validation**: Configuration validated for required fields and target existence
3. **Step Mapping**: Configuration steps mapped to module implementations
4. **Instance Creation**: Step instances created with type-safe configuration objects

## Development Patterns

### Adding New Module Types
1. **Define Module Interface**: Create new module interface in Pipeline.Shared
2. **Define Step Interface**: Create corresponding step interface
3. **Define Configuration**: Create base configuration class
4. **Update Registry**: Add new module category to ModuleRegistry
5. **Update Loader**: Add module type to ModuleLoader discovery

### Custom Module Development
```csharp
// Module registration class
public class MyCustomModule : IDistributeModule
{
    public string Name => "Custom Distribution";
    public Type StepType => typeof(MyCustomStep);
    public Type StepConfigType => typeof(MyCustomConfig);
    public string StepConfigName => "Custom Distribution";
}

// Step implementation class  
public class MyCustomStep : IDistributeStep<MyCustomConfig>
{
    public string Type => "Custom Distribution";
    
    public void Initialize(MyCustomConfig config, ITargetWorkspace workspace, ILogger logger)
    {
        // Initialize step with configuration
    }
    
    public DistributeStepResults ExecuteStep(ArchiveSequenceResults archiveResults, ITargetWorkspace workspace)
    {
        // Implement custom distribution logic
        return new DistributeStepResults { IsSuccess = true };
    }
}
```

### Error Handling Best Practices
- Always return result objects with success status and exception details
- Log errors at appropriate levels (step warnings vs. sequence failures)
- Provide actionable error messages for configuration and runtime issues
- Use structured exception handling to prevent pipeline crashes

## Performance Considerations

### Module Loading Optimization
- Modules loaded once at startup and reused across builds
- Assembly loading cached by .NET runtime
- Module discovery optimized with early type filtering

### Memory Management
- Step instances created per build to avoid state contamination
- Large files processed with streaming where possible
- Workspace cleanup handled automatically after builds

### Execution Efficiency
- Sequential step execution within phases prevents resource conflicts
- Parallel builds handled at agent level, not within single pipeline
- Result objects designed for minimal memory footprint