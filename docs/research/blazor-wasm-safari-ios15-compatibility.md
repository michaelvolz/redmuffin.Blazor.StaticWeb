---
date: 2026-06-13
title: Blazor WASM Safari iOS 15 Compatibility — Required .csproj Properties
tags: [research, blazor, wasm, safari, ios15, compatibility, importmap]
description: Five .NET 10 Blazor WASM defaults are incompatible with iOS 15 Safari. All five must be changed for the site to load on devices capped at iOS 15 (e.g., iPhone 7 Plus).
module: build-infrastructure
problem_type: compatibility
---

## Summary

The Blazor WASM standalone frontend does not load on iOS 15 Safari
(observed on iPhone 7 Plus running iOS 15.5). Five .NET 10 defaults
are incompatible. All five must be changed in the `.csproj` — omitting
any one causes silent startup failure.

## Symptom

The site fails to load on iOS 15 Safari. The Blazor runtime never
starts — the loading indicator spins indefinitely or a blank page is
shown. The `blazor.webassembly.js` script downloads successfully (no
network error) but the `Blazor` global is never defined. No error in
the browser console identifies the root cause. The build succeeds with
zero warnings.

## Root Cause

Five .NET 10 Blazor WASM defaults are incompatible with iOS 15 Safari:

### WASM Runtime Features

- **WASM SIMD** (`WasmEnableSIMD`) — vectorized instructions for spans,
  strings, and arrays. Requires Safari 16.4+. Default: `true`.
- **WASM exception handling** (`WasmEnableExceptionHandling`) — native
  try/catch without JavaScript bridge. Requires Safari 16.4+. Default:
  `true`.
