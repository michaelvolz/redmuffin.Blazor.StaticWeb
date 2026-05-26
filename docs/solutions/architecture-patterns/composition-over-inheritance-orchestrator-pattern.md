---
module: blazor-page-orchestration
date: 2026-05-23
problem_type: architecture_pattern
component: service_object
severity: high
applies_when:
  - Multiple Blazor WASM pages share the same multi-step workflow (load cached data, handle refresh, fetch and cache, error state machine)
  - Pages have copy-pasted lifecycle and event-handler logic that only differs in delegate callbacks
  - You reach for a base class and immediately need abstract members, virtual hooks, or sealed override chains
  - Testing page lifecycle requires bUnit setup that a plain class would not need
symptoms:
  - ~300 lines of duplicated orchestration code across Videos.razor.cs and Articles.razor.cs
  - Parallel maintenance bugs — a fix to one page's orchestration flow is not applied to the other
  - Page code-behind files exceed 200 lines with mixed concerns (state management, caching, API calls, error handling)
tags:
  - blazor-wasm
  - composition-over-inheritance
  - page-orchestration
  - static-orchestrator
  - testability
  - deep-module
  - code-deduplication
---

# Static Orchestrator Pattern — Composition Over ComponentBase Inheritance

## Context

`Features/Raindrop/Presentation/Videos.razor.cs` (271 lines) and
`Features/Raindrop/Presentation/Articles.razor.cs` (356 lines) implement
the same page lifecycle for displaying Raindrop.io content: load from
cache, background refresh with change detection, manual refresh with badge
state machine, error handling, image cache population, and image delegate
methods. The only differences between the two pages were the Raindrop
collection ID and the fetch method. Both pages were built independently on
the same Raindrop.io list-display pattern without a shared abstraction —
the view layer duplicated `<div class="card">` markup and the code-behind
duplicated `OnInitializedAsync`, `LoadCachedDataAsync`,
`RefreshDataInBackgroundAsync`, and `HandleRefreshClickAsync`
orchestration.

The instinct was a `RaindropPageBase : ComponentBase` with abstract members
— an approach PRD-019 fully specified in April 2026 with a 388-line
requirements document and 248-line task list. (session history) It was never
implemented. The base class approach couples the orchestrator to Blazor
internals, forces a single-inheritance slot, and requires bUnit for any test
coverage.

## Guidance

Decompose into two artifacts:

### 1. Context record — bundles all mutable page state

```csharp
public sealed class RaindropPageContext
{
    public IReadOnlyList<RaindropItem>? Items { get; set; }
    public IDictionary<string, string> ImageUrlCache { get; } = new(StringComparer.Ordinal);
    public string? ErrorMessage { get; set; }
    public RefreshBadgeState BadgeState { get; set; }
    public bool IsRefreshing { get; set; }
    public bool IsLoading { get; set; }
}
```

Instantiated per page with `new()`. The page component owns the instance.
The orchestrator reads and writes it through parameters. No dependency
injection, no lifecycle, no Blazor coupling.

### 2. Static orchestrator — pure methods, zero state

```csharp
public static class RaindropPageOrchestrator
{
    public static async Task LoadCachedDataAsync(
        RaindropPageContext ctx,
        string cacheKey,
        IRaindropItemsCache cache,
        Func<CancellationToken, Task<IEnumerable<RaindropItem>>> fetchAsync,
        Func<Task> populateImagesAsync,
        ILogger logger)
    { /* cache-hit-or-fetch flow with full error handling */ }

    public static async Task HandleRefreshClickAsync(
        RaindropPageContext ctx,
        string cacheKey,
        Func<CancellationToken, Task<IEnumerable<RaindropItem>>> fetchAsync,
        IRaindropItemsCache cache,
        Func<Task> populateImagesAsync,
        Func<Task> stateHasChangedAsync,
        ILogger logger)
    { /* manual refresh + badge state machine + error taxonomy */ }
}
```

Each method is a deep module (Ousterhout): 7 parameters (context + deps +
callbacks + logger) hiding 30-60 lines of orchestration.

### 3. Page becomes a thin shell

```csharp
public partial class Videos
{
    private const string CacheKey = "Videos";
    private readonly RaindropPageContext _context = new();

    [Inject] private IRaindropAPI RaindropAPI { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        await RaindropPageOrchestrator.LoadCachedDataAsync(
            _context, CacheKey, RaindropItemsCache,
            ct => RaindropAPI.GetVideosAsync(ct),
            () => ImageValidationCacheService.PopulateImageUrlCacheAsync(
                _context.Items, _context.ImageUrlCache, ...),
            Logger);
        StateHasChanged();
        _ = Task.Run(RefreshDataInBackgroundAsync);
    }

    private Task HandleRefreshClickAsync()
        => RaindropPageOrchestrator.HandleRefreshClickAsync(
            _context, CacheKey, ct => RaindropAPI.GetVideosAsync(ct),
            RaindropItemsCache, populateImages, () => InvokeAsync(StateHasChanged), Logger);
}
```

The code-behind drops from 271/356 lines to 161/165. All orchestration
logic lives in the static class. The page is pure wiring.

## Why This Matters

**Testability without bUnit.** The orchestrator methods accept
`new RaindropPageContext()` + fake `Func<>` callbacks + `Logger_Spy`. A
2-line test exercises the full cache-hit path. No `TestRenderer`, no
Blazor lifecycle simulation, no `bUnit.TestContext`.

