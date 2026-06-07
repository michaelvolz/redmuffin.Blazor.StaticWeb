---
title: feat/modular-monolith-first-module
prd: docs/plans/2026-06-06-001-feat-modular-monolith-first-module-prd.md
issues: docs/plans/2026-06-06-001-feat-modular-monolith-first-module-issues.md
date: 2026-06-07
status: complete
---

# Implementation Notes — Modular Monolith First Module

**PRD:** `docs/plans/2026-06-06-001-feat-modular-monolith-first-module-prd.md`
**Issues:** `docs/plans/2026-06-06-001-feat-modular-monolith-first-module-issues.md`

---

## Decisions Not In Plan

- **LoggerMessage attribute approach:** Used `[LoggerMessage]` source-gen
  attributes in `*.Logging.cs` partial files instead of ad hoc
  `LogInformation` calls, following the project's logging policy.
  Applied to both `LoggingBehavior` and `HealthCheckService`.
  - Tradeoff: Two files per class instead of one, but zero allocation
    on log paths that skip due to level filtering.

- **Synthetic naming (7 iterations).** The development-time service
  implementation went through 7 name candidates (Dummy→Local→Fallback→
  Sample→Demo→StandIn→Mimic→Synthetic) before settling on "Synthetic."
  Each failure mode: environment name, invented mechanism, generic
  qualifier, taxonomy misuse, behavioral claim. "Synthetic" won because
  it names the DATA characteristic (artificially generated data), not
  the environment or mechanism. Defined as a project-wide domain term
  in CONTEXT.md for all fake-data application implementations, distinct
  from test doubles in test projects.

- **PipelineBehaviors/ split.** The Common directory uses
  `PipelineBehaviors/` subdirectory for `IPipelineBehavior`
  implementations only, with `MediatorServiceExtensions` at `Common/`
  root as the DI entry point. Split avoids a compromised unified name
  for two different file types.

- **Environment-based IHealthCheckService switch in Program.cs, not
  the module.** `ApiHealthModuleServicesExtensions` registers both
  `HealthCheckService` and `SyntheticHealthCheckService`, but the choice
  of which implements `IHealthCheckService` is made at the composition
  root via `builder.HostEnvironment.BaseAddress.Contains("localhost")`.
  Module extensions register implementations; Program.cs wires the
  abstraction. Per ADR: "Mock switch evaluates BaseAddress at
  registration time, not NavigationManager at runtime."

---

## Key Discoveries

### U1 — Common Infrastructure

1. **IPipelineBehavior parameter order differs from README.** The Mediator
   library's `IPipelineBehavior<,>` interface expects parameter order
   `(TMessage, MessageHandlerDelegate, CancellationToken)`, while the v3.0.2
   README examples show `(TMessage, CancellationToken, MessageHandlerDelegate)`.
   The compiler enforces the correct order via the interface contract.

2. **Mediator.SourceGen auto-discovers handlers without assembly scanning.**
   Simply calling `builder.Services.AddMediator(options => ...)` is sufficient.
   The source generator scans the compilation at build time and finds all
   `IRequestHandler<,>` implementations in referenced projects. No manual
   assembly registration needed. PRD assumption confirmed.

### U2 — ApiHealth Module and Tests

3. **New test projects need `<IsTestProject>true</IsTestProject>` in .csproj.**
   Without this property, all analyzer packages (VS Threading, Meziantou,
   StyleCop, Roslynator, AsyncFixer) apply to the test project — flooding it
   with style/threading violations that don't belong in tests. The property
   excludes these analyzers per `Directory.Build.props`.

4. **New test projects must copy `.editorconfig` from an existing test
   project.** The editorconfig contains suppression patterns inherited across
   the solution (e.g., CA1707 underscore naming for test methods). Never create
   a new suppression file or modify analyzer severity — copy the existing
   editorconfig. CA1707 is suppressed because Uncle Bob's test naming convention
   uses underscores.

5. **Test naming convention: Uncle Bob plain English.** Renamed all tests
   from `Method_Scenario_Expected` to behavior-first plain English:
   `Returns_hello_message_when_service_is_available` instead of
   `Handle_ReturnsHelloResponse_WhenServiceReturnsData`. Documented in
   rm-testing and rm-naming skills. Convention: name the BEHAVIOR, not
   the method — renaming the method should never force renaming the test.

6. **HealthCheckService error response handling: non-2xx path is unified.**
   `HttpResponseMessage.EnsureSuccessStatusCode()` throws `HttpRequestException`
   for ANY non-2xx status code. 404, 500, 503 all exercise the identical code
   path. Writing N tests for N status codes is dead weight. Use TUnit's
   `[Arguments]` attribute to parameterize: one test method, multiple status
   code data rows.

7. **Timeout and cancellation conflated in production code.**
   `TaskCanceledException` (thrown by HttpClient on timeout) derives from
   `OperationCanceledException` — caught by the same `catch` block as explicit
   cancellation. Both log "Hello endpoint call was cancelled" at Warning level.
   The test faithfully tests what the code does. The logging conflates two
   different failure modes — a production code concern, not a test concern.

### U3 — Host Integration and Page

8. **Source-generated interfaces have hidden members.** `IMediator` from
   `Mediator.SourceGen` declares only 3 `Send`/`CreateStream`/`Publish`
   overloads in the source package, but the source generator adds
   `ICommand<TResponse>`, `IQuery<TResponse>`, `IStreamCommand`,
   `IStreamQuery`, and `INotification` overloads — 9 total. Writing a mock
   by reading the source code missed 6 members. Build first, let the compiler
   enumerate every missing member. Documented in rm-testing.

