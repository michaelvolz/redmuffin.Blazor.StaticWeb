---
date: 2026-08-03
version: 1.6.0
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
  - Full coverage including demo, sample, and tiny pages
  - Modularization vs lazy-load orthogonality
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
   becomes composition root + pages (or rare lazy RCL pages). Feature
   policy lives in Modules. Azure Functions stay a separate deploy unit
   (HTTP only). **No size or demo exemptions:** tiny pages, sample pages,
   and debug-style demos use the same triad quality bar (Contracts,
   handlers, Result where failures exist, host thin via Mediator). “Little
   domain” is not a reason to leave host-only feature services forever.
3. **Initial client load beats total published size.** Optimize first
   payload and time-to-interactive. Total CDN/publish size may grow within
   reason. Every feature must still work after its code is available.
   Prefer deferring page assemblies over arguing total zip size as the main
   gate (ApiHealth is the pilot pattern).
4. **Page assemblies lazy by default (normative).** Every page/feature
   implementation assembly is lazy-loaded. After first load in a session,
   soft navigation to that page has no assembly tax. Modular structure
   (triad, Mediator, Result) remains required and is **orthogonal** to
   download timing — modularize fully, then lazy-load the page graph.
   - **Co-load page-unique deps:** if a page needs multiple DLLs used only
     by that page, load them **together** on first navigation (same
     `LoadAssembliesAsync` batch; every assembly marked
     `BlazorWebAssemblyLazyLoad`).
   - **Shared deps stay lazy too:** an assembly used by more than one page
     is still lazy — first page that needs it loads it; later pages reuse
     the session-loaded assembly. Do not put shared page-impl DLLs on the
     eager boot graph “because shared.”
   - **Eager residual only:** framework/runtime; host shell (`Program`,
     router, layout chrome); contracts and registration types required at
     DI `Build`. Not page implementation or page-only RCLs.
   - **Optimize later with data:** do not pre-merge pages into Home, change
     default routes, or invent new co-location for load shape until measured
     traffic and Network/TTI evidence justify it. Apply page-lazy uniformly
     first; revisit boundaries only after enough data.
   - **Home prefetch (product choice):** when the user is on **Home**, after
     the shell is interactive, **only** prefetch the **Articles** and
     **Videos** page assembly batches (their page-unique DLLs together). No
     other pages. Prefer Blazor `LoadAssembliesAsync` (runtime attach + cache),
     not static HTML `rel=prefetch` of every DLL. Gate on
     `navigator.connection.saveData` when implementing. Silent on failure;
     real navigation still loads via `OnNavigateAsync`. **Host hook is live:**
     `IPageAssemblyLoader` + `PageAssemblyCatalog` (Articles/Videos →
     `Raindrop.dll`) + path→pageKey for `OnNavigateAsync` + Home first-render
     prefetch. **V1 activated** page-lazy for Articles/Videos (catalog fill +
     `BlazorWebAssemblyLazyLoad` + Raindrop module gate). Later pages: same
     pattern (fill catalog + LazyLoad + gate as needed).
   Procedure depth: skill `rm-blazor-lazy-loading`.

Phase 1 Raindrop (IO triad + Strategy, factory gone) is the foundation.
**P0 done:** ApiHealth implementation assembly lazy load (PRD 002; user
confirmed). **§6.3 done:** Raindrop Mediator use cases + Result + cache policy
in module (PRD 003). **§6.4 V1 done:** residual host Raindrop surface into
module; Articles/Videos RCL pages; `Raindrop.dll` lazy + Home prefetch real.
**Immediate priority:** §6.4 **V2 Image / placeholder policy**. Later
verticals are large capability batches — not one micro-slice per demo page.


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
| **V1 Complete Raindrop** (residual + RCL pages + page-lazy + prefetch) | **Done** (§6.4 V1) |
| **V2 Image / placeholder policy** | **Next** (§6.4 V2) |
| V3 Debug → V4 Samples batch → V5 Home/Auth | Backlog (§6.4) |
| Further `.razor` into modules | Prefer module RCL + lazy page graph (ApiHealth pattern) so page UI is not on the eager boot graph |
| Demo/sample/tiny pages | **In program scope** — same scorecard + same **page-lazy** rule as product features; **V4 samples batch** (not per-page PRDs); not skip |

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

**Out of scope for that vertical (historical — Raindrop only):**

- Any change to `src/redmuffin.Blazor.StaticWeb.Api/`
- Demo-page modularization (program-level requirement is §0 item 2 and
  §6.4 — not deferred forever; only out of *this* vertical)
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

### 6.4 Client vertical sequence (after Raindrop application layer)

**Sizing rule:** large verticals only. Do not open one PRD per toy page.
Demos and samples ship as **one batch** (V4), not Counter → Weather → …
separately. Page-lazy remains orthogonal and default for every page
(§0 item 4). Api Functions are **never** a module extract (§5).

