# Repository Guidelines

## Project Structure & Module Organization
- Solution: `SeudoCI.sln` at the repo root.
- Key projects:
  - `SeudoCI.Agent/` – CLI entry point and REST host.
  - `SeudoCI.Core/` – logging, serialization, filesystem.
  - `SeudoCI.Net/` – discovery, HTTP, network primitives.
  - `SeudoCI.Pipeline/` – pipeline engine; `SeudoCI.Pipeline.Shared/` – configs/base types.
  - `SeudoCI.Pipeline.Modules.*` – first‑party steps (Git, Unity, Archive, Distribute, Notify).
  - Tests: `*.Tests/` (e.g., `SeudoCI.Core.Tests`, `SeudoCI.Pipeline.Tests`).

## Build, Test, and Development Commands
- Restore/build all: `dotnet restore SeudoCI.sln` then `dotnet build SeudoCI.sln -c Debug`.
- Run tests (solution): `dotnet test SeudoCI.sln --collect:"XPlat Code Coverage"`.
- Run a single test project: `dotnet test SeudoCI.Core.Tests`.
- Run the agent locally:
  - Build: `dotnet run --project SeudoCI.Agent -- build path/to/config.yaml --build-target Windows64`.
  - Queue: `dotnet run --project SeudoCI.Agent -- queue --agent-name dev-01`.
- Publish CLI: `dotnet publish SeudoCI.Agent -c Release -o out/agent`.

## Coding Style & Naming Conventions
- C# 12 on .NET 9; `<Nullable>enable</Nullable>` and implicit usings are enabled.
- Indentation: 4 spaces. Braces on new line. Prefer expression‑bodied members when clearer.
- File names match public type names; namespaces begin `SeudoCI.*`.
- Use `SeudoCI.Core.ILogger` for output (avoid `Console.WriteLine`).
- New modules: create `SeudoCI.Pipeline.Modules.<Name>/` with a `<Name>.Shared/` sibling for config/contracts; derive from `*StepConfig` and register via attributes.

## Module Scaffolding
- Create projects:
  - `SeudoCI.Pipeline.Modules.<Name>.Shared/` (classlib, net9.0) referencing `SeudoCI.Pipeline.Shared`.
  - `SeudoCI.Pipeline.Modules.<Name>/` (classlib, net9.0) referencing the `.Shared` project and `SeudoCI.Pipeline.Shared`.
- Define config (Shared):
  - `public class <Name>Config : <Category>StepConfig { public override string Name => "My Step"; /* props */ }`.
- Implement module and step (Runtime):
  - Module: `public class <Name>Module : I<Category>Module { public string Name=>"<Short>"; public Type StepType=>typeof(<Name>Step); public Type StepConfigType=>typeof(<Name>Config); public string StepConfigName=>"My Step"; }`
  - Step: `public class <Name>Step : I<Category>Step<<Name>Config> { void Initialize(<Name>Config c, ITargetWorkspace w, ILogger l){...} /* ExecuteStep(...) */ }`
- Add to solution: `dotnet sln SeudoCI.sln add SeudoCI.Pipeline.Modules.<Name>.Shared SeudoCI.Pipeline.Modules.<Name>`.
- Build and stage for runtime discovery:
  - `dotnet build -c Release`.
  - Copy outputs to `Modules/<Name>/` beside the agent binary (include dependencies, e.g., `<Name>.dll`, `<Name>.Shared.dll`).
- YAML usage example:
  - `distributeSteps: [ { type: "My Step", archiveFileName: Artifacts.zip, /* other fields */ } ]`.

## Testing Guidelines
- Frameworks: NUnit + NSubstitute; coverage via `coverlet.collector`.
- Place tests in the corresponding `*.Tests/` project; name files `<TypeName>Test.cs`, classes `<TypeName>Tests`.
- Cover success and failure paths; add API/integration tests for new endpoints.

## Commit & Pull Request Guidelines
- Commit messages: imperative, concise subjects (e.g., "Fix ShellBuild quoting"); optional scopes like `docs:` are welcome.
- PRs should include: clear description/motivation, linked issues, tests for new/changed behavior, and docs updates (README/module notes). Add logs/screenshots for user‑visible changes.

## Security & Configuration Tips
- Never commit secrets (PATs, SMTP creds). Prefer environment variables (e.g., `UNITY_*`, Git tokens).
- Validate untrusted inputs in modules and avoid logging sensitive fields verbatim.
