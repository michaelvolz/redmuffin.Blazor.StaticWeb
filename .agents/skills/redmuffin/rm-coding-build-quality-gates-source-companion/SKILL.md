---
name: rm-coding-build-quality-gates-source-companion
description: QualityGates tool source home, build, feed, and wiring. Load when developing the QualityGates product source itself. General gate patterns live in rm-coding-build-quality-gates-source.
purpose: Repo-specific home and procedures for the QualityGates tool source.
---

# rm-coding-build-quality-gates-source-companion

Companion to `rm-coding-build-quality-gates-source`: the global skill
owns gate patterns and CLI conventions; this skill owns only where the
tool source lives and how it ships. The tool source lives in this repo
under `tools/` (own solution, own `global.json` pin).

## Build and test

Run all tool-source commands from `tools/`:

```bash
dotnet build src/redmuffin.Tools.QualityGates --verbosity quiet
dotnet run --project tests/redmuffin.Tools.QualityGates.Tests
```

Full verify cycle: `dotnet clean`, `dotnet build`, then the test run as
above. Run `dotnet format` from the tools solution root, never with
`--severity info`.

Source fallback for developing the tool itself:

```bash
cd tools
dotnet run --project src/redmuffin.Tools.QualityGates -- <gate> [options]
```

## Feed

The global `quality-gates` command installs from this repo's private
local feed through the mapped `tools/nuget.config`; it is not published
to NuGet.org. Pack and update the global command only after changing the
package version, following the private-feed procedure in `rm-sysadmin`.

## Project structure

```
tools/
├── global.json                        .NET 10 SDK pin
├── redmuffin.Tools.slnx               Separate solution
├── src/redmuffin.Tools.QualityGates/
│   ├── Program.cs                     CLI root
│   ├── Commands/                      CLI wiring + handlers (one per gate)
│   ├── Analysis/                      Gate engines (CC, coverage, mutation, etc.)
│   └── Models/                        YAML config parsing
├── tests/redmuffin.Tools.QualityGates.Tests/
│   ├── Commands/                      Handler + composition tests
│   ├── Analysis/                      Per-gate unit tests
│   └── Fixtures/                      Coverage XML + MutationTarget project
└── quality-gates/                     Architecture rules config
```

## New-gate wiring

New gates follow `CrapCommand` / `CrapHandler` exactly. Wire into
`Program.cs` (`rootCommand.Subcommands.Add(XxxCommand.Create());`) and
hook into `AllCommand.cs` with appropriate flags. Add test fixture
projects under `tests/.../Fixtures/` when the gate needs real project
execution.
