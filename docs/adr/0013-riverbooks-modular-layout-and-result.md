---
date: 2026-08-03
status: accepted
---

# RiverBooks-shaped modular layout and Result error model

Feature growth without enforced module boundaries risks accidental coupling.
This solution adopts Ardalis RiverBooks **structure** (not full DDD) for
bounded modules, and a shared `Result<T>` type for expected failures at module
boundaries.

## Decision

### Modules, pages, and components (homes)

**Razor litmus (non-negotiable):**

1. **Has `.razor` → not a module.** It is UI: a **component** package, or a
   **page** if it also has a route.
2. **No `.razor` → module.** Class library only (`Microsoft.NET.Sdk`). Domain,
   services, policies, ports, handlers, DI registration — never Razor SDK,
   never `_Imports.razor`, never markup.

Three client homes — do not collapse them:

| Home | Litmus | Contracts? |
| --- | --- | --- |
| **Modules** | **No `.razor`.** Reusable capability other code is meant to use | Yes, when the capability has a public API |
| **Pages** | **Has `.razor` and a route.** Route ⇒ page | **No.** Nothing depends on a page as an API |
| **Components** | **Has `.razor`, no route requirement.** Shared / multi-consumer UI | No module Contracts for pure UI |

Hard rules:

- **Razor never lives under Modules.** If you need markup, extract to
  Components (or keep it on a Page). Do not leave orphan `_Imports.razor` on
  a module to keep `Sdk.Razor`.
- **Route ⇒ page.** A routed surface is never a module — even with its own
  project or policy.
- **Pages use modules and components; they do not live inside modules.**
  Example: Articles and Videos are pages; they use Components (list/badge)
  and Raindrop **module** (domain, no razor).
- **Extract from a page only when something else needs that piece** — not “for
  architecture.” Until a second consumer exists, the piece stays with the page.
- **Contracts always have a sibling module project.** `{Name}.Contracts`
  never exists without `{Name}` (implementations of those contracts). Do not
  park implementations on a page or host while Contracts sit alone under
  Modules.
- **ApiHealth:** route is `Pages/ApiHealth` (`ApiHealth.Page.dll`); capability
  is the triad `AzureHealthCheck.Contracts` + `Modules/AzureHealthCheck` + tests.
- **One capability = one module.** Never fold two capabilities into one module
  project unless the user explicitly authorizes the merge. Cross-capability
  reuse is a project reference, not a nested folder.
- **Tests always mirror `src` layout** for whatever home the project uses
  (`tests/.../Modules/{Name}.Tests`, `tests/.../Pages/{Name}.Tests`, etc.).

### Assembly load (orthogonal to homes)

**Every product implementation DLL is lazy by default** — page, module, and
component alike. Homes classify code; they do not change download policy.

| Eager (boot / DI `Build`) | Lazy (navigate or Home prefetch batch) |
| --- | --- |
| Framework/runtime | Page DLLs |
| Host shell (router, layout chrome) | Module implementation DLLs |
| Common + Contracts + registration types required at `Build` | Component DLLs |

Rules: mark every lazy assembly with `BlazorWebAssemblyLazyLoad`; co-load the
full need-set for a route in one `LoadAssembliesAsync` batch; shared module/
component DLLs stay lazy until first need (never eager “because shared”).
Procedure: roadmap §0 item 4; skill `rm-blazor-lazy-loading`.

### Module shape (capability triad)

- Production projects under `src/redmuffin.Blazor.StaticWeb.Modules/`:
  - `{Module}.Contracts` — public queries, responses, service interfaces
  - `{Module}` — **required sibling** — handlers, services, DI extension only
    (**no `.razor`**). Contracts without this project are forbidden.
- Matching unit-test project under
  `tests/redmuffin.Blazor.StaticWeb.Modules/{Module}.Tests/` — one test
  project per module, structure mirrored; never fold into host or Api tests;
  never place test projects under `src/`
- **Page projects** live under a **Pages** home (not under `Modules/`),
  reference modules + components they use, and get mirrored tests — no Contracts
