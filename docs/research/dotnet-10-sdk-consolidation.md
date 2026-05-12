---
date: 2026-05-12
title: ".NET 10 SDK Consolidation — Full Migration Guide"
module: infrastructure
tags: [dotnet, sdk, global-json, migration, swa, ci-cd]
problem_type: architecture
---

# .NET 10 SDK Consolidation

## Decision

Use **.NET 10 SDK** as the single build tool for the entire repository.
All deployed projects target `net9.0` for Azure Static Web Apps compatibility.
The `.NET 10` SDK version is pinned in repo-root `global.json`
(`10.0.100` with `rollForward: latestMinor`).

## Why This Works

### SDK ≠ Runtime Target

The .NET SDK version and the project's `TargetFramework` are independent.
SDK 10 can build, test, and publish projects targeting `net9.0` without
issue. The SDK provides the build toolchain (MSBuild, Roslyn, NuGet). The
`TargetFramework` in each `.csproj` determines the runtime.

### Blazor WebAssembly Works Under SDK 10

The `wasm-tools` workload installed under SDK 10 includes the
`emscripten.net9` manifest, which provides all the WASM build toolchain
needed for `net9.0` Blazor projects.

Verified:

- `dotnet build` → 11 projects, 0 errors
- `dotnet publish` → 49 WASM assemblies, correctly trimmed
- `dotnet run --project tests/...` → 293 tests pass

### Azure Static Web Apps

The deployment pipeline bypasses Oryx entirely. Pre-built output from
`dotnet publish` is deployed via `swa deploy` CLI. The Functions runtime
is pinned to `dotnet-isolated:9.0` via `apiRuntime` in
`staticwebapp.config.json`.

The API project MUST target `net9.0` because SWA's managed Functions
host only supports up to .NET 9 (as of May 2026). When SWA adds
.NET 10 support, migration is a single-line change in each `.csproj`.

### Side-by-Side SDK Cleanup

Before this consolidation:

```
REPO_ROOT/global.json → SDK 9.0.100 (rollForward: latestMinor)
tools/global.json     → SDK 10.0.104 (rollForward: latestMinor)
```

After:

```
REPO_ROOT/global.json → SDK 10.0.100 (rollForward: latestMinor)
tools/global.json     → DELETED
```

### CI/CD Pipeline

Changed from `.NET 9.x` to `.NET 10.x` in `setup-dotnet@v5`. The test
step changed from `dotnet test` (VSTest, deprecated in SDK 10) to
`dotnet run --project tests/...` (TUnit native, no VSTest dependency).

All other pipeline steps remain unchanged — build and publish output
is identical.

## Verification Checklist

- [x] `dotnet build` from repo root — 11 projects, 0 errors
- [x] `dotnet run --project tests/redmuffin.Blazor.StaticWeb.Tests` — 293 pass, 0 fail
- [x] `dotnet build` from `tools/` — 3 projects, 0 errors
- [x] `dotnet run --project tests/redmuffin.Tools.QualityGates.Tests` — 283 pass, 0 fail
- [x] `dotnet publish src/redmuffin.Blazor.StaticWeb -c Release -p:PublishTrimmed=true` — 49 assemblies
- [x] `dotnet publish src/redmuffin.Blazor.StaticWeb.Api -c Release` — correct Functions output
- [x] `wasm-tools` workload installed under SDK 10
- [x] CI workflow updated to SDK 10 + `dotnet run` tests

## Rollback Plan

If SDK 10 causes unexpected issues:

1. Restore `global.json` → `version: "9.0.100"`
2. Reinstall `wasm-tools` under SDK 9: `sudo dotnet workload install wasm-tools`
3. Revert CI to `dotnet-version: "9.x"`
4. Revert CI test step to `dotnet test`

No code changes required — all `.csproj` files still target `net9.0`.