- **JITerpreter** (`BlazorWebAssemblyJiterpreter`) — browser-specific JIT
  compiler that optimizes hot code paths in interpreted (non-AOT) mode.
  Its `do_jit_call` path does not handle the JS-based exception fallback
  correctly on Safari < 16.4 (dotnet/runtime#95963). Default: `true`.

SIMD and exception handling require a unique rebuild of the .NET
runtime via the `wasm-tools` workload. The JITerpreter does not require
a rebuild — it is a JavaScript-layer flag.

### Memory

- **Memory ceiling** (`EmccMaximumHeapSize`) — maximum WASM linear memory.
  iOS Safari can reject module instantiation when the stated maximum
  exceeds available per-tab memory (dotnet/runtime#84638). Default:
  `2147483648` (2GB).

### JavaScript Module Resolution

- **Import maps** — .NET 10's Blazor bootloader loads `dotnet.js` via
  `import("./dotnet.js")` and relies on `<script type="importmap">` to
  resolve fingerprinted framework JS paths. Safari < 16.4 ignores
  `type="importmap"` entirely — the `<script>` is inert. The import
  call looks for the unfingerprinted path, which does not exist. The
  script downloads without error but the internal module resolution
  fails silently. Default: framework JS is fingerprinted.

iPhone 7 Plus is permanently capped at iOS 15.

## Fix

Set all five properties in the `.csproj`:

```xml
<PropertyGroup>
  <WasmEnableSIMD>false</WasmEnableSIMD>
  <WasmEnableExceptionHandling>false</WasmEnableExceptionHandling>
  <BlazorWebAssemblyJiterpreter>false</BlazorWebAssemblyJiterpreter>
  <EmccMaximumHeapSize>268435456</EmccMaximumHeapSize>
  <BlazorFingerprintBlazorJs>false</BlazorFingerprintBlazorJs>
  <WasmFingerprintAssets>false</WasmFingerprintAssets>
</PropertyGroup>
```

Apply to both Debug and Release configurations.

`WasmFingerprintAssets=false` disables fingerprinting for WASM SDK assets
(`dotnet.js`, `dotnet.native.js`, `dotnet.runtime.js`, `.wasm` files).
`BlazorFingerprintBlazorJs=false` disables it for `blazor.webassembly.js`.
Together they ensure all framework files exist at their literal names,
so `import("./dotnet.js")` resolves directly without the import map.

### JavaScript Syntax Transpilation

The .NET 10 SDK compiles `blazor.webassembly.js` with a TypeScript
target of ES2024. The generated file contains one class static
initialization block — `static{this.nextEventDelegatorId=0}` in the
`T` event delegator class — which is a syntax error on Safari < 16.4.

This cannot be prevented with MSBuild properties. The deploy workflow
replaces it with equivalent static class field syntax after publish:

```bash
sed -i 's/static{this\.nextEventDelegatorId=0}/static nextEventDelegatorId=0;/' blazor.webassembly.js
```

Static class fields (`static x = 0`) are supported since Safari 14.1.

After the transpilation, compressed variants (`.br`, `.gz`) must be
regenerated with `brotli --best -f` and `gzip`. The `-f` flag is
required because MSBuild generates the initial compressed files during
publish — brotli refuses to overwrite an existing output file without it.

A post-transpile grep gate verifies zero `static{` patterns remain. If a
future Blazor SDK update introduces new class static blocks with
different content, the sed won't catch them and the gate fails the build.

## Diagnosis History

The site initially failed with a blank loading screen on iOS 15.5
(iPhone 7 Plus). The root cause was identified through progressive
in-browser diagnostics — since iOS 15 Safari lacks console access,
four independent `<pre>` elements reported JavaScript execution state
directly on the page.

| Layer                  | Finding                                                                                    | Fix                                                              |
| ---------------------- | ------------------------------------------------------------------------------------------ | ---------------------------------------------------------------- |
| 1. WASM features       | SIMD and EH enabled by default (Safari 16.4+)                                              | `WasmEnableSIMD=false`, `WasmEnableExceptionHandling=false`      |
| 2. JITerpreter         | `do_jit_call` fails with JS-based EH fallback                                              | `BlazorWebAssemblyJiterpreter=false`                             |
| 3. Memory ceiling      | 2GB default can be rejected on iOS                                                         | `EmccMaximumHeapSize=268435456`                                  |
| 4. Import maps         | `<script type="importmap">` ignored (Safari 16.4+), `import("./dotnet.js")` fails silently | `BlazorFingerprintBlazorJs=false`, `WasmFingerprintAssets=false` |
| 5. Class static blocks | `static{this.nextEventDelegatorId=0}` is a SyntaxError (Safari 16.4+)                      | CI sed transpilation to static class field                       |

Each layer was proven before moving to the next — layers 1-3 produced
no visible improvement, layer 4 made the script download but Blazor
remained undefined, layer 5's `boot:parse:SyntaxError` diagnostic
confirmed the exact syntax failure.

## Detection

Test on iOS 15 Safari. The site either loads or shows a blank page /
infinite spinner. No build-time detection exists — all five defaults
compile without warnings.

## Future Re-Enable

When the minimum supported Safari version reaches 16.4+:

- **Three runtime properties** (SIMD, EH, JITerpreter) — re-enable to
  restore throughput on spans, strings, arrays, JSON parsing, and hot
  code paths. These share the same browser cutoff and must always
  change together.
- **Memory ceiling** — should remain reduced for iOS compatibility
  regardless of Safari version.
- **Framework JS fingerprinting** — remove `BlazorFingerprintBlazorJs`
  and `WasmFingerprintAssets` to restore immutable cache headers for
  framework assets.

## References

- [dotnet/runtime#84638](https://github.com/dotnet/runtime/issues/84638) — iOS Safari can reject large WASM memory ceilings
- [dotnet/runtime#95963](https://github.com/dotnet/runtime/issues/95963) — JITerpreter `do_jit_call` fails with JS-based exception fallback
- [dotnet/runtime#104895](https://github.com/dotnet/runtime/issues/104895) — Unresolved iOS 15.4 crash (Future milestone)
- [dotnet/sdk#46988](https://github.com/dotnet/sdk/pull/46988) — `BlazorFingerprintBlazorJs` MSBuild property
- [caniuse.com/import-maps](https://caniuse.com/import-maps) — Safari import map support starts at 16.4
