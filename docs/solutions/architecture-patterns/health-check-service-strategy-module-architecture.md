---
title: Health check service strategy and per-module CQRS architecture
date: 2026-06-06
category: architecture-patterns
module: api-health-module
problem_type: architecture_pattern
component: service_object
severity: medium
applies_when:
  - Creating a new module with real/mock service switching
  - Converting a page from direct service injection to Mediator.CQRS
  - Designing module boundaries with Contracts isolation
tags:
  - mediator-sourcegen
  - cqrs
  - strategy-pattern
  - modular-monolith
  - blazor-wasm
  - service-abstraction
---

# Health check service strategy and per-module CQRS architecture

## Context

Converting a Blazor WASM page to a bounded module revealed several
recurring architectural decisions: how to handle mock data, how to
organize per-module projects, and where cross-cutting concerns (logging,
validation, telemetry) belong. The first module (ApiHealth) serves as the
pattern testbed for future conversions.

## Guidance

### Mock data is a first-class feature, not middleware

Use the Strategy pattern: define an interface in the Contracts project
with two implementations swapped at the composition root based on
environment. Do not use `DelegatingHandler` interception for mock data —
that couples the handler to HTTP infrastructure and obscures the intent.

```
// Contracts project — public, cross-module boundary
public interface IHealthCheckService
{
    Task<Result<HelloResponse>> GetHelloAsync(CancellationToken ct);
}

// Module project — internal implementations
internal sealed class HealthCheckService(IHttpClientFactory clientFactory)
    : IHealthCheckService { /* real HTTP */ }

internal sealed class SyntheticHealthCheckService()
    : IHealthCheckService { /* hardcoded mock */ }
```

Registration in `Program.cs` evaluates the host environment once at
startup, not per-request:

```csharp
if (builder.HostEnvironment.BaseAddress.Contains("localhost:5233"))
    services.AddSingleton<IHealthCheckService, SyntheticHealthCheckService>();
else
    services.AddSingleton<IHealthCheckService, HealthCheckService>();
```

### Mediator.SourceGen over MediatR

Use `Mediator.SourceGen` for CQRS — zero-reflection, AoT-safe source
generation. Pipeline behaviors handle cross-cutting concerns without
polluting handler constructors.

- Handlers implement `IRequestHandler<TRequest, TResponse>` with a
  primary constructor. Static handlers are not discoverable.
- Pipeline behaviors implement `IPipelineBehavior<TRequest, TResponse>`
  and are registered as scoped services.
- Logging behaviors use `[LoggerMessage]` source-gen in a separate
  `*.Logging.cs` partial file. Never `LogInformation` or
  `LoggerMessage.Define` directly.

### Per-module project structure

Three projects per module, grouped under a shared parent directory:

```
src/redmuffin.Blazor.StaticWeb.Modules/
├── ApiHealth.Contracts/    # public types: queries, responses, interfaces
├── ApiHealth/              # internal implementations: handlers, services
└── ApiHealth.Tests/        # all module tests
```

- Module types are `internal`. Contracts types are `public`.
- `InternalsVisibleTo` in `AssemblyInfo.cs` grants access to the
  module's own test project only.
- Module registration uses a per-module extension method
  (e.g., `ApiHealthModuleServicesExtensions.AddApiHealthModuleServices()`)
  wired in `Program.cs` at startup.

### Infrastructure-agnostic error messages

Error messages from service implementations must not reference local
dev infrastructure (hostnames, ports, "SWA", "dotnet watch") or
production-specific paths. Describe the failure generically:

```
// Bad — references local infrastructure
"Connection failed to localhost:5233"

// Good — infrastructure-agnostic
"The API endpoint did not return a response"
```

Handle these five failure paths in HTTP-based services:

1. No response (`StatusCode` is null)
2. HTTP error codes (4xx, 5xx)
3. Timeout (`TaskCanceledException`)
4. Operation canceled (`OperationCanceledException`)
5. Empty response body

## Why This Matters

These patterns emerged from applying RiverBooks modular monolith
principles to a Blazor WASM + Azure Functions stack. Getting them right
on the first module avoids retrofitting later modules.

- Strategy pattern for mock data survives production use — it is not a
  temporary hack. The mock implementation is a real, tested component.
- Mediator.SourceGen with pipeline behaviors keeps handlers focused on
  domain logic. Adding logging, validation, or telemetry later requires
  a new behavior class, not a handler change.
- Infrastructure-agnostic error messages mean test assertions are
  independent of the deployment environment.

## When to Apply

- When adding a new page that calls an external API
- When converting an existing page to the modular pattern
- When designing service abstractions for mock/real switching
- Deferred: FluentValidation pipeline behavior, OpenTelemetry behavior,
  NsDepCop boundary enforcement

## Related

- `docs/solutions/architecture-patterns/hello-world-mock-environment-service-resolution.md` — earlier factory-pattern approach (moderate overlap; same problem area, different implementation)
- `docs/plans/2026-06-06-001-feat-modular-monolith-first-module-prd.md` — first module PRD
- `docs/solutions/conventions/blazor-wasm-folder-structure-conventions.md` — project layout conventions
