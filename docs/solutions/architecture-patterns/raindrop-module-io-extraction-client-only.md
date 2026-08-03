---
title: Raindrop module Phase 1 — client IO extraction without touching Api
date: 2026-08-03
category: architecture-patterns
module: raindrop-module
problem_type: architecture_pattern
component: service_object
severity: medium
applies_when:
  - Extracting the second (or later) feature into a RiverBooks-shaped module
  - Replacing a NavigationManager factory with host-time Strategy DI
  - Tempted to "modularize" by moving Azure Functions code into Modules
tags:
  - raindrop
  - modular-monolith
  - strategy-pattern
  - deployment-boundary
  - azure-functions
  - blazor-wasm
  - phased-extraction
---

# Raindrop module Phase 1 — client IO extraction without touching Api

## Context

ApiHealth proved the triad + Strategy + Result template. Raindrop still lived
under host `Features/Raindrop/Services` with `IRaindropAPIFactory` /
`NavigationManager` environment detection. A full big-bang move of cache,
orchestrator, pages, and Result/Mediator would rewrite every page mock at once.
Separately, modularization must never pull source out of
`src/redmuffin.Blazor.StaticWeb.Api/` — that project is a deployment unit.

## Guidance

**First vertical only (client):** move public `IRaindropAPI` into
`Raindrop.Contracts`, keep real + dummy implementations **internal** in
`Raindrop`, register with `AddRaindropModule(bool useSyntheticData)`, delete the
factory. Preserve `Task<IEnumerable<RaindropItem>>` and exception contracts in
this phase — Result and Mediator are a later vertical.

**Host composition:** one `localhost:5233` flag for both ApiHealth and Raindrop:

```csharp
var useSynthetic = builder.HostEnvironment.BaseAddress.Contains(
    "localhost:5233",
    StringComparison.OrdinalIgnoreCase);
builder.Services.AddApiHealthModule(useSynthetic);
builder.Services.AddRaindropModule(useSynthetic);
```

**Azure Functions stay put:** Phase 1 moves **host client** HTTP callers only
(paths like `/api/RaindropListVideos`). Never move Functions triggers, workers,
or Api-only types into Modules. Dual-consumed DTOs belong in Common when
deliberate. Hard rule in ADR 0013, module guide, and AGENTS.md structural gate.

**Tests:** unit-test IO in `Raindrop.Tests` with InternalsVisibleTo; host page
tests keep mocking the public Contracts interface only.

## Why This Matters

- Proves the second module without rewriting UI error handling and all mocks.
- Retires the factory pattern that ApiHealth already superseded.
- Makes the Api deploy boundary explicit so agents never “extract” Functions
  into Modules under the modular-monolith banner.

## When to Apply

- Converting host `Features/{Name}/Services` client IO into `Modules/{Name}*`.
- Any design that would touch the Api project solely to support a module move —
  stop and re-scope.
- Not for moving cache/orchestrator/pages in the same change as first compile of
  the triad (do those after IO + Strategy are green).

## Examples

| Before (host) | After (module) |
| --- | --- |
| Host `Features/Raindrop/Services/*` (removed) | `src/redmuffin.Blazor.StaticWeb.Modules/Raindrop.Contracts/IRaindropAPI.cs` |
| Public `RaindropAPI` / `DummyRaindropAPI` | Internal under `src/redmuffin.Blazor.StaticWeb.Modules/Raindrop/` |
| `IRaindropAPIFactory` + factory class | Deleted; host `AddRaindropModule(bool)` |
| `src/redmuffin.Blazor.StaticWeb.Api/` | Unchanged (deployment boundary) |

Verified this session: solution build zero warnings; Raindrop.Tests 21/21; host
tests 312/312; Api tree clean of extraction edits.

## Related

- `docs/adr/0013-riverbooks-modular-layout-and-result.md` (deploy boundary section)
- `docs/modular-monolith-module-guide-2026-08-03.md` (hard constraint + procedure)
- `docs/plans/2026-08-03-001-feat-raindrop-module-io-extraction-prd.md`
- `docs/solutions/architecture-patterns/health-check-service-strategy-module-architecture.md`
  (first module; Result + Mediator full shape)
- `docs/solutions/architecture-patterns/hello-world-mock-environment-service-resolution.md`
  (historical factory — superseded for Raindrop IO)
