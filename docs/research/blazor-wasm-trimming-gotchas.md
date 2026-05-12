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

### Symptom

`dotnet publish` with `PublishTrimmed=true` produces 204 assemblies
instead of 38. Build succeeds with no errors or warnings.

### Root Cause

`dotnet restore` does not know about `PublishTrimmed=true` and
`TrimMode=full` — those properties are only passed to `dotnet publish`.
The IL linker packages (`Microsoft.NET.ILLink.Tasks`) are absent from
the restore graph. When `dotnet publish --no-restore` runs, it cannot
resolve the trimmer, producing an untrimmed build.

### Fix

Remove `--no-restore` from `dotnet publish`. Let the publish command run
its own restore step so the trimming packages are resolved.

❌ Bad:

```bash
dotnet restore
dotnet publish -c Release -p:PublishTrimmed=true --no-restore
```

✅ Good:

```bash
dotnet restore
dotnet publish -c Release -p:PublishTrimmed=true
```

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
