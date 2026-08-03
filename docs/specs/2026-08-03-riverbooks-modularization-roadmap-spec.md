---
date: 2026-08-03
version: 2.3.0
last_updated: 2026-08-03
title: RiverBooks modularization roadmap and Mediator use
purpose: >
  Normative destination, sequencing, Mediator optimality rules, and
  anti-patterns for client modularization. Every future module PRD and
  plan must align with this spec.
scope:
  - End-state layout for WASM host, Modules, Pages, and Components
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
2. **Endgoal is full client RiverBooks modularization with three homes.**
   The WASM host becomes composition root + shell. Azure Functions stay a
   separate deploy unit (HTTP only). Client code splits by role:

   | Home | Rule |
   | --- | --- |
   | **Modules** | **No `.razor`.** Class library only. Domain, services, policies, ports, handlers — code other code is meant to use. Full triad + Contracts when there is a public API. |
   | **Pages** | **Has `.razor` and a route.** Route ⇒ page. **No Contracts.** Nothing depends on a page as an API. Pages use modules and components; they do not live inside modules. |
   | **Components** | **Has `.razor`.** Shared / multi-consumer UI (no route required). Never a “module with markup.” |

   **Razor litmus:** if the project has `.razor`, it is a component or page —
   never a module. If it has no `.razor`, it is a module.

   **Extract from a page only when something else needs that piece** — not
   “for architecture.” Until a second consumer exists, keep it on the page.
   **ApiHealth same rules:** route is a page; modularize reusable capability
   only when another consumer needs it. **One capability = one module.**
   Never fold two capabilities into one module (for example Articles inside
   Raindrop) unless the user **explicitly** authorizes that merge.
   Cross-capability reuse is a **project reference**, not a nested folder.
   **Tests always mirror `src`** for Modules, Pages, and Components.
   **No size or demo exemptions** for Mediator/Result quality when a page
   has real policy or IO. “Little domain” is not a reason to leave host-only
   feature services forever.
3. **Initial client load beats total published size.** Optimize first
   payload and time-to-interactive. Total CDN/publish size may grow within
   reason. Every feature must still work after its code is available.
   Prefer deferring page assemblies over arguing total zip size as the main
   gate (ApiHealth is the pilot pattern).
4. **Every product implementation DLL is lazy by default (normative).**
   Home does **not** matter: **page**, **module**, and **component**
   implementation assemblies are all deferred the same way. After first
   load in a session, soft navigation reuses the session-loaded set with
   no assembly tax. Structure (triad, Mediator, Result, homes) is
   **orthogonal** to download timing — split correctly, then lazy-load
   the whole graph.

   | Assembly kind | Load rule |
   | --- | --- |
   | Page DLL (e.g. `Articles.dll`, `Videos.dll`, `ApiHealth.Page.dll`) | Lazy; entry of the navigate batch |
   | Module impl DLL (e.g. `Raindrop.dll`, `AzureHealthCheck.dll`) | Lazy; co-loaded with the page that needs it |
   | Component DLL (e.g. `Components.dll`) | Lazy; co-loaded with the page that needs it; shared across pages on first need |
   | Contracts / Common / host shell / framework | Eager — required at DI `Build` or boot |

   - **Co-load the full need-set:** if a route needs page + module +
     component DLLs, load them **together** on first navigation (one
     `LoadAssembliesAsync` batch). Every one of those DLLs is marked
     `BlazorWebAssemblyLazyLoad`. Never leave a dependency of a lazy
     assembly on the eager graph or unmarked.
   - **Shared deps stay lazy:** a module or component used by two pages
     is still lazy — first page loads it; later pages reuse it. Do **not**
     eager-load shared impl DLLs “because shared,” “because module,” or
     “because primary product.”
   - **Eager residual only:** framework/runtime; host shell (`Program`,
     router, layout chrome); Contracts and registration types required at
     DI `Build`. Not page, module, or component **implementation**.
   - **New extract checklist:** when you add a Page, Module, or Component
     project, wire (1) `BlazorWebAssemblyLazyLoad`, (2) catalog
     co-load list for every route that needs it, (3) DI gate if the
     assembly owns runtime factories — same bar for all three homes.
   - **Optimize later with data:** do not pre-merge routes into Home or
     invent new co-location for load shape until measured traffic and
     Network/TTI evidence justify it. Uniform lazy first.
   - **Home prefetch (product choice):** when the user is on **Home**, after
     the shell is interactive, **only** prefetch the **Articles** and
     **Videos** batches (full need-set each). No other routes. Prefer
     Blazor `LoadAssembliesAsync` (runtime attach + cache), not static
     HTML `rel=prefetch` of every DLL. Gate on
     `navigator.connection.saveData` when implementing. Silent on failure;
     real navigation still loads via `OnNavigateAsync`. **Live:**
     `IPageAssemblyLoader` + `PageAssemblyCatalog` (Articles →
     `Articles.dll` + `Components.dll` + `Raindrop.dll`; Videos →
     `Videos.dll` + `Components.dll` + `Raindrop.dll`) + path→pageKey +
     Home first-render prefetch + module gates.
   Procedure depth: skill `rm-blazor-lazy-loading`.

