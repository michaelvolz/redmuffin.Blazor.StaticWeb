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
tags:
  - mediator-sourcegen
  - cqrs
  - strategy-pattern
  - modular-monolith
  - blazor-wasm
  - service-abstraction
  - result
---

# Health check service strategy and per-module CQRS architecture

> **Satellite learning.** Procedure to add modules lives in
> `docs/modular-monolith-module-guide-2026-08-03.md`. Decision record:
> `docs/adr/0013-riverbooks-modular-layout-and-result.md`.

## Context

Converting a Blazor WASM page to a bounded module produced recurring
decisions: synthetic data, per-module projects, and where cross-cutting
logging belongs. ApiHealth is the first module and the pattern testbed.

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
builder.Services.AddApiHealthModule(useSynthetic);
```

### Result for expected failures

- Success and expected API failures return `Result<T>` from Common
- Cancellation rethrows `OperationCanceledException` / `TaskCanceledException`
- Handler maps `Result<string>` → `Result<HelloResponse>`
- Page uses `Match` into immutable ViewModel states

### Mediator.SourceGen

- Handlers: `IRequestHandler<TRequest, TResponse>` (public for discovery)
- Pipeline behaviors in Common (`LoggingBehavior`)
- Services stay internal

### Per-module project structure

```text
src/redmuffin.Blazor.StaticWeb.Modules/
├── ApiHealth.Contracts/
├── ApiHealth/
└── ApiHealth.Tests/
```

- Contracts types are public
- Service implementations are internal; `InternalsVisibleTo` → module tests only
- Extension: `AddApiHealthModule(bool useSyntheticData)`

### Infrastructure-agnostic error messages

Describe failures generically (no localhost, ports, or product environment names).

HTTP service paths covered by ApiHealth:

1. Success with body
2. Connection failure (`HttpRequestException` → `Result.Failure`)
3. Non-2xx → `Result.Failure`
4. Empty body → `Result.Failure`
5. Cancellation / timeout → still throw (logged Warning)

## Consequences

Later modules copy Strategy + `Result` + module-owned registration from
ApiHealth (see the module guide), not flat `Features/.../Services` alone.

## Related

- `docs/modular-monolith-module-guide-2026-08-03.md`
- `docs/adr/0013-riverbooks-modular-layout-and-result.md`
- `docs/plans/2026-06-06-001-feat-modular-monolith-first-module-prd.md`
- `docs/solutions/architecture-patterns/hello-world-mock-environment-service-resolution.md`
