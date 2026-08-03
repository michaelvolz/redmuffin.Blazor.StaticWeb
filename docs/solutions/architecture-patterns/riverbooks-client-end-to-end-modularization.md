---
title: RiverBooks client end-to-end modularization — homes, lazy load, deploy boundary
date: 2026-08-03
category: architecture-patterns
module: riverbooks-client
problem_type: architecture_pattern
component: frontend_stimulus
severity: high
applies_when:
  - Restructuring a Blazor WASM client into RiverBooks-shaped modules, pages, and components
  - Deciding where client code lives — Modules (no razor), Pages (routes), Components (shared UI), Contracts (module-owned)
  - Adding product lazy loading — catalog need-sets, BlazorWebAssemblyLazyLoad, Home eager, demos/debug/auth lazy
  - Tempted to move Azure Functions code into Modules or module code into Api
  - Keeping cross-cutting host concerns in Core (image placeholder) while stripping rare deps (Markdig) off the boot critical path
  - Choosing work size for a modularization vertical after the template is proven once
tags:
  - riverbooks
  - modular-monolith
  - blazor-wasm
  - lazy-loading
  - deployment-boundary
  - mediator
  - result-pattern
  - client-architecture
  - page-homes
---

# RiverBooks client end-to-end modularization — homes, lazy load, deploy boundary

## Context

This repository started as a host-centric Blazor WebAssembly app
(`src/redmuffin.Blazor.StaticWeb/`) where pages, services, cache, and
orchestration lived mainly in the host, with an Azure Functions API as a
separate project. Over a multi-session program (ADR 0013, the module guide,
and the modularization roadmap), the **client** was reshaped into a four-home
RiverBooks layout with Mediator use-cases, `Result` at expected-failure
boundaries, and product assembly lazy loading.

This learning records the **whole proven process** — destination rules,
sequence, load policy, and traps — so a future agent can continue or repeat
the transformation. It is not a commit log and not a substitute for the
slice-specific sheets (ApiHealth template, Raindrop Phase 1 IO).

(session history) User direction that locked the destination: restructure the
**whole client** into RiverBooks structure; use Mediator for its benefits; Azure
Functions only at the HTTP edge — never as a module. User hard stop mid-Phase 1:
nothing leaves `src/redmuffin.Blazor.StaticWeb.Api/`. Later corrections:
modularize **everything** including demos (no size exemptions); demos are
modular **and** default lazy; once each valuable concept is proven once, larger
verticals with per-step green gates beat micro-PRs for toy pages.

Final shape shipped on master as of 2026-08-03:

| Home | Rule | Confirmed in tree |
|------|------|-------------------|
| **Modules** (`src/redmuffin.Blazor.StaticWeb.Modules/`) | Domain, IO, ports, policies. No `.razor`. Class libraries only. (Mediator handlers stay host-eager.) | `src/redmuffin.Blazor.StaticWeb.Modules/AzureHealthCheck/`, `…/Raindrop/` — plain SDK libraries |
| **Pages** (`src/redmuffin.Blazor.StaticWeb.Pages/`) | Anything with a route. No Contracts. Nothing depends on a page as an API. | Eleven page RCLs under `src/redmuffin.Blazor.StaticWeb.Pages/` (Home eager; product, demos, Debug, Auth) |
| **Components** (`src/redmuffin.Blazor.StaticWeb.Components/`) | Shared multi-consumer UI (`.razor`, no route). | e.g. Raindrop list/badge shared by Articles and Videos |
| **Contracts** (`src/redmuffin.Blazor.StaticWeb.Modules/*.Contracts`) | Module-owned, never page-owned; eager seam for gates and Mediator types. | `…/Raindrop.Contracts`, `…/AzureHealthCheck.Contracts` |

**Non-negotiable:** the Api project is a separate Azure Functions deployment
unit. Modularization moves **host client** code only. Modules HTTP-call
`/api/...`; dual-consumed DTOs go in Common when deliberate. Never move code
Api ↔ Modules either direction.

## Guidance

### Proven extraction sequence

Each step stayed green before the next opened:

1. **ApiHealth pilot** — triad + host-time Strategy + `Result` + Mediator +
   page `Match` + LoggingBehavior + architecture gate. Hardened until it was
   an honest template (public host-eager handlers, internal module services,
   synthetic only on pure client host `localhost:5233`, sole Hello owner).
   Slice learning: `health-check-service-strategy-module-architecture.md`.
2. **Raindrop Phase 1 — client IO only** — public `IRaindropAPI` into
   Contracts; real/dummy **internal** in Raindrop; `AddRaindropModule(bool)`;
   delete NavigationManager factory; keep exception-shaped IO for this phase.
   Slice learning: `raindrop-module-io-extraction-client-only.md`.
