---
date: 2026-05-13
last_updated: 2026-05-13
tags:
  - blazor
  - components
  - architecture
  - composition
  - naming
  - best-practice
---

# Blazor Component Architecture Standard

## What Belongs in This File

- **Viewpoint**: Agent or developer working on Blazor components in this
  repo — knows C#, Blazor WASM, and this project's feature-sliced folder
  structure.
- **What belongs**: Component architecture rules (granularity, composition
  patterns, naming, rendering), concrete decomposition plans for existing
  components, the Lego-brick philosophy.
- **What does NOT belong**: Render mode configuration (standalone WASM has
  one mode), CSS/styling decisions (those belong in `rm-ui-styling`),
  generic C# code quality rules (those belong in `rm-guide-cleanup`).

## 0 — Critical Viewpoint (READ FIRST)

Components are **Lego bricks**, not monoliths. Every component must be
instantly understandable from its name and its single responsibility. If
you can't describe what a component does in one sentence without "and",
it's doing too much.

**Two files that are 95% identical copy-pastes are the #1 anti-pattern.**
The fix is never "extract a shared base class." The fix is decompose both
into smaller single-purpose components and compose them differently.

## 1 — Component Granularity Rules

| Rule                                | Guideline                                                                                                                                                              |
| ----------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Dumb (presentational) component** | 30-80 lines of `.razor` markup. Zero or near-zero code-behind. Takes `[Parameter]` props, fires `EventCallback`s, renders markup. No data fetching, no business logic. |
| **Smart (orchestrator) component**  | 80-150 lines of `.razor` + code-behind. Fetches data, manages state, composes dumb children. May inject services.                                                      |
| **Code-behind limit**               | 30-60 lines for any `.razor.cs`. Beyond that, extract pure logic to services or helpers.                                                                               |
| **Single responsibility**           | A component does ONE thing. "Display page metrics" is not one thing. "Display a timing breakdown table" IS one thing.                                                  |
| **Cohesion over size**              | Extract what belongs together. Don't split just because of line count — split because of conceptual boundaries.                                                        |

## 2 — The Smart/Dumb (Container/Presentational) Pattern

```
Smart parent (orchestrates)
  ├── Dumb child A (renders props)
  ├── Dumb child B (renders props)
  └── Dumb child C (renders props)
```

**Smart components** own the data flow. They call services, handle state,
and pass data down as `[Parameter]` values.

**Dumb components** own the visual presentation. They receive data, render
markup, and fire callbacks up. They do not inject services or call APIs.

**Why this matters for CRAP:** Dumb components have zero cyclomatic
complexity in their `{ get; }` properties. Smart components have the CC
concentrated in their orchestration logic, where it's testable via service
abstraction.

## 3 — Naming Rules

Names are the hardest part. They must answer "what IS this?" instantly.

| Anti-pattern                     | Problem                                | Fix                                           |
| -------------------------------- | -------------------------------------- | --------------------------------------------- |
| `PageLoadSpeed` / `LoadSpeed`    | Generic, overlapping, no distinction   | `PageLoadMetricsView` / `AppStartMetricsView` |
| `Panel` / `Widget` / `Control`   | UI furniture — says nothing about WHAT | `TimingBreakdownCard`, `ResourceSizeCard`     |
| Acronyms or abbreviations        | Opaque to readers                      | Full descriptive words                        |
| `Helper` / `Utility` / `Manager` | Vague, catch-all                       | Name the specific action                      |

**Naming pattern for our components:**

```
[Scope][Subject][RenderPurpose]
```

Examples:

- `TimingBreakdownCard` → renders a card showing timing breakdown
- `AppStartMetricsView` → view of application startup metrics
- `MetricProgressBar` → progress bar for a single metric

## 4 — The Lego-Brick Philosophy

Components should fit together like physical bricks — you can see the
connection points and know which pieces snap together.

**A good component tree tells a story:**

```
AppStartMetricsView
  Reuses PageLoadMetricsView's cards...
    ├── TimingBreakdownCard     (shared)
    ├── ContentfulPaintCard     (shared)
    ├── ResourceSizeCard        (shared)
    └── WasmBootstrapCard       (unique to AppStart)
      └── MetricProgressBar    (reusable primitive)
```

