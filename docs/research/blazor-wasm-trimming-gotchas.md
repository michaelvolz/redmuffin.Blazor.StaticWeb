---
date: 2026-06-13
title: Blazor WASM Build Gotchas — --no-restore, --no-build, TrimMode=full, and Old Safari WASM Features
tags: [research, blazor, trimming, fingerprinting, ci, build, deployment, wasm, safari]
description: Four subtle Blazor WASM build configuration mistakes that cause deploy or runtime failures: --no-restore silently disables trimming, --no-build breaks .NET 10 fingerprinting, TrimMode=full crashes the runtime, and default WASM SIMD/exception handling breaks Safari < 16.4.
module: build-infrastructure
problem_type: deployment
---

## Summary

Four build configuration mistakes caused repeated deploy or runtime
failures. All four are subtle — none produces a build error. The first
produces an untrimmed (204-assembly) build. The second produces a
correctly-sized (38-assembly) build that crashes at runtime. The third
leaves fingerprint placeholders unreplaced (404 on the Blazor runtime).
The fourth silently breaks the runtime on Safari < 16.4 (iOS 15,
macOS 12).

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

### Detection (.NET 9 and earlier)

Check assembly count in `blazor.boot.json` (removed in .NET 10):

```bash
jq '.resources.fingerprinting | keys | map(select(endswith(".wasm"))) | length' \
  publish/wwwroot/_framework/blazor.boot.json
```

- Expected (trimmed): 38
- Untrimmed: 204

On .NET 10, count WASM assemblies directly:

```bash
find publish/wwwroot/_framework -maxdepth 1 -name '*.wasm' -not -name 'dotnet.native.*' | wc -l
# Expected: ≤60, Untrimmed: >120
```

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

### 3. IL2026: Trim warnings unmasked when trimming actually works

**Date discovered**: 2026-05-12

**The duality**: When trimming is broken (--no-restore), the linker never
runs and IL2026 warnings never fire. Fixing trimming unmasked pre-existing
trim-unsafe JSON API usage in `CreatorReferenceConverter.cs`.

**Detection**: IL2026 errors during `dotnet publish -p:PublishTrimmed=true`.

**Root cause**: `JsonSerializer.Deserialize<TValue>()` and
`JsonSerializer.Serialize<TValue>()` (generic overloads) carry
`[RequiresUnreferencedCode]`. These are the _only_ overloads flagged by
the trim analyzer — even the non-generic `Type`-based overloads are marked
trim-unsafe in .NET 9.

**Fix**: Source-generated `JsonSerializerContext` with `JsonTypeInfo<T>`.
Add the affected type to the context's `[JsonSerializable]` attributes,
then call the `JsonTypeInfo`-based overloads:

```csharp
// BEFORE (IL2026):
JsonSerializer.Deserialize<CreatorReference>(ref reader, options);
JsonSerializer.Serialize(writer, value, options);

// AFTER (trim-safe, source-generated):
JsonSerializer.Deserialize(ref reader, RaindropJsonSerializerContext.Default.CreatorReference);
JsonSerializer.Serialize(writer, value, RaindropJsonSerializerContext.Default.CreatorReference);
```

**Verification**: `dotnet publish -p:PublishTrimmed=true` must produce
zero IL2026/IL2067/IL2070 warnings. `TreatWarningsAsErrors=true` makes
this enforcement automatic.

## Gotcha 3: --no-build Breaks Blazor WASM Fingerprinting (.NET 10)

**Date discovered**: 2026-06-12

### Symptom

`dotnet publish --no-build -c Release` leaves `#[.{fingerprint}]`
placeholders unreplaced in `index.html`. The browser requests
`_framework/blazor.webassembly#[.{fingerprint}].js` literally, gets
a 404, and the app never loads. The build succeeds with zero errors
or warnings. The `dotnet.publish.manifest.json` references are also
left with placeholder names.

### Root Cause

