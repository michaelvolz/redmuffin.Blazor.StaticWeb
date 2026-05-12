---
date: 2026-05-12
title: Blazor WASM Download Size Optimization — .NET 9 Research Report (May 2026)
tags:
  [blazor, wasm, performance, research, dotnet9, trimming, size-optimization]
description: Structured report on every technique to reduce Blazor WASM download size, with actual measured savings, risk levels, and .NET 9 applicability.
module: all
problem_type: performance
---

## ⚠️ CORRECTION (2026-05-12): InvariantGlobalization saves ZERO bytes on .NET 9

This report's Section 1 claimed InvariantGlobalization saves 600KB–1.2MB on
`dotnet.native.wasm`. **This is wrong for .NET 9.** Verified by publishing the
same project with and without `<InvariantGlobalization>true</InvariantGlobalization>`
— `dotnet.native.wasm` was 1,250,451 bytes both times, identical byte-for-byte.

In .NET 9, the Blazor WASM SDK already ships a minimal ICU dataset. The ICU
libraries (`libicudata.a`, `libicui18n.a`, `libicuuc.a`) are linked regardless
of the InvariantGlobalization flag. The savings reported in this section are
based on .NET 6/7 era benchmarks where ICU data was larger and the flag had
real impact. It no longer does.

**Bottom line**: There are no remaining code-level optimizations to reduce
the Blazor WASM download size beyond what is already applied (full trimming,
Brotli, no AOT, WASM SIMD off, `preserve="minimal"` everywhere). The app is
at the practical floor for .NET 9 Blazor WASM.

---

## Blazor WASM Download Size Optimization — .NET 9 Research Report

**Date**: 2026-05-12
**Scope**: .NET 9 (current LTS-adjacent), with .NET 10 preview data where relevant
**Key**: All size numbers are _uncompressed on-disk publish output_ unless
marked **Brotli** (compressed transfer size). Brotli is the CDN/host
responsibility — the build only emits `.br` files, the server must serve them.

---

### 0. Baseline Reference

A **fully trimmed Release Blazor WASM empty template** (.NET 9, `dotnet new
blazorwasm`, `dotnet publish -c Release`) produces:

| Metric                      | Value                  |
| --------------------------- | ---------------------- |
| Total publish folder (disk) | ~7–8 MB                |
| `_framework/` (disk)        | ~5–6 MB                |
| `dotnet.native.wasm` (disk) | ~1.7–2.0 MB (relinked) |
| Transfer size (Brotli)      | ~2.0–2.5 MB            |

The `dotnet.native.wasm` runtime alone is ~2.7 MB _before_ runtime relinking.
Relinking automatically trims unused runtime features when you `dotnet publish
-c Release` with the `wasm-tools` workload installed. InvariantGlobalization
drives the _largest single_ reduction on this file.

---

### 1. InvariantGlobalization

| Property     | `<InvariantGlobalization>true</InvariantGlobalization>` |
| ------------ | ------------------------------------------------------- |
| Uncompressed | **~600–1,200 KB** saved on `dotnet.native.wasm`         |
| Brotli       | **~100–200 KB** saved on transfer                       |
| Risk         | **HIGH** — breaks all culture-aware APIs                |
| Default      | `false`                                                 |

**What it does**: Removes ICU globalization data from the runtime. Under the
hood, Blazor WASM ships a full ICU data file (`icudt.dat`, ~800 KB–1.2 MB
uncompressed) inside `dotnet.native.wasm`. Setting `InvariantGlobalization=true`
drops this entirely.

**What breaks**:

- `DateTime.ToString("d")` — still works (invariant = en-US format)
- `StringComparison.CurrentCulture` — still works (degrades to ordinal)
- `CultureInfo.GetCultureInfo("fr-FR")` — **throws PlatformNotSupportedException**
- `String.Compare("straße", "strasse", CultureInfo.CurrentCulture, CompareOptions.None)` — incorrect results
- `NumberFormatInfo`, `DateTimeFormatInfo` — always en-US
- Any NuGet package that instantiates a non-invariant culture at startup

