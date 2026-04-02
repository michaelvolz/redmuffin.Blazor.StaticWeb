---
title: fix: Stop Blazor init refresh drift
type: fix
status: active
date: 2026-04-02
origin: none
---

# fix: Stop Blazor init refresh drift

## Overview

The page load widget is recomputing the WebAssembly/Blazor initialization end point on every refresh, which causes the displayed `Blazor init` value to grow after the app has already finished initializing. The fix is to separate one-time initialization finalization from repeated metric reads so refreshes preserve the true init duration.

## Problem Frame

This bug affects the page load widget’s new `webassembly` block and its refresh button. Today, the shared metrics path updates the WASM end marker inside `GetMetricsAsync()` and `GetWasmMetricsAsync()`, so a late refresh can shift the end timestamp forward and inflate `Blazor init`.

The intended behavior is that Blazor initialization ends once, at the actual completion moment, and later refreshes only reread already-finalized data. This matches Blazor lifecycle guidance: `OnAfterRenderAsync(firstRender)` is the right one-time interactive initialization boundary, while repeated refreshes should not re-run initialization logic.

## Requirements Trace

- R1. `Blazor init` must stop increasing after the application finishes its initial WebAssembly/Blazor startup.
- R2. Manual refresh must not change historical initialization timing once it has been finalized.
- R3. The widget must still update live page-load data on refresh where appropriate.
- R4. The fix must preserve existing fallback behavior when JS metrics are unavailable.

## Scope Boundaries

- Do not redesign the page load widget UI.
- Do not change the meaning of non-WASM timing, size, or cache rating fields.
- Do not add a new public API surface unless the existing JS/C# boundary needs a clearer split between “finalize once” and “read many”.

## Context & Research

### Relevant Code and Patterns

