---
title: feat/apihealth-assembly-lazy-load
date: 2026-08-03
status: done
last_updated: 2026-08-03
---

> **Archive note (2026-08-03):** This PRD shipped lazy load for the health-check
> **implementation** DLL. Current names and homes: lazy
> `AzureHealthCheck.dll` + `ApiHealth.Page.dll`; Contracts
> `AzureHealthCheck.Contracts` stay eager; route UI is under `Pages/ApiHealth`
> (not the module). SN-0060’s module-RCL page placement is **superseded** —
> see roadmap §6.2b. Historical body below still says `ApiHealth` for the
> module where that was the name at ship time.

## Problem

The ApiHealth **implementation** assembly ships with the host and is part of
the normal WASM startup graph even when the user never opens `/api-health`.
A Release property `BlazorWebAssemblyLazyLoad` exists on the host project but
does not list assemblies or wire `LazyAssemblyLoader`, so it does not defer
download. `Program.cs` calls `AddApiHealthModule` at boot, which roots types
in that assembly on the cold path.

Product priority has shifted: **initial page load** matters more than total
published size (total may grow within reason). Every feature must still work.
Total CDN size is not the stop condition for this work.

## Decision

1. **P0 is Blazor assembly lazy loading for ApiHealth on the current layout**
   (pages stay in the host; no razor-into-module prerequisite).
2. **Optimize first payload / time-to-interactive**, not minimum total zip.
3. **Prove:** outside `/api-health`, the ApiHealth implementation assembly is
   not loaded initially; on `/api-health`, it loads and the feature works.
4. **Raindrop stays eager** in this vertical (Articles/Videos still need it).
5. **Razor move (SN-0060)** was out of scope for this PRD; it is now the
   **authorized follow-on** (module RCL owns the page). See roadmap §6.2b.

This vertical is **client load-path work**, not a new RiverBooks triad and
not Raindrop Phase 2. The modularization roadmap inserts it as **priority 1**
ahead of Raindrop Mediator use cases.

## Solution

Lazy-load the ApiHealth **implementation** project assembly only.

| Piece | Load timing |
| --- | --- |
| Host page `Features/ApiHealth/*` | Eager (stays in host) |
| `ApiHealth.Contracts` | Eager (page already uses queries/ports) |
| `ApiHealth` implementation (services, and any types that must not root boot) | **Lazy** — load on navigate to `/api-health` |

Required shape:

1. Mark the implementation assembly with a proper
   `BlazorWebAssemblyLazyLoad` **item** (not only a boolean property).
2. Remove eager `AddApiHealthModule` type use from the cold `Program` path.
3. Register host-safe hooks at startup (`LazyAssemblyLoader`, deferred
   service gate/proxy so MS.DI stays fixed after `Build()`).
4. In `App` `OnNavigateAsync`, when the path is `/api-health`, await
   `LoadAssembliesAsync` for the implementation assembly, then attach real
   implementations before the page needs them.
5. Keep Mediator page shape (`IMediator` + `GetHelloQuery`). If
   SourceGen/`typeof(GetHelloHandler)` would force the implementation
   assembly at boot, use a **thin eager handler** (host or always-loaded
   surface) that depends only on `IHealthCheckService`, and keep HTTP /
   synthetic services lazy — prefer the smallest change that keeps the page
   working without loading the impl DLL at startup.

## Success Metrics

- Release publish: initial boot resources (or network on first non–ApiHealth
  route) do **not** include the ApiHealth implementation assembly.
- Navigate to `/api-health`: assembly loads; health check succeeds
  (synthetic on pure client host, real HTTP when that path applies).
- Other features (Articles, Videos, Raindrop, Home, etc.) still work without
  requiring the ApiHealth impl assembly.
- Total published size may increase within reason; that is acceptable.
- Host and module test suites remain green for existing coverage; add or
  adjust tests only where the new load gate needs a seam.

## Key Technical Decisions

- **Current layout, not razor move first.** Pages stay under host
  `Features/ApiHealth`. **Why:** P0 is initial-load proof, not UI ownership.
- **Initial load over total size.** **Why:** product care is first paint and
  feature correctness; CDN total is secondary.
- **Implementation assembly only.** Contracts and host page stay eager.
  **Why:** page already references Contracts; skipping Contracts is not
  required for the proof.
- **Deferred DI via gate/proxy, not second host Build.** **Why:** default
  MS.DI does not re-open the container after `Build()`.