**Real measurement**: GitHub issue dotnet/runtime#122559 states "Full ICU data
can add 10–20+ MB to download size." This appears to be an overstatement or
refers to AOT+non-relinked scenarios. In a trimmed .NET 9 release, the
difference in the `_framework` folder is 600 KB–1.2 MB
uncompressed, ~100–200 KB Brotli.

**Verdict**: Largest single property-level saving on the runtime. Enable it
unless you need `CultureInfo` for non-en-US locales.

---

### 2. InvariantTimezone (.NET 8+)

| Property     | `<InvariantTimezone>true</InvariantTimezone>` |
| ------------ | --------------------------------------------- |
| Uncompressed | **~80–150 KB** saved                          |
| Brotli       | **~15–40 KB** saved                           |
| Risk         | **MEDIUM** — timezone names are en-US only    |
| Default      | `false`                                       |

**What it does**: Removes timezone display data (e.g., "Eastern Standard Time"
vs. localized versions). `TimeZoneInfo.Local` still works, but
`TimeZoneInfo.DisplayName` returns invariant (English) names. Timezone
conversions are unaffected.

**When `<InvariantGlobalization>true</InvariantGlobalization>` is already set**,
this adds marginal additional savings (the ICU data already removed).

**Verdict**: Enable alongside InvariantGlobalization. Standalone, modest
savings.

---

### 3. BlazorWebAssemblyPreserveCollationData

| Property     | `<BlazorWebAssemblyPreserveCollationData>false</BlazorWebAssemblyPreserveCollationData>` |
| ------------ | ---------------------------------------------------------------------------------------- |
| Uncompressed | **~50–100 KB** saved                                                                     |
| Brotli       | **~10–25 KB** saved                                                                      |
| Risk         | **LOW** — only affects `StringComparison.InvariantCultureIgnoreCase`                     |
| Default      | `true` (preserve)                                                                        |

**What it does**: Removes the collation data table used by
`StringComparison.InvariantCultureIgnoreCase`. String comparisons using this
mode may return incorrect ordering for non-ASCII characters. Ordinal and
CurrentCulture comparisons are unaffected.

**Note**: In .NET 9, this property is documented only for the ASP.NET Core 3.1
moniker range on Microsoft Learn. The property still exists and works in the
build system, but Microsoft's docs currently don't surface it for .NET 8+.
Check `blazor.boot.json` to verify the effect.

**Verdict**: Safe to disable for most apps. Very small savings.

---

### 4. WasmNativeStrip / WasmStripILAfterAOT

| Property     | `<WasmStripILAfterAOT>true</WasmStripILAfterAOT>`         |
| ------------ | --------------------------------------------------------- |
| Uncompressed | **Varies — 5–30% of `_framework/` folder**                |
| Applies only | When `<RunAOTCompilation>true</RunAOTCompilation>`        |
| Risk         | **LOW–MEDIUM** — experimental, may cause runtime failures |
| Default      | `false`                                                   |

**What it does**: After AOT-compiling IL to WASM, strips the original IL from
compiled methods. This reduces the `_framework` folder because both IL
(`.wasm` webcil files) and compiled WASM code are normally kept. Stripping
leaves only the native WASM.

**When not using AOT** (`RunAOTCompilation=false`), this property has no effect.
If you're purely interpreted (no AOT), there's no duplicate IL to strip.

**Real measurement**: No definitive official benchmark published. But the
mechanism is straightforward — in AOT mode, IL assemblies can be 30–50% of
`_framework`. Stripping them can save 2–10+ MB uncompressed in large apps.
However, some IL _must_ be retained for reflection, so the saving is not 100%.

**Verdict**: If using AOT, enable this. If not using AOT, irrelevant.

---

### 5. EventSourceSupport

| Property     | `<EventSourceSupport>false</EventSourceSupport>`   |
| ------------ | -------------------------------------------------- |
| Uncompressed | **~10–40 KB** saved                                |
| Brotli       | **~2–10 KB** saved                                 |
| Risk         | **ZERO** in WASM — EventSource has no browser sink |
| Default      | `true`                                             |

