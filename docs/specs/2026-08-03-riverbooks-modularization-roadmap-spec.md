---
date: 2026-08-03
version: 1.2.0
last_updated: 2026-08-03
title: RiverBooks modularization roadmap and Mediator use
purpose: >
  Normative destination, sequencing, Mediator optimality rules, and
  anti-patterns for client modularization. Every future module PRD and
  plan must align with this spec.
scope:
  - End-state layout for the WASM host and Modules
  - Mediator as the module application API
  - Result at expected-failure boundaries
  - Azure Functions deployment boundary
  - Phased sequencing (especially Raindrop after IO extraction)
  - Client initial-load priority and assembly lazy-load pilots
  - Quality scorecard and forbidden moves
exclude:
  - Step-by-step how to scaffold a triad (module guide)
  - Single-feature implementation checklists (feature PRDs)
  - NsDepCop, OTEL, and validation pipeline expansion details
  - Azure Functions internal redesign beyond the deploy boundary rule
canonical_for:
  - riverbooks-modularization-roadmap
  - mediator-module-application-api
tags:
  - modular-monolith
  - riverbooks
  - mediator
  - result
  - raindrop
  - architecture
  - lazy-load
---

# RiverBooks modularization roadmap and Mediator use

> **Canonical** for modularization *destination*, *sequencing*, *Mediator
> optimality*, and *anti-patterns*. Satellites (ADR, module guide, feature
> PRDs, learnings) must not restate this program plan — link here.

## What Belongs in This File

- **Viewpoint**: Planners and implementers writing the next module PRD or
  vertical slice; assumes ADR 0013 and the module guide are known
- **What belongs**: End-state map, Mediator/Result rules, phase order,
  Raindrop next-vertical contract, initial-load / lazy-assembly priority,
  repo-wide priority hints, quality scorecard, explicit non-goals and
  anti-patterns
- **What does NOT belong**: Copy-paste project scaffolding steps, ApiHealth
  code tutorials, per-file edit lists, commit messages, session notes

## 0 — Critical Viewpoint (READ FIRST)

Three product decisions bind every future plan:

1. **Mediator is first-class.** Use it for its full benefit: use-case
   requests, pipeline behaviors, host decoupling, and test seams — not as
   an optional wrapper around services the page still injects.
2. **Endgoal is full client RiverBooks modularization.** The WASM host
   becomes composition root + pages. Feature policy lives in Modules.
   Azure Functions stay a separate deploy unit (HTTP only).
3. **Initial client load beats total published size.** Optimize first
   payload and time-to-interactive. Total CDN/publish size may grow within
   reason. Every feature must still work after its code is available.
   Prefer Blazor assembly lazy loading for rare capabilities (ApiHealth
   pilot first) over arguing total zip size as the main gate.

Phase 1 Raindrop (IO triad + Strategy, factory gone) is the foundation.
**P0 done:** ApiHealth implementation assembly lazy load (PRD 002; user
confirmed). **§6.3 done:** Raindrop Mediator use cases + Result + cache policy
in module (PRD 003). ApiHealth lazy load + razor pilot (§6.2a–b) are done —
do not cascade more razor moves. **Immediate priority:** next remaining client
feature triad after Raindrop application layer (§6.4).

## 1 — Scope and Definitions

| Term | Meaning in this program |
| --- | --- |
| **Triad** | `{Name}.Contracts`, `{Name}`, `{Name}.Tests` under Modules |
| **Host** | `redmuffin.Blazor.StaticWeb` — UI pages + DI composition root |
| **Module application API** | Public Mediator requests in Contracts; handlers in the module |
| **Port** | Contracts abstraction for IO or storage; implementations internal |
| **Use case** | One user or application intention (load, refresh), not a private helper |
| **Vertical slice** | One shippable change with build + module tests + host tests green |
| **Api boundary** | `src/redmuffin.Blazor.StaticWeb.Api/` — Functions deploy unit |

## 2 — End-state layout

| Area | End state |
| --- | --- |
| **Modules** | One triad per capability that has real policy or IO |
| **Host** | Pages and composition only; prefer no long-lived feature services |
| **Common** | Kernel only (`Result`, pipeline behaviors, dual-consumed DTOs) |
| **Core** | True app chrome / shared UI infrastructure — or promote to a module when it gains multi-feature policy |
| **Api / Functions** | Separate deployment; never extracted into Modules “for modularity” |
| **Cross-boundary share** | HTTP, or deliberate dual-consumed types in Common — not shared project source across deploy units |

Host owns shell layout, nav, and composition. **Default** remains pages in
the host; **authorized exception:** rare lazy modules may own their page UI
in a module RCL (ApiHealth pilot, §6.2b / SN-0060) so route UI rides the same
lazy assembly boundary.

## 3 — Mediator optimality rules

### 3.1 Required shape

```text
Page → IMediator.Send(request)
         → pipeline behaviors (e.g. LoggingBehavior)
         → public Handler
         → internal ports/services (HTTP, cache, etc.)
         → Result for expected failures
Page ← Match Result → ViewModel / UI state
```