- **Component projects** live under
  `src/redmuffin.Blazor.StaticWeb.Components/` (`Sdk.Razor`)
- Host (`redmuffin.Blazor.StaticWeb`) owns shell layout, nav, and composition
  root; routed product pages prefer the Pages home when extracted
- Common holds cross-module kernel types and pipeline behaviors
- Service implementations are `internal`; Contracts types are `public`
- Mediator handlers are `public` so Mediator.SourceGen can discover them
  (MA0182 rejects unused internal handlers)
- Module registration: `Add{Module}Module(...)` wires abstractions; host
  passes environment policy flags only
- Production projects never `ProjectReference` test projects (deploy isolation)

### Synthetic vs real

- Strategy pattern: interface in Contracts, real + synthetic implementations
  in the module
- Synthetic data is used only on pure client host `localhost:5233`, matching
  Raindrop — not every `localhost` (SWA local `localhost:4280` uses real HTTP)

### Error model: `Result<T>`

- Expected IO/business failures return `Result<T>` from Common
  (`Result.Success` / `Result.Failure`)
- Cancellation and programmer bugs remain exceptions
- Service → handler → page map `Result` explicitly; pages do not use
  `try/catch` for expected API failures
- Non-generic `Result` factory class avoids CA1000 statics on generic types

### Architecture gate

- Shared components may depend on Shared (Contracts → Common for `Result<T>`)

### Azure Functions deployment boundary (hard constraint)

- `src/redmuffin.Blazor.StaticWeb.Api/` is a **separate deployment unit**
  (Azure Functions isolated worker). It is not a RiverBooks module and is not
  folded into `Modules/`.
- **Never move code out of the Api project** into Modules, the WASM host,
  Common, or any other project as part of modularization.
- **Never move module or host client code into the Api project** to “share”
  handlers or DTOs across the deploy boundary.
- Frontend modules may **call** Functions over HTTP only. Client-side ports
  (e.g. `IRaindropAPI` implementations that hit `/api/...`) live in Modules
  or the host; Functions implementations stay in Api.
- Shared contracts that both sides need (e.g. JSON DTOs) belong in
  `Common` when deliberately dual-consumed — not by extracting Functions
  source into Modules.

## Considered options

**Keep exception-driven expected failures.** Rejected for module Contracts:
UI and handlers should not depend on exception type taxonomies for normal
unreachable-API outcomes.

**Put `Result<T>` only in AzureHealthCheck.Contracts.** Rejected: second module would
duplicate the type. Common is the shared kernel.

**Factory pattern for every dual implementation (Raindrop-style).** Superseded
for new modules by Strategy registration inside `Add{Module}Module(bool)`.

**Route pages as “page modules” under `Modules/`.** Superseded: Modules hold
only reusable capability. Routed surfaces live under **Pages** (Articles,
Videos, ApiHealth); shared Razor under **Components**.

## Consequences

- New **capability** modules copy the triad under `Modules/` plus mirrored
  tests — not flat host `Features/.../Services` for policy/IO
- New **routed** extracts go under **Pages** with mirrored tests and **no**
  Contracts; they reference modules (and shared components) as needed
- New page / module / component projects are **lazy** (catalog +
  `BlazorWebAssemblyLazyLoad` + co-load need-set); only Contracts stay eager
- Extract page pieces only when a second consumer appears
- Hello connectivity lives only in the AzureHealthCheck capability (page
  `Pages/ApiHealth`); Raindrop does not expose `GetHelloWorldAsync`
- Deferred still deferred: NsDepCop, validation/OTEL pipeline behaviors

## Related

- `docs/specs/2026-08-03-riverbooks-modularization-roadmap-spec.md`
- `docs/modular-monolith-module-guide-2026-08-03.md`
- `docs/plans/2026-06-06-001-feat-modular-monolith-first-module-prd.md`
- `docs/solutions/architecture-patterns/health-check-service-strategy-module-architecture.md`