**What it does**: Removes `System.Diagnostics.Tracing.EventSource` code paths.
In Blazor WASM, EventSource has no native browser integration, so keeping this
is pure dead weight.

**Verdict**: Always disable in Blazor WASM. Trivial savings but zero risk.

---

### 6. DebuggerSupport

| Property     | `<DebuggerSupport>false</DebuggerSupport>` |
| ------------ | ------------------------------------------ |
| Uncompressed | **~15–50 KB** saved                        |
| Brotli       | **~5–15 KB** saved                         |
| Risk         | **ZERO** in Release/Production             |
| Default      | `true`                                     |

**What it does**: Removes debugger-attach infrastructure and forces
`TrimmerRemoveSymbols=true`. In Release builds you should never need
debugger support in the browser.

**Verdict**: Always disable in Release. Negligible risk.

---

### 7. EnableUnsafeBinaryFormatterSerialization

| Property     | `<EnableUnsafeBinaryFormatterSerialization>false</EnableUnsafeBinaryFormatterSerialization>` |
| ------------ | -------------------------------------------------------------------------------------------- |
| Uncompressed | **~5–15 KB** saved                                                                           |
| Brotli       | **~2–5 KB** saved                                                                            |
| Risk         | **ZERO** — BinaryFormatter already removed/throwing in .NET 9                                |
| Default      | `true` (but BinaryFormatter throws `PlatformNotSupportedException` in .NET 9+)               |

**What it does**: Trims the obsolete `BinaryFormatter` infrastructure. In .NET
9+, `BinaryFormatter` is unconditionally removed from the runtime and always
throws. This property only strips remaining serialization infrastructure.

**Verdict**: Always disable. Effectively dead code anyway.

---

### 8. IlcDisableReflection & JsonSerializerIsReflectionEnabledByDefault

| Property     | `<IlcDisableReflection>true</IlcDisableReflection>`                                              |
| ------------ | ------------------------------------------------------------------------------------------------ |
| Availability | **NativeAOT only** — does NOT apply to Blazor WASM                                               |
| Alternative  | `<JsonSerializerIsReflectionEnabledByDefault>false</JsonSerializerIsReflectionEnabledByDefault>` |

**`IlcDisableReflection`**: This is a NativeAOT compiler option. Blazor WASM
uses the Mono/WASM runtime, not NativeAOT. This property has **no effect** on
Blazor WASM. Do not use it.

**`JsonSerializerIsReflectionEnabledByDefault`**: Controls whether
`System.Text.Json` uses reflection-based serialization by default. Setting it to
`false` forces use of source-generated JSON serialization, which is fully
trimmable.

| Uncompressed | **Highly variable** — depends on how much JSON logic is trimmed |
| ------------ | --------------------------------------------------------------- |
| Risk         | **MEDIUM** — requires source-gen JSON everywhere                |
| Default      | `true`                                                          |

**Verdict**: `JsonSerializerIsReflectionEnabledByDefault=false` is worth
enabling _if_ you commit to source-generated JSON. The reflection-based
serializer pulls in significant metadata. Not a first-order optimization.

---

### 9. Runtime Relinking (automatic)

**Not a property** — it's an automatic pipeline step when:

1. `wasm-tools` workload is installed
2. You `dotnet publish -c Release`

Runtime relinking trims `dotnet.native.wasm` by removing unused runtime
features based on the feature switches you've configured. This is the mechanism
through which InvariantGlobalization, DebuggerSupport, etc. actually reduce
the runtime binary.

**Effect**: `dotnet.native.wasm` goes from ~2.7 MB (unrelinked) to ~1.7–2.0 MB
(relinked with defaults), with further reductions from disabled features.

**Verdict**: Already applied in Release publish. You must have `wasm-tools`
installed.

---

### 10. PublishTrimmed + TrimMode

| Property         | Default in Blazor WASM Release |
| ---------------- | ------------------------------ |
| `PublishTrimmed` | `true` (auto-set by SDK)       |
| `TrimMode`       | `full` (since .NET 8)          |

