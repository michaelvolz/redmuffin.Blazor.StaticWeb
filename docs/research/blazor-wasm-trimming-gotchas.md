---
date: 2026-05-12
title: Blazor WASM Trimming Gotchas — --no-restore and TrimMode=full
tags: [research, blazor, trimming, ci, build, deployment]
description: Two subtle Blazor WASM build configuration mistakes that cause deploy failures: --no-restore silently disables trimming, and TrimMode=full crashes the runtime.
module: build-infrastructure
problem_type: deployment
---

## Summary

Two build configuration mistakes caused repeated deploy failures. Both are
subtle — neither produces a build error. The first produces an untrimmed
(204-assembly) build. The second produces a correctly-sized (38-assembly)
build that crashes at runtime.

## Gotcha 1: --no-restore Silently Disables Trimming

Caused two deploy failures before root cause was found. The build
always succeeds — the failure is silent.

### Symptom

`dotnet publish -c Release -p:PublishTrimmed=true --no-restore`
produces 204 assemblies instead of 38. Zero build errors or warnings.
The deploy verification (assembly count check) catches it downstream.

### Root Cause

Blazor WASM's trimming requires the IL linker, which is an SDK-injected
package (`Microsoft.NET.ILLink.Tasks`) — not a project dependency, not
visible in `Directory.Packages.props`. It is resolved based on the
current SDK version and publish properties.

The CI workflow ran:

```bash
dotnet restore              # defaults to Debug, no PublishTrimmed
dotnet publish -c Release -p:PublishTrimmed=true --no-restore
```

The restore step runs in Debug mode and never sees
`PublishTrimmed=true`. The IL linker packages are never resolved.

When `dotnet publish --no-restore` runs, it cannot find the trimmer.
MSBuild silently skips the trim step — the build succeeds but produces
an untrimmed output. The `--no-restore` flag prevents recovery.

### Fix (Verified — 204 → 38 assemblies)

Remove `--no-restore` from the publish command. Let publish run its own
restore in the correct configuration so the SDK can resolve the IL
linker package.

❌ Broke twice:

```bash
dotnet restore
dotnet publish -c Release -p:PublishTrimmed=true --no-restore
```

✅ Working:

```bash
dotnet restore
dotnet publish -c Release -p:PublishTrimmed=true
```

**Alternative fix** (if `--no-restore` is truly needed): run the restore
in Release mode with trim properties set. But removing `--no-restore`
is simpler and adds negligible time (restore cache hits).

### Detection

Check assembly count in `blazor.boot.json`:

```bash
jq '.resources.fingerprinting | keys | map(select(endswith(".wasm"))) | length' \
  publish/wwwroot/_framework/blazor.boot.json
```

- Expected (trimmed): 38
- Untrimmed: 204

## Gotcha 2: TrimMode=full Crashes Blazor WASM at Runtime

### Symptom

Site loads the Blazor runtime (`blazor.webassembly.js`) but immediately
shows "An unhandled error has occurred." Build succeeds. Assembly count
is correct (38). No build warnings.

### Root Cause

`TrimMode=full` tells the IL linker to strip ALL unused types from ALL
assemblies, including framework assemblies. Blazor WASM uses reflection
extensively for:

- Dependency injection (service resolution)
- Component routing and instantiation
- JSON serialization (System.Text.Json source generators or reflection)
- JS interop marshalling

Static analysis cannot see these reflection-only references. The trimmer
removes types the runtime needs, causing a crash on first component
render or DI resolution.

The Blazor WebAssembly SDK was designed for `TrimMode=partial`, which
only trims assemblies that opt in with `[AssemblyMetadata("IsTrimmable",
"True")]`. Framework assemblies have trimmer annotations that preserve
reflection-accessed types in partial mode.

### Fix

Use `TrimMode=partial` (the Blazor SDK default). Never use
`TrimMode=full` for Blazor WASM projects.

❌ Bad:

```xml
<PublishTrimmed>true</PublishTrimmed>
<TrimMode>full</TrimMode>
```

✅ Good:

```xml
<PublishTrimmed>true</PublishTrimmed>
<!-- TrimMode omitted — defaults to partial -->
```

### Detection

The assembly count is the same (38) in both modes. Detection is
runtime-only: the site shows a Blazor error page.

## Key Insight: Assembly Count vs Trim Depth

Assembly count tells you whether trimming is active (38 vs 204). It does
NOT tell you whether trimming is configured correctly. Both `partial` and
`full` produce 38 assemblies — the difference is in how much is stripped
inside each assembly. `full` strips types the Blazor runtime needs via
reflection.

## References

- Microsoft docs: [Trimming options](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trimming-options)
- Microsoft docs: [Blazor WASM trimming](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly#trim-net-il-linker)
