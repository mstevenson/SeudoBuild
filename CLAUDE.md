# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.


## Build Commands

- **Build entire solution**: `dotnet build SeudoCI.sln`
- **Build specific project**: `dotnet build SeudoCI.Agent/SeudoCI.Agent.csproj`
- **Run tests**: `dotnet test` (runs all test projects in solution)
- **Run specific test project**: `dotnet test SeudoCI.Core.Tests/SeudoCI.Core.Tests.csproj`
- **Clean build artifacts**: `dotnet clean SeudoCI.sln`

## SeudoCI Agent Commands

The main executable is SeudoCI.Agent. Common usage patterns:

- **Local build**: `dotnet run --project SeudoCI.Agent build [project-config.json] -t [target-name] -o [output-path]`
- **Start build queue server**: `dotnet run --project SeudoCI.Agent queue -n [agent-name] -p [port]`
- **Scan for agents**: `dotnet run --project SeudoCI.Agent scan`
- **Submit remote build**: `dotnet run --project SeudoCI.Agent submit -p [project-config.json] -t [target-name] -a [agent-name]`

## Architecture Overview

SeudoCI is a distributed build system designed for Unity and other projects. The system is composed of a core framework and a set of independent modules which are distributed as separate loadable plugins. A module consists of an implementation assembly and a configuration assembly. After a module assembly has been loaded its Initialize method is called, which is passed a configuration object, a target workspace, and a logger object.

The architecture follows a modular pipeline approach:

### Core Components

1. **SeudoCI.Agent**: Main executable that provides CLI interface for build operations
2. **SeudoCI.Pipeline**: Core pipeline execution engine that orchestrates build steps
3. **SeudoCI.Core**: Foundation classes for workspaces, logging, file systems, and macros
4. **SeudoCI.Net**: Network discovery and communication for distributed builds

### Pipeline Module System

The system uses a plugin architecture with dynamically loaded modules organized by type:

- **Source Modules**: Git, Perforce source control integration
- **Build Modules**: Unity, Shell command execution
- **Archive Modules**: Folder, ZIP compression
- **Distribute Modules**: FTP, SFTP, SMB, Steam deployment
- **Notify Modules**: Email notifications

Modules are loaded via `ModuleLoader` which scans directories for DLLs implementing module interfaces (`ISourceModule`, `IBuildModule`, etc.).

### Key Execution Flow

1. **Project Configuration**: JSON files define project name and build targets
2. **Build Target**: Contains ordered lists of pipeline steps (Source → Build → Archive → Distribute → Notify)
3. **Pipeline Execution**: `PipelineRunner` orchestrates step execution with workspace management
4. **Step Instantiation**: `ProjectPipeline` creates step instances using module loader and step configurations

### Workspace System

- **ProjectWorkspace**: Base directory for project builds with macro substitution
- **TargetWorkspace**: Target-specific subdirectory with input/output/temp folders
- **Macros**: Dynamic variable replacement (project_name, build_target_name, commit_id, etc.)

### Network Architecture

- **Agent Discovery**: mDNS-based discovery of build agents on local network
- **REST API**: Nancy-based HTTP server for receiving build requests (/build, /queue, /info endpoints)
- **Distributed Builds**: Submit builds to remote agents via HTTP POST

Each module pair includes a "Shared" project for configuration classes and an implementation project for the actual step logic.