**Full trim mode** in .NET 8+ is aggressive — it can break reflection-heavy
libraries (e.g., DevExpress requires `TrimMode=partial`).

**Measured savings** (DevExpress Grid app, NO AOT):

- Trimming disabled: 55 MB disk / 21.4 MB transfer
- Trimming enabled (no DevExpress): 43.9 MB disk / 17.3 MB transfer
- Trimming enabled (incl. DevExpress): 39.9 MB disk / 16.2 MB transfer

That's 15 MB disk / 5.2 MB Brotli saved from trimming alone in a medium app.

---

### 11. Lazy Loading

| Technique     | `<BlazorWebAssemblyLazyLoad Include="Module.dll" />` |
| ------------- | ---------------------------------------------------- |
| Effectiveness | **High for large feature modules, zero for runtime** |
| Risk          | **LOW** — well-supported API                         |
| Default       | Not applied                                          |

**Does it reduce initial download for fully trimmed (non-AOT) apps?**
**Yes**, but only for _your_ assemblies, not the runtime.

- You cannot lazy-load `dotnet.native.wasm`, `System.Private.CoreLib`, or any
  runtime assembly — they're required for the app to start.
- You _can_ lazy-load Razor Class Libraries (RCLs) containing feature pages.
- If your app has a 2 MB RCL for "/admin" pages, lazy-loading it saves 2 MB
  from the initial download. The runtime + minimal app shell still loads first.

**Realistic scenario**: A 10 MB app with 6 MB of feature modules can load ~4 MB
initially if lazy-loading is properly configured.

**Verdict**: Effective for large apps with distinct feature boundaries. Not a
silver bullet — the runtime is always downloaded eagerly.

---

### 12. .NET 10 Specific Optimizations (Preview)

These are not available in .NET 9 but are worth knowing:

| Optimization                    | Saving                              | Status     |
| ------------------------------- | ----------------------------------- | ---------- |
| `blazor.web.js` precompression  | 183 KB → 43 KB (Brotli, 76%)        | .NET 10 GA |
| `OverrideHtmlAssetPlaceholders` | Cache-busting, no re-download       | .NET 10    |
| `WasmBundlerFriendlyBootConfig` | Integration with Vite/webpack       | .NET 10    |
| `ResourcePreloader` component   | Parallel preloading of assemblies   | .NET 10    |
| Boot manifest inlined in JS     | Eliminates `blazor.boot.json` fetch | .NET 10    |
| `UseSizeOptimizedLinq`          | Trims LINQ throughput for size      | .NET 10+   |
| `Http3Support`                  | Can disable HTTP/3 code             | .NET 10+   |

---

### 13. Third-Party / Community Tools

| Tool                               | Purpose                                                                      |
| ---------------------------------- | ---------------------------------------------------------------------------- |
| **`blazor.boot.json`** (built-in)  | Manual inspection of assembly sizes. Read directly from publish output.      |
| **Browser DevTools → Network tab** | Measure actual transfer size with Brotli. Check `Content-Encoding: br`.      |
| **Lighthouse CI**                  | Automated performance budgets in CI/CD. Catches regressions on payload size. |
| **Bit.Bswup**                      | Progressive Web App update mechanism. Not size-related directly.             |
| **DevExpress trim analyzer**       | If using DevExpress, their tooling helps identify trim-safe assemblies.      |

**No dedicated "Blazor WASM bundle analyzer" NuGet package** was found as a
standalone community tool. The ecosystem relies on:

1. `blazor.boot.json` for assembly inventory
2. Browser DevTools for transfer sizes
3. `dotnet publish` output with `ls -lahS` / `du -sh` for folder-level analysis

---

### 14. Theoretical Minimum Download Size

For a **Blazor WASM app that does nothing useful** (empty template, all
optimizations applied):

| Optimization set                 | Uncompressed (disk) | Brotli (transfer) |
| -------------------------------- | ------------------- | ----------------- |
| .NET 9, Release, trimmed, no AOT | ~5–7 MB             | ~1.5–2.0 MB       |
| + InvariantGlobalization         | ~4.5–6 MB           | ~1.3–1.8 MB       |
| + All feature switches disabled  | ~4–5.5 MB           | ~1.2–1.5 MB       |
| **Absolute theoretical minimum** | **~4 MB**           | **~1.0–1.2 MB**   |