3. **Raindrop Mediator vertical** — `Result` on IO; Contracts load/refresh
   queries/commands; host-eager public handlers under
   `src/redmuffin.Blazor.StaticWeb/Features/Raindrop/`;
   `IRaindropItemsStorage` port + cache policy in the module; pages
   Mediator-only via `Result.Match`; delete host orchestrator.
4. **Page home split** — Articles, Videos, ApiHealth as Pages RCLs; capability
   module renamed AzureHealthCheck (pages are not modules).
5. **Lazy-load infrastructure** — `PageAssemblyCatalog` need-sets,
   `IPageAssemblyLoader` / `PageAssemblyLoader`, host gates that type-root
   Contracts only, `BlazorWebAssemblyLazyLoad`, Router
   `AdditionalAssemblies`, Home prefetch of Articles/Videos need-sets.
6. **Remaining-pages batch** — Counter, Weather, FoundationExamples, Icons,
   MarkdownExamples, Debug, Home, Auth as page RCLs in one vertical. Home
   **eager** (not in LazyLoad); demos/debug/auth **lazy**. `Markdig.dll` only
   on the MarkdownExamples need-set. Promote `IDelayProvider` and
   `IPageAssemblyLoader` to Common when Home leaves the host.
7. **Image placeholder policy** — keep implementation in
   `src/redmuffin.Blazor.StaticWeb/Core/ImagePlaceholder/` (not a Modules
   triad): contracts in Common, `AddImagePlaceholderServices`, Core-internal
   collaborators + `InternalsVisibleTo` for tests.

### Wiring facts the tree proves

- `src/redmuffin.Blazor.StaticWeb/Program.cs:38` — `AddImagePlaceholderServices()`;
  `:44-48` — Mediator + module pipeline behaviors; `:56-69` — **lazy gates** only
  (`LazyAssemblyLoader`, `IPageAssemblyLoader`, module gates/options). Comment at
  `:57`: do **not** call `AddAzureHealthCheckModule` / `AddRaindropModule` on the
  cold path (that would force impl DLLs at boot).
- `src/redmuffin.Blazor.StaticWeb/App.razor.cs:34` —
  `EagerRouteAssemblies = [typeof(Home).Assembly]`: even an eager page RCL must
  appear in Router `AdditionalAssemblies` or `/` 404s. Navigate path loads catalog
  need-sets, then capability readiness via reflection factories on the **loaded**
  module DLL (no static type roots to lazy impl assemblies).
- `src/redmuffin.Blazor.StaticWeb/Core/Services/PageAssemblyCatalog.cs:25-43` —
  need-sets (e.g. Articles → `Articles.dll` + `Components.dll` + `Raindrop.dll`;
  MarkdownExamples → `MarkdownExamples.dll` + `Markdig.dll`);
  `HomePrefetchPageKeys` = Articles + Videos; nested debug routes map to one
  Debug need-set.
- Host csproj `BlazorWebAssemblyLazyLoad` — all page DLLs **except Home**, plus
  `Markdig.dll`, `Components.dll`, `AzureHealthCheck.dll`, `Raindrop.dll`.
- Module extensions expose both eager test registration (`Add…Module`) and
  post-load factory methods used by gates (e.g. `CreateRaindropItemsFacade`).

### Home rules (no size or demo exemptions)

- **Modules:** class libraries, no Razor. Public surface = Contracts + one
  module services extensions type. **Mediator handlers are public and
  host-resident** under host `src/redmuffin.Blazor.StaticWeb/Features/{Capability}/`
  so SourceGen never roots
  lazy impl DLLs at boot; modules ship no handlers. Service impls (APIs, facade,
  use-cases, health services) stay internal + `InternalsVisibleTo` module tests;
  a few supporting cache types may still be public in the module until tightened.
- **Pages:** every routed `.razor` is its own RCL under
  `src/redmuffin.Blazor.StaticWeb.Pages/`, including tiny demos. No page-owned
  Contracts.
- **Components:** shared `.razor` without a route, only when a second consumer
  exists.
- **Contracts:** module-owned and eager; pages and gates type-root them so
  implementation DLLs stay off the boot graph.
- **Tests:** always under `tests/`, mirroring `src/` layout. Never ship tests
  inside product module DLLs. Host tests mock Contracts only.
- **Host shell residual is intentional:** layout, storage, warm-up, Common
  timing after pages leave — not an unfinished page-extract backlog.

### Working style that worked

- **Large verticals with per-step green gates** once the template is proven —
  not one undivided mega-PR, and not Counter→Weather micro-PRs. (session
  history) User rejected “only tiny steps forever” for this trivial codebase
  after concepts were proven once.
- **Demos as one samples batch** after product lazy infrastructure exists.
- **Modular and lazy are independent axes.** Modular RCL does not imply lazy;
  lazy is default for product impl DLLs; Home is modular + eager by policy.
