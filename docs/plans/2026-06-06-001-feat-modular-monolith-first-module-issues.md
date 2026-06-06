---
title: feat/modular-monolith-first-module
prd: 2026-06-06-001-feat-modular-monolith-first-module-prd.md
date: 2026-06-06
status: draft
---

### U1. Pipeline infrastructure in Common

- **Status:** pending
- **Type:** AFK
- **Blocked by:** none
- **What to build:** Add `Mediator.Abstractions` package to Common.
  Create `LoggingBehavior<,>` implementing `IPipelineBehavior<,>`
  with `[LoggerMessage]` source-gen logging in a separate
  `*.Logging.cs` partial file (per rm-logging convention). Create
  `MediatorServiceExtensions` registering the behavior as scoped.
  The behavior is `public` — Common is shared infrastructure, not
  a module. Blazor host packages are handled in U3.
- **Files:**
  - `Directory.Packages.props` — add `<PackageVersion Include="Mediator.Abstractions" Version="3.0.2" />`
  - `src/redmuffin.Blazor.StaticWeb.Common/redmuffin.Blazor.StaticWeb.Common.csproj` — add `Mediator.Abstractions`
  - `src/redmuffin.Blazor.StaticWeb.Common/Pipeline/LoggingBehavior.cs`
  - `src/redmuffin.Blazor.StaticWeb.Common/Pipeline/LoggingBehavior.Logging.cs`
  - `src/redmuffin.Blazor.StaticWeb.Common/Pipeline/MediatorServiceExtensions.cs`
- **Acceptance criteria:**
  - [ ] `dotnet build` on `redmuffin.Blazor.StaticWeb.Common` succeeds.
  - [ ] `MediatorServiceExtensions.AddPipelineBehaviors()` registers `LoggingBehavior<,>` as `IPipelineBehavior<,>` with scoped lifetime.
  - [ ] `LoggingBehavior<,>` implements `IPipelineBehavior<,>` and is `public`.
  - [ ] `[LoggerMessage]` declarations live in a separate `LoggingBehavior.Logging.cs` partial file.
  - [ ] Three `[LoggerMessage]` methods exist: entry (Information), success with elapsed ms (Information), failure with elapsed ms + exception (Error). They use `ILogger` (non-generic).

### U2. ApiHealth module — handler, services, and module tests

- **Status:** pending
- **Type:** AFK
- **Blocked by:** U1 (LoggingBehavior must exist before module tests can reference it)
- **What to build:** Create three projects under
  `src/redmuffin.Blazor.StaticWeb.Modules/`: ApiHealth.Contracts,
  ApiHealth, and ApiHealth.Tests. Contracts define `GetHelloQuery`,
  `HelloResponse`, and `IHealthCheckService`. ApiHealth project
  implements `GetHelloHandler`, `HealthCheckService` (real HTTP,
  5 error paths + success), `DummyHealthCheckService` (mock data),
  and `ApiHealthModuleServicesExtensions`. ApiHealth.Tests covers
  the handler pass-through, real HTTP error handling, mock response,
  and LoggingBehavior (adds ProjectReference to Common). All module
  internals are `internal`; only Contracts types are `public`.
- **Files:**
  - `Directory.Packages.props` — add `<PackageVersion Include="Ardalis.Result" Version="10.1.0" />`
  - `redmuffin.Blazor.StaticWeb.slnx` — add 3 new `<Project Path="...">` entries for ApiHealth.Contracts, ApiHealth, and ApiHealth.Tests
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth.Contracts/ApiHealth.Contracts.csproj` — package refs: `Mediator.Abstractions`, `Ardalis.Result`
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth.Contracts/GetHelloQuery.cs`
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth.Contracts/HelloResponse.cs`
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth.Contracts/IHealthCheckService.cs`
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth/ApiHealth.csproj` — package refs: `Ardalis.Result`, `Microsoft.Extensions.Http` (for `IHttpClientFactory`); project ref: `ApiHealth.Contracts`
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth/GetHelloHandler.cs`
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth/HealthCheckService.cs`
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth/DummyHealthCheckService.cs`
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth/ApiHealthModuleServicesExtensions.cs`
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth/AssemblyInfo.cs`
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth.Tests/ApiHealth.Tests.csproj` — package refs: `TUnit`, `Microsoft.Testing.Platform`; project refs: `ApiHealth`, `Common` (for LoggingBehaviorTests)
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth.Tests/GetHelloHandlerTests.cs`
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth.Tests/GetHelloHandlerTests.Helpers.cs`
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth.Tests/HealthCheckServiceTests.cs`
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth.Tests/DummyHealthCheckServiceTests.cs`
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth.Tests/LoggingBehaviorTests.cs`
- **Acceptance criteria:**
  - [ ] `dotnet build` on each project succeeds.
  - [ ] `GetHelloHandler` injects `IHealthCheckService`. Mock returns `Result.Success(new HelloResponse("test"))` — handler returns same value.
  - [ ] `HealthCheckService` handles all 5 HTTP error paths (connection failure, 404, 503, timeout, cancellation) plus success path — 6 test cases pass.
  - [ ] `HealthCheckService` error messages contain no hostnames, ports, or local-dev infrastructure references.
  - [ ] `DummyHealthCheckService` returns `"Hello World from Mock Data - Not from Azure Functions"` — test asserts exact string.
  - [ ] `LoggingBehaviorTests` creates behavior directly (ProjectReference to Common), sends request through, and asserts `Logger_Spy<T>` captured the request type name.
  - [ ] Module project types are `internal`. Contracts project types (`GetHelloQuery`, `HelloResponse`, `IHealthCheckService`) are `public`.
  - [ ] `AssemblyInfo.cs` grants `InternalsVisibleTo("ApiHealth.Tests")`.
  - [ ] `ApiHealthModuleServicesExtensions.AddApiHealthModuleServices()` is an empty method ready for future registrations.
  - [ ] `dotnet test` on `ApiHealth.Tests` passes all tests.

