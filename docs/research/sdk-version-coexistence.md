---
date: 2026-05-12
title: "Cross-SDK Project Discovery: Solving .NET 9/10 Coexistence"
module: quality-gates
tags: [sdk, dotnet, global-json, slnx, discovery]
problem_type: architecture
---

# Cross-SDK Project Discovery: Solving .NET 9/10 Coexistence

## Problem

Two .slnx solutions coexist in the repo with different SDK requirements:

| Solution                          | SDK  | Reason                                          |
| --------------------------------- | ---- | ----------------------------------------------- |
| `redmuffin.Blazor.StaticWeb.slnx` | 9.0  | Blazor WASM requires `wasm-tools-net9` workload |
| `tools/redmuffin.Tools.slnx`      | 10.0 | Originally chosen without constraint            |

The QualityGates CLI lives in the tools solution. Running `dotnet run -- all`
from `tools/` auto-discovers tools projects. Analyzing the main solution
required explicit `--project` and `--test-project` flags. This was the
motivation for the `SlnxProjectDiscovery` feature.

## Research

### Can SDK 10 build .NET 9 projects?

**Yes.** Tested: clean build of the main .NET 9 solution with SDK 10.0.104.
The `TargetFramework` in `.csproj` determines the runtime, not the SDK version.
.NET SDKs are backward-compatible for builds and tests.

```
$ DOTNET_ROOT=/usr/share/dotnet PATH="/usr/share/dotnet:$PATH" dotnet build
  ✅ 2 projects, 0 errors, 0 warnings

$ DOTNET_ROOT=/usr/share/dotnet PATH="/usr/share/dotnet:$PATH" dotnet test
  ✅ 307 tests passed
```

### Can SDK 10 build Blazor WASM targeting .NET 9?

**No.** SDK 10's workload set only includes `wasm-tools-net10`. The
`wasm-tools-net9` workload is not available in SDK 10. Without `global.json`
pinning to SDK 9, `dotnet build` fails:

```
NETSDK1147: To build this project, the following workloads must be
installed: wasm-tools-net9
```

### Does Azure Static Web Apps support .NET 10?

**No.** As of May 2026, Azure SWA's Oryx build system rejects `net10.0`.
Microsoft moderator response (Jan 2026): "only support up to .NET 9.0 ...
no timeline announced." Follow-up (Jan 18, 2026) confirmed still unsupported.

### Can we merge global.json files?

**No.** Two incompatible requirements coexist:

1. Blazor WASM needs SDK 9 + `wasm-tools-net9`
2. Tools project targets `net10.0`, needs SDK 10

A single `global.json` cannot satisfy both.

## Solution: `--solution` flag + auto-discovery

### Architecture

```
SlnxProjectDiscovery
├── Discover(startDirectory?)     → walks up from CWD to find nearest .slnx
└── DiscoverFromSlnx(slnxPath)    → parses explicit .slnx path

AllCommand
├── --project      → explicit override
├── --test-project → explicit override
├── --solution     → explicit .slnx path (overrides auto-discovery)
└── (default)      → auto-discovers from CWD
```

### Usage

```bash
# Analyze tools solution (from tools/)
dotnet run -- all

# Analyze main solution (from tools/)
dotnet run -- all --solution ../redmuffin.Blazor.StaticWeb.slnx

# Analyze main solution (from repo root, needs --project for dotnet)
dotnet run --project tools/src/redmuffin.Tools.QualityGates -- all

# Override individual projects
dotnet run -- all --project ../src/MyApp --test-project ../tests/MyApp.Tests
```

### Project Classification

`.csproj` files are classified by checking `<IsTestProject>true</IsTestProject>`.
Fallback: projects under `tests/` directories are treated as test projects.
Discovered `.csproj` paths are resolved to their parent directories before
passing to gates.

## Future Consideration: Downgrade Tools to .NET 9

The quality gates tool only uses standard .NET APIs (System.CommandLine,
Roslyn, YamlDotNet). It does NOT require .NET 10. Downgrading to `net9.0`
would:

- Eliminate the SDK gap entirely
- Allow single `global.json`
- Remove need for `--solution` flag (auto-discovery from CWD suffices)
- No impact on functionality

This is the cleanest long-term solution. The `--solution` flag remains
valuable for overriding auto-discovery in edge cases.
