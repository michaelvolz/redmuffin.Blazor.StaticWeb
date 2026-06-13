---
date: 2026-06-13
title: Blazor WASM Safari iOS 15 Compatibility — Required .csproj Properties
tags: [research, blazor, wasm, safari, ios15, compatibility]
description: Four .NET 10 Blazor WASM defaults are incompatible with iOS 15 Safari. All four must be changed for the site to load on devices capped at iOS 15 (e.g., iPhone 7 Plus).
module: build-infrastructure
problem_type: compatibility
---

## Summary

The Blazor WASM standalone frontend does not load on iOS 15 Safari
(observed on iPhone 7 Plus). Four .NET 10 defaults are incompatible.
All four must be changed in the `.csproj` — omitting any one causes
silent startup failure.

## Symptom

The site fails to load on iOS 15 Safari. The Blazor runtime never
starts — the loading indicator spins indefinitely or a blank page is
shown. No error in the browser console identifies the root cause. The
build succeeds with zero warnings.

## Root Cause

Four .NET 10 Blazor WASM defaults are incompatible with iOS 15 Safari:

- **WASM SIMD** (`WasmEnableSIMD`) — vectorized instructions for spans,
  strings, and arrays. Requires Safari 16.4+. Default: `true`.
- **WASM exception handling** (`WasmEnableExceptionHandling`) — native
  try/catch without JavaScript bridge. Requires Safari 16.4+. Default:
  `true`.
- **JITerpreter** (`BlazorWebAssemblyJiterpreter`) — browser-specific JIT
  compiler that optimizes hot code paths in interpreted (non-AOT) mode.
  Its `do_jit_call` path does not handle the JS-based exception fallback
  correctly on Safari < 16.4 (dotnet/runtime#95963). Default: `true`.
- **Memory ceiling** (`EmccMaximumHeapSize`) — maximum WASM linear memory.
  iOS Safari can reject module instantiation when the stated maximum
  exceeds available per-tab memory (dotnet/runtime#84638). Default:
  `2147483648` (2GB).

Both SIMD and exception handling require a unique rebuild of the .NET
runtime (different `.wasm` and `.js` files). When the browser doesn't
support a feature, the runtime binary is rejected at load time and the
app never starts.

iPhone 7 Plus is permanently capped at iOS 15.

## Fix

Set all four properties in the `.csproj`:

```xml
<PropertyGroup>
  <WasmEnableSIMD>false</WasmEnableSIMD>
  <WasmEnableExceptionHandling>false</WasmEnableExceptionHandling>
  <BlazorWebAssemblyJiterpreter>false</BlazorWebAssemblyJiterpreter>
  <EmccMaximumHeapSize>268435456</EmccMaximumHeapSize>
</PropertyGroup>
```

Apply to both Debug and Release configurations. The wasm-tools workload
handles the runtime rebuild for SIMD and exception handling during
`dotnet publish`. The JITerpreter and memory ceiling do not require a
rebuild — they are JavaScript-layer and linker parameters respectively.

## Detection

Test on iOS 15 Safari. The site either loads or shows a blank page /
infinite spinner. No build-time detection exists — all four defaults
compile without warnings.

## Future Re-Enable

When the minimum supported Safari version reaches 16.4+, the three
runtime properties (SIMD, EH, JITerpreter) can be re-enabled to restore
throughput on spans, strings, arrays, JSON parsing, and hot code paths.
The three share the same browser cutoff and must always change together.
The memory ceiling should remain reduced for iOS compatibility.

## References

- [dotnet/runtime#84638](https://github.com/dotnet/runtime/issues/84638) — iOS Safari rejects large WASM memory ceilings
- [dotnet/runtime#95963](https://github.com/dotnet/runtime/issues/95963) — JITerpreter `do_jit_call` fails with JS-based exception fallback
- [dotnet/runtime#104895](https://github.com/dotnet/runtime/issues/104895) — Unresolved iOS 15.4 crash (Future milestone)
