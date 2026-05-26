---
title: Freeze Blazor Init After Startup
problem_type: bug
category: performance-issues
component: PerformanceMetricsService
module: page-load-speed
tags:
  - blazor
  - wasm
  - performance
  - regression
  - javascript-interop
date: 2026-04-02
track: bug
---

# Freeze Blazor Init After Startup

## Problem

Repeated reads of page-load metrics were moving the WASM init end marker forward, so the Blazor initialization duration shown in the widget was wrong.

## Symptoms

- `GetMetricsAsync()` and `GetWasmMetricsAsync()` could be called more than once.
- Each read could re-finalize the Blazor init boundary.
- `BlazorInitTime` drifted instead of staying stable after startup.

## What Didn't Work

- Reading metrics on demand without freezing the init boundary.
- Letting `window.pageLoadSpeed.wasmMetrics.markEnd()` run on every read.

## Solution

Add one-time finalization in `PerformanceMetricsService`:

```csharp
if (!_wasmInitFinalized)
{
    await jsRuntime.InvokeVoidAsync(
        "eval",
        cts.Token,
        "window.pageLoadSpeed && window.pageLoadSpeed.wasmMetrics && window.pageLoadSpeed.wasmMetrics.markEnd()").ConfigureAwait(false);

    _wasmInitFinalized = true;
}
```

That keeps the JS boundary fixed before reading `getPageLoadMetrics` and `getWasmMetrics`.

Regression coverage now verifies:

- repeated metric reads keep `BlazorInitTime` at `100`
- `markEnd()` is called once

## Why This Works

The init window should close once, not every time metrics are queried. A guarded finalization step preserves the original startup timing and prevents later reads from rewriting the metric.

## Prevention

- Treat timing markers as one-time state.
- Add regression tests for repeated reads whenever JS timing is involved.
- Keep the service and JS contract explicit around `getPageLoadMetrics`, `getWasmMetrics`, and `markEnd()`.

## Related Docs

- `docs/plans/2026-04-02-002-fix-blazor-init-refresh-plan.md` — same boundary, same fix area, and the same prevention shape.
- `docs/solutions/performance-issues/wasm-performance-metrics-widget.md` — broader WASM timing surface and widget implementation
- `docs/solutions/logic-errors/wasm-metrics-showing-zero-bytes-2026-04-03.md` — same `page-load-timing.js` surface; fixes WASM file-type lookup in `findWasmEntry()` and `getAssemblyInfo()`.
