---
date: 2026-08-03
status: accepted
---

# RiverBooks-shaped modular layout and Result error model

Feature growth without enforced module boundaries risks accidental coupling.
This solution adopts Ardalis RiverBooks **structure** (not full DDD) for
bounded modules, and a shared `Result<T>` type for expected failures at module
boundaries.

## Decision

### Module shape

- Three projects per module under `src/redmuffin.Blazor.StaticWeb.Modules/`:
  - `{Module}.Contracts` — public queries, responses, service interfaces
  - `{Module}` — handlers, services, DI extension
  - `{Module}.Tests` — unit tests for that module
- Host (`redmuffin.Blazor.StaticWeb`) owns pages/UI and composition root only
- Common holds cross-module kernel types and pipeline behaviors
- Service implementations are `internal`; Contracts types are `public`
- Mediator handlers are `public` so Mediator.SourceGen can discover them
  (MA0182 rejects unused internal handlers)
- Module registration: `Add{Module}Module(...)` wires abstractions; host
  passes environment policy flags only

### Synthetic vs real

- Strategy pattern: interface in Contracts, real + synthetic implementations
  in the module
- Synthetic data is used only on pure client host `localhost:5233`, matching
  Raindrop — not every `localhost` (SWA local `localhost:4280` uses real HTTP)

### Error model: `Result<T>`

- Expected IO/business failures return `Result<T>` from Common
  (`Result.Success` / `Result.Failure`)
- Cancellation and programmer bugs remain exceptions
- Service → handler → page map `Result` explicitly; pages do not use
  `try/catch` for expected API failures
- Non-generic `Result` factory class avoids CA1000 statics on generic types

### Architecture gate

- Shared components may depend on Shared (Contracts → Common for `Result<T>`)

## Considered options

**Keep exception-driven expected failures.** Rejected for module Contracts:
UI and handlers should not depend on exception type taxonomies for normal
unreachable-API outcomes.

**Put `Result<T>` only in ApiHealth.Contracts.** Rejected: second module would
duplicate the type. Common is the shared kernel.

**Factory pattern for every dual implementation (Raindrop-style).** Superseded
for new modules by Strategy registration inside `Add{Module}Module(bool)`.

## Consequences

- New modules copy ApiHealth triad + guide, not flat `Features/.../Services`
- Hello connectivity lives only in ApiHealth; Raindrop does not expose
  `GetHelloWorldAsync`
- Deferred still deferred: NsDepCop, validation/OTEL pipeline behaviors

## Related

- `docs/modular-monolith-module-guide-2026-08-03.md`
- `docs/plans/2026-06-06-001-feat-modular-monolith-first-module-prd.md`
- `docs/solutions/architecture-patterns/health-check-service-strategy-module-architecture.md`