**Connection points are `[Parameter]` properties.** If you need a new
connection, add a parameter. If a component has no parameters, it's
either a leaf node or it's doing too much internally.

## 5 — PageLoadSpeed / LoadSpeed Decomposition Plan

### Current state (anti-pattern)

Two 255-line `.razor.cs` files that are ~95% identical. `LoadSpeed` is
`PageLoadSpeed` plus a WASM metrics section. Same fields, same methods,
same state machine. Every bug must be fixed twice.

### Target state

```
Features/Common/PageLoadSpeed/Components/
├── PageLoadMetricsView.razor        (smart — orchestrates page metrics)
│   ├── TimingBreakdownCard.razor     (dumb — ServerResponse, DomProcessing, ResourceLoad)
│   ├── ContentfulPaintCard.razor     (dumb — FCP, LCP)
│   └── ResourceSizeCard.razor        (dumb — transfer/encoded/decoded sizes)
├── AppStartMetricsView.razor         (smart — orchestrates full startup metrics)
│   ├── TimingBreakdownCard.razor     ← same component, different props
│   ├── ContentfulPaintCard.razor     ← same component
│   ├── ResourceSizeCard.razor        ← same component
│   └── WasmBootstrapCard.razor       (dumb — wasm download time/size/decompress)
└── MetricProgressBar.razor           (dumb primitive — single value + color bar)
```

### Rename map

| Old name        | New name              | Why                                     |
| --------------- | --------------------- | --------------------------------------- |
| `PageLoadSpeed` | `PageLoadMetricsView` | Measures page-load-specific metrics     |
| `LoadSpeed`     | `AppStartMetricsView` | Superset: page metrics + WASM bootstrap |
| (extracted)     | `TimingBreakdownCard` | Server/DOM/Resource timing breakdown    |
| (extracted)     | `ContentfulPaintCard` | FCP + LCP metrics                       |
| (extracted)     | `ResourceSizeCard`    | Transfer/encoded/decoded sizes          |
| (extracted)     | `WasmBootstrapCard`   | WASM download/size/decompress           |
| (extracted)     | `MetricProgressBar`   | Reusable progress bar primitive         |

### `HasBreakdownMetrics` consolidation

This method is identical in both files (CC=9, CRAP 90.0). After
decomposition, it becomes:

```csharp
// On TimingBreakdownCard (CC=1, CRAP 1.0)
private bool HasData => ServerResponseTime > 0 || DomProcessingTime > 0;
```

Each card owns its own `HasData` check — no shared method needed.
The `> 0` checks are simple enough to inline as expression-bodied
properties.

## 6 — CRAP and Component Architecture

CRAP violations in Blazor code-behinds are almost always a signal that
the component is too large or that presentation logic is mixed with
business logic.

**Decision tree for CRAP-signaled refactoring:**

1. Is the method >10 lines and in a code-behind? → Extract to a service
2. Is the method a pure UI concern but CC>3? → Decompose into a child
   component with `[Parameter]` props
3. Is the method duplicated across two files? → Consolidate into a shared
   component or helper

## 7 — .NET 9 Considerations

This repo is standalone Blazor WASM. .NET 9's render mode features
(`InteractiveServer`, `InteractiveAuto`) do not apply — everything runs
on the client via WebAssembly.

.NET 9 features that DO matter for component architecture:

- **C# 13**: `params` collections, improved pattern matching for switch
  expressions in markup helpers
- **Performance**: Incremental rendering improvements — no change to
  architecture patterns
- **Stream rendering**: Available but only for Blazor Web Apps (SSR)

The smart/dumb pattern established by React in 2013 is unchanged — it
remains the gold standard for Blazor component architecture in 2026.

## Related

- `.opencode/skills/redmuffin-standards/rm-guide-blazor/SKILL.md`
- `.opencode/skills/redmuffin-standards/rm-guide-naming/SKILL.md` (Blazor
  Component Naming section)
- `docs/solutions/best-practices/crap-driven-functional-refactoring-2026-05-12.md`
- `.opencode/skills/redmuffin-standards/rm-guide-cleanup/SKILL.md` §2.1
  (Feathers Seam Pattern for extractions)