ApiHealth is the reference for a thin use case. Raindrop and richer features
use the **same shape** with larger handlers and progressive payloads.

### 3.2 Benefits that must be realized

| Benefit | Required practice |
| --- | --- |
| One entry surface | Pages send use-case requests; they do not resolve module services |
| Decoupling | Host never depends on module internal types |
| Pipeline | Cross-cutting lives in Common behaviors, not copy-pasted in pages |
| Testability | Module tests: handlers + fakes. Host page tests: mock `IMediator` |
| Discovery | Public handlers; SourceGen; Contracts hold `IRequest<>` types |
| Expected failures | `IRequest<Result<TResponse>>` (or service `Result` mapped in handler) |

### 3.3 Forbidden Mediator patterns

| Pattern | Why forbidden |
| --- | --- |
| Query that only forwards to a service **while the page still injects that service** | Double path; no decoupling |
| One request per private helper (image map, badge, pure map) | Not use cases; noise |
| Mediator around pure sync mappers with no boundary | No pipeline or async value |
| Page owns cache/refresh policy and only wraps HTTP in a query | Host remains the application layer |

### 3.4 Raindrop destination use cases

IO ports (`IRaindropAPI` and similar) stay **internal to the module**.
Handlers (and module collaborators) call them. Pages do not.

| Request (Contracts) | Responsibility |
| --- | --- |
| Load articles / load videos | Cache-first load, failure mapping, progressive-capable payload |
| Refresh articles / refresh videos | Manual refresh and cache write |
| Background refresh (optional) | Same policy as product background path, if retained |

Progressive UX (cache paint, then network, badges, partial image failure)
must be modeled in Contracts responses or ViewModels. Do not crush Raindrop
into ApiHealth’s three-state hello ViewModel.

## 4 — Result rules

- Expected IO/business failures return `Result` / `Result<T>` from Common.
- Cancellation and programmer bugs remain exceptions (do not wrap cancel in
  `Result`).
- Failure strings stay infrastructure-agnostic (no hostnames, ports, product
  env names).
- Presentation must not classify `HttpRequestException` vs other expected
  failures once the module boundary returns `Result`.

## 5 — Azure Functions deployment boundary

| Allowed | Forbidden |
| --- | --- |
| Module HTTP clients calling `/api/...` | Moving code from Api into Modules, host, or Common for modularity |
| Dual-consumed DTOs deliberately in Common | Moving module/host client code into Api to “share” handlers |
| Leaving Functions in the Api project forever | Treating Api as a RiverBooks triad extraction candidate |

If a change would touch Api files only to support a module move, stop and
re-scope. See ADR 0013.

## 6 — Sequencing rules

### 6.1 Global

1. One vertical slice at a time; full verify before the next.
2. Prefer capability order by blast radius and product value, not folder size.
3. Do not add host `Features/.../Services` for new work that belongs in a
   module.
4. Pipeline expansion (validation, OTEL) is deferred unless a PRD names it;
   logging behavior already exists.

### 6.2 Status snapshot (2026-08-03)

| Slice | Status |
| --- | --- |
| ApiHealth triad + Result + Mediator + Strategy | Done (template) |
| Raindrop IO triad + Strategy; factory deleted; Api untouched | Done (Phase 1) |
| ApiHealth implementation assembly lazy load | **Done** (lazyAssembly + user-confirmed) |
| ApiHealth razor into module RCL (lazy UI pilot) | **Done** (SN-0060 / §6.2b) |
| Raindrop Mediator use cases + Result + cache policy in module | **Done** (PRD 003 / §6.3) |
| Remaining client features → triads | **Next** (§6.4) |
| Further `.razor` into modules | Only when that feature is deliberately lazy |
| Demo/sample pages | Last or never if pure samples |

### 6.2a ApiHealth lazy load (P0 contract) — DONE

**Title intent:** defer download/load of the ApiHealth **implementation**
assembly until `/api-health`; pages were still in the host for this slice.

**Product metrics:** initial load and feature correctness. Total publish
size may increase within reason.

**In scope:** host lazy-load item, deferred module DI gate, navigate-time
`LoadAssembliesAsync`, Mediator-compatible registration without boot-time
type roots into the lazy DLL, Release proof (boot resources / network).

**Out of scope for P0:** razor-into-module (now §6.2b), Raindrop lazy load,
Api project, total size minimization campaigns.

**Normative plan:**
`docs/plans/2026-08-03-002-feat-apihealth-assembly-lazy-load-prd.md`
(status: **done**).

### 6.2b ApiHealth razor-into-module (authorized)

**Title intent:** page UI lives in the ApiHealth **RCL** so the lazy assembly
carries both services and the rare route; host keeps shell + load gate +
eager thin handler.

**Big steps only (no micro-task list):**

1. Module → RCL; move page + ViewModels into module (no host Common component
   references from the module).
2. Host Router `AdditionalAssemblies` after `LoadAssembliesAsync` on
   `/api-health`; keep gate / load options / `GetHelloHandler` on host.
3. One prove pass: build, host ApiHealth tests, cold path vs route load.
4. Stop. Next program vertical after §6.3 is §6.4 (remaining client feature
   triads), not more razor moves.