- **Raindrop not lazy in this PRD.** **Why:** primary product routes depend
  on it; defer until Articles/Videos share a lazy boundary.
- **Mediator boot footgun.** SourceGen registration that `typeof`s into the
  lazy assembly at startup defeats lazy load. Fix by eager thin handler or
  post-load registration — do not leave boot-time type roots into the lazy
  DLL.

## Modules and Seams

| Area | Path | Change |
| --- | --- | --- |
| Host csproj | `src/redmuffin.Blazor.StaticWeb/*.csproj` | Lazy-load item for ApiHealth impl; fix inert property-only setup |
| Composition | `Program.cs` | No eager `AddApiHealthModule` type path; register loader + gate |
| Navigation | `App.razor` / `App.razor.cs` | Load impl assembly on `/api-health` |
| Module | `Modules/ApiHealth/*` | Remain implementations; optional registration entry for after-load |
| Contracts | `Modules/ApiHealth.Contracts/*` | Stay eager; no load-path change required |
| Page | `Features/ApiHealth/*` | Stay in host; keep Mediator usage if registration allows |
| Tests | Host / ApiHealth.Tests | Green; gate tests if a pure host seam exists |

## Testing Strategy

- **Proof (required):** Release `dotnet publish` of the host; inspect
  `blazor.boot.json` / initial resources so the ApiHealth impl assembly is
  listed as lazy (or equivalent), not initial. Browser or static check:
  cold open non–ApiHealth route does not fetch that assembly; open
  `/api-health` does, and the check runs.
- **Regression:** existing ApiHealth module tests; host suites that touch
  routing or health if any; smoke that Articles/Videos still resolve
  Raindrop.
- Do not treat total publish folder byte growth as failure.

## Non-Functional Requirements

- Prefer clear failure if the page is hit before load completes (no silent
  wrong service).
- Keep synthetic vs real Strategy policy (`localhost:5233`) behavior after
  deferred registration.
- No new product packages unless required for lazy loading APIs already in
  the Blazor WASM stack.

## Out of Scope

- Moving `.razor` into module projects (SN-0060 — later)
- Lazy-loading Raindrop, Articles, Videos, or framework assemblies
- Total published size minimization / AOT on-off for this vertical
- Azure Functions / Api project changes
- Raindrop Mediator + Result + cache Phase 2 (still next modularization
  vertical after this P0)
- Pipeline zoo, OTEL, NsDepCop

## Assumptions

- Blazor WASM lazy-load items work for project-reference assemblies on the
  current TFM when listed correctly.
- Implementation assembly file name in publish output can be confirmed from
  the boot manifest (`.dll` / webcil naming as published).
- A thin eager handler (if needed) does not reintroduce a large eager
  dependency graph from the module.

## Acceptance Criteria

- [x] Decision and this plan are linked from the modularization roadmap as P0.
- [x] ApiHealth implementation assembly is not part of the initial assembly
      set (Release publish evidence in `dotnet.js` resources):
      - `ApiHealth.wasm` listed only under `resources.lazyAssembly`
      - not present in `resources.assembly`
      - `ApiHealth.Contracts.wasm` remains eager (page/contracts path)
      - Raindrop stays eager (unchanged)
- [x] Navigate-time load wired: `App` `OnNavigateAsync` loads `ApiHealth.dll`
      on `/api-health` and fills `ApiHealthModuleGate` before page use.
- [x] Host/module tests green (host 314; ApiHealth.Tests 10). Manual browser
      smoke of `/api-health` after deploy remains recommended.
- [x] Other primary features keep eager Raindrop/host path; no ApiHealth impl
      on cold start graph.
- [x] `dotnet build` clean; no Api project changes.
- [x] Lazy behavior confirmed working (user; synthetic frontend host).

## Follow-on (authorized)

Move ApiHealth `.razor` into the module RCL while keeping lazy load — coarse
steps in `docs/sidenotes/SN-0060.md` and roadmap §6.2b. Not part of this PRD’s
implementation scope; tracked as the next ApiHealth slice.

## Related

- `docs/specs/2026-08-03-riverbooks-modularization-roadmap-spec.md`
- `docs/sidenotes/SN-0060.md`
- `docs/adr/0013-riverbooks-modular-layout-and-result.md`
- `docs/research/blazor-wasm-size-optimization-report.md`