.NET 10's Blazor WASM asset fingerprinting (`OverrideHtmlAssetPlaceholders`)
requires the full publish pipeline — the build step computes asset
hashes that the publish step uses to replace placeholders. The
`--no-build` flag skips the build step, so the publish step receives
no hash data. Microsoft has confirmed this is by design: the publish
pipeline always sources content from the intermediate (`obj/`)
directory regardless of `--no-build` (dotnet/sdk#52168).

The .NET 9 content-file naming bug (dotnet/aspnetcore#58321) was
fixed in the .NET 10 SDK (dotnet/sdk#44160). The fingerprinting
issue (dotnet/aspnetcore#64543, reported 2025-11-26 for .NET
10.0.100) is a separate, unfixed limitation.

### Fix

Never use `--no-build` for Blazor WASM `dotnet publish`. Always run
the full publish command:

✅ Working:

```bash
dotnet publish -c Release -p:PublishTrimmed=true --no-restore
```

❌ Broken (.NET 10):

```bash
dotnet publish -c Release -p:PublishTrimmed=true --no-restore --no-build
```

The `--no-restore` flag is safe (restore was run separately with
correct properties). The `--no-build` flag is never safe for Blazor
WASM publish on .NET 10.

### Detection

After publish, grep the output `index.html` for unreplaced
placeholders:

```bash
grep -c '#\[\.{fingerprint}\]' publish/wwwroot/index.html
# Expected: 0
# Broken:  >0 (placeholders not replaced)
```

## Gotcha 4: WASM SIMD and Exception Handling Break Old Safari

**Date discovered**: 2026-06-13

### Symptom

The site fails to load on Safari < 16.4 (iOS 15, macOS 12 and earlier).
The Blazor runtime never starts — the loading indicator spins forever or
a blank page is shown. No error in the browser console that identifies
the root cause. The build succeeds with zero warnings.

### Root Cause

.NET 8+ enables two WASM features by default that Safari did not support
until version 16.4 (March 2023):

- **WASM SIMD** (`WasmEnableSIMD`) — vectorized instructions for spans,
  strings, and arrays. Requires Safari 16.4+.
- **WASM exception handling** (`WasmEnableExceptionHandling`) — native
  try/catch without JavaScript bridge. Requires Safari 16.4+.

Both features require a unique rebuild of the .NET runtime (different
`.wasm` and `.js` files). When the browser doesn't support the feature,
the runtime binary is rejected at load time and the app never starts.

iPhone 7 Plus is permanently capped at iOS 15. Any device that cannot
upgrade past iOS 15 or macOS 12 is affected.

### Fix

Set both properties to `false` in the `.csproj`:

✅ Working:

```xml
<PropertyGroup>
  <WasmEnableSIMD>false</WasmEnableSIMD>
  <WasmEnableExceptionHandling>false</WasmEnableExceptionHandling>
</PropertyGroup>
```

Apply to both Debug and Release configurations. The wasm-tools workload
handles the runtime rebuild during `dotnet publish`.

### Detection

Test on the oldest supported device. The site either loads (compatible)
or shows a blank page / infinite spinner (incompatible). No build-time
detection exists — both features compile without warnings regardless of
target browser support.

### Future re-enable

When the minimum supported Safari version reaches 16.4+, remove both
properties (or set to `true`) to restore throughput on spans, strings,
arrays, and JSON parsing. The two settings should always be changed
together — they share the same browser cutoff.

## References

- Microsoft docs: [Trimming options](https://learn.microsoft.com/en-us/dotnet/core/deploying/trimming/trimming-options)
- Microsoft docs: [Blazor WASM trimming](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly#trim-net-il-linker)
- [dotnet/aspnetcore#58321](https://github.com/dotnet/aspnetcore/issues/58321) — .NET 9 `--no-build` content file naming (fixed)
- [dotnet/aspnetcore#64543](https://github.com/dotnet/aspnetcore/issues/64543) — .NET 10 `--no-build` fingerprinting broken (open)
- [dotnet/sdk#52168](https://github.com/dotnet/sdk/issues/52168) — publish sources from obj/, not bin/ (by design)