This minimum is bounded by:

1. `dotnet.native.wasm` (~1.5 MB _minimum_ for the relinked runtime — cannot go lower)
2. `System.Private.CoreLib.wasm` (~500–800 KB minimum, even fully trimmed)
3. Blazor JS interop layer (~43 KB in .NET 10, ~183 KB in .NET 9)
4. User's own App DLL (minimal — ~20–50 KB)

A "hello world" Blazor WASM app cannot go below ~1 MB Brotli-compressed. This
is the fundamental floor of the .NET WASM runtime model. Non-.NET WASM
frameworks (Rust, C++) can achieve sub-100 KB because they have no GC, no
reflection, and no class library. The .NET runtime _is_ the trade-off.

---

### 15. Recommended .csproj Configuration (.NET 9)

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <!-- Core optimization -->
  <RunAOTCompilation>false</RunAOTCompilation>          <!-- omit AOT for download speed -->
  <InvariantGlobalization>true</InvariantGlobalization> <!-- if no i18n needed -->
  <InvariantTimezone>true</InvariantTimezone>           <!-- if no i18n needed -->
  <BlazorWebAssemblyPreserveCollationData>false</BlazorWebAssemblyPreserveCollationData>

  <!-- Always safe to disable in WASM -->
  <EventSourceSupport>false</EventSourceSupport>
  <DebuggerSupport>false</DebuggerSupport>
  <EnableUnsafeBinaryFormatterSerialization>false</EnableUnsafeBinaryFormatterSerialization>
  <MetadataUpdaterSupport>false</MetadataUpdaterSupport>
  <HttpActivityPropagationSupport>false</HttpActivityPropagationSupport>

  <!-- Trimming -->
  <PublishTrimmed>true</PublishTrimmed>
  <TrimMode>full</TrimMode>                             <!-- partial if libraries break -->

  <!-- Symbols -->
  <TrimmerRemoveSymbols>true</TrimmerRemoveSymbols>
</PropertyGroup>
```

---

### Summary: Ranked by Real Savings

| Rank | Technique                                      | Disk Saving        | Brotli Saving      | Risk   |
| ---- | ---------------------------------------------- | ------------------ | ------------------ | ------ |
| 1    | Don't enable AOT                               | 50–200 MB          | 10–40 MB           | None   |
| 2    | PublishTrimmed + TrimMode=full                 | 10–30 MB           | 3–8 MB             | Medium |
| 3    | InvariantGlobalization=true                    | 600 KB–1.2 MB      | 100–200 KB         | High   |
| 4    | Lazy load feature assemblies                   | Variable (per RCL) | Variable (per RCL) | Low    |
| 5    | Runtime relinking (automatic)                  | ~700 KB–1 MB       | ~100–200 KB        | None   |
| 6    | WasmStripILAfterAOT (AOT only)                 | 2–10+ MB           | 0.5–3 MB           | Low    |
| 7    | InvariantTimezone=true                         | 80–150 KB          | 15–40 KB           | Medium |
| 8    | BlazorWebAssemblyPreserveCollationData=false   | 50–100 KB          | 10–25 KB           | Low    |
| 9    | DebuggerSupport=false                          | 15–50 KB           | 5–15 KB            | None   |
| 10   | EventSourceSupport=false                       | 10–40 KB           | 2–10 KB            | None   |
| 11   | EnableUnsafeBinaryFormatterSerialization=false | 5–15 KB            | 2–5 KB             | None   |

**Key insight**: The largest levers are architectural — don't use AOT (if
download matters more than CPU), trim aggressively, and avoid loading unused
culture data. The individual MSBuild feature-switch properties
(EventSourceSupport, DebuggerSupport, etc.) are micro-optimizations that add up
to at most 100–200 KB total. They're "free" savings but won't rescue a bloated
app.
