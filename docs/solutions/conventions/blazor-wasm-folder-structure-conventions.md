---
title: Blazor WASM Feature Folder Structure Conventions
date: 2026-05-23
category: conventions
module: Blazor WASM solution structure
problem_type: convention
component: development_workflow
severity: medium
applies_when:
  - Adding a new page or feature folder to a Blazor WASM project
  - Reviewing folder organization for redundant nesting
  - Deciding where a service belongs (Core/ vs feature-scoped)
  - Discovering files in the wrong directory
  - Namespacing a new C# file in Features/
  - Running a dead-code check before structural moves
tags:
  - folder-structure
  - feature-folders
  - service-placement
  - blazor-wasm
  - solution-organization
  - namespace-convention
  - dead-code
---

# Blazor WASM Feature Folder Structure Conventions

## Context

A .NET 9 Blazor WASM solution accumulated organizational drift over
time: redundant directory nesting (`Features/Pages/`), orphaned service
files in a root `Services/` folder, misleading folder names (`Core/` for
POCOs), and dead models with zero consumers.

The goal was to establish and enforce folder-structure conventions
aligned with the Blazor feature-folder pattern (Giesel 2022, Hilton
2021), where every feature owns its pages, models, and services in a
single directory tree. This document codifies the rules that emerged.

## Guidance

### Before/After Structure

```
Before (drifted):                          After (convention):
─────────────────────                      ─────────────────────
Features/                                  Features/
├── Pages/           ← redundant           ├── HomePage/
│   ├── HomePage/                          │   ├── Home.razor
│   ├── ArticlesPage/                      │   ├── Home.razor.cs
│   ├── DebugPage/                         │   └── Home.Logging.cs
│   ├── VideosPage/                        ├── ArticlesPage/
│   ├── WeatherPage/                       ├── DebugPage/
│   └── ... (7 more)                       │   ├── CacheReset/
│                                          │   ├── Components/
Core/                                      │   └── Services/
├── Models/                                ├── VideosPage/
│   └── BatchPerformanceMetrics.cs †       ├── Raindrop/
│                                          │   ├── Api/
Services/           ← orphaned             │   ├── Cache/
│   ├── BrowserStorageService*.cs          │   ├── Models/
│   ├── PerformanceMetricsService*.cs      │   ├── Presentation/
│   ├── CacheStats.cs †                    │   ├── Services/
│   ├── CacheHealthMetrics.cs †            │   └── Enums/
│   └── ... (5 more dead)                  ├── Common/
│                                          │   ├── Components/
│                                          │   └── PageLoadSpeed/
│                                          │       ├── Models/
│                                          │       └── Services/
                                          Core/
                                          ├── Abstractions/
                                          ├── ImagePlaceholder/
                                          ├── Layout/
                                          └── Services/
                                              ├── BrowserStorageService*.cs
                                              ├── StorageStats.cs
                                              └── StoredItemMetadata.cs
```

† = deleted (dead code, zero consumers)

### Placement Rules

| What                               | Where                                   | Rationale                                           |
| ---------------------------------- | --------------------------------------- | --------------------------------------------------- |
| Single page `.razor` + code-behind | `Features/<PageName>/`                  | One folder per page. No `Pages/` nesting            |
| Page-specific components           | `Features/<PageName>/Components/`       | Colocated with their page                           |
| Feature-scoped services            | `Features/<Feature>/Services/`          | Service stays with its consumer                     |
| Feature-scoped models              | `Features/<Feature>/Models/`            | Same as above                                       |
| Cross-cutting shared services      | `Core/Services/`                        | Infrastructure used by 2+ features                  |
| Cross-cutting domain features      | `Core/<Domain>/`                        | ImagePlaceholder with Abstractions/Models/Services/ |
| Shared reusable components         | `Features/Common/Components/`           | Truly generic UI (RefreshBadge)                     |
| Layout components                  | `Core/Layout/`                          | MainLayout, NavMenu                                 |
| App-wide interfaces                | `Core/Abstractions/`                    | IDelayProvider                                      |
| Logging partials                   | Same directory as class, `*.Logging.cs` | Partial class convention                            |

