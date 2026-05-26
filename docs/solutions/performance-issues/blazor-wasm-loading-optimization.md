---
date: 2026-03-30
title: "Blazor WASM Loading Time Optimization (AOT, Full Trimming, Resource Hints)"
tags: [blazor, wasm, performance, size, optimization, timeline]
problem_type: optimization
---

## Problem

The Blazor WebAssembly application was missing several critical performance configurations: AOT compilation was not enabled (only in test projects), trimming was set to `partial` leaving unused code in the bundle, lazy loading was not configured forcing all assemblies to download upfront, and `index.html` had no preload/prefetch hints for critical resources. The initial bundle was 7.6 MB compressed, with the largest contributors being `System.Private.CoreLib` (4.43 MB), `System.Private.Xml` (2.95 MB), and `dotnet.native` (2.89 MB).

## Root Cause

Build configuration gaps in `src/redmuffin.Blazor.StaticWeb/redmuffin.Blazor.StaticWeb.csproj` and `Directory.Build.props`:

- `RunAOTCompilation` was absent from the Blazor project (only present in test projects)
- `TrimMode` was `partial` instead of `full` in Release configuration
- No `TrimmerRoots.xml` existed to guide the trimmer on which types to preserve
- `BlazorEnableTimeZoneSupport`, `EventSourceSupport`, `HttpActivityPropagationSupport` were not explicitly disabled
- `WasmEnableSIMD` was enabled by default, increasing native WASM code size
- `IlcOptimizationPreference` was not set to `Size`, and `IlcFoldIdenticalMethodBodies` was not enabled
- No `<link rel="preload">` hints for critical CSS and JavaScript in `index.html`

## Solution

**AOT compilation** (`RunAOTCompilation=true`): Compiles .NET IL directly to WebAssembly ahead of time, improving runtime performance and enabling better trimming.

**Full trimming** (`TrimMode=full` + `TrimmerRoots.xml`):

- Switch from `partial` to `full` in Release configuration
- Create `TrimmerRoots.xml` preserving essential Blazor types, JSON serialization contexts, and component assemblies
- Iterate on build failures to identify missing preserved types

**Resource loading hints** in `index.html`:

- `<link rel="preload">` for `blazor.webassembly.js` and critical CSS
- `<link rel="prefetch">` for font files and external scripts

**Disabled runtime features**: `WasmEnableSIMD=false` (disables SIMD vector instructions, shrinking native WASM), `BlazorEnableTimeZoneSupport=false`, `EventSourceSupport=false`, `HttpActivityPropagationSupport=false`

**ILC size optimizations**: `IlcOptimizationPreference=Size` and `IlcFoldIdenticalMethodBodies=true` applied in `Directory.Build.props`

**Results achieved**:

- Compressed bundle size: 7.6 MB → 3.31 MB (**56.5% reduction**)
- Uncompressed size: 23 MB (unchanged -- trimming removes code, but AOT adds native WASM)
- Release build time: ~20-28 seconds (AOT compilation overhead)
- All 27 smoke tests passing
- Compatible with GitHub Actions and Azure Static Web Apps deployment

**CI/CD measurement**: Automated bundle measurement via `scripts/Measure-BundleSize.ps1` on Release builds with size regression alerts and a defined size budget threshold.

**Deferred**: Lazy loading (FR-003) and dependency evaluation (FR-006) deferred to future PRD. Dependency evaluation includes:

- Moving `Markdig`-based markdown processing server-side to Azure Functions to remove the heavy Markdig NuGet package from the WASM bundle
- Evaluating `MessagePack` or `MemoryPack` for binary serialization as a lighter alternative to `System.Text.Json`

## Prevention

- **AOT compilation is now the default Release build path** -- any future project file restructure must preserve this
- **TrimmerRoots.xml maintenance**: When adding reflection-based code or new serialization contexts, update the descriptor
- **Pre/post measurement discipline**: Always measure bundle size before and after optimization changes via `dotnet publish -c Release`
- **Smoke test requirement**: All 27 smoke tests must pass after any build configuration change
- **CI/CD compatibility**: Changes tested against the exact `dotnet publish -c Release` command used in GitHub Actions
- **Size monitoring in CI**: Run `scripts/Measure-BundleSize.ps1` on every Release build; fail on regression
- **Dependency audit on package updates**: Review transitive impact before upgrading to avoid pulling in large assemblies
- **Phase-by-phase rollback**: Each optimization is atomic in git, independently revertible
- **Feature-preservation checklist**: Full functional testing after each trimming change
- **Testing methodology**: iPhone Safari private session, browser dev tools network transfer, compressed size measurement with Brotli/Gzip
