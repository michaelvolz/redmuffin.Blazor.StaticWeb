---
date: 2026-08-03
last_updated: 2026-08-03
tags:
  - modular-monolith
  - riverbooks
  - mediator
  - result
  - blazor-wasm
---

# How to add a RiverBooks-shaped module

Step-by-step procedure for adding the next **reusable capability module**.
Structure only — not full DDD. **Homes (roadmap §0 / ADR 0013):** Modules =
reusable capability; **Pages** = anything with a route (no Contracts);
**Components** = shared Razor multi-consumer UI. Do not put a route under
Modules just because it has an RCL. Do not leave host-only services for
real policy/IO because the page is small. **Page, module, and component
implementation DLLs are all lazy by default** (roadmap §0 item 4; skill
`rm-blazor-lazy-loading`): co-load a route’s full need-set; shared deps
lazy on first need. Eager residual is shell/framework/contracts only. Wire
`BlazorWebAssemblyLazyLoad` + catalog co-load for every new extract.

## What Belongs in This File

- **Viewpoint**: Implementers adding or converting a **capability** to a
  module, or wiring a **page** that uses modules
- **What belongs**: Project layout (Modules / Pages / Components),
  registration, Result usage, tests, architecture-gate and solution wiring
- **What does NOT belong**: Domain design of a specific feature, OTEL/validation
  pipeline expansion, NsDepCop (deferred)

## Prerequisites

- Read ADR `docs/adr/0013-riverbooks-modular-layout-and-result.md`
- Read roadmap `docs/specs/2026-08-03-riverbooks-modularization-roadmap-spec.md`
  before writing a module PRD or choosing the next vertical (destination,
  Mediator optimality, sequencing, anti-patterns, **homes**)
- AzureHealthCheck **full triad** under Modules (`AzureHealthCheck.Contracts` +
  `AzureHealthCheck` + tests) and **page** under `Pages/ApiHealth/`
  (`ApiHealth.Page.dll`). Raindrop is the pure module triad without a same-named page
- Host already calls `AddMediator` and `AddModulePipelineBehaviors`
- Every extracted page follows **page-lazy** (roadmap §0 item 4), including
  demos and product features.

## Hard constraint — Azure Functions stay in Api

`src/redmuffin.Blazor.StaticWeb.Api/` is a **deployment boundary**, not a
module extraction candidate.

| Allowed | Forbidden |
| --- | --- |
| Extract **client** IO from the WASM host `Features/` into `Modules/` | Move any code **from** `Api/` into `Modules/`, host, or Common “for modularity” |
| Module services that **HTTP-call** `/api/...` endpoints | Pull Functions triggers, workers, or Api-only types into Modules |
| Dual-consumed DTOs deliberately placed in `Common` | “Share” by relocating Api project source across the deploy boundary |

If a change would touch Api project files to support a module move, **stop**
and re-scope. Client modules and the Functions app deploy independently.

## Phase 1 — Projects

**Hard rules (non-negotiable):**

1. **Razor litmus.** Has `.razor` → component or page (**never** a module).
   No `.razor` → module (`Microsoft.NET.Sdk` only — no `Sdk.Razor`, no
   `_Imports.razor`).
2. **Modules = reusable capability only.** Domain, services, policies, ports,
   handlers — code other code is meant to use. **Route ⇒ page** (Pages home).
   Pages get **no Contracts**. Shared multi-consumer Razor → Components home.
3. **Contracts always have a sibling implementation project.**
   `{Name}.Contracts` **must** ship with `{Name}` under Modules (and mirrored
   `{Name}.Tests`). Never leave Contracts alone with implementations on a
   page, host, or “later.” Implementations of Contracts types live in the
   sibling module only.
4. **One capability = one module.** Do not place another feature’s pages or
   policy inside an existing module (for example do not put Articles/Videos
   under Raindrop). Cross-capability reuse is a **project reference**, not a
   nested folder. Merge only when the user explicitly says to.
5. **Extract from a page only when something else needs that piece.** Until
   then it stays with the page. When you extract capability that needs
   Contracts, create the **full triad** (Contracts + sibling module + tests)
   in the same change — never Contracts-only.
6. **Tests always mirror production layout** for Modules, Pages, and
   Components — never leave tests under host `Features/` after extract.

### Capability module (triad)

Create under `src/redmuffin.Blazor.StaticWeb.Modules/` and mirrored tests:

```text
src/.../Modules/{Name}.Contracts/   # public: queries, responses, interfaces
src/.../Modules/{Name}/             # handlers, services, Add{Name}Module — NO .razor
tests/.../Modules/{Name}.Tests/     # unit tests; IsTestProject=true
```

### Page project (no Contracts)

Destination home is **Pages** (not Modules). Example shape:

```text
src/.../Pages/{Name}/               # routed RCL; references modules it uses
tests/.../Pages/{Name}.Tests/       # page tests; IsTestProject=true
```

Live page projects: `src/.../Pages/Articles`, `Pages/Videos`,
`Pages/ApiHealth` (`ApiHealth.Page.dll`). Articles/Videos reference
`Components` + `Raindrop.Contracts`. ApiHealth page references
`AzureHealthCheck.Contracts` only; module impl co-loads as `AzureHealthCheck.dll`.

Never put `{Name}.Tests` under `src/`. Never fold module/page tests into the
host or Api test projects.

1. Target framework: Contracts align with Common (`net9.0`). Page/impl RCLs
   (Raindrop, Articles, Videos, …) target `net10.0` to match the WASM host.
   Do not force RCLs to `net9.0`.
