---
date: 2026-04-01
title: "WebAssembly Performance Metrics for Page Load Widget"
tags: [blazor, wasm, performance]
problem_type: optimization
---

## Problem

The Page Performance Widget displayed only web-level metrics (TTFB, FCP, LCP, DOM, Load) and data transfer metrics, providing zero visibility into Blazor WebAssembly-specific startup characteristics. Developers could not see WASM download time, assembly sizes, runtime initialization time, or memory heap usage, making WASM performance regressions invisible.

## Root Cause

The widget's metric collection (`page-load-timing.js`) and C# models (`PerformanceMetrics.cs`) only captured standard Navigation Timing API and Resource Timing API entries. No WASM-specific performance marks were instrumented, no `dotnet.wasm` or `.dll` resource filtering was implemented, and no `performance.memory` API calls were made. There was no data model for WASM metrics to flow from JavaScript to the Blazor component.

## Solution

Five new WASM metrics added to the widget between "Timing" and "Data Transfer" sections:

1. **WASM Download** -- time in ms + size in KB/MB, from `PerformanceResourceTiming` for `dotnet.wasm`
2. **Assemblies** -- count + total size, filtered from `.dll` entries in `_framework`
3. **Runtime Startup** -- time from `wasm-start` to `wasm-end` marks (JS initializer: `beforeStart` / `afterStarted`)
4. **Memory Heap** -- used/total from `performance.memory` API (Chrome/Edge only; shows "N/A" elsewhere)
5. **Blazor Init** -- time from navigation start to first component render

**Architecture**:

- JavaScript: `redmuffin.Blazor.StaticWeb.lib.module.js` (JS initializer with `beforeStart`/`afterStarted` marks), `page-load-timing.js` (`getWasmMetrics()` function querying Performance API)
- C# models: `WasmMetrics.cs` (record struct with 5 properties), `PerformanceMetrics.cs` (adds `WasmMetrics` property)
- C# service: `IPerformanceMetricsService` / `PerformanceMetricsService` (`GetWasmMetricsAsync` via JS interop)
- Blazor component: `LoadSpeed.razor` (WASM section markup between Timing and Data Transfer), `LoadSpeed.razor.cs` (data binding, progress bar thresholds, semantic color coding)

**Visual design**:

- Section icon: 🎯 -- section label: "WEBASSEMBLY"
- Purple color scheme (`#9d4edd`) distinct from existing sections (yellow Timing, blue Data Transfer, orange Breakdown)
- Progress bars with thresholds: Excellent (green) → Good (yellow) → Fair (orange) → Poor (red)
- Widget width expands to 430px on iPhone 15 (376-430px viewport); stays 330px on iPhone SE/mini (≤375px)

**Thresholds**:
| Metric | Excellent | Good | Fair | Poor |
|--------|-----------|------|------|------|
| WASM Download | <200ms | <500ms | <1000ms | ≥1000ms |
| Assemblies | <30 | <50 | <100 | ≥100 |
| Runtime Startup | <200ms | <400ms | <800ms | ≥800ms |
| Memory Heap | <50MB | <100MB | <200MB | ≥200MB |
| Blazor Init | <500ms | <1000ms | <2000ms | ≥2000ms |

**Compatibility**: Full data on Chrome/Edge; partial data (no memory) on Firefox/Safari with graceful "N/A" display.

## Prevention

- **No metric removal without replacement**: All 12 existing metrics must remain unchanged
- **Section order is fixed**: Timing → WASM → Data Transfer → Breakdown → Rating
- **Widget height constraint**: Must fit within viewport without internal scrolling; total ~800px on mobile
- **No toggles**: WASM metrics display directly without buttons, tabs, or view switching
- **Performance overhead budget**: Metric collection must add <50ms to page load; benchmarked in CI
- **Cross-browser testing**: Verify graceful degradation on Chrome, Firefox, Safari at both 375px and 430px viewports
