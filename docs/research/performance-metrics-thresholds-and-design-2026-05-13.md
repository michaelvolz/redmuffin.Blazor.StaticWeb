---
date: 2026-05-13
tags:
  - research
  - blazor
  - performance
  - web-vitals
  - core-web-vitals
  - design
  - color
---

# PageLoad Component Optimization Research

Research in response to SN-0036 ("Re-evaluate pageload component — modern design,
delays, live updates, and color thresholds"). Covers official Google Web Vitals
thresholds, color-coding standards, dashboard design patterns, and Blazor
real-time update options.

## Official Google Web Vitals Thresholds (2026)

All values are 75th percentile of page loads. Source: web.dev, DebugBear,
Google Search Central, PageSpeed Insights.

| Metric                              | Good    | Needs Improvement | Poor    |
| ----------------------------------- | ------- | ----------------- | ------- |
| **LCP** (Largest Contentful Paint)  | < 2.5s  | 2.5–4.0s          | > 4.0s  |
| **INP** (Interaction to Next Paint) | < 200ms | 200–500ms         | > 500ms |
| **CLS** (Cumulative Layout Shift)   | < 0.1   | 0.1–0.25          | > 0.25  |
| **TTFB** (Time to First Byte)       | < 800ms | 800ms–1.8s        | > 1.8s  |
| **FCP** (First Contentful Paint)    | < 1.8s  | 1.8–3.0s          | > 3.0s  |

**Key insight:** Every metric has its OWN thresholds. They are not
interchangeable. LCP threshold (2.5s) is different from TTFB (800ms),
which is different from INP (200ms).

### DomContentLoaded and LoadComplete

These are not Core Web Vitals (they were deprecated from the spec).
Google recommends LCP and FCP instead. However, `DOMContentLoaded` and
`LoadComplete` are useful for internal diagnostics — especially for
SPA/Blazor WASM where the DOM changes dynamically.

**Recommended diagnostic thresholds** (industry consensus, not Google-official):

| Metric           | Good   | Needs Improvement | Poor   |
| ---------------- | ------ | ----------------- | ------ |
| DOMContentLoaded | < 1.5s | 1.5–3.0s          | > 3.0s |
| LoadComplete     | < 2.5s | 2.5–5.0s          | > 5.0s |

## Industry-Standard Color Coding

Every major tool (Google Lighthouse, PageSpeed Insights, DebugBear,
Sentry, WebPageTest) uses the SAME 3-tier system:

| Tier              | Color        | Hex                    |
| ----------------- | ------------ | ---------------------- |
| Good              | Green        | `#0cce6b` (Lighthouse) |
| Needs Improvement | Amber/Orange | `#ffa400` (Lighthouse) |
| Poor              | Red          | `#ff4e42` (Lighthouse) |

**Lighthouse official colors** (source: [Lighthouse GitHub](https://github.com/GoogleChrome/lighthouse)):

- Green: `#0cce6b` or `#18a957`
- Orange: `#ffa400` or `#fa7d0e`
- Red: `#ff4e42` or `#cc3300`

Consensus in 2026: use the Lighthouse palette. It's the most recognized.

## Our Current Thresholds vs. Standard

**Current code (TimingMetricsCard.TimingColor):**

```
≤1000ms → #00ff41 (green)
≤2500ms → #ffd700 (yellow)
≤4000ms → #ff8c42 (orange)
>4000ms → #ff4757 (red)
```

**Problems:**

1. **Wrong number of tiers.** Google uses 3 (Good / Needs Improvement /
   Poor). We use 4 (green / yellow / orange / red). This creates a 4th
   category that doesn't exist in any industry tool. Users can't map our
   colors to anything they've seen before.

2. **One-size-fits-all thresholds.** TTFB, FCP, LCP, DOM, Load all use
   the same 1000/2500/4000 scale. A 2.4s LCP IS "Good" per Google, but
   our component shows it as yellow. A 900ms TTFB IS "Poor" per Google,
   but we show it as green. The component actively MISINFORMS.

3. **Non-standard colors.** `#00ff41` (pure green) is aggressive on
   screens. `#ffd700` (gold) is OK. `#ff8c42` (dark orange) is
   indistinguishable from a third tier of "red" at a glance. Lighthouse
   green (`#0cce6b`) is gentler and more professional.

4. **WASM metrics have no industry standard.** Our `WasmColor` is
   configurable but defaults are arbitrary (200/500/1000 for download
   time). These numbers were never benchmarked or validated. They
   should either be based on actual measurement data or marked as
   purely diagnostic (no color coding).

## Recommendation: Metric-Specific Thresholds

Each metric uses Google's official threshold. WASM metrics use
diagnostic-only display (no color-coded judgment).

| Card                  | Metric         | Green    | Amber      | Red      |
| --------------------- | -------------- | -------- | ---------- | -------- |
| TimingMetricsCard     | TTFB           | < 800ms  | 800ms–1.8s | > 1.8s   |
| TimingMetricsCard     | FCP            | < 1.8s   | 1.8–3.0s   | > 3.0s   |
| TimingMetricsCard     | LCP            | < 2.5s   | 2.5–4.0s   | > 4.0s   |
| TimingMetricsCard     | DOM            | < 1.5s   | 1.5–3.0s   | > 3.0s   |
| TimingMetricsCard     | Load           | < 2.5s   | 2.5–5.0s   | > 5.0s   |
| CalculatedMetricsCard | ServerResponse | < 600ms  | 600ms–1.0s | > 1.0s   |
| CalculatedMetricsCard | DomProcessing  | < 200ms  | 200–400ms  | > 400ms  |
| CalculatedMetricsCard | ResourceLoad   | < 500ms  | 500ms–1.0s | > 1.0s   |
| WasmBootstrapCard     | All            | No color | No color   | No color |

**WASM metrics reasoning:** WASM download time, assembly count, memory,
Blazor init time have zero industry benchmarks. Color-coding them
suggests a "good" or "bad" judgment that has no basis. Display them
neutrally as raw diagnostics. WASM metrics are for developers, not users.

**Lighthouse colors to use:**

```csharp
private static string MetricColor(double value, double good, double poor) => value switch
{
    _ when value <= good   => "#0cce6b",  // Lighthouse green
    _ when value <= poor   => "#ffa400",  // Lighthouse amber
    _                      => "#ff4e42"   // Lighthouse red
};
```

## Modern Design for Mobile (iPhone 15)

The 2026 consensus (AdminLTE, DebugBear, DesignRush) points to:

1. **Card-based metric strip** — 4-6 KPI cards in a CSS Grid with
   `auto-fill`. Each card is independently responsive.

2. **Mobile-first CSS** — start with single-column vertical stack.
   At ≥768px, switch to 2-column grid. At ≥1024px, switch to 4-column.

3. **3-second clarity rule** — user should understand the system state
   within 3 seconds of landing. Our current widget buries metrics behind
   a click-to-expand accordion. A better approach: show the top-level
   rating badge always visible, expand to details on tap.

4. **Cohesive color palette** — use Lighthouse green/amber/red
   throughout. Consistent visual language reduces cognitive load.

**Our decomposition already supports this.** Each card is 5-55 lines.
Mobile-responsive redesign is per-card CSS, not structural refactoring.

## Progressive Rendering Strategy

User requirement: display each metric the moment the data arrives. Never
wait for ALL data before showing ANY data. Never use arbitrary delays.

### Feasibility

**Safe.** The Browser Performance API is read-only. Calling
`performance.getEntriesByType('navigation')` or `PerformanceObserver`
does not affect the measured values. Multiple JS interop calls do not
contaminate results.

### Data availability timeline

```
Page loads → Blazor WASM bootstraps
  ├── Phase 1 (immediate): Navigation Timing API
  │     TTFB, FCP, DOM, LCP, Load — available from performance.timing
  │     the instant JS runs. No dependency on any custom JS.
  │     Display: TimingMetricsCard flips from "—" to real values.
  │
  ├── Phase 2 (async, availability-checked): Comprehensive + WASM metrics
  │     Full PageLoadMetrics via JS interop. WASM metrics depend on
  │     window.pageLoadSpeed.wasmMetrics being populated.
  │     Display: ResourceSizeCard, CalculatedMetricsCard, WasmBootstrapCard
  │     flip to values. WasmBootstrapCard shows "—" until ready.
  │
  └── Phase 3 (complete): All data loaded
        Component removes pending indicators. Rating badge shows.
```

### Architecture impact

Smart parents (`PageLoadMetricsView`, `AppStartMetricsView`) split their
`OnAfterRenderAsync` into two fetches:

1. `FetchTimingMetricsAsync()` — calls immediately, no delay
2. `FetchFullMetricsAsync()` — calls after timing, checks JS readiness
   rather than waiting `AutoLoadDelayMs`

Cards don't change. They take `[Parameter]` props — they render whatever
data they receive, whether partial or complete. The `—` placeholder
is the card's default when the prop is null/zero.

### Delay elimination

**Remove** the `AutoLoadDelayMs` check + `Task.Delay` pattern. Replace
with explicit availability check:

```csharp
// Before (arbitrary delay)
await Task.Delay(PageLoadSpeedConfig.AutoLoadDelayMs, cts.Token);
await UpdateMetricsAsync();

// After (event-driven)
await FetchTimingMetricsAsync();  // Nav Timing API always available
await FetchFullMetricsAsync();     // Waits for JS interop readiness internally
```

**Analysis:** Page load metrics are measured ONCE per page load. TTFB,
FCP, LCP, LoadComplete are final values after the page finishes loading.
Live polling would show the same numbers over and over. The "Refresh"
button is the correct UX.

**Exception:** WASM metrics (memory usage, assembly count) COULD change
over time if the app dynamically loads assemblies. But this is an edge
case and not needed for current use.

**Recommendation:** Keep manual refresh. If real-time WASM memory
monitoring is desired later, add `PeriodicTimer` (5s interval) only
to the `AppStartMetricsView` — the smart parent owns timing, cards
don't care.

## Implementation Priority

| Priority | Task                                    | Files affected               | Impact                                    |
| -------- | --------------------------------------- | ---------------------------- | ----------------------------------------- |
| 1        | Fix color thresholds to Google standard | `TimingMetricsCard.razor.cs` | Correctness — stops misinforming          |
| 2        | Progressive rendering                   | `*MetricsView.razor.cs` (×2) | UX — shows data the moment it exists      |
| 3        | Remove `AutoLoadDelayMs`                | `*MetricsView.razor.cs` (×2) | Eliminates arbitrary empty-component wait |
| 4        | Remove WASM metric color                | `WasmBootstrapCard.razor.cs` | Removes judgments with no industry basis  |
| 5        | Mobile-responsive CSS                   | SCSS files                   | iPhone 15 readability                     |