2. Contracts references Common (for `Result<T>`) and Mediator.Abstractions
3. Module references Contracts + Common; package refs as needed (Http, DI)
4. Page projects `ProjectReference` the Contracts/components they use
   (Articles/Videos → Components + Raindrop.Contracts); never nest pages
   as subfolders of a module
5. Tests reference their production project + Contracts/Common as needed;
   copy test `.editorconfig` from an existing module test project; set
   `<IsTestProject>true</IsTestProject>`
6. Register production + test projects in `redmuffin.Blazor.StaticWeb.slnx`
7. Host references Module + Contracts (and page/component projects) only —
   never test projects
8. Map assemblies in `quality-gates/architecture-rules.yml`
   (Contracts → Shared, Module/Page/Component + Tests → Frontend)
9. **Lazy load (mandatory for product impl DLLs):** add
   `BlazorWebAssemblyLazyLoad` for the new assembly; put it on every
   `PageAssemblyCatalog` need-set that requires it; co-load with the route’s
   other page/module/component DLLs. Contracts stay eager.

## Phase 2 — Contracts and Result

1. Put queries/responses/interfaces in Contracts only
2. Service methods that can fail for expected reasons return `Task<Result<T>>`
3. Queries that surface those outcomes use `IRequest<Result<TResponse>>`
4. Use infrastructure-agnostic failure strings (no hostnames, ports, "SWA")
5. Cancellation stays exceptional — do not wrap `OperationCanceledException`
   in `Result`

Factory usage:

```csharp
return Result.Success(value);
return Result.Failure<string>("The API endpoint did not return a response.");
```

## Phase 3 — Module internals

1. Real + synthetic (or single) implementations of the Contracts interface
2. Mark service implementations `internal`
3. Handlers implement `IRequestHandler<,>` and stay `public` for Mediator.SourceGen
4. `InternalsVisibleTo` only for `{Name}.Tests`
5. `Add{Name}Module(this IServiceCollection services, bool useSyntheticData)`
   registers implementations and binds the interface — host must not resolve
   concrete service types
6. Logging: `[LoggerMessage]` in `*.Logging.cs` partials only

## Phase 4 — Host composition and page

1. Decide synthetic policy. Default match Raindrop/ApiHealth:

   ```csharp
   var useSynthetic = builder.HostEnvironment.BaseAddress.Contains(
       "localhost:5233",
       StringComparison.OrdinalIgnoreCase);
   builder.Services.Add{Name}Module(useSynthetic);
   ```

2. Routed UI is a **page**: host `Features/{Name}/` while still host-owned, or
   a **Pages** project when extracted (lazy RCL). Not a module solely for
   having a route
3. Page sends Mediator queries; map `Result` with `Match` to an immutable
   ViewModel
4. Do not inject module service interfaces into the page when a query exists

## Phase 5 — Tests

| Seam | Assert |
| --- | --- |
| Handler | Success and failure `Result` paths with fake service |
| Real service | Happy path + expected failures + cancel still throws |
| Synthetic | Exact success payload |
| Host or page | Renders idle/success/failure via mocked `IMediator` (route UI under `Pages/`; module impl co-loads as a separate lazy DLL) |

Run:

```text
dotnet run --project tests/redmuffin.Blazor.StaticWeb.Modules/{Name}.Tests
dotnet run --project tests/redmuffin.Blazor.StaticWeb.Tests
```

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| MA0182 on internal handler | Keep handler public for source-gen discovery |
| CS0053 on test helpers | Properties exposing `internal` types must be `internal` |
| Shared→Shared architecture fail | Contracts may reference Common; Shared is allowed to depend on Shared |
| Relative HttpClient URI fails in tests | Set `HttpClient.BaseAddress` on fakes |

## File inventory (reference)

| Path | Role |
| --- | --- |
| `src/.../Modules/AzureHealthCheck.Contracts/*` | Contracts: query, response, `IHealthCheckService` |
| `src/.../Modules/AzureHealthCheck/*` | Sibling module: services, Strategy DI, `CreateHealthCheckService` (no razor) |
| `tests/.../Modules/AzureHealthCheck.Tests/*` | Module unit tests |
| `src/.../Pages/ApiHealth/*` | ApiHealth **page** only (`ApiHealth.Page.dll` — route/UI) |
| `src/.../Modules/Raindrop*` | Domain IO, cache, facade (no razor) |
| `src/.../Components/Raindrop/*` | Shared Raindrop UI (`RaindropItemList`, `RefreshBadge`, page context helpers) |
| `src/.../Pages/Articles/*` | Articles **page** project |
| `src/.../Pages/Videos/*` | Videos **page** project |
| `tests/.../Pages/Articles.Tests/*` | Articles page tests |
| `tests/.../Pages/Videos.Tests/*` | Videos page tests |
| `Common/Result.cs` | Shared `Result` / `Result<T>` |
| `Common/PipelineBehaviors/LoggingBehavior.cs` | Cross-module pipeline |
| Host shell / gates | Eager thin Mediator handlers + module gates only |

## Related

- `docs/specs/2026-08-03-riverbooks-modularization-roadmap-spec.md`
- `docs/adr/0013-riverbooks-modular-layout-and-result.md`
- `docs/plans/2026-06-06-001-feat-modular-monolith-first-module-prd.md`
- `docs/solutions/architecture-patterns/health-check-service-strategy-module-architecture.md`
