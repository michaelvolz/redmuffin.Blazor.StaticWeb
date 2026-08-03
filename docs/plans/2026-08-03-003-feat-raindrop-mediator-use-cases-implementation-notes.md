---
title: feat/raindrop-mediator-use-cases
prd: 2026-08-03-003-feat-raindrop-mediator-use-cases-prd.md
issues: none (PRD four big steps)
date: 2026-08-03
status: complete
---

# Implementation Notes — Raindrop Mediator Use Cases

**PRD:** [2026-08-03-003-feat-raindrop-mediator-use-cases-prd.md](2026-08-03-003-feat-raindrop-mediator-use-cases-prd.md)
**Issues:** None — PRD implements four gated big steps directly.

---

## Decisions Not In Plan

- **Decision:** Temporary host bridge kept `RaindropPageOrchestrator` until Step 3 by accepting `Result`-returning fetch. **Why:** Step 1 required IO `Result` and green host suites without Mediator page cutover. **Tradeoffs:** Orchestrator stayed exception-adjacent for cancel; deleted in Step 3.
- **Decision:** Single `RaindropItemsResponse` with `IsFromCache` + `HasUpdateAvailable` for load and refresh. **Why:** Progressive UX (cache paint, update badge) needs flags on Contracts payloads (roadmap §3.4 / §6.3).
- **Decision:** Host `IRaindropItemsStorage` adapter over existing `IRaindropItemsCache` / LocalStorage. **Why:** Browser storage stays out of Contracts; cache policy lives in module handlers.
- **Decision:** Pages still use `RaindropBackgroundRefreshHelper.HasDataChanged` for badge visibility after refresh `Match`. **Why:** Badge decision is pure presentation; fetch/cache policy is in handlers. `TryFetchFreshDataAsync` removed with the orchestrator.

## Key Discoveries

1. Host page tests mocked `IRaindropAPI` / cache in four Helpers files; Step 3 replaced those with `IMediator` mocks (`SetupLoad` / `SetupRefresh`).
2. Dummy missing-file path returns empty success (not failure) — preserved for local-dev UX.
3. Background `Task.Run` refresh re-renders before image `onload` tests can fire; await `BackgroundRefreshTask` before triggering image events.
4. Mediator SourceGen (MSG0005) requires a public handler per `IRequest` in the module assembly — thin handlers first, then full policy.

## Changes to Plan

| Plan said | What actually happened | Why |
| --------- | --------------------- | --- |
| Progressive-capable payloads | `RaindropItemsResponse` with `IsFromCache` + `HasUpdateAvailable` | Badge/cache paint without page-owned policy |
| Remove obsolete host cache surface | Cache implementation stays host-side behind `IRaindropItemsStorage` port | LocalStorage adapter; policy moved to handlers |
| Delete orchestrator | Deleted `RaindropPageOrchestrator` (+ tests/logging); helper reduced to `HasDataChanged` | Zero callers after page cutover |

## Pending Issues

- (none)

## Final Verification

| Check | Result |
| --- | --- |
| `dotnet build` | 0 warnings / 0 errors |
| Raindrop.Tests | 27 passed, 0 skipped |
| Host Articles/Videos/Raindrop filter | 120 passed (post-orchestrator delete) |
| Full host suite | 312 passed, 0 skipped |
| Api project | Untouched |
| Pages inject | `IMediator` + `IImageUrlResolver` only (no logger/navigation for policy) |