- `src/redmuffin.Blazor.StaticWeb/Features/Common/PageLoadSpeed/PageLoadSpeed.razor`
- `src/redmuffin.Blazor.StaticWeb/Features/Common/PageLoadSpeed/PageLoadSpeed.razor.cs`
- `src/redmuffin.Blazor.StaticWeb/Services/PerformanceMetricsService.cs`
- `src/redmuffin.Blazor.StaticWeb/wwwroot/js/page-load-timing.js`
- `src/redmuffin.Blazor.StaticWeb/Features/Common/PageLoadSpeed/Core/WasmMetrics.cs`
- `src/redmuffin.Blazor.StaticWeb/Features/Common/PageLoadSpeed/Core/PerformanceMetrics.cs`
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Cache/Components/RefreshBadgeTests.Behavior.cs`
- `tests/redmuffin.Blazor.StaticWeb.Tests/Integration/HomePageIntegrationTests.cs`

The repo’s page-load widget already follows a first-render initialization path followed by a manual refresh path. The bug is that both paths currently flow through the same finalization logic.

### Institutional Learnings

- No directly relevant `docs/solutions/` learning was found for this specific timing bug.

### External References

- Microsoft Learn: ASP.NET Core Razor component lifecycle — `OnAfterRenderAsync(firstRender)` is the proper post-render one-time initialization boundary, and it is not invoked during prerendering.
- Microsoft Learn: Blazor JS interop — JS interop is asynchronous, should be used after render for DOM-attached work, and should not mutate component state in a way that creates repeated lifecycle drift.

## Key Technical Decisions

- Split “finalize Blazor init” from “read metrics”: the finalization step should happen once at startup completion, not inside the metric retrieval path. This directly addresses the accumulating value.
- Keep refresh read-only for finalized WASM init timing: refresh may re-fetch live values, but it must not re-mark the init end timestamp.
- If the delayed first-load refresh and a manual refresh overlap, treat the first completed finalization as authoritative and prevent the later path from moving the end marker again.

## Open Questions

### Resolved During Planning

- Is the bug caused by repeated end-marker writes? Yes — the shared C# service calls `markEnd()` before reading WASM metrics, and the JS metric calculator derives `Blazor init` from that end marker.
- Is `OnAfterRenderAsync(firstRender)` the right lifecycle boundary for the one-time start-up work? Yes — Microsoft’s guidance supports first-render-only post-render initialization.

### Deferred to Implementation

- Whether the cleanest fix lives primarily in `PerformanceMetricsService`, primarily in `page-load-timing.js`, or as a small contract split across both layers. This depends on the exact test seam and the smallest safe change once the code is edited.
- Whether the manual refresh should cancel the pending auto-load delay or simply ignore later finalization attempts. The plan assumes the implementation will choose the smallest change that guarantees a single finalization.

## Implementation Units

- [ ] **Unit 1: Separate one-time WASM init finalization from metric reads**

**Goal:** Make `Blazor init` immutable after initial completion by removing end-marker mutation from repeated metric read paths.

**Requirements:** R1, R2, R4

**Dependencies:** None

**Files:**

- Modify: `src/redmuffin.Blazor.StaticWeb/Services/PerformanceMetricsService.cs`
- Modify: `src/redmuffin.Blazor.StaticWeb/wwwroot/js/page-load-timing.js`
- Test: `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Common/PageLoadSpeed/PerformanceMetricsServiceTests.Behavior.cs`

**Approach:**

- Introduce a clear separation between a one-time “finalize init” action and the existing metric retrieval calls.
- Preserve the current metric shape so the widget still receives the same fields, but stop allowing ordinary refresh reads to advance the init end time.
- Keep the fallback path intact when JS is unavailable or the metric contract is missing.

**Patterns to follow:**

- `PerformanceMetricsService` for centralized JS interop and timeout handling.
- `WasmMetrics.CreateDefault()` for unavailable metric fallbacks.
- The repository’s existing first-render initialization pattern in `PageLoadSpeed.razor.cs`.

**Test scenarios:**

- Happy path: first metric collection after startup finalizes init once and returns a non-zero `Blazor init` value.
- Happy path: a later refresh reads the same `Blazor init` value instead of increasing it.
- Edge case: repeated calls after initialization do not change the stored end marker.
- Error path: when JS interop is unavailable, the service still returns the existing fallback/default metric shape without throwing.
- Integration: the C# service and JS helper agree on the finalized init boundary and continue returning the same contract fields.

**Verification:**

- After the fix, the initialization duration should be stable across repeated refreshes once startup has completed.

- [ ] **Unit 2: Protect the widget refresh flow from re-finalizing startup timing**

**Goal:** Ensure the page widget’s refresh path updates visible metrics without reopening the init timing window.

**Requirements:** R1, R2, R3

**Dependencies:** Unit 1

**Files:**

- Modify: `src/redmuffin.Blazor.StaticWeb/Features/Common/PageLoadSpeed/PageLoadSpeed.razor.cs`
- Test: `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Common/PageLoadSpeed/PageLoadSpeedTests.Behavior.cs`

**Approach:**

- Keep the initial delayed load behavior, but ensure the refresh action can only read already-finalized metrics.
- Preserve the component’s current hidden/collapsed/refreshing UI state machine while changing only the timing semantics.
- Avoid coupling the refresh button to any logic that mutates historical startup state.

**Patterns to follow:**

- `RefreshBadge` behavior/state coverage as the repository’s button-state pattern.
- `PageLoadSpeed.razor.cs`’s existing `InitializeWithEmptyMetricsAsync()`, `UpdateMetricsAsync()`, and `RefreshMetricsAsync()` split.

**Test scenarios:**

- Happy path: the first automatic load shows metrics, then a manual refresh updates the widget without changing `Blazor init`.
- Happy path: the refresh button still toggles loading state correctly while the request is in flight.
- Edge case: a delayed manual refresh after waiting does not increase `Blazor init`.
- Edge case: if the initial auto-load and a manual refresh overlap, the widget ends in a stable state with one finalized init value.
- Integration: the rendered widget continues to display live timing/size data while preserving the original initialization duration.

**Verification:**

- A user can refresh the widget repeatedly after startup and see live values update without `Blazor init` drift.

- [ ] **Unit 3: Add regression coverage for the WASM init boundary**

**Goal:** Lock in the bug fix with focused tests that fail if the init end marker becomes mutable again.

**Requirements:** R1, R2, R3, R4

**Dependencies:** Units 1-2

**Files:**

- Test: `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Common/PageLoadSpeed/PerformanceMetricsServiceTests.Behavior.cs`
- Test: `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Common/PageLoadSpeed/PageLoadSpeedTests.Behavior.cs`

**Approach:**

- Add regression scenarios that model late refreshes, repeated refreshes, and overlapping init/refresh completion.
- Prefer assertions around stable values and single finalization rather than internal implementation details.

**Patterns to follow:**

- The repo’s existing behavior-focused test naming and `TestScope`-based component setup.
- `HomePageIntegrationTests` for full component rendering assertions.

**Test scenarios:**

- Happy path: after initial completion, the same metric value is returned on successive reads.
- Edge case: a refresh after a long idle period does not change the frozen init value.
- Edge case: manual refresh before the delayed auto-load finishes still results in one stable init reading.
- Error path: fallback metrics remain usable when the WASM JS contract is unavailable.
- Integration: the rendered widget reflects the stable init value across multiple refresh interactions.

**Verification:**

- The test suite contains a regression that would fail if `Blazor init` starts growing again on refresh.

## System-Wide Impact

- **Interaction graph:** `PageLoadSpeed.razor` → `PageLoadSpeed.razor.cs` → `PerformanceMetricsService` → `page-load-timing.js`.
- **Error propagation:** JS interop failures should continue to fall back to default or legacy metrics rather than breaking the widget.
- **State lifecycle risks:** the timing boundary is currently mutable during refresh; the fix must make the init end marker write-once.
- **API surface parity:** both `GetMetricsAsync()` and `GetWasmMetricsAsync()` currently touch the same WASM finalization path and need consistent behavior.
- **Integration coverage:** a rendered widget test is needed to prove refreshes preserve the finalized init value across UI interactions.
- **Unchanged invariants:** the widget layout, refresh affordance, collapse/visibility state, and fallback display modes should remain unchanged.

## Risks & Dependencies

| Risk                                                                                               | Mitigation                                                                           |
| -------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------ |
| Fixing only the C# service leaves the JS state mutable and allows another call path to drift again | Make the init finalization boundary explicit across the service/JS contract          |
| Refresh still races with the delayed first-load completion                                         | Ensure only the first completion can finalize init, and later completions read only  |
| Regression coverage is too shallow to catch timing drift                                           | Add a stable-value assertion after multiple refreshes and a delayed-refresh scenario |

## Documentation / Operational Notes

- No user-facing docs change is required unless the widget’s metric semantics are documented elsewhere.
- If the metric label is later described publicly, document that `Blazor init` is a one-time initialization duration, not a live refresh metric.

## Sources & References

- **Origin document:** none
- Related code: `src/redmuffin.Blazor.StaticWeb/Features/Common/PageLoadSpeed/PageLoadSpeed.razor.cs`
- Related code: `src/redmuffin.Blazor.StaticWeb/Services/PerformanceMetricsService.cs`
- Related code: `src/redmuffin.Blazor.StaticWeb/wwwroot/js/page-load-timing.js`
- External docs: https://learn.microsoft.com/en-us/aspnet/core/blazor/components/lifecycle?view=aspnetcore-10.0
- External docs: https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/?view=aspnetcore-10.0
