---
title: feat/modular-monolith-first-module
date: 2026-06-06
status: implemented
---

## Problem

Our codebase uses a flat project structure with no enforced module
boundaries between features. As features grow, so does the risk of
accidental coupling. We want to evolve toward a modular monolith
style inspired by Ardalis's RiverBooks — but we need to learn how
to implement these patterns with AI-Augmented Development before
applying them at scale. The modular monolith patterns themselves
are well-documented; the new skill is executing them through an
AI pairing workflow.

The API Example page is the simplest feature in the repo. It makes one
HTTP call and displays the response.

## Solution

Convert the API Example page into the first module: **ApiHealth**.
The name reflects the domain (health check), not the mechanism (calls
an API). Three projects per module with Mediator.SourceGen, pipeline
behaviors, and a service abstraction for mock/real switching.
Extend incrementally with OpenTelemetry and validation behaviors as
next steps. Document findings for future module conversions.

## Success Metrics

- `dotnet build` succeeds with zero warnings.
- All existing tests pass unchanged.
- `/api-health` route renders and displays response in both real
  (backend API) and local (mock) modes.
- Module pattern proven: future modules replicate the same three-project
  structure and registration pattern without modification.

## Key Technical Decisions

- **Mediator.SourceGen for CQRS.** Pipeline behaviors for cross-cutting
  concerns (logging now, validation + OpenTelemetry next). Handler
  receives only its domain dependency.
- **Per-module service abstraction for mock switching.** Handler
  injects `IHealthCheckService` — an interface in the Contracts
  project. Two implementations (`HealthCheckService` real HTTP,
  `SyntheticHealthCheckService` for local dev). Program.cs chooses at
  startup via `builder.HostEnvironment.BaseAddress`.
- **Three projects per module** (Module, Module.Contracts, Module.Tests)
  grouped under `src/redmuffin.Blazor.StaticWeb.Modules/`.
- **Internal-by-default.** Module internals are `internal`; only Contracts
  types are `public`. `InternalsVisibleTo` grants access to the module's
  own test project only.

## Modules & Seams

| Module              | Path                                                                                                                                          | Change                                                                                                                                                                   | Test surface                                                                              |
| ------------------- | --------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------- |
| Common              | `src/redmuffin.Blazor.StaticWeb.Common/`                                                                                                      | Add `PipelineBehaviors/LoggingBehavior<>`. Add `MediatorServiceExtensions` at root level (DI entry point).                                                               | `Logger_Spy<T>` verifies `LogInformation` called before and after handler invocation      |
| ApiHealth.Contracts | `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth.Contracts/`                                                                                 | New project: `GetHelloQuery`, `HelloResponse`, `IHealthCheckService`                                                                                                     | — (contracts have no logic)                                                               |
| ApiHealth           | `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth/`                                                                                           | New project: `GetHelloHandler`, `HealthCheckService`, `SyntheticHealthCheckService`, `ApiHealthModuleServicesExtensions`                                                 | Handler passes through, real HTTP error handling (5 cases), local returns expected string |
| ApiHealth.Tests     | `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth.Tests/`                                                                                     | New project: handler tests, service tests, behavior test                                                                                                                 | All pass                                                                                  |
| Web host            | `src/redmuffin.Blazor.StaticWeb/Program.cs`                                                                                                   | Add Mediator + pipeline + module registrations                                                                                                                           | `dotnet build` succeeds                                                                   |
| Web host — pages    | `src/redmuffin.Blazor.StaticWeb/Features/ApiHealth/ApiHealth.razor`<br>`src/redmuffin.Blazor.StaticWeb/Features/ApiHealth/ApiHealth.razor.cs` | Renamed from `ApiExamplePage/CallApiExample` — folder, both files, class, route (`/api-health`), namespace. Page uses `IMediator.Send()` instead of direct service call. | Page load test                                                                            |
| Web host — tests    | `tests/redmuffin.Blazor.StaticWeb.Tests/Features/ApiHealth/ApiHealthTests*.cs`                                                                | Renamed from `CallApiExampleTests.*` — class names, namespace, folder. Tests adapted to new page structure.                                                              | Mock `IMediator`, assert page renders and response displays correctly                     |

## Testing Strategy

- **Handler test:** mock `IHealthCheckService`, assert result matches.
- **Real service test:** `ControlledHttpHandler_Fake` per `rm-testing`.
  Covers 4 distinct code paths: connection failure (handler throws),
  non-2xx responses (parameterized via `[Arguments]` for multiple
  status codes), cancellation, and timeout.
- **Mock service test:** instantiate `SyntheticHealthCheckService`, assert
  exact response string.
- **Behavior test:** `Logger_Spy<T>` per `rm-testing`. Once, applies
  to all handlers.
- **Existing tests:** host-level page tests (`ApiHealthTests.*`) updated to match new names and structure. Full suite passes at the end.

## Non-Functional Requirements

- **Compile-time:** Mediator.SourceGen generates handler dispatch at
  compile time — zero reflection, no runtime startup penalty.
- **Encapsulation:** Module internals are `internal`. Only Contracts
  types are `public`. No cross-module coupling beyond Contracts.
- **Testability:** Every seam (handler, service, behavior) is
  injectable. No static calls, no hidden dependencies.

## Out of Scope

- Rewriting any existing feature besides the API Example page.
- NsDepCop (evaluate after pattern is proven).
- Database or storage decisions.

## Assumptions

- The unnamed `HttpClient` registration in `Program.cs` is sufficient
  for `HealthCheckService` (no named clients needed).
- Mediator.SourceGen auto-discovers `IRequestHandler<>` across all
  referenced projects without manual assembly scanning.
- `builder.HostEnvironment.BaseAddress` on `localhost:5233` uses the
  same port as the existing mock-data URL check, so the conditional
  registration works identically.

## Acceptance Criteria

- [x] `dotnet build` succeeds with zero warnings.
- [x] All existing tests pass.
- [x] `ApiHealthTests.*` pass with mocked `IMediator`.
- [x] `/api-health` route renders and displays response in both real
      and local modes.
- [x] `GetHelloHandler` returns `HelloResponse` when
      `IHealthCheckService` returns data.
- [x] `LoggingBehavior<TRequest, TResponse>` logs before and after
      handler invocation.
