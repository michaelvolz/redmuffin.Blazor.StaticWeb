---
title: feat/raindrop-mediator-use-cases
date: 2026-08-03
status: done
---

## Problem

Raindrop IO is modular (Phase 1), but Articles/Videos still inject
`IRaindropAPI` and `IRaindropItemsCache`, and host
`RaindropPageOrchestrator` owns load/refresh/cache policy. That leaves the
application layer on the host and blocks Mediator/Result benefits from the
roadmap.

## Solution

Move load and refresh use cases into the Raindrop module: Contracts requests
return progressive-capable payloads via `Result`; handlers own cache-first
and refresh policy against internal ports; Articles/Videos send via
`IMediator` and map UI only. Delete host orchestrator when pages no longer
need it.

## Success Metrics

- Articles and Videos inject `IMediator` (plus UI-only helpers such as image
  resolvers), not `IRaindropAPI` or `IRaindropItemsCache`.
- Pages map expected outcomes with `Result.Match` (or equivalent explicit
  Result mapping) — no presentation-layer `catch (HttpRequestException)`
  for expected load failures.
- `dotnet build` clean; Raindrop.Tests and host Articles/Videos/Raindrop
  suites green; product suites zero skips.
- Each implementation big step ends green before the next starts (roadmap
  §6.1 / anti-pattern §8.1).

## Key Technical Decisions

- **Same shape as ApiHealth, larger payload.** Mediator + `Result` for
  expected failures; progressive UX (cache paint, badges) in Contracts
  responses or ViewModels — not a three-state hello clone. **Why:** roadmap
  §3.4 / §6.3.
- **IO and cache ports not page-facing.** Handlers (and module collaborators)
  call them. `IRaindropAPI` may remain a Contracts port for the module only,
  or become internal — pages must not inject it once queries exist. **Why:**
  roadmap §3.3 / §3.4; ADR allows public Contracts interfaces.
- **Browser storage behind a port.** Concrete LocalStorage stays out of
  Contracts; adapter lives in the module or host infrastructure registered
  into the module. **Why:** §6.3 item 4; anti-pattern §8.5.
- **Public handlers, internal services.** Handlers stay `public` for
  Mediator.SourceGen (ADR / module guide). IO and storage implementations
  stay `internal`.
- **One vertical, gated steps — not one undivided mega-PR.** Four big steps
  below; each finishes with build + affected tests green before the next.
  **Why:** §6.3 scope is correct; §8.1 forbids big-bang without verify.
  **Rejected:** wrap HTTP in Mediator while pages still inject services.
- **Raindrop stays eager.** No lazy assembly work in this PRD.

## Modules & Seams

| Module | Path | Change | Test surface |
| --- | --- | --- | --- |
| Raindrop.Contracts | `src/.../Modules/Raindrop.Contracts/` | Load/refresh queries (articles + videos); response DTOs; ports stay non-page-facing | Compile |
| Raindrop | `src/.../Modules/Raindrop/` | Handlers; Result on IO; cache policy; storage port impl wiring | Raindrop.Tests handlers + fakes |
| Raindrop.Tests | `src/.../Modules/Raindrop.Tests/` | Handler + IO/cache fakes | Happy/fail/cancel |
| Host pages | `Features/ArticlesPage/`, `Features/VideosPage/` | `IMediator` only for Raindrop policy; map UI | Host bUnit mock `IMediator` |
| Host Raindrop leftovers | `Features/Raindrop/Presentation/`, `Features/Raindrop/Cache/` | Remove orchestrator when unused; cache moves or becomes adapter | Cache/unit as needed |
| Host Program | `Program.cs` | Registration for module handlers/ports; drop page-facing cache reg if obsolete | Build |
| Api | `src/.../Api/` | **Untouched** | — |

## Testing Strategy

- Module: handlers with faked IO + storage ports; expected failures as
  `Result`, cancel exceptional.
- Host: Articles/Videos mock `IMediator`; no `IRaindropAPI` mocks for policy
  paths.
- Do not re-test Azure Functions or Api project code.

## Non-Functional Requirements

- Preserve progressive UX (cache-first paint, background/manual refresh
  semantics) or document an intentional product change.
- Failure strings remain infrastructure-agnostic.
- Synthetic policy stays `localhost:5233` only (unchanged from Phase 1).

## Out of Scope

- Azure Functions / Api project changes
- Lazy-loading Raindrop
- Moving Articles/Videos `.razor` into the module
- Dummy → Synthetic rename
- New pipeline behaviors beyond existing logging
- Demo pages

## Assumptions

- `RaindropItem` and list UX stay host-renderable from Contracts payloads.
- Image URL population can remain a page/UI helper (`IImageUrlResolver`)
  unless it is true load policy (then it becomes a later slice).
- Phase 1 Strategy registration remains the HTTP Strategy switch.

## Acceptance Criteria

- [x] Contracts expose load/refresh use cases for articles and videos.
- [x] Handlers live in Raindrop and are `public`; IO methods return `Result`
      for expected failures; cancel stays exceptional; services internal.
- [x] Cache/load/refresh policy is not in `RaindropPageOrchestrator` (deleted
      or reduced to zero callers).
- [x] Articles and Videos use `IMediator` for those use cases; map via
      `Result.Match` (or equivalent); no page inject of `IRaindropAPI` /
      `IRaindropItemsCache`.
- [x] Host tests mock `IMediator`; Raindrop.Tests cover handlers + fakes.
- [x] Each big step verified green before the next; final `dotnet build`
      clean; affected test projects green; Api project untouched.
- [x] Roadmap §6.3 status updated when this PRD is complete.

## Implementation big steps (after approval)

Each step: implement → `dotnet build` → run affected Raindrop/host tests →
only then start the next step.

1. Result on IO + Contracts requests/responses for load and refresh.
2. Handlers + cache policy in module; storage port; host registration
   (public handlers).
3. Articles/Videos Mediator-only with `Match`; remove orchestrator and
   obsolete host cache surface.
4. Final suite pass; mark PRD/roadmap done.

## Related

- `docs/specs/2026-08-03-riverbooks-modularization-roadmap-spec.md` §6.3
- `docs/plans/2026-08-03-001-feat-raindrop-module-io-extraction-prd.md`
- `docs/adr/0013-riverbooks-modular-layout-and-result.md`
