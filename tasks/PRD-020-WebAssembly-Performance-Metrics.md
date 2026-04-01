# PRD-020: WebAssembly Performance Metrics for Page Load Widget

## 1. Overview

### Goal

Add WebAssembly (WASM)-specific performance metrics to the existing Page Performance Widget to provide comprehensive visibility into Blazor WebAssembly startup performance. These metrics will help developers understand and optimize the WASM download, runtime initialization, and memory usage of their Blazor applications.

### Problem Statement

Currently, the Page Performance Widget only displays web-level metrics (TTFB, FCP, LCP, etc.) but provides no insight into the WebAssembly-specific performance characteristics that are critical for Blazor WebAssembly apps. Developers cannot easily see:

- How long the WASM runtime takes to download
- Size of the .NET assemblies being loaded
- Time spent in runtime initialization
- Memory heap usage of the WASM runtime

### Solution

Add a new "WebAssembly Metrics" section to the existing widget that captures and displays 5 key WASM performance indicators between the "Timing" and "Data Transfer" sections.

---

## 2. Goals

1. **Capture WASM-specific metrics** from browser Performance API and custom timing marks
2. **Display metrics in consistent widget styling** matching existing Timing and Data Transfer sections
3. **Maintain widget performance** with minimal overhead during metric collection
4. **Support both development and production environments** with appropriate data availability
5. **Preserve all existing metrics** without modification or removal

---

## 3. User Stories

**As a developer**, I want to see WebAssembly download time and size so that I can understand the initial payload cost of my Blazor app.

**As a developer**, I want to see the number and total size of .NET assemblies loaded so that I can identify opportunities for assembly trimming or lazy loading.

**As a developer**, I want to see the .NET runtime startup time so that I can measure the impact of initialization code in `Program.cs`.

**As a developer**, I want to see WASM memory heap usage so that I can monitor memory consumption during app startup.

**As a developer**, I want all metrics displayed in the existing widget format so that I have a unified view of both web and WASM performance.

---

## 4. Functional Requirements

### 4.1 Metric Collection

**FR-001:** The system must capture the following 5 WebAssembly metrics:

1. **WASM Download** (time + size)
   - Source: PerformanceResourceTiming for `dotnet.wasm` file
   - Display: Time in ms + size in KB/MB
   - Example: "245ms / 2.1 MB"

2. **Assemblies** (count + total size)
   - Source: PerformanceResourceTiming for all `.dll` files in `_framework`
   - Display: Count + total size in KB/MB
   - Example: "42 assemblies / 856 KB"

3. **Runtime Startup** (time from WASM loaded to Blazor ready)
   - Source: Custom performance marks at `beforeStart` and `afterStarted` JS initializers
   - Display: Time in ms
   - Example: "320ms"

4. **Memory Heap** (used / total)
   - Source: `performance.memory` API (Chrome only)
   - Display: Used memory / Total heap in MB
   - Example: "45 MB / 128 MB" or "N/A" if unavailable

5. **Blazor Init** (time from page load to first render)
   - Source: Navigation timing start to first component render
   - Display: Time in ms
   - Example: "680ms"

**FR-002:** Metrics must be collected asynchronously without blocking the main thread.

**FR-003:** Metric collection must gracefully handle cases where Performance API data is unavailable (e.g., Firefox, Safari limitations).

### 4.2 Widget Layout

**FR-004:** The widget must display sections in this vertical order with **compact layout to fit without scrolling**:

```
┌──────────────────────────────────────────┐
│ ⚡ Page Performance          15:30:45  − │
├──────────────────────────────────────────┤
│ ⏱️ TIMING (MS)                           │
│ • TTFB: 12                               │
│ • FCP: 145                               │
│ • DOM: 89                                │
│ • LCP: 420                               │
│ • Load: 156                              │
├──────────────────────────────────────────┤
│ 🎯 WEBASSEMBLY                           │  ← NEW SECTION (compact)
│ • WASM↓: 245ms / 2.1 MB                  │
│ • Assemblies: 42 / 856 KB                │
│ • Runtime: 320ms                         │
│ • Memory: 45 MB / 128 MB                 │
│ • Blazor Init: 680ms                     │
├──────────────────────────────────────────┤
│ 📊 DATA TRANSFER                         │
│ • Transfer: 2.1 MB                       │
│ • Encoded: 2.3 MB                        │
│ • Decoded: 3.1 MB                        │
│ • Compression: 32.3%                     │
├──────────────────────────────────────────┤
│ [🚀 EXCELLENT]     Score: 95/100  [🔄]   │
└──────────────────────────────────────────┘
```

