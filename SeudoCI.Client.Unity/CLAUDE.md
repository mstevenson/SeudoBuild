# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with the SeudoCI.Client.Unity project.

## Project Overview

SeudoCI.Client.Unity is a Unity Editor plugin that provides a graphical user interface for configuring and managing SeudoCI build pipelines directly within the Unity Editor. The plugin enables Unity developers to set up distributed builds, discover build agents, and submit build jobs without leaving their development environment.

## Build Commands

- **Build Unity client library**: `dotnet build SeudoCI.Client.Unity/SeudoCI.Client.Unity.csproj`
- **Clean build artifacts**: `dotnet clean SeudoCI.Client.Unity/SeudoCI.Client.Unity.csproj`

## Unity Integration

This project is designed to be compiled as a .NET Standard 2.1 library and imported into Unity projects as an Editor plugin.

### Unity Version Requirements
- **Target Framework**: .NET Standard 2.1
- **Unity Editor**: 6000.0.46f1 (Unity 6) (as referenced in project file)
- **Language Version**: C# 9.0

### Required Unity Modules
- `UnityEditor` - Core editor functionality
- `UnityEditor.CoreModule` - Essential editor APIs
- `UnityEngine` - Runtime engine APIs
- `UnityEngine.CoreModule` - Core runtime functionality
- `UnityEngine.IMGUIModule` - Immediate mode GUI system
- `UnityEngine.UnityWebRequestModule` - HTTP request functionality

## Project Structure

The project contains three main components, all currently commented out:

### SeudoCIEditor.cs (Main Configuration Window)
**Purpose**: Primary Unity Editor window for configuring SeudoCI projects and build targets

**Key Features**:
- Project name configuration with auto-population from Unity's `PlayerSettings.productName`
- Build target management with configurable target names
- Pipeline step configuration for all step types:
  - Source Steps (version control)
  - Build Steps (compilation)
  - Archive Steps (packaging)
  - Distribute Steps (deployment)
  - Notify Steps (notifications)
- Automatic configuration caching in Unity's Library folder
- Real-time configuration saving on GUI changes

**Menu Integration**: `Window/SeudoCI`

**Configuration Persistence**:
- Cache location: `[Project]/Library/SeudoBuildCache.json`
- JSON serialization using Newtonsoft.Json
- Automatic loading and saving of configuration changes

### AgentBrowser.cs (Network Discovery Window)
**Purpose**: Unity Editor window for discovering and monitoring available build agents on the local network

**Key Features**:
- Real-time agent discovery using `AgentLocator` from SeudoCI.Net
- Dynamic agent list management with automatic add/remove
- Agent connection monitoring with event-driven updates
- Simple GUI displaying agent names and addresses
- Automatic cleanup on window close

**Menu Integration**: `Window/Build Agent Browser`

**Network Integration**:
- Uses port 5511 for agent discovery (configurable)
- Event-based agent discovery (`AgentFound`/`AgentLost` events)
- Graceful handling of network connectivity changes

### EditorCoroutine.cs (Async Operations Support)
**Purpose**: Custom coroutine system enabling asynchronous operations in Unity Editor context

**Key Features**:
- Editor-safe coroutine execution outside play mode
- Nested coroutine support with stack-based management
- Integration with Unity's `EditorApplication.update` loop
- Automatic coroutine lifecycle management
- Support for `yield return` patterns used in HTTP requests

**Technical Implementation**:
- Static coroutine management with global update loop
- Stack-based enumeration for nested coroutines
- Automatic cleanup of completed coroutines
- Protection against Unity runtime coroutine conflicts

## HTTP Communication Features

### Agent Information Retrieval
- Asynchronous HTTP GET requests to agent `/info` endpoints
- Uses `UnityWebRequest` for cross-platform compatibility
- Coroutine-based request handling for non-blocking operations

### Build Job Submission
- HTTP POST requests to agent `/build` endpoints
- JSON payload containing project configuration
- Asynchronous submission with progress monitoring

## Current State: Commented Implementation

**Important**: All core functionality is currently commented out throughout the codebase. This suggests:

1. **Development Status**: The plugin is in a dormant or experimental state
2. **Integration Issues**: Possible conflicts with newer Unity versions or dependencies
3. **Maintenance Mode**: Code preserved for future reactivation

## Dependencies

### External Packages
- **Newtonsoft.Json 13.0.1**: JSON serialization for configuration persistence and HTTP communication

### Internal References
- **SeudoCI.Pipeline**: Access to `ProjectConfig` and pipeline configuration classes
- **SeudoCI.Net**: Network discovery and agent communication (`AgentLocator`, `AgentLocation`)

## Activation Instructions

To reactivate this Unity plugin:

1. **Uncomment all code** in the three main source files
2. **Verify Unity API compatibility** - check for deprecated methods in Unity 2022.3.24f1+
3. **Update assembly references** - ensure SeudoCI.Pipeline and SeudoCI.Net assemblies are available
4. **Test HTTP functionality** - verify `UnityWebRequest` API usage is current
5. **Validate GUI components** - ensure `EditorGUILayout` calls are compatible

## Development Workflow

### Configuration Management
1. Open Unity project
2. Access `Window/SeudoCI` to configure build settings
3. Set project name, build targets, and pipeline steps
4. Configuration automatically saves to Library folder

### Agent Discovery
1. Open `Window/Build Agent Browser`
2. View real-time list of available build agents
3. Monitor agent connectivity status
4. Select agents for build submission

### Build Submission
1. Configure project settings in main SeudoCI window
2. Discover available agents in Agent Browser
3. Submit builds via HTTP POST to selected agents
4. Monitor build progress through agent REST API

## Integration Considerations

### Unity Project Setup
- Place compiled DLL in `Assets/Editor` folder or install as Unity package
- Ensure all dependencies are properly referenced
- Configure firewall rules for network discovery (port 5511)

### Network Requirements
- Local network connectivity for agent discovery
- HTTP access to build agent servers
- mDNS/Bonjour support for service discovery

### Platform Support
- Windows and macOS supported through Unity Editor
- Linux support dependent on Unity Editor availability
- Network discovery requires platform-specific mDNS implementation