**Out of scope:** Raindrop/Articles/Videos UI move, Api project, shared-UI
library extraction unless a later PRD names it.

**Normative notes:** `docs/sidenotes/SN-0060.md`

### 6.3 Raindrop next vertical (mandatory contract) — DONE

**Title intent:** Mediator use cases + Result + cache policy into the
Raindrop module; host pages Mediator-only.

**Status:** **Done** (2026-08-03). Normative plan:
`docs/plans/2026-08-03-003-feat-raindrop-mediator-use-cases-prd.md`
(status: **done**).

**In scope (completed):**

1. IO methods return `Result` for expected failures; cancel exceptional.
2. Contracts queries/commands for load and refresh (progressive-capable).
3. Handlers in `Raindrop`; internal ports for HTTP and storage.
4. Move cache and load/refresh policy into the module (browser storage behind
   a port — do not put concrete browser APIs in Contracts).
5. Collapse or delete host `RaindropPageOrchestrator` once pages only `Send`
   and map UI.
6. Host tests mock `IMediator`; module tests cover handlers + fakes.

**Out of scope for that vertical:**

- Any change to `src/redmuffin.Blazor.StaticWeb.Api/`
- Demo-page modularization
- Dummy → Synthetic rename (unless free and isolated)
- New pipeline behaviors beyond existing logging
- Moving `.razor` pages into module projects (that is §6.2b / other pilots —
  not this vertical)

**Acceptance (met):**

- Pages inject `IMediator` (plus UI-only services such as image helpers if
  still host-bound), not `IRaindropAPI` or `IRaindropItemsCache`.
- No presentation-layer `catch (HttpRequestException)` for expected load
  failures.
- Module and host test suites green; product suites zero skips.

### 6.4 After Raindrop application layer

| Priority | Candidate | Notes |
| --- | --- | --- |
| High | Finish Raindrop (presentation ports, residual host `Features/Raindrop`) | Second real module complete |
| High | Image / placeholder policy if multi-feature | Core vs module decision |
| Medium | Debug, Home, Auth slices with real services | |
| Low | Counter, Weather, Foundation samples | Little domain |
| Never as module extract | Api Functions deploy unit | HTTP boundary only |

## 7 — Quality scorecard

| Signal | Good | Bad |
| --- | --- | --- |
| Host page constructor | `IMediator` (+ UI-only deps) | Module services + cache + orchestrator |
| Expected API failure | `Result` + `Match` | Exception-type switches in UI |
| Cross-cutting | Pipeline behavior | Copy-paste logging per page |
| New feature | Triad + `AddXModule` + requests | New host `Features/.../Services` |
| Api project | Untouched except intentional HTTP contracts | Types dragged across deploy boundary |
| Mediator | One `Send` per use case | Dozens of micro-requests |
| Modularization | Policy moved; ports clear | Folder move without Contracts/handlers |

## 8 — Anti-patterns (do not plan these)

1. Big-bang Result + Mediator + cache move + both pages + all mocks in one PR.
2. Mediator **and** continued page injection of the same module service.
3. Moving Azure Functions into Modules (or the reverse).
4. Moving pages into module projects without a new ADR for UI ownership.
5. Dragging concrete `IJSRuntime` / browser storage types into Contracts.
6. Measuring success as “matches the guide checklist” without scorecard gains.
7. Modularization that only relocates files under `Features/` with no triad.

## 9 — Document roles in this cluster

| Doc | Role | Edit when… |
| --- | --- | --- |
| **This spec** | Canonical roadmap and Mediator optimality | Destination, sequence, or anti-patterns change |
| `docs/adr/0013-riverbooks-modular-layout-and-result.md` | Accepted layout + Result + Api boundary decision | Decision changes |
| `docs/modular-monolith-module-guide-2026-08-03.md` | How to scaffold and wire a triad | Procedure steps change |
| Feature PRDs under `docs/plans/` | One vertical’s problem/solution/acceptance | That vertical is planned or shipped |
| `docs/solutions/architecture-patterns/*` | Evidence and learnings | After a slice compounds |

Future plans MUST cite this spec under Related (or Key Technical Decisions)
when they extend modularization or Mediator usage.

## 10 — Verification

A future plan or PR aligns with this spec when all of the following hold:

1. Destination matches §2 (host thin, modules own policy, Api boundary intact).
2. Mediator usage matches §3 (use cases, no double path).
3. Result usage matches §4 where expected failures exist.
4. Slice size matches §6 (one vertical; lazy load §6.2a; razor pilot §6.2b;
   Raindrop next vertical matches §6.3 if claimed).
5. None of §8 anti-patterns appear as in-scope work.

## Related

- `docs/plans/2026-08-03-002-feat-apihealth-assembly-lazy-load-prd.md`
- `docs/adr/0013-riverbooks-modular-layout-and-result.md`
- `docs/modular-monolith-module-guide-2026-08-03.md`
- `docs/plans/2026-08-03-001-feat-raindrop-module-io-extraction-prd.md`
- `docs/sidenotes/SN-0060.md`