Phase 1 Raindrop (IO triad + Strategy, factory gone) is the foundation.
**P0 done:** AzureHealthCheck implementation assembly lazy load (PRD 002; user
confirmed). **§6.3 done:** Raindrop Mediator use cases + Result + cache policy
in module (PRD 003). **§6.4 V1 done:** residual host Raindrop surface into
module; **Articles**, **Videos**, and **ApiHealth** as separate **page**
projects under **Pages**; shared list/badge UI under **Components**
(`Components.dll`); domain stays `Raindrop.dll`; page-lazy + Home prefetch
real. **§6.4 V2 done:** image stays Core + Common contracts; single Core DI
extension; collaborators internal. **§6.4 V3–V5 page homes done:** Debug,
samples, Home, Auth as Pages RCLs (lazy; Home eager). **Immediate priority:**
none remaining on the §6.4 client sequence table — further work is new product
scope, not backlog page extracts.


## 1 — Scope and Definitions

| Term | Meaning in this program |
| --- | --- |
| **Triad** | `{Name}.Contracts`, `{Name}`, `{Name}.Tests` under Modules (capability only; **no `.razor`**) |
| **Module** | No `.razor`. Reusable capability other code is meant to use — not a route |
| **Page** | Has `.razor` and a route; no Contracts; uses modules and components |
| **Component** | Has `.razor`. Shared multi-consumer UI; not a module |
| **Host** | `redmuffin.Blazor.StaticWeb` — shell, composition root, residual host pages |
| **Module application API** | Public Mediator requests in Contracts; handlers in the module |
| **Port** | Contracts abstraction for IO or storage; implementations internal |
| **Use case** | One user or application intention (load, refresh), not a private helper |
| **Vertical slice** | One shippable change with build + module/page tests + host tests green |
| **Api boundary** | `src/redmuffin.Blazor.StaticWeb.Api/` — Functions deploy unit |

## 2 — End-state layout

| Area | End state |
| --- | --- |
| **Modules** | One triad per **reusable** capability — **zero** `.razor` files; never a route |
| **Pages** | Routed surfaces with `.razor` (Articles, Videos, demos, …); **no** Contracts; reference modules + components |
| **Components** | Shared / multi-consumer `.razor` UI under the Components home |
| **Host** | Shell, nav, composition; prefer no long-lived feature services |
| **Common** | Kernel only (`Result`, pipeline behaviors, dual-consumed DTOs) |
| **Core** | True app chrome / shared UI infrastructure — or promote when it gains multi-feature policy |
| **Api / Functions** | Separate deployment; never extracted into Modules “for modularity” |
| **Cross-boundary share** | HTTP, or deliberate dual-consumed types in Common — not shared project source across deploy units |