```csharp
// Characterization test — zero mocks
var ctx = new RaindropPageContext();
var cache = new RaindropItemsCache_Fake { GetResult = ... };
await RaindropPageOrchestrator.LoadCachedDataAsync(
    ctx, "Videos", cache, fetchStub, imageStub, loggerSpy);
await Assert.That(ctx.Items).IsNotNull();
await Assert.That(ctx.Items!.Count).IsEqualTo(1);
```

**Single-responsibility.** The orchestrator owns the workflow. The page owns
Blazor lifecycle mapping. Each can change independently — adding a loading
state to the workflow requires no page-code changes.

**No hierarchy lock-in.** A page can compose zero, one, or multiple
orchestrators. Base-class inheritance forces exactly one parent and one set
of abstract members. If Articles later needs a different cache strategy, the
orchestrator changes without affecting Videos.

**Static class = no DI registration.** The orchestrator is a pure function
library. No interface declaration, no service registration, no lifetime
management.

**Delegates over interfaces.** `Func<CancellationToken, Task<T>>` means the
page maps `RaindropAPI.GetVideosAsync(ct)` with a one-line arrow. No
`IVideoFetcher` interface needed for every variation.

## When to Apply

- Two or more pages share the same multi-step workflow (load → cache →
  fetch → error → badge → refresh)
- A page code-behind exceeds ~150 lines due to orchestration logic mixed
  with lifecycle calls
- You reach for a base class and immediately need `abstract` members,
  `virtual` hooks, or `sealed override` chains
- The workflow involves state that would be tedious to mock through
  Blazor's render tree machinery
- Pages process the same domain type with different filter/fetch parameters

Counter-indications: A single page with unique workflow. A component tree
where inheritance IS the Blazor pattern (LayoutComponentBase,
OwningComponentBase). Two occurrences where the differences are fundamental,
not delegate-level (Metz Rule of Three still applies — test the pattern with
a third consumer before declaring it a reusable abstraction).

## Side-Effects of This Extraction

**Behavioral unification.** During extraction, both pages were standardized:

- Articles' LoggerMessage delegates moved to `Articles.Logging.cs` partial
  (matching Videos' existing pattern)
- Videos gained `_isLoading` flag + loading UI (matching Articles)
- Videos' `FetchVideosAsync` gained `_imageUrlCache.Clear()` before fresh
  fetch (matching Articles)
- Loop variable renamed from `video`/`article` to `item` — both pages
  process `RaindropItem`, the name now reflects that (session history)
- LoggerMessage delegates migrated from `LoggerMessage.Define` to
  `[LoggerMessage]` source-gen — all 8 delegates consolidated to the
  orchestrator's partial class `RaindropPageOrchestrator.Logging.cs`

**Dead code elimination.** 26 unused LoggerMessage delegates removed — the
orchestrator handles all logging during cache load, fetch, and refresh; only
background-refresh delegates remain in page partials.

**Results.** Net -126 lines of code. 627 lines duplicated → 501 lines
(155 shared + 326 page). Build 0/0, 766/766 tests pass. Two new
characterization tests for the orchestrator.

## Prevention

1. **Unify behavior before extracting.** Resolve behavioral divergences
   between pages first to prevent wrong abstractions (per Sandi Metz).
   Here, `_isLoading`, `_imageUrlCache.Clear()`, and LoggerMessage
   patterns were standardized before the orchestrator was built.
2. **Rule of Three holds.** Two pages sharing ~70% identical flow was
   sufficient for extraction, but wait for a 3rd consumer before
   declaring the abstraction reusable across the codebase.
3. **Composition over inheritance for shared orchestration.**
   `RaindropPageContext` record + static orchestrator beats abstract
   `ComponentBase` base class. The context record is trivially
   testable; orchestrator methods are pure static functions accepting
   `Func<Task>` callbacks.
4. **Logging is first-class infrastructure.** LoggerMessage delegates
   belong to the orchestrator, not individual pages. The
   `[LoggerMessage]` source-gen pattern (preferred over
   `LoggerMessage.Define`) keeps log templates in one place.
5. **Compile dead code into the orchestrator, don't scatter it.**
   26 unused LoggerMessage delegates were removed during extraction;
   background-refresh delegates remain in page partials only where
   they're called.

## Related

- PRD-019 (`tasks/PRD-019-Refactor-Videos-Articles-Shared-Components.md`) —
  the rejected inheritance approach
- `docs/solutions/design-patterns/raindrop-presentation-helper-extraction-2026-05-14.md` —
  prior Feathers extraction from same files (DisplayTitle/DisplayExcerpt);
  "What Was NOT Extracted" section superseded by this pattern
- `docs/solutions/architecture-patterns/architecture-deepening-dead-code-consolidation-2026-05-23.md` —
  architecture survey that deferred this work as SN-0046
- `docs/sidenotes/SN-0046.md` — the deferred intent, now fulfilled
- `rm-guide-blazor` — component granularity and lifecycle conventions
- `rm-guide-testing` — test double patterns (RaindropItemsCache_Fake,
  Logger_Spy, Func<> callback fakes)
- `rm-guide-csharp-features` — functional C# patterns (pure static methods,
  Func<> delegates, context records)
