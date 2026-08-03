---
title: Health check service strategy and per-module CQRS architecture
date: 2026-06-06
last_updated: 2026-08-03
category: architecture-patterns
module: api-health-module
problem_type: architecture_pattern
component: service_object
severity: medium
canonical_procedure: docs/modular-monolith-module-guide-2026-08-03.md
applies_when:
  - Creating a new module with real/synthetic service switching
  - Converting a page from direct service injection to Mediator.CQRS
  - Designing module boundaries with Contracts isolation
  - Adopting Result for expected API failures at module boundaries
  - Removing a dual-owned connectivity surface once a module owns it
tags:
  - mediator-sourcegen
  - cqrs
  - strategy-pattern
  - modular-monolith
  - blazor-wasm
  - service-abstraction
  - result
  - module-template
---

# Health check service strategy and per-module CQRS architecture

> **Satellite learning.** Procedure to add modules lives in
> `docs/modular-monolith-module-guide-2026-08-03.md`. Decision record:
> `docs/adr/0013-riverbooks-modular-layout-and-result.md`.

## Context

Converting a Blazor WASM page into the first bounded module produced recurring
decisions: synthetic vs real data, per-module projects, where cross-cutting
logging belongs, and how expected API failures reach the UI. AzureHealthCheck
(page: `Pages/ApiHealth`) is the pattern testbed.

A later hardening pass closed the gaps that blocked copying the template:
exception-shaped control flow for normal unreachable-API outcomes, synthetic
policy that did not match Raindrop’s pure-client host rule, UI “health” rows
that were not measured, and a second Hello connectivity path on Raindrop.

## Guidance

### Synthetic data is a first-class feature

Use Strategy: interface in Contracts, two module implementations. Do not use
`DelegatingHandler` for mock data.

```csharp
// Contracts
public interface IHealthCheckService
{
    Task<Result<string>> GetHelloAsync(CancellationToken ct = default);
}

// Module — internal implementations
internal sealed class HealthCheckService(...) : IHealthCheckService { /* real HTTP → Result */ }
internal sealed class SyntheticHealthCheckService() : IHealthCheckService { /* synthetic → Result */ }
```

Registration lives in the module extension; host passes policy only:

```csharp
var useSynthetic = builder.HostEnvironment.BaseAddress.Contains(
    "localhost:5233",
    StringComparison.OrdinalIgnoreCase);
// Eager/tests:
// builder.Services.AddAzureHealthCheckModule(useSynthetic);
// WASM host: lazy AzureHealthCheck.dll + AzureHealthCheckModuleGate +
// reflection CreateHealthCheckService (no Add* on cold Program path).
```

Synthetic is only the pure client host `localhost:5233` (same as Raindrop).
SWA local (`localhost:4280`) and production use the real HTTP implementation —
not every `localhost`.

### Result for expected failures

- Success and expected API failures return `Result<T>` from Common
  (`Result.Success` / `Result.Failure` non-generic factories; `Match` at boundaries)
- Cancellation rethrows `OperationCanceledException` / `TaskCanceledException`
- Handler maps `Result<string>` → `Result<HelloResponse>`
- Page uses `Match` into immutable ViewModel states; it does not `try/catch`
  expected API failures
- Failure strings stay infrastructure-agnostic (no hostnames, ports, or product
  environment names)

HTTP service paths covered by AzureHealthCheck:

1. Success with body → `Result.Success`
2. Connection failure (`HttpRequestException`) → `Result.Failure`
3. Non-2xx → `Result.Failure`
4. Empty body → `Result.Failure`
5. Cancellation / timeout → still throw (logged Warning)

### Mediator.SourceGen

- Handlers: `IRequestHandler<TRequest, TResponse>` (public for discovery;
  MA0182 rejects unused internal handlers)
- Pipeline behaviors in Common (`LoggingBehavior`)
- Services stay internal; `InternalsVisibleTo` only for the module’s tests

### Per-module project structure

```text
src/redmuffin.Blazor.StaticWeb.Modules/
├── AzureHealthCheck.Contracts/
└── AzureHealthCheck/

tests/redmuffin.Blazor.StaticWeb.Modules/
└── AzureHealthCheck.Tests/

src/redmuffin.Blazor.StaticWeb.Pages/
└── ApiHealth/   # route page only (ApiHealth.Page.dll)
```

- Contracts types are public
- Service implementations are internal
- Extension: `AddAzureHealthCheckModule(bool useSyntheticData)` (eager/tests)
- Test projects live under `tests/` only — never under `src/`; one test
  project per module, never folded into host or Api tests
- Architecture gate: Contracts map to Shared and may reference Common
  (`Shared: [Shared]` in `quality-gates/architecture-rules.yml`) so
  Contracts can take `Result<T>` without a Frontend edge

### Measured page checks only

Host page health rows assert data the response actually supplies:

- **Message Valid** — non-empty message preview
- **Latency** — elapsed time against a threshold

Do not invent SSL, “endpoint reachable”, or status rows the module does not
measure.

### Sole owner for Hello connectivity

Feature-level connectivity to `/api/HelloWorld` lives in ApiHealth
(`IHealthCheckService` / `GetHelloQuery`). Raindrop does not expose
`GetHelloWorldAsync`; articles and videos remain its surface. Keep the Azure
Functions `HelloWorld` endpoint for ApiHealth. App startup may still warm the
endpoint via `WarmupService` (best-effort probe, not a module Result surface).

## Consequences

Later modules copy Strategy + `Result` + module-owned registration from
ApiHealth (see the module guide), not flat `Features/.../Services` alone.
Do not re-add Hello connectivity on Raindrop.

Deferred outside this pattern: NsDepCop, expanded OTEL/validation pipeline
behaviors, and splitting timeout vs cancel UX messaging.

## Related

- `docs/modular-monolith-module-guide-2026-08-03.md`
- `docs/adr/0013-riverbooks-modular-layout-and-result.md`
- `docs/plans/2026-06-06-001-feat-modular-monolith-first-module-prd.md`
- `docs/solutions/architecture-patterns/hello-world-mock-environment-service-resolution.md`
- `docs/solutions/best-practices/naming-deep-modules-and-service-variants.md`
- `docs/solutions/features/dummy-raindrop-data-locally.md`
