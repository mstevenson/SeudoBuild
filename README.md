# SeudoBuild

SeudoBuild is a modular, distributed build and deployment system designed around the needs of teams shipping Unity games. It orchestrates the entire lifecycle of a build — sourcing the project, compiling it, packaging the output, distributing the artifacts, and notifying stakeholders — while remaining extensible enough to support custom workflows. The platform is composed of a lightweight command line agent, a library of pipeline modules, and optional REST endpoints that allow other tools to trigger or monitor work.

The repository contains the core runtime (`SeudoCI.Core`), the pipeline engine (`SeudoCI.Pipeline`), a shared set of abstractions for modules (`SeudoCI.Pipeline.Shared`), and a suite of first-party modules that implement common tasks such as downloading source code, invoking Unity, archiving build outputs, distributing artifacts over SFTP/SMB/Steam, and sending notifications. Each module can be combined declaratively through YAML configuration so that builds remain reproducible and easy to audit.

## Key capabilities

* **Unity-first build automation.** Automatic editor discovery on Windows, macOS, and Linux and purpose-built pipeline steps make running Unity batch builds straightforward.
* **Configurable pipeline stages.** Source, build, archive, distribute, and notify stages are described in YAML so they can be versioned alongside the project.
* **Network-aware agents.** Agents can announce themselves, accept remote jobs, and expose REST APIs for external orchestration.
* **Cross-platform .NET tooling.** All projects target .NET 9.0, making it possible to run agents on any environment that supports the modern .NET runtime.

## Repository layout

```
SeudoCI.Agent/                # Command line entry point for running builds and managing agents
SeudoCI.Agent.Shared/         # Shared utilities for the agent
SeudoCI.Core/                 # Core services such as logging, serialization, and file system wrappers
SeudoCI.Net/                  # Networking primitives, discovery, and REST hosting
SeudoCI.Pipeline/             # Pipeline execution engine
SeudoCI.Pipeline.Shared/      # Configuration objects and base types for pipeline modules
SeudoCI.Pipeline.Modules.*/   # First-party modules for source control, builds, archiving, and distribution
```

## Prerequisites

* [.NET SDK 9.0](https://dotnet.microsoft.com/) or newer
* Access to the external tools required by your pipeline steps (for example the Unity Editor, Git, 7-Zip, Steam command line tools, etc.)
* Network access between the machines that will run the queue/deploy agents and the machines that will submit builds

### Optional environment variables for Unity discovery

Unity build steps attempt to locate the appropriate Unity editor automatically. When auto-discovery is insufficient, provide environment variables to point at specific installations:

* `UNITY_WINDOWS_EDITOR_PATH` – full path to a specific `Unity.exe`.
* `UNITY_WINDOWS_INSTALLATION_ROOT` – directory that contains Unity versions (for example `C:\\Program Files\Unity\Hub\Editor`).
* `UNITY_LINUX_EDITOR_PATH` – full path to the Unity binary on Linux.
* `UNITY_LINUX_INSTALLATION_ROOT` – directory that contains Unity versions on Linux (for example `~/Unity/Hub/Editor`).

When the root variables are provided, the build step expects each Unity version to reside in a subdirectory named after the version string (e.g. `2022.1.0f1`) with the editor binary located in `Editor/Unity[.exe]`.

## Getting started

Clone the repository and restore the dependencies:

```
git clone https://github.com/<your-org>/SeudoBuild.git
cd SeudoBuild
dotnet restore SeudoCI.sln
```

Build the solution to verify the environment:

```
dotnet build SeudoCI.sln
```

Run the automated tests (optional but recommended when modifying the code base):

```
dotnet test SeudoCI.sln
```

## Defining a project pipeline

Pipelines are described via YAML files that match the `ProjectConfig` object model. A minimal configuration contains at least one `BuildTarget` describing all of the steps required to produce artifacts. Below is a condensed example illustrating the common structure — adjust the step types and fields to match the modules you intend to use.

```yaml
projectName: Sample Unity Game
buildTargets:
  - targetName: Windows64
    version: 1.0.0
    sourceSteps:
      - type: GitSource
        repositoryURL: https://github.com/example/game.git
        repositoryBranchName: main
    buildSteps:
      - type: UnityBuild
        projectPath: GameProject
        buildTarget: StandaloneWindows64
        outputFolder: Builds/Windows
    archiveSteps:
      - type: ZipArchive
        sourcePath: Builds/Windows
        destinationPath: Artifacts/Windows.zip
    distributeSteps:
      - type: SftpDistribute
        host: builds.example.com
        username: buildbot
        remotePath: /var/www/downloads
    notifySteps:
      - type: EmailNotify
        recipients:
          - team@example.com
        subject: Windows build complete
```

Each step `Type` corresponds to a module in `SeudoCI.Pipeline.Modules.*`. Refer to the module-specific documentation or source code for the full list of supported properties.

## Command line usage

All workflows are orchestrated through the `SeudoCI.Agent` project. You can run commands directly via `dotnet run` or by publishing the project with `dotnet publish`. The examples below assume you are executing from the repository root.

### Run a local build

Execute a build locally using the configuration defined above. The build output defaults to a folder next to the project configuration file unless `--output-folder` is set.

```
dotnet run --project SeudoCI.Agent -- build path/to/project-config.yaml \
  --build-target Windows64 \
  --output-folder /absolute/path/to/output
```

### Discover agents on the network

Search for remote agents broadcasting their availability. The command runs until you cancel it.

```
dotnet run --project SeudoCI.Agent -- scan
```

### Host a queueing agent

Start an agent that listens for incoming build requests. Provide an explicit name to make it easier to target the machine when submitting jobs.

```
dotnet run --project SeudoCI.Agent -- queue \
  --agent-name BuildStation-01 \
  --port 9000
```

While the agent is running it exposes REST endpoints such as `/info`, `/build`, `/queue`, and `/queue/{id}` for integration with external tooling.

### Submit a remote build

Submit a job to a remote agent using the same project configuration. If no `--agent-name` is supplied, the request is broadcast to all listening agents.

```
dotnet run --project SeudoCI.Agent -- submit \
  --project-config path/to/project-config.yaml \
  --build-target Windows64 \
  --agent-name BuildStation-01
```

### Listen for deployment requests

Agents can also wait for deployment triggers and execute the configured deployment steps when a request arrives.

```
dotnet run --project SeudoCI.Agent -- deploy
```

## REST API overview

When a queue agent is active it hosts a REST API (using ASP.NET Core) for remote management:

* `GET /info` – Retrieve metadata about the running agent.
* `POST /build` – Trigger the default target defined in the submitted project configuration.
* `POST /build/{targetName}` – Trigger a specific build target.
* `GET /queue` – List all active build tasks.
* `GET /queue/{id}` – Inspect a single build task.
* `POST /queue/{id}/cancel` – Cancel a pending or running task.

All endpoints expect YAML payloads encoded as `application/x-yaml`.

## Next steps

* Explore the module directories to tailor the pipeline to your infrastructure (Git, Perforce, SMB, Steam, email, and more).
* Add new modules by implementing the appropriate interfaces from `SeudoCI.Pipeline.Shared` and registering them with the module loader.
* Containerize the queue agent or run it as a Windows service/systemd unit to keep build infrastructure online 24/7.

By combining declarative configuration with composable modules, SeudoBuild helps teams standardize their Unity build and release process across local workstations, build farms, and deployment environments.
