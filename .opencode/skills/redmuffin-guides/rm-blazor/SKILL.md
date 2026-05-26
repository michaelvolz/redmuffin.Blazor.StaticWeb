---
name: rm-blazor
description: "Blazor component architecture, render behavior, lifecycle code, component DI, and granularity decisions. Use when building or reviewing Blazor components."
---

# rm-blazor

See also: `rm-code-quality` §5 for Blazor-specific false-positives and
lifecycle rules, §4 for ConfigureAwait in Blazor WASM.

## CRITICAL

- Never use component inheritance where composition suffices.
- Use `required` injected properties in component code-behind.
- Keep UI state small and renders intentional.
- **Smart/dumb pattern (MANDATORY):** Smart components orchestrate data;
  dumb components take `[Parameter]` props and render. Never mix data
  fetching and rendering in the same component.
- **Single responsibility:** A component does ONE thing. If the name
  contains "and", split it.
- **No copy-paste components:** Two 95%-identical files is the #1
  anti-pattern. Decompose into shared Lego bricks; never duplicate.
- **Progressive rendering (MANDATORY):** Render data the moment it
  arrives. Never wait for all data before showing ANY data. Show "—"
  for pending values. Never use arbitrary `Task.Delay()` between
  data-fetching phases — use event-driven or availability-checked
  patterns instead.

## WHEN TO LOAD

- Creating or refactoring `.razor` components.
- Adjusting lifecycle, state, event, or render behavior.
- Deciding component boundaries and granularity.

## GUIDANCE

- Use `OnInitializedAsync` / `OnParametersSetAsync` for lifecycle work.
- Never use custom delegates where `EventCallback` suffices.
- Use `ShouldRender()` only when you can justify the optimization.

### Shared page orchestration — composition over ComponentBase

When multiple pages share the same data-fetching workflow (cache load →
background refresh → manual refresh → error state machine), do NOT
extract a `ComponentBase` subclass. Use the **context-record +
static-orchestrator pattern** instead:

1. **Context record** — plain class bundling mutable page state
   (`Items`, `ImageUrlCache`, `ErrorMessage`, `BadgeState`, `IsLoading`,
   `IsRefreshing`). Instantiated per page with `new()`.
2. **Static orchestrator** — `static class` with pure methods taking
   the context + `Func<>` callbacks for page-specific behavior
   (`fetchAsync`, `populateImagesAsync`, `stateHasChangedAsync`).
3. **Page becomes thin wiring** — DI properties + delegate arrows.

Benefits: Unit-testable without bUnit (pass `new Context()` + fake
callbacks + `Logger_Spy`). No hierarchy lock-in. Delegates serve as
the seam — no per-page interfaces needed.

Canonical example:
`docs/solutions/architecture-patterns/composition-over-inheritance-orchestrator-pattern-2026-05-23.md`

### Blazor lifecycle methods (not dead code)

`OnInitialized`, `OnInitializedAsync`, `OnParametersSet`, `OnAfterRender`,
`OnAfterRenderAsync` are called by the runtime. Static analyzers may flag
them as dead code or uncovered — they are **false positives**. Do not
remove them or add pragmas.

### [Inject] properties and MA0015

The MA0015 warning on `[Inject]` properties is a **false positive** for
Blazor. These are injected by the DI container, not passed as method
parameters. Do not suppress; add the comment `// Injected by DI container`.

### ConfigureAwait in Blazor WASM

Blazor WASM runs single-threaded with no `SynchronizationContext`. Do NOT
use `ConfigureAwait(false)` in component code-behind — it is unnecessary.

### Fire-and-forget

Use `InvokeAsync(() => ...)` and call `StateHasChanged()` after async
operations that modify state outside lifecycle events.

### Progressive data rendering

When a component fetches data from multiple sources or in phases:

1. **Render immediately** with placeholder values ("—") for all fields.
   Never show a blank or "Loading..." component when you can show the
   skeleton with pending slots.
2. **Fetch fastest data first.** Call `StateHasChanged()` after each
   phase completes. Each card flips from "—" to real values individually.
3. **No arbitrary delays.** Never use `Task.Delay()` between fetch
   phases. If data readiness depends on JS interop availability, check
   for it explicitly. If data takes time, it takes time — the user
   sees the partial results, not an empty wait.
4. **Last phase sets "complete" state.** When all data is loaded,
   the component stops showing pending indicators.

**Example:** Page load metrics. Navigation Timing API values (TTFB,
FCP, LCP) are available immediately after JS interop is ready. Fetch
them first. WASM metrics (download time, memory) depend on
`window.pageLoadSpeed.wasmMetrics` being populated — check for
that explicitly rather than waiting an arbitrary delay.

## NEVER

- Do not hide large logic blocks inside markup.
- Do not use unnecessary re-renders as a state strategy.
- Do not extract `[LoggerMessage]` partial method declarations during cleanup.
  These are compile-time source-generated contracts — the method body is
  compiler IL, not logic. Depth gate shallow(3) failures on LoggerMessage
  methods are false positives per `rm-quality-gates` §4 Q4. Always KEEP.
