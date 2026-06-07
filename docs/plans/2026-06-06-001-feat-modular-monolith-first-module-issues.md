---
title: feat/modular-monolith-first-module
prd: docs/plans/2026-06-06-001-feat-modular-monolith-first-module-prd.md
date: 2026-06-07
status: active
---

# Issues — Modular Monolith First Module

**PRD constraints to satisfy:**

- Success Metrics: zero-warning build, all tests pass, `/api-health` renders in both modes, pattern is reusable.
- NFRs: compile-time (Mediator source-gen, no reflection), encapsulation (internal-by-default, no cross-module coupling), testability (every seam injectable).
- AC: build passes, all tests pass, handler returns response, behavior logs before/after.

---

### U1. Common pipeline behaviors and Mediator registration

- **Status:** done
- **Type:** AFK
- **Blocked by:** none
- **What to build:** Add `LoggingBehavior<TRequest, TResponse>` (implements
  `IPipelineBehavior`) and `MediatorServiceExtensions` (registers Mediator
  source-gen + behaviors via `IServiceCollection`) in the existing Common project.
  This is foundational infrastructure that all modules consume.
- **Files:**
  - `src/redmuffin.Blazor.StaticWeb.Common/PipelineBehaviors/LoggingBehavior.cs` (new)
  - `src/redmuffin.Blazor.StaticWeb.Common/MediatorServiceExtensions.cs` (new)
- **Acceptance criteria:**
  - [x] `LoggingBehavior` implements `IPipelineBehavior<TRequest, TResponse>`.
  - [x] `LoggingBehavior` logs `LogInformation` before and after calling `next()`.
  - [x] `MediatorServiceExtensions` adds Mediator source-gen services with
        the pipeline behavior registered.
  - [x] `dotnet build --project src/redmuffin.Blazor.StaticWeb.Common` succeeds
        with zero warnings.

---

### U2. ApiHealth Contracts, Core, and Unit Tests

- **Status:** done
- **Type:** AFK
- **Blocked by:** U1
- **What to build:** Create three new projects under
  `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth/`: the Contracts project
  (query, response, service interface), the ApiHealth project (handler, real
  HTTP service, synthetic service, module registration), and the ApiHealth.Tests
  project (handler test, service tests, synthetic service test, behavior test
  for LoggingBehavior). The handler receives `IHealthCheckService` and returns
  `HelloResponse`. `HealthCheckService` makes HTTP calls;
  `SyntheticHealthCheckService` returns generated data.
- **Files:**
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth.Contracts/` (new project)
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth.Contracts/GetHelloQuery.cs`
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth.Contracts/HelloResponse.cs`
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth.Contracts/IHealthCheckService.cs`
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth/` (new project)
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth/GetHelloHandler.cs`
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth/HealthCheckService.cs`
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth/SyntheticHealthCheckService.cs`
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth/ApiHealthModuleServicesExtensions.cs`
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth.Tests/` (new project)
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth.Tests/GetHelloHandlerTests.cs`
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth.Tests/HealthCheckServiceTests.cs`
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth.Tests/SyntheticHealthCheckServiceTests.cs`
  - `src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth.Tests/LoggingBehaviorTests.cs`
  - NuGet version entries in `Directory.Packages.props` for Mediator.SourceGen
  - Solution registration for all three new projects in `.slnx`
- **Acceptance criteria:**
  - [x] `# Packages.props` lists `Mediator.SourceGen` with version (use
        latest stable).
  - [x] All three projects registered in `.slnx`.
  - [x] `ApiHealthModuleServicesExtensions` registers `HealthCheckService`
        and `SyntheticHealthCheckService`. `Program.cs` wires
        `IHealthCheckService` to the correct implementation based on
        `BaseAddress`.
  - [x] `GetHelloHandler` returns `HelloResponse` when `IHealthCheckService`
        returns data (handler test passes).
  - [x] `HealthCheckService` handles 4 distinct code paths: connection
        failure, non-2xx responses (parameterized via `[Arguments]`),
        cancellation, timeout (controlled via `ControlledHttpHandler_Fake`).
  - [x] `SyntheticHealthCheckService` returns expected string constant.
  - [x] `LoggingBehaviorTests` verifies `LogInformation` called before and
        after handler invocation (using `Logger_Spy<T>`).
  - [x] `dotnet build --project src/redmuffin.Blazor.StaticWeb.Modules/ApiHealth.Tests`
        succeeds with zero warnings.
  - [x] All unit tests in ApiHealth.Tests pass.

---

### U3. Web host integration and ApiHealth page

- **Status:** done
- **Type:** AFK
- **Blocked by:** U2
- **What to build:** Wire Mediator, pipeline behaviors, and the ApiHealth module
  into the Blazor host. Register everything in `Program.cs`. Rename the existing
  `ApiExamplePage`/`CallApiExample` to `ApiHealth` (folder, files, class, route
  `/api-health`, namespace). The page uses `IMediator.Send()` with `GetHelloQuery`
  instead of calling `IHealthCheckService` directly. Update host-level page tests
  to match new names and structure. This is the demoable slice — loading
  `/api-health` displays the response.
- **Files:**
  - `src/redmuffin.Blazor.StaticWeb/Program.cs` (modify)
  - `src/redmuffin.Blazor.StaticWeb/Features/ApiHealth/ApiHealth.razor` (rename)
  - `src/redmuffin.Blazor.StaticWeb/Features/ApiHealth/ApiHealth.razor.cs` (rename)
  - `src/redmuffin.Blazor.StaticWeb/Features/ApiExamplePage/` → delete (after renames confirmed)
  - `tests/redmuffin.Blazor.StaticWeb.Tests/Features/ApiHealth/ApiHealthTests*.cs` (rename and adapt)
- **Acceptance criteria:**
  - [x] `Program.cs` registers Mediator, pipeline behaviors, and
        `ApiHealthModuleServicesExtensions`.
  - [x] `ApiHealth.razor` injects `IMediator`, sends `GetHelloQuery`, displays
        response.
  - [x] Route `/api-health` resolves and renders the page.
  - [x] Old `/call-api-example` route no longer exists (redirects or 404).
  - [x] `ApiExamplePage` folder and `CallApiExample` class are removed.
  - [x] Host-level page tests pass with mocked `IMediator`.
  - [x] `dotnet build` succeeds with zero warnings (SM-1).
  - [x] All existing tests pass unchanged (SM-2, AC-2).

---

### Deferred

- NsDepCop evaluation (evaluate after module pattern is proven).
- OpenTelemetry or Validation pipeline behaviors.