Host owns shell layout, nav, and composition. Routed product pages belong
under **Pages** when extracted. A lazy page RCL is still a **page**, not a
module. Modules are razor-free capability (e.g. Raindrop IO, AzureHealthCheck
services). Shared multi-consumer UI lives under **Components** (e.g. Raindrop
list and badge used by Articles and Videos).

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
| AzureHealthCheck triad + Result + Mediator + Strategy | Done (template) |
| Raindrop IO triad + Strategy; factory deleted; Api untouched | Done (Phase 1) |
| AzureHealthCheck implementation assembly lazy load | **Done** (lazyAssembly + user-confirmed) |
| ApiHealth route UI under `Pages/` (not in module) | **Done** (homes: page vs capability; SN-0060 module-RCL placement superseded) |
| Raindrop Mediator use cases + Result + cache policy in module | **Done** (PRD 003 / §6.3) |
| **V1 Complete Raindrop** (residual + RCL pages + page-lazy + prefetch) | **Done** (§6.4 V1) |
| **V2 Image / placeholder policy** | **Done** (Core + Common; single DI extension; internal collaborators; PRD 004) |
| V3 Debug → V4 Samples batch → V5 Home/Auth | **Done** (Pages RCLs + catalog lazy need-sets; Home eager) |
| Further route UI | Prefer **Pages/** + lazy need-set (page DLL + module/component DLLs); modules stay razor-free capability |
| Demo/sample/tiny pages | **In program scope** — same scorecard + same **page-lazy** rule as product features; **V4 samples batch** (not per-page PRDs); not skip |

### 6.2a ApiHealth lazy load (P0 contract) — DONE

**Title intent:** defer download/load of the AzureHealthCheck **implementation**
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

### 6.2b ApiHealth route UI home — DONE (supersedes module-RCL placement)

**Title intent (current):** `/api-health` route UI lives under
`Pages/ApiHealth` (`ApiHealth.Page.dll`). Capability triad is razor-free
`Modules/AzureHealthCheck` + `AzureHealthCheck.Contracts` + tests.
Navigate co-loads `ApiHealth.Page.dll` + `AzureHealthCheck.dll`; host keeps
shell + load gate + eager thin `GetHelloHandler`.

**History:** SN-0060 briefly placed the page inside the module RCL as a lazy
UI pilot. Homes policy later split **Pages** (routes) from **Modules**
(capability); that placement is superseded — do not re-home route UI into
modules.

**Out of scope:** folding the page back into `Modules/AzureHealthCheck`,
Api project, shared-UI library extraction unless a later PRD names it.

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
- Moving route `.razor` into module projects (forbidden by homes; pages use
  **Pages/** — not this Raindrop vertical)

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
| **V1** | **Complete Raindrop + page projects** | **Done.** Residual host Raindrop into module; **Articles** and **Videos** as **separate page** projects; page DLL + `Raindrop.dll` lazy; catalog + Home prefetch active |
| **V2** | Image / placeholder policy | **Done.** Implementations stay host Core; page-facing contracts in Common; `AddImagePlaceholderServices`; Core collaborators internal — not a Modules triad |
| **V3** | Debug island | **Done** as Pages RCL (`Debug.dll` lazy) — not a domain Modules triad |
| **V4** | Samples batch | **Done.** Counter, Weather, Foundation, Icons, MarkdownExamples as Pages RCLs + page-lazy (Markdig co-lazy with MarkdownExamples) |
| **V5** | Home / Auth leftovers | **Done.** Home eager RCL; Auth/redirect lazy RCL |
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
3. **Raindrop domain module** (IO/cache/facade). Shared multi-consumer
   Razor (`RaindropItemList`, `RefreshBadge`, badge state, page context
   helpers) lives under the **Components** home, not inside Raindrop.
   **Do not** put Articles or Videos pages inside Raindrop.
4. **Separate Articles and Videos page projects.** Each is its own RCL
   under **Pages** (`src/.../Pages/{Name}/`). They reference
   **Components** (shared list/badge UI) and `Raindrop.Contracts`
   (Mediator messages); runtime co-loads `Raindrop.dll` for domain DI.
   Tests mirror under `tests/.../Pages/{Name}.Tests/`. ApiHealth page
   is under Pages (`ApiHealth.Page.dll`); capability triad is
   `AzureHealthCheck.Contracts` + sibling `Modules/AzureHealthCheck` + tests.
5. **Host-eager thin Mediator handlers.** Load/Refresh handlers stay on
   the host (ApiHealth `GetHelloHandler` pattern) so Mediator SourceGen
   does not root lazy impl DLLs at boot. Handlers depend only on Contracts
   ports; Raindrop owns use-case implementation behind a gate-resolved
   facade.
6. **Lazy load + catalog + gate.** `BlazorWebAssemblyLazyLoad` includes
   `Articles.dll`, `Videos.dll`, `Components.dll`, and `Raindrop.dll`.
   Catalog: articles →
   `["Articles.dll", "Components.dll", "Raindrop.dll"]`; videos →
   `["Videos.dll", "Components.dll", "Raindrop.dll"]`. Deferred DI gate
   after `LoadAssembliesAsync`. Home prefetch of Articles/Videos is real.
7. **Prove.** Build; Raindrop.Tests; Articles.Tests; Videos.Tests; host
   suites; Api project **untouched**.

**Out of scope for V1:** full image policy module (V2); debug; samples;
Home/Auth; Api; HTML `rel=prefetch` of DLLs; co-locating Articles into Home
without measured data (§0 item 4); merging Articles/Videos into Raindrop.

**Acceptance:**

- No residual policy/cache under host `Features/Raindrop` (host may keep
  thin eager handlers + gate only).
- `/articles` lives in lazy `Articles.dll`; `/videos` in lazy
  `Videos.dll`; both co-load shared `Raindrop.dll` on first need.
- Pages inject `IMediator` (+ UI-only deps such as image interfaces from
  Common); not `IRaindropAPI` or cache types.
- Scorecard §7 and page-load graph row green for Articles/Videos.

**Demo / tiny page bar (normative — applies from V4 onward, same bar as
product):**

1. No permanent host `Features/.../Services` for reusable capability or for
   page policy that belongs behind Mediator — including samples.
2. Page injects `IMediator` (+ UI-only deps), not module service interfaces.
3. Expected failures use `Result` + `Match` when the use case can fail
   expectedly; cancel stays exceptional.
4. Page/module tests green (mirroring `src`); scorecard in §7 applies.
5. Same **assembly-lazy** rule as every product extract (§0 item 4): page +
   module + component need-set co-loads; shared deps lazy on first need.
6. Route stays a **page**; promote reusable pieces to Modules/Components only
   when a second consumer exists.

## 7 — Quality scorecard

| Signal | Good | Bad |
| --- | --- | --- |
| Host page constructor | `IMediator` (+ UI-only deps) | Module services + cache + orchestrator |
| Expected API failure | `Result` + `Match` | Exception-type switches in UI |
| Cross-cutting | Pipeline behavior | Copy-paste logging per page |
| New reusable capability | Own module (+ Contracts/tests mirror) + requests | Folded into another module “because related”; host `Features/.../Services` |
| New routed surface | Page home; no Contracts; uses modules | Called a “module” only because it has a route or RCL |
| Extract from page | When a second consumer needs the piece | “For architecture” with no second consumer |
| Api project | Untouched except intentional HTTP contracts | Types dragged across deploy boundary |
| Mediator | One `Send` per use case | Dozens of micro-requests |
| Modularization | One capability per module; ports clear | Multi-capability mega-module without explicit user merge OK |
| Assembly load graph | Page + module + component impl DLLs lazy; co-load full need-set; shared deps lazy on first need | Any product impl DLL on eager boot; missing `BlazorWebAssemblyLazyLoad`; split co-load |
| Demo / tiny page | Mediator + Result when policy/IO exists; same lazy bar; extract capability only if reused | Left host-only forever with feature services; eager page/module/component impl |
| Tests layout | Mirrors `src` (Modules / Pages / Components) | Page tests left under host Features after extract |

## 8 — Anti-patterns (do not plan these)

1. Big-bang Result + Mediator + cache move + both pages + all mocks in one PR.
2. Mediator **and** continued page injection of the same module service.
3. Moving Azure Functions into Modules (or the reverse).
4. Putting two capabilities in one module (Articles/Videos inside Raindrop)
   without an **explicit** user authorization to merge.
5. Dragging concrete `IJSRuntime` / browser storage types into Contracts.
6. Measuring success as “matches the guide checklist” without scorecard gains.
7. Leaving page or module tests under host `tests/.../Features/` after extract
   (tests must mirror project structure).
8. Modularization that only relocates files under `Features/` with no
   capability triad where policy/IO warrants one.
9. Skipping demo/sample/tiny pages permanently because they have little
   domain or “are not real product.”
10. Leaving a **page, module, or component** implementation DLL on the
    **eager** boot graph “because primary product,” “because shared,” or
    “because it is a module not a page.” All product impl DLLs are lazy
    (§0 item 4). Contracts stay eager.
11. Calling a routed surface a **module** solely because it has an RCL or
    project. Route ⇒ page; Modules = no `.razor`.
12. Putting `.razor` (or leftover `_Imports.razor` + `Sdk.Razor`) in a
    module project. Markup ⇒ Components or Pages; modules are class libraries.
13. Extracting page pieces into Modules/Components with no second consumer
    “for architecture.”
14. Loading only one of several need-set DLLs on navigate (forgetting
    co-load or `BlazorWebAssemblyLazyLoad` on a module/component dep).

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

1. Destination matches §2 (host thin; Modules / Pages / Components homes;
   modules own reusable policy; Api boundary intact).
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
