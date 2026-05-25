---
name: rm-review-heuristics
description: Review heuristics for structural quality signals automated gates miss — redundant state, parameter bloat, hot-path latency, no-op updates. Loaded by rm-cleanup-session during cleanup. Do not load independently.
---

# Review Heuristics — Signals Gates Miss

## Quick start

Load `rm-cleanup-session` first. After gates pass (CRAP, Depth,
Architecture all green), check the surviving code against the four
heuristics below before declaring cleanup complete.

## The Four Heuristics

Gates detect measurable structural problems: method complexity (CRAP),
module depth (Depth), dependency violations (Architecture), test
coverage gaps (Mutation), code duplication (Dupes). These four
heuristics catch what gates cannot: design-level problems that don't
manifest as numbers.

### 1. Redundant State

**What it is:** A field or property that duplicates information already
stored elsewhere. The same fact exists in two places — change one and
you must remember to change the other. Beck calls duplication "the root
of all evil in software design."

**Why gates miss it:** A redundant field is low-complexity, shallow (low
CRAP, passes Depth). No architecture violation. No duplication detected
(structurally different from the source of truth). Mutation gate can't
kill it — tests don't break when redundant state drifts; the field
simply goes stale silently.

**Rule:** Never store state that can be derived from existing state at
the point of use. Derive it instead.

**Author grounding:**

- Beck: "The first instance of duplication is acceptable. The second
  is a design problem." A field that shadows existing state is the
  second instance.
- Fowler: Replace Derived Variable with Query — compute at call site
  instead of caching and maintaining consistency.

```csharp
// Before — _isLoading shadows BadgeState
private bool _isLoading;

protected override async Task OnInitializedAsync()
{
    _isLoading = true;
    await LoadDataAsync();
    _isLoading = false;
}

// After — derive from existing state
// _isLoading deleted. Consumers check BadgeState directly.
// The loading state is BadgeState.Loading — no separate field needed.
```

### 2. Parameter Accumulation

**What it is:** Methods that grow parameters over time instead of
restructuring to eliminate the need. Each new parameter widens the
interface without deepening the module. Ousterhout: "The best modules
are deep — simple interfaces wrapped around complex functionality."
Every parameter added is a step toward shallowness.

**Why gates miss it:** A 12-parameter method may have low CC (CRAP
ignores parameter count). Depth gate counts parameters but only at
threshold 7 — 6 parameters passes. Architecture gate does not check
method signatures for cohesion.

**Rule:** Never add a parameter without first checking whether
restructuring the callers would eliminate the need for it entirely.

**Author grounding:**

- Ousterhout: deep modules have simple interfaces. Adding a parameter
  makes the interface wider without making the implementation deeper.
- Fowler: Introduce Parameter Object — when three or more parameters
  cluster, they are hiding a missing abstraction. Preserve Whole
  Object — pass the whole object when the caller already has it.

```csharp
// Before — 11 parameters, one added per feature
public static GateRunResult Analyze(
    string projectPath, string testProjectPath,
    string coverageFile, bool autoCoverage,
    bool verbose, bool noDepth, bool noDuplicates,
    string mutationSource, string architectureConfig,
    int exitCode, string[] warnings)

// After — GateRunRequest record, 2 parameters
public static GateRunResult Analyze(
    GateRunRequest request, ILogger logger)
// request aggregates project info, flags, and config.
// Callers construct the request; method signature never grows.
```

### 3. Hot-Path Bloat

**What it is:** Blocking work added to Blazor lifecycle methods
(`OnInitializedAsync`, `OnParametersSetAsync`) or constructor DI
resolution chains that delays first render. Farley: "If your tests take
minutes to start, you stop running them. Fast feedback is the
foundation of continuous delivery."

**Why gates miss it:** CRAP measures complexity, not runtime cost. Depth
measures structural depth, not execution time. Architecture measures
dependency rules, not startup sequencing. No gate measures wall-clock
time.

**Rule:** Never add blocking I/O, computation, or service resolution to
a Blazor lifecycle method without measuring the impact on first-render
time. Anything that runs before the first `StateHasChanged` is on the
critical path.

**Author grounding:**

- Farley: fast feedback enables continuous delivery. A slow test suite
  stops being run. The same applies to component initialization —
  slow startup degrades every debug cycle, every browser refresh, every
  deployment verification.
- Feathers: characterization tests run on every save during cleanup.
  Bloated `OnInitializedAsync` compounds across every test fixture that
  renders the component.

```csharp
// Before — HTTP call blocks first render
protected override async Task OnInitializedAsync()
{
    items = await FetchItemsAsync();  // 2s network call
    isLoading = false;
}

// After — fire-and-forget, render immediately
protected override async Task OnInitializedAsync()
{
    _ = RefreshInBackgroundAsync();  // returns void, non-blocking
}
// Component renders with loading state immediately.
// Data populates when the background task completes.
```

### 4. No-Op Updates

**What it is:** State updates or re-renders that fire even when the
value has not changed. A `StateHasChanged` call after setting a field
to its current value. A cache invalidation that rebuilds an identical
cache. Beck: "Write no code that doesn't do work."

**Why gates miss it:** No gate measures state-change frequency. CRAP
measures complexity, not redundancy. Mutation gate tests behavior
correctness — a no-op update produces correct behavior, just wasted
cycles. No test fails when code runs twice instead of once.

**Rule:** Never trigger a state update, re-render, or cache rebuild
when the new value is identical to the current value.

**Author grounding:**

- Beck: every line of code must earn its existence. A state update
  that produces the same value is dead work — it earned nothing.
- Fowler: Remove Setting Method — if a setter always sets the same
  value, remove the setter. The same applies to event handlers that
  fire unconditionally.

```csharp
// Before — StateHasChanged fires on every poll, even when nothing changed
private async Task PollStatusAsync()
{
    var newStatus = await FetchStatusAsync();
    _status = newStatus;
    await InvokeAsync(StateHasChanged);  // fires even if identical
}

// After — guard against no-change
private async Task PollStatusAsync()
{
    var newStatus = await FetchStatusAsync();
    if (newStatus == _status) return;    // no change → no update
    _status = newStatus;
    await InvokeAsync(StateHasChanged);
}
```

## Integration

Load `rm-cleanup-session` first. Run gates. When CRAP, Depth, and
Architecture all pass, survey surviving code against these four
heuristics. Fix any violations found. Re-run tests. Then continue to
SCRAP and Mutation.

Never skip the heuristics check and proceed directly to SCRAP/Mutation.
Gate-passing code can still carry redundant state, parameter bloat,
hot-path latency, and no-op cycles.
