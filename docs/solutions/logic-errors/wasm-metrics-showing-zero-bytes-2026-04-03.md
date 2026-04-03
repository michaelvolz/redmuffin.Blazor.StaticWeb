---
title: "WASM download metrics show 0 B in page-load-timing widget"
problem_type: bug
category: logic-errors
date: 2026-04-03
track: bug
component: page-load-timing.js
module: frontend
tags:
  - wasm
  - page-load-timing
  - metrics
  - javascript
  - blazor
symptoms: "Page-load-timing widget displays 'WASM↓: 5 ms / 0 B' despite WASM files loading successfully with real payloads (200 status in network tab)."
root_cause: "findWasmEntry() matched dotnet.native.*.js (JS loader, often cached with transferSize: 0) instead of actual .wasm files. getAssemblyInfo() counted framework JS/boot files instead of .wasm assembly files."
resolution_type: code-fix
---

# WASM Download Metrics Show 0 B in Page-Load-Timing Widget

## Problem

The page-load-timing widget reported "WASM↓: 5 ms / 0 B" for WebAssembly download metrics because the timing code was measuring the JS loader file (`dotnet.native.*.js`) instead of the actual `.wasm` binary files.

## Symptoms

- Widget displayed `WASM↓: 5 ms / 0 B` despite WASM files loading successfully (confirmed 200 responses with real payloads in network tab)
- Two `.wasm` files were being downloaded but not counted by the timing script
- "Assemblies" count showed 4 files at 11.6 KB — these were JS/boot files, not actual assemblies
- Misleading metrics suggested no WASM payload was being transferred

## What Didn't Work

- **Checking the network tab alone** — confirmed `.wasm` files returned 200 with real payloads, but didn't reveal the bug was in the measurement code, not the download itself
- **Looking at `dotnet.native.*.js` transferSize** — this file is the JS loader/bootstrap, not the WASM binary. It's often served from cache with `transferSize: 0`, which is why the widget showed 0 B

## Solution

Two functions in `wwwroot/js/page-load-timing.js` were rewritten to filter for actual `.wasm` files instead of framework JS/boot files.

### `findWasmEntry()` — before

Searched for `dotnet.native.*.js` (the JS loader):

```javascript
findWasmEntry: function() {
    try {
        const resources = performance.getEntriesByType('resource');
        return resources.find(r =>
            r.name && r.name.includes('dotnet.native') && r.name.endsWith('.js')
        );
    } catch (e) {
        return null;
    }
}
```

### `findWasmEntry()` — after

Filters for `.wasm` files under `_framework/`, returns the largest by `transferSize`, with fallback to the JS loader if no `.wasm` entries exist:

```javascript
findWasmEntry: function() {
    try {
        const resources = performance.getEntriesByType('resource');

        // Prefer actual .wasm files — pick the largest one by transferSize
        const wasmFiles = resources.filter(r =>
            r.name && r.name.includes('_framework') && r.name.endsWith('.wasm')
        );

        if (wasmFiles.length > 0) {
            return wasmFiles.reduce((largest, current) =>
                (current.transferSize || 0) > (largest.transferSize || 0) ? current : largest
            );
        }

        // Fallback: .NET 9+ JS loader (often cached, may report transferSize: 0)
        return resources.find(r =>
            r.name && r.name.includes('dotnet.native') && r.name.endsWith('.js')
        );
    } catch (e) {
        return null;
    }
}
```

### `getAssemblyInfo()` — before

Counted framework JS files (`dotnet.js`, `dotnet.runtime.*.js`, `dotnet.native.*.js`, `blazor.boot.json`):

```javascript
resources.forEach((r) => {
  if (r.name && r.name.includes("_framework")) {
    const name = r.name.split("/").pop() || "";
    if (
      name === "dotnet.js" ||
      name.startsWith("dotnet.runtime") ||
      name.startsWith("dotnet.native") ||
      name === "blazor.boot.json"
    ) {
      info.count++;
      info.totalSize += r.transferSize || 0;
    }
  }
});
```

### `getAssemblyInfo()` — after

Counts only files ending in `.wasm` under `_framework/`:

```javascript
resources.forEach((r) => {
  if (r.name && r.name.includes("_framework") && r.name.endsWith(".wasm")) {
    info.count++;
    info.totalSize += r.transferSize || 0;
  }
});
```

Minified output regenerated via `dotnet build -c Debug-Sass`. Build succeeded with 0 warnings, 0 errors. Verified locally: widget now shows `WASM↓: 289.2 KB` and `Assemblies: 2 / 500.3 KB` on production.

## Why This Works

Blazor WASM serves actual WebAssembly binaries as `.wasm` files under the `_framework/` path. The JS loader (`dotnet.native.*.js`) is a separate bootstrap file that initializes the WASM runtime — it is not the WASM payload itself and is frequently cached, resulting in `transferSize: 0`. By filtering explicitly for `.wasm` extensions, the timing code now measures the correct resources. The fallback to `dotnet.native.*.js` in `findWasmEntry()` preserves graceful degradation if `.wasm` entries are somehow absent from the resource timing buffer.

## Prevention

**1. Name functions and filters by what they actually measure:**

```javascript
// Bad: function named "findWasmEntry" but searches for .js
// Good: filter explicitly for .wasm extension
r.name.endsWith(".wasm");
```

**2. Add a dev-mode warning when no WASM files are detected:**

```javascript
if (info.count === 0 && location.hostname === "localhost") {
  console.warn(
    "[page-load-timing] No .wasm files detected in resource timing buffer",
  );
}
```

**3. Code review checklist item:** When adding or modifying resource timing measurements, verify that the file extension filter matches the actual resource type being measured (`.wasm` for WASM, not `.js` loaders).

**4. Verify metrics against network tab:** After any change to timing code, cross-check widget values against the browser's Network panel to confirm the measured resources match the actual loaded files.

## Related Docs

- `docs/solutions/performance-issues/freeze-blazor-init-after-startup-2026-04-02.md` — same `page-load-speed` module, same `PerformanceMetricsService`, same `page-load-timing.js` JS surface. Covers a different bug (init boundary drift from repeated metric reads).
- `docs/plans/2026-04-02-002-fix-blazor-init-refresh-plan.md` — prior plan that touched the same JS file for Blazor init timing.
- `tasks/PRD-020-WebAssembly-Performance-Metrics.md` — broader feature context for the WASM timing surface.