- **Preserve contract shape across phases** when possible (Phase 1 kept
  exception IO; Result/Mediator was its own vertical).
- **Extract only when a second consumer arrives** (Components, Common ports).
- **Gitignore traps:** the repo ignore pattern for debug build folders collides
  with `src/redmuffin.Blazor.StaticWeb.Pages/Debug` — exception + force-track
  required.
- **CI budget:** modularization raises product DLL count; raise WASM assembly
  canaries deliberately rather than undoing the structure.

### What not to treat as modularization work

(session history) Parked as future-only sidenotes, not plan phases: NsDepCop,
OTEL pipeline zoo, validation pipeline, timeout-vs-cancel UX without a
scenario, moving Azure Functions, Api internal redesign, commercial NsDep
variants. Measure razor/page RCL publish size when in doubt; dynamic route
loading is the reason separate DLLs pay off when a page is never visited.

## Why This Matters

- **Boot time is the product.** Every cold-path type root into an
  implementation DLL enters the first download. Gates + catalog keep module
  and page impl assemblies off `Build()` until navigate (or Home prefetch).
- **Homes prevent category mistakes.** Mixing routes into Modules or Contracts
  into pages rewrites dependency and load rules silently.
- **Deploy boundary is load-bearing.** Crossing Api ↔ Modules couples the WASM
  client to a Functions deploy unit and violates ADR 0013.
- **The test model survives extraction.** Internals stay in the owning module’s
  test project; host tests stay on Contracts — future verticals do not force a
  mock rewrite.
- **Process compounds.** Sequence + green gates + one honest template made the
  rest of the client conversion mechanical instead of redesign-by-fear.

## When to Apply

- Continuing any remaining client capability: pilot template → IO-only triad
  when needed → Mediator/`Result` vertical → page RCL + need-set + LazyLoad.
- New page or demo: create `Pages/{Name}` RCL, catalog key, LazyLoad entry
  (unless it is the landing route), depend only on modules/Common/Components.
- New shared UI: Components only with a second consumer.
- Host-only infrastructure (single consumer, not a domain capability): Core +
  Common contracts — image placeholder is the precedent.
- Api type sharing: dual-consumed DTO in Common deliberately; never relocate
  Functions source.
- **Do not apply** for folder aesthetics alone, or for “modularize” by moving
  Api code.

## Examples

Anchors on master as of 2026-08-03:

- `src/redmuffin.Blazor.StaticWeb/Program.cs` — image DI, Mediator, lazy gates
  only (no eager `Add…Module` on WASM cold path).
- `src/redmuffin.Blazor.StaticWeb/App.razor.cs` — `EagerRouteAssemblies`,
  catalog navigate load, post-load module readiness via reflection factories.
- `src/redmuffin.Blazor.StaticWeb/Core/Services/PageAssemblyCatalog.cs` —
  need-sets and Home prefetch keys.
- `src/redmuffin.Blazor.StaticWeb/redmuffin.Blazor.StaticWeb.csproj` —
  `BlazorWebAssemblyLazyLoad` list (Home omitted; Markdig co-lazy with
  MarkdownExamples).
- `src/redmuffin.Blazor.StaticWeb.Modules/Raindrop/` — Strategy extension,
  use-cases, storage port, internal IO.
- `src/redmuffin.Blazor.StaticWeb.Pages/Articles/`,
  `…/Videos/`, `…/ApiHealth/` — `Send` + `Match` only.
- `src/redmuffin.Blazor.StaticWeb/Core/ImagePlaceholder/` + Common image
  abstractions — keep-in-Core policy.
- `src/redmuffin.Blazor.StaticWeb.Common/Abstractions/IDelayProvider.cs`,
  `…/IPageAssemblyLoader.cs` — seams promoted when Home left the host.

## Related

- `docs/solutions/architecture-patterns/raindrop-module-io-extraction-client-only.md` —
  Phase 1 client IO vertical (moderate overlap; keep both).
- `docs/solutions/architecture-patterns/health-check-service-strategy-module-architecture.md` —
  first module template (Strategy, Mediator, Result).
- `docs/adr/0013-riverbooks-modular-layout-and-result.md` — homes + Result +
  deploy boundary decision.
- `docs/modular-monolith-module-guide-2026-08-03.md` — implementer procedure.
- `docs/specs/2026-08-03-riverbooks-modularization-roadmap-spec.md` — sequencing
  and non-goals.
- Optional refresh targets (stale pre-homes paths or deleted orchestrator):
  `docs/solutions/architecture-patterns/composition-over-inheritance-orchestrator-pattern.md`,
  `docs/solutions/conventions/blazor-wasm-folder-structure-conventions.md`,
  `docs/solutions/features/articles-page.md`.
