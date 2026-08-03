---
title: feat/raindrop-module-io-extraction
date: 2026-08-03
status: approved
---

## Problem

Raindrop IO still lives under host `Features/Raindrop/Services` with a
NavigationManager factory. ApiHealth already proves the RiverBooks triad and
host-time Strategy. Raindrop should match that seam without a big-bang move of
cache, orchestrator, or pages.

## Solution

Extract Raindrop IO into a three-project module. Public `IRaindropAPI` in
Contracts; internal real and dummy implementations plus
`AddRaindropModule(bool)` in the module. Host registers Strategy once from
`BaseAddress` (same `localhost:5233` policy as ApiHealth). Delete the factory.

## Success Metrics

- `dotnet build` succeeds with zero warnings.
- Existing host page/cache tests pass with namespace-only updates.
- Raindrop.Tests cover real and dummy IO paths (happy, fail, cancel, deserialize).
- No remaining `IRaindropAPIFactory` / `RaindropAPIFactory` references.

## Key Technical Decisions

- **IO + Strategy only.** Move interface and two implementations; leave cache,
  orchestrator, and pages in the host. **Why:** first vertical proves the
  module boundary without rewriting all page mocks.
- **Preserve exception contracts.** `Task<IEnumerable<RaindropItem>>` still
  throws `HttpRequestException` / cancel / `InvalidOperationException`.
  **Why:** Result + Mediator rewrite is phase 2.
- **Host-time Strategy, not factory.** `AddRaindropModule(bool useSyntheticData)`
  mirrors ApiHealth. **Rejected:** keep `IRaindropAPIFactory` — factory is the
  pattern being retired.
- **Internal implementations.** `RaindropAPI` and `DummyRaindropAPI` are
  `internal`; `InternalsVisibleTo` only `Raindrop.Tests`.
- **Keep `DummyRaindropAPI` name.** No rename to Synthetic this phase.
  **Why:** zero extra test/docs churn; name still clear.

## Modules & Seams

| Module | Path | Change | Test surface |
| ------ | ---- | ------ | ------------ |
| Raindrop.Contracts | `src/redmuffin.Blazor.StaticWeb.Modules/Raindrop.Contracts/` | New: public `IRaindropAPI` | — |
| Raindrop | `src/redmuffin.Blazor.StaticWeb.Modules/Raindrop/` | New: internal APIs + logging + `AddRaindropModule` | Raindrop.Tests |
| Raindrop.Tests | `src/redmuffin.Blazor.StaticWeb.Modules/Raindrop.Tests/` | New: moved IO unit tests | Happy/edge/deserialize |
| Web host | `src/redmuffin.Blazor.StaticWeb/Program.cs`, pages, orchestrator usings | Strategy registration; delete Services factory/API files | Host build |
| Host tests | `tests/.../Articles*`, `Videos*`, `Raindrop/Cache` | Usings → Contracts; remove moved service tests | Existing suites |
| Solution / arch | `redmuffin.Blazor.StaticWeb.slnx`, `quality-gates/architecture-rules.yml` | Register triad; map Contracts→Shared, Module+Tests→Frontend | architecture gate |

## Testing Strategy

- Unit-test IO at the module boundary with HTTP stubs (existing patterns).
- Host page tests keep mocking `IRaindropAPI` only.
- Do not re-test factory environment detection (deleted).

## Non-Functional Requirements

- Encapsulation: only Contracts types public from the module.
- Compatibility: same synthetic host policy (`localhost:5233`).
- No new runtime packages beyond ApiHealth-parity DI/Http.

## Out of Scope

- `Result` / Mediator queries / page `Match`
- Moving cache, orchestrator, models, pages into the module
- Renaming Dummy → Synthetic
- NsDepCop, OTEL, validation pipeline, timeout-vs-cancel UX
- **Any change to `src/redmuffin.Blazor.StaticWeb.Api/`** — Azure Functions
  is a separate deployment boundary. Phase 1 moves **host client** IO only
  (`Features/Raindrop/Services` → Modules). Functions stay in Api; modules
  only call `/api/...` over HTTP. See ADR 0013 and the module guide hard
  constraint.

Later verticals (Mediator use cases, Result, cache policy in module, full
client modularization) are governed by
`docs/specs/2026-08-03-riverbooks-modularization-roadmap-spec.md` — not by
expanding this Phase 1 PRD.

## Assumptions

- Unnamed `HttpClient` registration remains sufficient for both APIs.
- Host tests compile against Contracts transitively via the host project (same
  as ApiHealth.Contracts).
- Mock JSON paths (`mockdata/*.json`) and Azure Function routes stay unchanged.
- Api project source is not a source of types for this extraction.

## Acceptance Criteria

- [x] `dotnet build` succeeds with zero warnings.
- [x] `dotnet run --project src/redmuffin.Blazor.StaticWeb.Modules/Raindrop.Tests` passes.
- [x] Host tests pass (`dotnet run --project tests/redmuffin.Blazor.StaticWeb.Tests`).
- [x] Program uses `AddRaindropModule(useSynthetic)` with no factory.
- [x] `Features/Raindrop/Services/` factory and API types are gone.
- [x] Architecture rules list Raindrop triad correctly.
- [x] Api project untouched; Functions deployment boundary documented.
