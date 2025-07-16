# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with the SeudoCI.Agent project.

## Project Overview

SeudoCI.Agent is the main executable and primary entry point for the SeudoCI distributed build system. It provides both a command-line interface for local operations and an HTTP server for distributed build operations. The agent can operate in multiple modes: local builds, build queue server, network scanning, and build submission.

## Build Commands

- **Build the agent**: `dotnet build SeudoCI.Agent/SeudoCI.Agent.csproj`
- **Run the agent**: `dotnet run --project SeudoCI.Agent [command] [options]`
- **Build and run**: `dotnet build && dotnet run --project SeudoCI.Agent [command]`
- **Create executable**: `dotnet publish SeudoCI.Agent/SeudoCI.Agent.csproj -c Release`

The compiled executable is named `SeudoCI` (specified by `AssemblyName` in project file).

## CLI Commands

### Local Build
```bash
dotnet run --project SeudoCI.Agent build [project-config.json] -t [target-name] -o [output-path]
```
- Executes a complete build pipeline locally
- Loads pipeline modules dynamically
- Supports custom output directories
- Uses first target if none specified

### Queue Mode (Build Server)
```bash
dotnet run --project SeudoCI.Agent queue -n [agent-name] -p [port]
```
- Starts HTTP server listening for build requests
- Automatically generates agent name if not provided
- Defaults to port 5511
- Advertises availability via mDNS discovery
- Processes builds sequentially in background queue

### Network Scanning
```bash
dotnet run --project SeudoCI.Agent scan
```
- Discovers available build agents on local network
- Uses mDNS/Bonjour for agent discovery
- Displays agent names and addresses
- Press any key to exit

### Build Submission
```bash
dotnet run --project SeudoCI.Agent submit -p [project-config.json] -t [target-name] -a [agent-name]
```
- Submits build requests to remote agents
- Targets specific agent by name or broadcasts to all
- Transfers project configuration via HTTP POST

### Agent Name Generation
```bash
dotnet run --project SeudoCI.Agent name [-r]
```
- Displays unique agent identifier
- Deterministic names based on MAC addresses
- Random names with `-r` flag
- Format: `[adjective]-[animal]` (e.g., "brilliant-dolphin")

## Architecture Components

### Core Classes

**Program.cs**: Main entry point with CommandLineParser integration
- Handles verb-based command routing
- Manages CLI option parsing and validation
- Coordinates between different operation modes

**Builder.cs**: Core build execution engine
- Orchestrates pipeline execution via PipelineRunner
- Manages build state and error handling
- Supports default target selection

**BuildQueue.cs**: Concurrent build job management
- Thread-safe queuing using ConcurrentQueue and ConcurrentDictionary
- Background processing with long-running tasks
- Build status tracking (Queued, Running, Completed, Cancelled)
- Output directory management in user Documents folder

**AgentName.cs**: Unique agent identification system
- Hardware-based deterministic naming using MAC addresses
- Human-readable adjective-animal combinations
- Extensive word lists for variety and uniqueness

### REST API (AgentNancyModule.cs)

**Endpoints**:
- `POST /build` - Queue build with default target
- `POST /build/{target}` - Queue build with specific target
- `GET /queue` - List all build results
- `GET /queue/{id}` - Get specific build status
- `POST /queue/{id}/cancel` - Cancel specific build

**Features**:
- JSON project configuration deserialization with custom converters
- Client IP logging for build requests
- Error handling with appropriate HTTP status codes
- Integration with module loader for configuration validation

### HTTP Server Infrastructure

**Bootstrapper.cs**: Nancy framework configuration
- Dependency injection container setup
- Service registration (Logger, ModuleLoader, FileSystem, BuildQueue)
- Platform-specific file system selection
- Automatic queue startup

**Startup.cs**: ASP.NET Core hosting configuration (minimal implementation)

### Network Integration

**BuildSubmitter.cs**: Remote build submission (implementation incomplete)
- Agent discovery integration
- HTTP request preparation for remote agents
- Currently contains commented placeholder code

## Key Dependencies

- **CommandLineParser**: CLI argument parsing and verb handling
- **Nancy.Hosting.Kestrel**: HTTP server framework for REST API
- **SeudoCI.Pipeline**: Core pipeline execution engine
- **SeudoCI.Net**: mDNS agent discovery and networking
- **SeudoCI.Core**: Logging, file systems, workspace management

## Usage Patterns

### Development Workflow
1. **Local testing**: Use `build` command for immediate local builds
2. **Agent setup**: Use `queue` command to start build server
3. **Network discovery**: Use `scan` to verify agent visibility
4. **Remote builds**: Use `submit` to delegate builds to other agents

### Production Deployment
- Run agents in `queue` mode on dedicated build machines
- Use unique agent names for identification
- Configure firewall rules for port 5511 (default)
- Ensure mDNS/Bonjour service availability for discovery

## Error Handling

- **Build failures**: Logged with detailed error messages
- **Network issues**: Graceful fallback with connection retry logic
- **Configuration errors**: Validation with descriptive error messages
- **Queue management**: Build cancellation and status tracking

## Platform Support

- **Primary target**: .NET 8.0
- **Windows**: Full support with WindowsFileSystem
- **macOS**: Full support with MacFileSystem  
- **Linux**: Planned support (file system abstraction ready)
- **mDNS**: Requires platform-specific Bonjour/Avahi services