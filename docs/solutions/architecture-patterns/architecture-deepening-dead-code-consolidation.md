---
module: solution-architecture
date: 2026-05-23
problem_type: architecture_pattern
component: solution-structure
severity: medium
applies_when:
  - surveying a solution for dead code and misplaced files
  - consolidating scattered code that shares a domain
  - dissolving directories whose remaining contents belong elsewhere
  - verifying a module is truly dead before deletion
symptoms:
  - dead service class with zero consumers (561 lines)
  - mirrored duplicate type with zero references
  - feature code split across 3 directories sharing a domain
  - cache directory with only 2 leftover files after consolidation
  - page-specific component hosted in wrong feature directory
tags:
  - architecture
  - dead-code
  - consolidation
  - namespaces
  - locality
  - blazor
  - deletion-test
related_components:
  - quality-gates
  - tooling
---

# Architecture Deepening: Dead Code, Locality, Consolidation, Dissolution

## Context

The `improve-codebase-architecture` skill (Ousterhout/Fowler/Metz/Feathers
principles) was applied to the main Blazor WASM solution. A breadth-first
survey of ~80 source files surfaced 7 candidates. The patterns that emerged
form a repeatable architecture deepening workflow.

## Guidance

### Pattern 1: Deletion Test — Verify Dead Code Before Removal

For every file or type flagged as potentially unused, run a two-phase
deletion test before acting:

1. **DI Registration Check**: grep for DI registrations of the type. Zero
   hits is necessary but not sufficient.
2. **Consumer Check**: grep every `.cs` and `.razor` file for the type name.
   Subtract self-references and test-suite-only references. If net count is
   zero, the type is dead.

Only proceed with deletion when both phases return zero external consumers.

The full dead-code removal protocol is documented in
`docs/solutions/superfluous-code-principles.md` — characterization tests
before removal, isolated commits, CI enforcement.

**Applied**: `CacheMonitoringService` (482 lines + 2 partials + 2 test files
= 561 lines, zero consumers, zero DI registrations) and
`Core/ImagePlaceholder/Models/ImageValidationResult` (47 lines, mirror
duplicate with zero consumers). Both deleted safely.

**Counter-example**: `PerformanceMetricsService` was flagged as possibly
unused. Grep revealed it is injected by `PageLoadMetricsView` and
`AppStartMetricsView` plus has its own test suite. Verified alive —
no deletion.

### Pattern 2: Locality — Place Files Where Developers Naturally Look

Apply the **locality principle** (Ousterhout): place each file in the
directory a developer would naturally look first — the one whose name matches
the file's primary purpose or consumer.

Rules:

1. **Consumer proximity**: If a file has exactly one consumer, colocate it
   with that consumer's directory.
2. **Purpose match**: If a file is shared, place it where other shared items
   of the same category live.
3. **Zero tolerance for misleading paths**: A file under a directory whose
   name contradicts its purpose is a lie. Move it.

**Applied**:

- `Redirect.razor` (OAuth token exchange) from `VideosPage/` → `AuthPage/`
- `RefreshBadge` (shared UI component) from `Features/Cache/Components/` →
  `Features/Common/Components/`
- `LocalStorageDebugService` + models from `Features/Cache/` →
  `Features/Pages/DebugPage/`

### Pattern 3: Namespace Consolidation — Unifying Split Domain Code

When code for one domain is scattered across multiple top-level directory
trees, consolidate under a single feature directory. Use sub-directories to
distinguish concerns within the domain:

```
Features/Raindrop/          ← unified root
├── Cache/                  ← caching concerns
├── Models/                 ← domain models
├── Presentation/           ← UI helpers
├── Enums/                  ← domain enums
├── Extensions/             ← domain extensions
└── Services/               ← domain API client
```

**Applied**: Raindrop code was split across `Features/Raindrop/` (API
client), `Features/RaindropItems/` (cache models + presentation helpers),
and `Features/Cache/Services/` (cache implementation). All 11 source files
and 10 test files consolidated under `Features/Raindrop/`. Two consumer
pages (Videos, Articles) and Program.cs updated. Build 0/0, 341/341.

### Pattern 4: Directory Dissolution — When a Directory's Purpose Has Evaporated

After consolidation, a directory may hold only a few files that don't belong
together. If those files can be moved to their natural homes elsewhere,
dissolve the directory entirely.

**Applied**: After Raindrop code moved out, `Features/Cache/` held only
`RefreshBadge` (Pattern 2 → `Common/Components/`) and
`LocalStorageDebugService` (Pattern 2 → `DebugPage/`). The directory was
empty — deleted. `Features/Cache/` no longer exists in the codebase.

## Why This Matters

Directory structure is a discovery mechanism and a teaching tool. When files
live where developers naturally look, navigation is fast and the structure
itself teaches the codebase. When related code is split across directories,
the connection between pieces is invisible — a developer might change a
model in one directory without realizing a service in another directory
depends on it.

The four patterns form a workflow: **survey** (breadth-first scan) →
**verify** (deletion test, grep) → **relocate** (locality) →
**consolidate** (merge split domains) → **dissolve** (remove empty
directories).

## When to Apply

- During architecture deepening with `improve-codebase-architecture`
- When a new developer asks "where is X?" more than once
- When a directory's contents don't match its name
- Before adding significant new functionality to a split domain
- When a shared directory accumulates code that only one domain consumes

## Examples

**Session results — 7 candidates, 5 executed**:

| Candidate                    | Files           | Action           | Result                                                                                     |
| ---------------------------- | --------------- | ---------------- | ------------------------------------------------------------------------------------------ |
| Dead CacheMonitoringService  | 5 files (-561L) | Delete           | Pattern 1                                                                                  |
| Dead ImageValidationResult   | 1 file (-47L)   | Delete           | Pattern 1                                                                                  |
| Redirect in wrong dir        | 4 files         | Move to AuthPage | Pattern 2                                                                                  |
| Raindrop split across 3 dirs | 21 files        | Consolidate      | Pattern 3                                                                                  |
| Cache/ directory leftovers   | 10 files        | Dissolve         | Pattern 4                                                                                  |
| PerformanceMetricsService    | 0 files         | Verify alive     | Pattern 1 counter                                                                          |
| Page orchestration dup       | 2 files         | Extracted        | Composition pattern: see `composition-over-inheritance-orchestrator-pattern-2026-05-23.md` |

## Related

- `docs/solutions/superfluous-code-principles.md` — dead code removal
  protocol (characterization tests, isolated commits, CI enforcement)
- `docs/solutions/conventions/design-changes-are-the-point-cleanup-philosophy-2026-05-16.md` —
  design-change philosophy that motivated structural relocation
- `docs/solutions/best-practices/crap-driven-functional-refactoring-2026-05-12.md` —
  structural-first workflow order (Depth → Architecture → CRAP)
- SN-0046: Extract shared page orchestration
- SN-0047: Image validation service consolidation
