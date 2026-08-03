---
date: 2026-08-03
last_updated: 2026-08-03
tags:
  - modular-monolith
  - riverbooks
  - mediator
  - result
  - blazor-wasm
---

# How to add a RiverBooks-shaped module

Step-by-step procedure for adding the next bounded module after ApiHealth.
Structure only — not full DDD.

## What Belongs in This File

- **Viewpoint**: Implementers adding or converting a feature to a module
- **What belongs**: Project layout, registration, Result usage, tests,
  architecture-gate and solution wiring steps
- **What does NOT belong**: Domain design of a specific feature, OTEL/validation
  pipeline expansion, NsDepCop (deferred)

## Prerequisites

- Read ADR `docs/adr/0013-riverbooks-modular-layout-and-result.md`
- ApiHealth under `src/redmuffin.Blazor.StaticWeb.Modules/` is the reference
- Host already calls `AddMediator` and `AddModulePipelineBehaviors`

## Phase 1 — Projects

Create three projects under `src/redmuffin.Blazor.StaticWeb.Modules/`:

```text
{Name}.Contracts/   # public: queries, responses, interfaces
{Name}/             # handlers, services, Add{Name}Module
{Name}.Tests/       # unit tests; IsTestProject=true
```

1. Target `net9.0` (same as ApiHealth / Common) unless the host forces otherwise
2. Contracts references Common (for `Result<T>`) and Mediator.Abstractions
3. Module references Contracts + Common; package refs as needed (Http, DI)
4. Tests reference Module, Contracts, Common; copy test `.editorconfig` from an
   existing test project; set `<IsTestProject>true</IsTestProject>`
5. Register all three in `redmuffin.Blazor.StaticWeb.slnx`
6. Host references Module + Contracts
7. Map assemblies in `quality-gates/architecture-rules.yml`
   (Contracts → Shared, Module + Tests → Frontend)

## Phase 2 — Contracts and Result

1. Put queries/responses/interfaces in Contracts only
2. Service methods that can fail for expected reasons return `Task<Result<T>>`
3. Queries that surface those outcomes use `IRequest<Result<TResponse>>`
4. Use infrastructure-agnostic failure strings (no hostnames, ports, "SWA")
5. Cancellation stays exceptional — do not wrap `OperationCanceledException`
   in `Result`

Factory usage:

```csharp
return Result.Success(value);
return Result.Failure<string>("The API endpoint did not return a response.");
```

## Phase 3 — Module internals

1. Real + synthetic (or single) implementations of the Contracts interface
2. Mark service implementations `internal`
3. Handlers implement `IRequestHandler<,>` and stay `public` for Mediator.SourceGen
4. `InternalsVisibleTo` only for `{Name}.Tests`
5. `Add{Name}Module(this IServiceCollection services, bool useSyntheticData)`
   registers implementations and binds the interface — host must not resolve
   concrete service types
6. Logging: `[LoggerMessage]` in `*.Logging.cs` partials only

## Phase 4 — Host composition and page

1. Decide synthetic policy. Default match Raindrop/ApiHealth:

   ```csharp
   var useSynthetic = builder.HostEnvironment.BaseAddress.Contains(
       "localhost:5233",
       StringComparison.OrdinalIgnoreCase);
   builder.Services.Add{Name}Module(useSynthetic);
   ```

2. Page lives under `Features/{Name}/` in the host project
3. Page sends Mediator queries; map `Result` with `Match` to an immutable
   ViewModel
4. Do not inject module service interfaces into the page when a query exists

## Phase 5 — Tests

| Seam | Assert |
| --- | --- |
| Handler | Success and failure `Result` paths with fake service |
| Real service | Happy path + expected failures + cancel still throws |
| Synthetic | Exact success payload |
| Host page | Renders idle/success/failure via mocked `IMediator` |

Run:

```text
dotnet run --project src/.../Modules/{Name}.Tests
dotnet run --project tests/redmuffin.Blazor.StaticWeb.Tests
```

## Troubleshooting

| Symptom | Fix |
| --- | --- |
| MA0182 on internal handler | Keep handler public for source-gen discovery |
| CS0053 on test helpers | Properties exposing `internal` types must be `internal` |
| Shared→Shared architecture fail | Contracts may reference Common; Shared is allowed to depend on Shared |
| Relative HttpClient URI fails in tests | Set `HttpClient.BaseAddress` on fakes |

## File inventory (ApiHealth reference)

| Path | Role |
| --- | --- |
| `Modules/ApiHealth.Contracts/*` | Query, response, `IHealthCheckService` |
| `Modules/ApiHealth/*` | Handler, services, DI extension |
| `Modules/ApiHealth.Tests/*` | Module unit tests |
| `Common/Result.cs` | Shared `Result` / `Result<T>` |
| `Common/PipelineBehaviors/LoggingBehavior.cs` | Cross-module pipeline |
| `Features/ApiHealth/*` | Host page + ViewModel |

## Related

- `docs/adr/0013-riverbooks-modular-layout-and-result.md`
- `docs/plans/2026-06-06-001-feat-modular-monolith-first-module-prd.md`
- `docs/solutions/architecture-patterns/health-check-service-strategy-module-architecture.md`