### Namespace Convention

Namespaces mirror the folder structure exactly. A file at
`Features/Raindrop/Cache/RaindropItemsCache.cs` has namespace
`redmuffin.Blazor.StaticWeb.Features.Raindrop.Cache`.

When flattening or renaming directories, update all namespaces, `_Imports.razor`,
and `@namespace` directives.

### Feature Isolation (Hilton)

A component in one feature folder must never reference a component in
a sibling feature. If 2+ features need the same component, pull it up
to `Features/Common/Components/`.

Do not extract a shared component after only 2 consumers. Wait for 3+
distinct features to prove the abstraction is real (Metz: Rule of Three).

### Dead Code Before Moves

Before any structural move, verify the file is alive:

```bash
grep -r "ClassName" src/ tests/ --type cs
```

A file with zero consumer references is dead and should be **deleted**,
not relocated.

## Why This Matters

**Following these conventions:**

- Feature discovery is O(1): navigate to `Features/<FeatureName>/` and
  everything for that feature is there
- Namespaces are predictable — they match the folder structure exactly
- New contributors don't waste time guessing whether a service belongs
  in `Services/`, `Core/`, or `Features/Pages/Common/`
- Dead code doesn't accumulate and create false signals during search
  and refactoring

**Not following these conventions:**

- Redundant nesting adds clicks and namespace verbosity for zero gain
- Orphaned services create "where does this live?" friction on every change
- Misleading folder names cause incorrect assumptions
- Dead code survives structural moves, migrating files with no reason to exist

## When to Apply

- When creating a new Blazor page or feature directory
- When onboarding a new service, model, or component and deciding where it belongs
- When discovering files in the wrong directory during review or grep
- When planning any structural refactor or cleanup sweep
- When a `_Imports.razor` or namespace no longer matches the folder layout
- When running a dead-code analysis before structural moves

## Examples

### Flattening redundant nesting

```csharp
// BEFORE: Features/Pages/DataSources/DataSources.razor.cs
namespace redmuffin.Blazor.StaticWeb.Features.Pages.DataSources;

// AFTER: Features/DataSources/DataSources.razor.cs
namespace redmuffin.Blazor.StaticWeb.Features.DataSources;
```

Update `_Imports.razor` to remove the stale namespace.

### Renaming misleading folder names

`Features/Common/PageLoadSpeed/Core/` contained 8 POCO files. The name
`Core` implied infrastructure — rename to `Models/`. Update all 18
references across `.cs`, `.razor`, and `.razor.cs` files.

### Rehoming orphaned services

`Services/BrowserStorageService.cs` sat alone at the project root with
no indication of ownership. It's cross-cutting infrastructure → move
to `Core/Services/` alongside `WarmupService`.

A feature-scoped service like `PerformanceMetricsService` → move to
`Features/Common/PageLoadSpeed/Services/` colocated with its consumers.

### Dead code first, moves second

7 cache model files (`CacheStats.cs`, `CacheHealthMetrics.cs`, etc.) had
zero consumers across the entire codebase. Deleted them. `BatchPerformanceMetrics`
similarly dead. Never relocate dead code — delete it.

## Related

- [Architecture Deepening Case Study](../architecture-patterns/architecture-deepening-dead-code-consolidation-2026-05-23.md) — proof of these conventions working in practice
- [Superfluous Code Principles](superfluous-code-principles.md) — dead code taxonomy and deletion protocol
- [Design Changes Are The Point](../conventions/design-changes-are-the-point-cleanup-philosophy-2026-05-16.md) — why structural design changes matter
- [C# Standards Final](../best-practices/csharp-standards-final-2026-04-06.md) §Feature-based structure — file-scoped namespace rules
- [Composition over Inheritance Orchestrator Pattern](../architecture-patterns/composition-over-inheritance-orchestrator-pattern-2026-05-23.md) — service placement example
- `rm-guide-naming` SKILL.md — "Directory & Namespace Structure" section
- `rm-guide-architecture` SKILL.md — "Feature Folder Structure" section