9. **HttpClient.BaseAddress is production-only.** Blazor host sets
   `BaseAddress` at startup via `builder.HostEnvironment.BaseAddress`. Test
   fakes bypass the host, so `IHttpClientFactory.CreateClient(string.Empty)`
   produces a client with null `BaseAddress`. Any relative URI throws
   `InvalidOperationException`. Every `HttpClientFactory_Fake.CreateClient()`
   must assign `client.BaseAddress = new Uri("http://localhost/")`.
   Documented in rm-testing.

10. **MA0049 false positive on Blazor component naming.** When a Razor
    component class name matches its folder/namespace (e.g., class `ApiHealth`
    in namespace `Features.ApiHealth`), MA0049 fires. This is a standard
    Blazor component pattern — suppress with pragma: `#pragma warning disable
MA0049 // Type name matches namespace — standard Blazor component pattern`.

11. **Deleting a Razor feature folder breaks `_Imports.razor`.** The file
    had `@using redmuffin.Blazor.StaticWeb.Features.ApiExamplePage` — after
    deleting the folder and creating `ApiHealth`, the import pointed at a
    deleted namespace. Must update `_Imports.razor` when renaming or removing
    feature folders.

### ConfigureAwaitFixer — Cross-Platform Fix

12. **`unzip` does not exist on Windows.** The `CopyAnalyzerDll` MSBuild
    target used 5 `<Exec>` commands running `unzip` and `cp` — shell utilities
    that only exist on Linux. On Windows, all 5 commands silently failed
    because `ContinueOnError="true"` suppressed every error. The fixer
    compiled and "ran" but loaded zero analyzers — it was fully non-operational
    on Windows since the initial install.

13. **`GeneratePathProperty` eliminates hardcoded versions.** NuGet
    `GeneratePathProperty="true"` on `PackageReference` produces
    `$(PkgMicrosoft_CodeAnalysis_NetAnalyzers)` pointing to the restored
    package path. No need to hardcode `10.0.300` or extract `.nupkg` files —
    the DLLs are already on disk in the NuGet cache.

14. **BuildHost contentFiles are auto-copied by NuGet.** The
    `Microsoft.CodeAnalysis.Workspaces.MSBuild` package includes BuildHost
    files as `contentFiles` with `CopyToOutputDirectory`. The `unzip`
    extraction was redundant — NuGet already copies them during restore.

15. **`cp ConfigureAwaitFixer&amp;` was dead code since day one.**
    `&amp;` decodes to `&` — the file `ConfigureAwaitFixer&` does not exist
    on any platform. This command always failed silently.

16. **Error guards required for `$(Pkg*)` properties.** If restore hasn't
    populated the property, MSBuild resolves the relative path as a warning
    (MSB3030), not an error. Added explicit `<Error Condition="..." />` to
    fail the build if the DLL source path doesn't exist. Never rely on
    MSBuild's default warning behavior for critical paths.

17. **Fix: pure MSBuild, zero shell, zero `ContinueOnError`.** Replaced all
    5 `<Exec>` commands (54 lines) with `<Copy>` and `<MakeDir>` MSBuild
    tasks (23 lines). Works identically on Windows and Linux. No OS-specific
    commands, no hardcoded version numbers, no silent failures.

---

## Changes to Plan

| Plan said                                       | What actually happened                                                                          | Why                                                                                                               |
| ----------------------------------------------- | ----------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| `LoggingBehavior` uses `ILogger.LogInformation` | Uses `[LoggerMessage]` static partial methods                                                   | CA1848 requires LoggerMessage delegates; project policy enforces `*.Logging.cs` partial file convention           |
| 5 error paths tested (404, 503, etc.)           | 4 test methods: connection, non-2xx (parameterized), cancel, timeout                            | 404 and 503 exercise identical code path (EnsureSuccessStatusCode); parameterized with `[Arguments]`              |
| `Program.cs` registers `IHealthCheckService`    | `ApiHealthModuleServicesExtensions` registers implementations; Program.cs wires the abstraction | ADR: mock switch evaluates BaseAddress at registration time. Module registers services; composition root chooses. |
| `IMediator` mock has 3 members                  | `IMediator` mock has 9 members                                                                  | Mediator.SourceGen adds hidden interface members via partial interface generation                                 |
| ConfigureAwaitFixer works on Windows            | Fully non-operational — `unzip` doesn't exist                                                   | Cross-platform fix deployed: pure MSBuild, zero shell                                                             |

---

## Pending Issues

None.

---

## Final Verification

- `dotnet build`: 13 projects, 0 errors, 0 warnings
- `ApiHealth.Tests`: 10 tests pass (8 test methods, 10 cases — 3 from `[Arguments]` parameterized rows)
- `redmuffin.Blazor.StaticWeb.Tests`: 339 tests pass, 0 regressions
- `/api-health` route: renders and returns synthetic response in local dev mode
- ConfigureAwaitFixer: builds cross-platform, deployed to local NuGet feed, verified end-to-end (detects CA2007, adds `.ConfigureAwait(false)`)
- Skills updated: rm-testing (3 additions: parameterized tests, BaseAddress trap, source-gen interface members)