**Height Constraint:** The widget uses `position: fixed` and must fit entirely within the viewport without internal scrolling. Current height is ~666px. Adding 5 WASM metrics will increase height by ~120px (5 metrics × ~24px each). Total expected height: ~800px, which fits comfortably in mobile viewport (812-932px height).

**FR-005:** Each WASM metric must display with:

- Label on the left
- Value on the right (formatted with appropriate units)
- Progress bar below (styled consistently with existing metrics)
- Semantic color coding:
  - Green: Excellent (fast/low)
  - Yellow: Good
  - Orange: Fair
  - Red: Poor (slow/high)

### 4.3 Styling Requirements

**FR-006:** The WASM Metrics section must match the existing visual style:

- **Section container:** Same as `.metric-group` class
- **Section title:** Same as `.metric-group-title` with icon 🎯 (target/bullseye)
- **Metric items:** Same as `.metric-item` class with `.metric-label` and `.metric-value`
- **Progress bars:** Same as `.metric-progress` with `.metric-progress-bar`
- **Colors:** Use `.wasm` CSS class variant for metric values (new color: `#9d4edd` purple)

**FR-007:** The widget must expand to utilize available width:

- **Current width:** 330px
- **iPhone 15 (430px viewport):** Expand widget to 430px (utilizing 100px of white space on left)
- **Implementation:** Modify `$page-speed-widget-width` variable in SCSS

### 4.4 Responsive Behavior

**FR-008:** On viewports ≤ 375px (iPhone SE/mini), widget remains 330px width (current behavior).

**FR-009:** On viewports 376-430px (iPhone 15), widget expands to full available width (430px).

---

## 4.5 Constraints (Must Requirements)

**FR-010:** All 12 existing metrics must be preserved unchanged:

- Timing section: 5 metrics (TTFB, FCP, DOM, LCP, Load)
- Data Transfer section: 4 metrics (Transfer, Encoded, Decoded, Compression)
- Performance Breakdown section: 3 metrics (Server Response, DOM Processing, Resource Load)

**FR-011:** WASM metrics must display directly without requiring user interaction:

- No buttons, tabs, or toggles to show/hide WASM metrics
- No "Web" vs "WASM" view switching
- All metrics visible immediately when widget expands

---

## 5. Non-Goals (Out of Scope)

1. **No historical data** - Only current page load metrics, no tracking over time
2. **No server-side metrics** - Focus remains on client-side WASM performance
3. **No detailed profiling** - High-level metrics only, not function-level traces
4. **No cross-browser polyfills** - If `performance.memory` unavailable, show "N/A" rather than implement fallback

---

## 6. Design Considerations

### 6.1 Visual Design

**Icon for WASM section:** 🎯 (target/bullseye) representing precision/performance targeting

**Color scheme for WASM metrics:**