| Order | Vertical | Intent |
| --- | --- | --- |
| **V1** | **Complete Raindrop** | Residual host `Features/Raindrop` into module; Articles + Videos as module RCL pages; `Raindrop.dll` lazy; catalog + Home prefetch **activate** |
| **V2** | Image / placeholder policy | Multi-feature image policy (Core vs module decision); full ownership beyond V1 abstraction enabler |
| **V3** | Debug island | LocalStorage debug pages + host services → triad + page-lazy |
| **V4** | Samples batch | Counter, Weather, Foundation, Icons, MarkdownExamples (and peers) in **one** batch — same scorecard + page-lazy; not per-page PRDs |
| **V5** | Home / Auth leftovers | Shell-adjacent and redirect leftovers last |
| Never | Api Functions deploy unit | HTTP boundary only |

#### V1 — Complete Raindrop (contract)

**Title intent:** finish Raindrop as the second real product module **and**
activate Articles/Videos page-lazy so Home prefetch is real (not dormant).
One vertical with **gated internal steps** (verify each step green before
the next). Do not modularize residue then rewire pages in a later vertical.

**In scope (gated steps):**

1. **Residual into module.** Move host `Features/Raindrop` cache, models,
   extensions, and presentation helpers into `Modules/Raindrop`. Storage
   port fully module-owned; browser LocalStorage stays behind the port /
   module factory (no concrete browser types in Contracts).
2. **Image abstraction enabler only.** Promote image *interfaces*
   (`IImageUrlResolver`, `IImagePlaceholderService`, and any other
   abstractions Raindrop RCL must inject) into `Common` so the RCL does not
   `ProjectReference` the host. Implementations remain host Core until V2.
3. **RCL + pages + Raindrop-only UI.** Convert Raindrop implementation to
   `Sdk.Razor` (net10, ApiHealth pattern). Move Articles + Videos pages and
   Raindrop-only components (`RaindropItemList`, `RefreshBadge` / state,
   page presentation helpers) into the module. No module → host
   `Features/Common` component references.
4. **Host-eager thin Mediator handlers.** Move Load/Refresh handlers out of
   the lazy DLL onto the host (ApiHealth `GetHelloHandler` pattern) so
   Mediator SourceGen does not root `Raindrop.dll` at boot. Handlers depend
   only on Contracts ports; module owns use-case implementation behind a
   gate-resolved facade/service.
5. **Lazy load + catalog + gate.** `BlazorWebAssemblyLazyLoad` include
   `Raindrop.dll`; `PageAssemblyCatalog` articles + videos =
   `["Raindrop.dll"]`; deferred DI gate after `LoadAssembliesAsync` (no
   boot-time `AddRaindropModule` type root into the lazy DLL). Home
   prefetch of Articles/Videos becomes a real load.
6. **Prove.** Build; Raindrop.Tests; host Articles/Videos/Raindrop suites;
   Api project **untouched**.

**Out of scope for V1:** full image policy module (V2); debug; samples;
Home/Auth; Api; HTML `rel=prefetch` of DLLs; co-locating Articles into Home
without measured data (§0 item 4).

**Acceptance:**

- No residual policy/cache under host `Features/Raindrop` (host may keep
  thin eager handlers + gate only).
- `/articles` and `/videos` route UI live in lazy `Raindrop.dll`; first
  navigation (or Home prefetch) loads the assembly; session free after.
- Pages inject `IMediator` (+ UI-only deps such as image interfaces from
  Common); not `IRaindropAPI` or cache types.
- Scorecard §7 and page-load graph row green for Articles/Videos.

**Demo / tiny page bar (normative — applies from V4 onward, same bar as
product):**

1. No permanent host `Features/.../Services` for a page that should be a
   module capability — including samples.
2. Page injects `IMediator` (+ UI-only deps), not module service interfaces.
3. Expected failures use `Result` + `Match` when the use case can fail
   expectedly; cancel stays exceptional.
4. Module + host (or module page) tests green; scorecard in §7 applies.
5. Same **page-lazy** rule as every other page (§0 item 4): page-unique
   DLLs co-load; shared page deps lazy on first need.

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
| Page load graph | Page impl (+ page-unique deps) lazy; co-load unique set; shared deps lazy | Page impl on eager boot; unique deps split across separate navigations |
| Demo / tiny page | Same triad + Mediator + tests; page-lazy like product | Left host-only “because sample”; eager page impl |

## 8 — Anti-patterns (do not plan these)

1. Big-bang Result + Mediator + cache move + both pages + all mocks in one PR.
2. Mediator **and** continued page injection of the same module service.
3. Moving Azure Functions into Modules (or the reverse).
4. Moving pages into module projects without a new ADR for UI ownership.
5. Dragging concrete `IJSRuntime` / browser storage types into Contracts.
6. Measuring success as “matches the guide checklist” without scorecard gains.
7. Modularization that only relocates files under `Features/` with no triad.
8. Skipping demo/sample/tiny pages permanently because they have little
   domain or “are not real product.”
9. Leaving page implementation on the **eager** boot graph “because primary
   product” or “because shared.” Page-lazy is the default (§0 item 4).
   Modular structure still required; do not skip the triad because the
   assembly is lazy.
10. Loading only one of several page-unique DLLs on navigate (forgetting
    co-load / `BlazorWebAssemblyLazyLoad` on deps).

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