### U3. Integration — wire into Blazor host, rename page and tests

- **Status:** pending
- **Type:** AFK
- **Blocked by:** U1 (needs Mediator infrastructure + pipeline registration),
  U2 (needs ApiHealth project references and test project)
- **What to build:** Add Mediator host packages and project references
  to the Blazor host. Wire Mediator, pipeline, and module registrations
  in Program.cs. Rename the existing `ApiExamplePage/CallApiExample`
  page to `ApiHealth` with route `/api-health`, update the component
  class name and namespace. Update the page to inject `IMediator`
  and call `Mediator.Send(new GetHelloQuery())` instead of
  `IRaindropAPI`. Rename and update the host-level tests from
  `CallApiExampleTests.*` to `ApiHealthTests.*` — mock `IMediator`,
  update namespaces and class names.
- **Files:**
  - `src/redmuffin.Blazor.StaticWeb/redmuffin.Blazor.StaticWeb.csproj` — add `Mediator.SourceGenerator`, `Ardalis.Result` packages; add ProjectReference to `ApiHealth` and `ApiHealth.Contracts`
  - `Directory.Packages.props` — add `<PackageVersion Include="Mediator.SourceGenerator" Version="3.0.2" />` (Ardalis.Result version already added in U2)
  - `src/redmuffin.Blazor.StaticWeb/Program.cs` — add `AddMediator()`, `AddPipelineBehaviors()`, `AddApiHealthModuleServices()`
  - `src/redmuffin.Blazor.StaticWeb/Features/ApiExamplePage/` — rename to `src/redmuffin.Blazor.StaticWeb/Features/ApiHealth/`
  - `src/redmuffin.Blazor.StaticWeb/Features/ApiHealth/CallApiExample.razor` — rename to `ApiHealth.razor`; update `@page "/api-health"`, `@code` class name to `ApiHealth`, namespace to `redmuffin.Blazor.StaticWeb.Features.ApiHealth`
  - `src/redmuffin.Blazor.StaticWeb/Features/ApiHealth/CallApiExample.razor.cs` — rename to `ApiHealth.razor.cs`; replace `IRaindropAPI` injection with `required IMediator`; replace body with `Mediator.Send(new GetHelloQuery())`; remove `OnInitialized` override and both catch blocks; update namespace and class name
  - `tests/redmuffin.Blazor.StaticWeb.Tests/Features/ApiExamplePage/` — rename to `tests/redmuffin.Blazor.StaticWeb.Tests/Features/ApiHealth/`
  - `tests/redmuffin.Blazor.StaticWeb.Tests/Features/ApiHealth/CallApiExampleTests.cs` — rename to `ApiHealthTests.cs`; mock `IMediator`; update namespace and class name
  - `tests/redmuffin.Blazor.StaticWeb.Tests/Features/ApiHealth/CallApiExampleTests.Behavior.cs` — rename to `ApiHealthTests.Behavior.cs`; update namespace and class name
  - `tests/redmuffin.Blazor.StaticWeb.Tests/Features/ApiHealth/CallApiExampleTests.EdgeCases.cs` — rename to `ApiHealthTests.EdgeCases.cs`; update namespace and class name
  - `tests/redmuffin.Blazor.StaticWeb.Tests/Features/ApiHealth/CallApiExampleTests.Helpers.cs` — rename to `ApiHealthTests.Helpers.cs`; update namespace and class name
  - `tests/redmuffin.Blazor.StaticWeb.Tests/Features/ApiHealth/CallApiExampleTests.Infrastructure.cs` — rename to `ApiHealthTests.Infrastructure.cs`; update namespace and class name
- **Acceptance criteria:**
  - [ ] `dotnet build` succeeds with zero errors and zero new warnings.
  - [ ] `dotnet clean && dotnet build && dotnet test` passes with zero failures across the entire suite.
  - [ ] `Program.cs` registers Mediator (scoped, namespace `redmuffin.Blazor.StaticWeb.Mediator`), pipeline behaviors via `AddPipelineBehaviors()`, and ApiHealth module via `AddApiHealthModuleServices()`.
  - [ ] `ApiHealth.razor` has `@page "/api-health"` and component class `ApiHealth` (partial).
  - [ ] `ApiHealth.razor.cs` injects `private required IMediator Mediator` and calls `Mediator.Send(new GetHelloQuery())` in `CallApiAsync`. No reference to `IRaindropAPI` exists in this file.
  - [ ] Both `.razor` and `.razor.cs` use namespace `redmuffin.Blazor.StaticWeb.Features.ApiHealth`.
  - [ ] All 5 host-level test files use namespace `redmuffin.Blazor.StaticWeb.Tests.Features.ApiHealth` and class `ApiHealthTests` (partial).
  - [ ] Grep for `CallApiExample` or `ApiExamplePage` in `src/` and `tests/` directories returns zero hits.

### Deferred

- **FluentValidation + test properties.** Add a validation behavior to
  the pipeline once a query with real parameters exists. Then add test
  properties to verify validation rules.
- **OpenTelemetry behavior.** Add a telemetry pipeline behavior that
  captures handler duration and outcome.
- **NsDepCop evaluation.** Assess compile-time module boundary
  enforcement after the pattern is proven on more than one module.
- **Remove `GetHelloWorldAsync` from `IRaindropAPI`.** Once the API
  Health page is the only consumer and no page calls it, clean up the
  method and its implementations.
- **Remaining page conversions.** Convert Videos, Articles, and other
  features to the modular pattern, one page at a time.