- Base color: `#9d4edd` (purple) to distinguish from existing colors:
  - Timing: Yellow (#ffd700)
  - Data Transfer: Blue (#00bfff)
  - Breakdown: Orange (#ff8c42)
  - WASM: Purple (#9d4edd)

**Progress bar thresholds:**

| Metric          | Excellent | Good     | Fair     | Poor     |
| --------------- | --------- | -------- | -------- | -------- |
| WASM Download   | < 200ms   | < 500ms  | < 1000ms | ≥ 1000ms |
| Assemblies      | < 30      | < 50     | < 100    | ≥ 100    |
| Runtime Startup | < 200ms   | < 400ms  | < 800ms  | ≥ 800ms  |
| Memory Heap     | < 50MB    | < 100MB  | < 200MB  | ≥ 200MB  |
| Blazor Init     | < 500ms   | < 1000ms | < 2000ms | ≥ 2000ms |

### 6.2 Layout Specifications

**Widget width by viewport:**

- Desktop (> 430px): 440px (unchanged)
- iPhone 15 (376-430px): 430px (expanded)
- iPhone SE/mini (≤ 375px): 330px (unchanged)

**Section ordering:**

1. Timing (5 metrics)
2. **WebAssembly Metrics (5 metrics)** ← NEW
3. Data Transfer (4 metrics)
4. Performance Breakdown (3 metrics)
5. Rating + Score + Refresh (inline layout to save vertical space)

---

## 7. Technical Considerations

### 7.1 Files to Modify

**JavaScript:**

- `wwwroot/js/page-load-timing.js` - Add WASM metric collection functions

**C# Models:**

- `Features/Common/PageLoadSpeed/Core/PerformanceMetrics.cs` - Add `WasmMetrics` property
- `Features/Common/PageLoadSpeed/Core/WasmMetrics.cs` - New record for WASM data
- `Features/Common/PageLoadSpeed/Core/TimingMetrics.cs` - Reference only (no changes)

**C# Service:**

- `Services/IPerformanceMetricsService.cs` - Add method to retrieve WASM metrics
- `Services/PerformanceMetricsService.cs` - Implement WASM metric retrieval

**Blazor Component:**

- `Features/Common/PageLoadSpeed/LoadSpeed.razor` - Add WASM section markup
- `Features/Common/PageLoadSpeed/LoadSpeed.razor.cs` - Add WASM data binding logic

**Styling:**

- `scss/abstracts/_variables.scss` - Update widget width variables
- `scss/features/shared/_page-load-speed.scss` - Add WASM-specific styles

### 7.2 JavaScript Implementation

Add to `page-load-timing.js`:

```javascript
// WASM metric collection
getWasmMetrics: function() {
    const metrics = {
        wasmDownloadTime: 0,
        wasmDownloadSize: 0,
        assemblyCount: 0,
        assemblyTotalSize: 0,
        runtimeStartupTime: 0,
        memoryUsed: 0,
        memoryTotal: 0,
        blazorInitTime: 0
    };

    // Query PerformanceResourceTiming for dotnet.wasm and .dll files
    // Calculate timing differences from marks
    // Return formatted metrics object
}
```

### 7.3 Data Flow

1. **Page Load** → JS initializes and collects timing data
2. **Blazor Starts** → JS marks `blazorStartTime`
3. **After Render** → C# service calls `window.getWasmMetrics()` via JS interop
4. **Data Binding** → Metrics populate in `LoadSpeed.razor` component
5. **UI Update** → Widget displays with all sections including WASM

### 7.4 Browser Compatibility

- **Chrome/Edge:** Full support (all 5 metrics)
- **Firefox:** Partial support (WASM download, assemblies, runtime - no memory)
- **Safari:** Partial support (WASM download, assemblies, runtime - no memory)

Graceful degradation: Show "N/A" for unavailable metrics rather than hiding them.

---

## 8. Success Metrics

1. **WASM metrics display correctly** on Chrome/Edge with all 5 values populated
2. **Widget renders without errors** on Firefox/Safari with partial data
3. **All existing 12 metrics remain functional** and unchanged
4. **Widget width expands to 430px** on iPhone 15 viewport
5. **Metric collection adds < 50ms** to page load time
6. **No horizontal scrolling** on any mobile viewport (330px or 430px)
7. **Widget fits without vertical scrolling** - total height must not exceed viewport height

---

## 9. Implementation Notes

### 9.1 Critical Implementation Details

**Order is critical:** Timing → WASM → Data Transfer → Breakdown → Rating

**CSS class naming:**

- Use existing `.metric-group` for section container
- Use existing `.metric-group-title` for section header
- Use existing `.metric-item` for metric rows
- Add new `.wasm` class for WASM-specific value styling (purple color)

**JavaScript timing marks:**

- Mark `wasm-start` when `beforeStart` initializer runs
- Mark `wasm-end` when `afterStarted` initializer runs
- Calculate runtime startup as difference between marks

**Assembly counting:**

- Filter PerformanceResourceTiming entries by URL containing `.dll`
- Exclude system assemblies if possible (or count all)
- Sum `transferSize` for total assembly size

### 9.2 Testing Considerations

- Test on Chrome with full metric availability
- Test on Firefox/Safari with partial availability
- Test on mobile viewport sizes (375px and 430px)
- Verify no metrics are removed or reordered incorrectly
- Verify progress bar colors match thresholds

---

## 10. Open Questions

None. Requirements are clear and comprehensive.

---

**PRD Author:** AI Assistant  
**Date:** 2026-04-01  
**Status:** Ready for Implementation  
**Priority:** Medium  
**Estimated Effort:** 2-3 hours
