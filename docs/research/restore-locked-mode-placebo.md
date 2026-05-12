---
date: 2026-05-12
title: RestoreLockedMode is a Placebo — Zero Performance Benefit, Actively Harmful for Blazor WASM
tags: [research, nuget, ci, blazor, performance, build]
description: RestoreLockedMode provides no CI speed improvement and causes NU1004 failures in Blazor WASM projects due to SDK-linked transitive package drift.
module: build-infrastructure
problem_type: infrastructure
---

## Summary

`RestoreLockedMode` in `Directory.Build.props` was set to `true` based on a common copy-pasted pattern claiming CI performance improvements. Research disproves this — zero speed benefit. For Blazor WASM projects, it causes repeated NU1004 deploy failures because the SDK injects transitive packages (`Microsoft.NET.ILLink.Tasks`, `Microsoft.NET.Sdk.WebAssembly.Pack`) that vary by SDK version.

## The Claim

Many repos copy-paste this pattern:

```xml
<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
<RestoreLockedMode>true</RestoreLockedMode>
```

The claim: NuGet skips dependency graph resolution when a lock file is present, making `dotnet restore` faster in CI.

## The Reality

**Null result.** G-Research ran real benchmarks (2022) using NuGet.Client's own performance test scripts:

| Scenario                | Without lock | With lock |
| ----------------------- | ------------ | --------- |
| Cold cache              | Same         | Same      |
| Re-restore              | Same         | Same      |
| NoOp (already restored) | Same         | Same      |

The .NET profiler confirmed why: **`WalkDependenciesAsync` is always called** even with a lock file present. The NuGet team's own `.NET 9 / NuGet 6.12` resolver rewrite (5x+ improvement) is the actual performance play — not lock files.

The official Microsoft announcement blog post for lock files (2018) uses the words "performance" and "faster" **zero times**. The feature is a **reproducibility** and **supply-chain security** tool (content hash validation), not a performance optimization.

## Why It Breaks Blazor WASM

The Blazor WebAssembly SDK injects transitive packages that don't appear in the project file:

- `Microsoft.NET.ILLink.Tasks` (varies by SDK build)
- `Microsoft.NET.Sdk.WebAssembly.Pack` (varies by SDK version)
- `Microsoft.DotNet.HotReload.WebAssembly.Browser` (appears/disappears across SDK releases)

These packages vary across SDK versions. Our `global.json` uses `9.0.100` with `rollForward: latestMinor` — the CI runner installs whatever `9.x` is current via `setup-dotnet@v5`. When the lock file was generated under SDK 9.0.313 on the dev machine, CI's SDK 9.0.315 produces a different graph → NU1004.

Open, unresolved `.NET SDK` issues confirming this:

- **dotnet/sdk#51675**: `contentHash` of `Microsoft.NET.Sdk.WebAssembly.Pack` differs between Windows and Linux
- **dotnet/sdk#52331**: Phantom SDK-linked packages cause NU1004 mismatches
- **dotnet/aspnetcore#64897**: `Microsoft.AspNetCore.App.Internal.Assets` appears in lock file but isn't referenced

## What Major .NET Projects Do

- **dotnet/runtime**: No `RestoreLockedMode`. No `RestorePackagesWithLockFile`. No lock files in the repo.
- **dotnet/aspnetcore**: Same — neither property used.

## Real CI Performance Wins

1. NuGet 6.12 resolver rewrite (.NET 9): claimed 5x resolution speed improvement
2. Package source mapping: cuts unnecessary server queries
3. NuGet cache persistence on CI runners: eliminates download I/O
4. `dotnet restore` built-in no-op when assets are already fresh

## Decision

Remove `RestoreLockedMode=true` from the main solution's `Directory.Build.props`. Keep `RestorePackagesWithLockFile=true` — the lock file is harmless and provides dependency auditability.

**Performance impact of this change: zero.**
**CI stability impact: fixes NU1004 permanently.**

## References

- G-Research lock file benchmarks (2022): internal CI tests on NuGet.Client perf suite
- Microsoft lock file announcement: <https://devblogs.microsoft.com/nuget/enable-repeatable-package-restores-using-a-lock-file/>
- dotnet/sdk#51675: Blazor WASM lock file cross-platform contentHash mismatch
- dotnet/sdk#52331: Phantom SDK-linked packages in Blazor lock files
