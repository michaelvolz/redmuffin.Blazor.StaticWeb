---
title: C# Coding Standards
date: 2026-04-06
category: best-practices
module: csharp
problem_type: coding_standards
applies_when:
  - Writing or reviewing C# code in the repository
  - Configuring code analyzers or .editorconfig
  - Updating coding standards documentation or skills
  - Onboarding new developers who reference coding standards
tags:
  - csharp
  - coding-standards
  - dotnet9
  - blazor
  - tunit
  - dependency-injection
---

# C# Coding Standards

This document is the single source of truth for C# coding standards in this repository. All C# code must follow these standards.

**Resolved Contradictions:**

| Topic             | Resolution                                                               |
| ----------------- | ------------------------------------------------------------------------ |
| Testing Framework | TUnit with built-in fluent assertions (NOT FluentAssertions package)     |
| Blazor DI         | `required` modifier (C# 11+), `default!` pattern is invalid for new code |
| Indentation       | 4 spaces for C# files                                                    |
| .editorconfig     | Conflicting rules documented; effective setting is space (4)             |

**Notes to Investigate:**

1. `ArgumentNullException.ThrowIfNull` may trigger analyzer warnings - need ONE solution
2. `LoggerMessage.Define` pattern may have updated syntax - investigate current best practice

---

## 1. Naming Conventions

| Element            | Convention                | Example                                        |
| ------------------ | ------------------------- | ---------------------------------------------- |
| Types/Namespaces   | PascalCase                | `HomePage`, `UserService`                      |
| Methods/Properties | PascalCase                | `GetUser()`                                    |
| Private fields     | camelCase                 | `_userService`                                 |
| Static readonly    | UpperCamelCase_underscore | `LogEvent`                                     |
| Interfaces         | Prefix "I"                | `IUserService`                                 |
| Test doubles       | `[Class]_[Type]`          | `NavigationManager_Mock`, `DelayProvider_Stub` |

---

## 2. C# 12/13 Features

### C# 12

- Primary constructors
- Collection expressions: `[1, 2, 3]`
- `ref readonly` parameters
- `nameof` instead of string literals

### C# 13

**params Collections**

```csharp
// Before C# 13: Only arrays
void Concat(params string[] items) { }

// C# 13+: Any collection type
void Concat<T>(params List<T> items) { }
void Concat<T>(params IEnumerable<T> items) { }
void Concat<T>(params ReadOnlySpan<T> items) { }
```

**Lock Type**

```csharp
Lock myLock = new();

void Process()
{
    lock (myLock)
    {
        // Thread-safe operation
    }
}
```

**Escape Sequence `\e`**

```csharp
Console.WriteLine("\e[1mBold text\e[0m"); // ANSI escape codes
```

**ref struct Interface Support**

```csharp
public ref struct MyRefStruct : IDisposable
{
    public void Dispose() { }
}
```

**ref struct Generic Type Parameters**

```csharp
void Process<T>(T value) where T : allows ref struct
{
    // Can now accept ref struct types
}
```

**Partial Properties and Indexers**

```csharp
public partial class MyClass
{
    public partial string Name { get; set; }
    public partial int this[int index] { get; set; }
}
```

**Overload Resolution Priority**

```csharp
[OverloadResolutionPriority(1)]
public void Process(ReadOnlySpan<byte> data) { }

public void Process(byte[] data) { }
```

**field Backed Properties (Preview)**

```csharp
public string Name
{
    get => field;
    set => field = value ?? throw new ArgumentNullException(nameof(value));
}
```

### Usage

```csharp
// Collection expressions
List<int> numbers = [1, 2, 3];
int[] array = [1, 2, 3];
Span<int> span = [1, 2, 3];

// Primary constructors for DI
public class UserService(ILogger<UserService> logger, IUserRepository repository)
{
    public User GetUser(int id) => repository.GetById(id);
}
```

---

## 3. Async Programming

### Naming

- Use `Async` suffix for all async methods
- Match sync counterparts: `GetDataAsync()` for `GetData()`

### Return Types

- Return `Task<T>` when returning a value
- Return `Task` when no value
- Consider `ValueTask<T>` for high-performance scenarios to reduce allocations
- Avoid `void` except for event handlers

### Exception Handling

- Use try/catch around await expressions
- Use `ConfigureAwait(false)` to prevent deadlocks in library code
- NEVER swallow exceptions silently

### Performance

- Use `Task.WhenAll()` for parallel execution
- Use `Task.WhenAny()` for timeouts/first-completed
- Consider cancellation tokens for long-running operations

### Common Pitfalls (NEVER DO)

- Never use `.Wait()`, `.Result`, or `.GetAwaiter().GetResult()`
- Avoid mixing blocking and async code
- Don't create async void methods (except event handlers)
- Always await Task-returning methods

### Patterns

- Async command pattern for long-running operations
- `IAsyncEnumerable<T>` for async streams
- Task-based asynchronous pattern (TAP) for public APIs

### ConfigureAwait(false)

```csharp
// All async calls use ConfigureAwait(false)
var response = await httpClient.GetAsync(apiUrl, token).ConfigureAwait(false);
var json = await response.Content.ReadAsStringAsync(token).ConfigureAwait(false);
await InvokeAsync(StateHasChanged).ConfigureAwait(false);
```

**Rule:** Apply `ConfigureAwait(false)` to ALL async calls except at the end of assert statements in tests.

**Codebase verification:** 257 occurrences of ConfigureAwait(false) in production code.

---

## 4. File-Scoped Namespaces

All C# files use file-scoped namespaces (C# 10+ feature):

```csharp
// ✅ CORRECT: File-scoped namespace
namespace redmuffin.Blazor.StaticWeb.Features.RaindropItems.Models;

// ❌ AVOID: Block-scoped namespace
namespace redmuffin.Blazor.StaticWeb.Features.RaindropItems.Models
{
    // ...
}
```

**Codebase verification:** 116 occurrences of file-scoped namespaces.

---

## 5. Logging

**CRITICAL**: LoggerMessage declarations MUST be in `*.Logging.cs` files, NEVER in the main file.

- Use `LoggerMessage` delegates, NEVER `Logger.LogError()`
- Main file: ONLY contains function calls like `LogEvent(logger, exception)`
- Logging file: ONLY contains delegate declarations

```csharp
// Main file - only calls, NO declarations
LogEvent(Logger, null);

// Logging file - declarations only
private static readonly Action<ILogger, Exception?> LogEvent = LoggerMessage.Define(...);
```

### Partial Class Organization

#### Verification Checklist (check BEFORE implementing)

- [ ] Does the main file contain LoggerMessage declarations? → Move them to `*.Logging.cs`
- [ ] Does `*.Logging.cs` exist? → Create it if not
- [ ] Are function calls in `*.Logging.cs`? → Move them to main file

#### Blazor Components

Split into `ComponentName.razor.cs` (logic, lifecycle, properties, events) and `ComponentName.Logging.cs` (`LoggerMessage` declarations)

```csharp
// ComponentName.razor.cs
public partial class Home : ComponentBase
{
    [Inject] public required ILogger<Home> Logger { get; set; }
    private async Task HandleClickAsync()
    {
        LogButtonClicked(Logger, null);
    }
}

// ComponentName.Logging.cs
public partial class Home
{
    private static readonly Action<ILogger, Exception?> LogButtonClicked = LoggerMessage.Define(LogLevel.Information, new EventId(5, "ButtonClicked"), "Button clicked");
}
```

**Note:** `LoggerMessage.Define` pattern may have an updated syntax. Investigate current best practice and update if needed.

#### Services (includes Azure Functions)

Split into `ServiceName.cs` (logic, methods, properties, function calls) and `ServiceName.Logging.cs` (`LoggerMessage` declarations only)

#### Tests

Split into `TestClassName.cs` (`[Test]` methods) and `TestClassName.Helpers.cs` (TestScope, mocks, utilities in same partial class). NEVER create separate helper files.

#### File Naming (MUST follow)

- Components: `Home.razor.cs` (calls), `Home.Logging.cs` (declarations)
- Services: `UserService.cs` (calls), `UserService.Logging.cs` (declarations)
- Azure Functions: `FunctionName.cs` (calls), `FunctionName.Logging.cs` (declarations)
- Tests: `HomeTests.cs`, `HomeTests.Helpers.cs`
- Test files: Use `Component_Tests.cs` for new test files (e.g., `Home_Tests.cs`)

**Codebase verification:** 126 occurrences of LoggerMessage.Define pattern.

---

## 6. Dependency Injection

### General Principles

- Never create dependencies inside methods/constructors (except primitives, DTOs, or pure value objects)
- Always use constructor injection for required dependencies
- Use `IServiceProvider` only for optional/runtime factories
- Register via extension methods in Infrastructure layer: `AddMyFeature(this IServiceCollection)`
- Use `Microsoft.Extensions.DependencyInjection` only (no third-party containers unless explicitly approved)
- No Service Locator pattern (`GetService<T>()` inside business code = anti-pattern)
- All services small, testable, no statics/stateful globals

### Service Lifetimes

| Lifetime  | Use Case                              | Notes                         |
| --------- | ------------------------------------- | ----------------------------- |
| Transient | Lightweight, no state                 | Created each time requested   |
| Scoped    | Per-request (DbContext, repositories) | One instance per scope        |
| Singleton | Thread-safe, expensive, global state  | One instance for app lifetime |

**CRITICAL:** NEVER inject Scoped into Singleton (captive dependency). Use `IServiceScopeFactory` in singletons when needed.

Validate scopes in dev: `validateScopes: true`

### Configuration

Use Options pattern only: `IOptions<T>`, never raw `IConfiguration`.

### Keyed Services

Use keyed services for multiple implementations of the same interface.

### Blazor Components

Use `[Inject]` with `required` modifier (C# 11+):

```csharp
// ✅ CORRECT: C# 11+ required modifier
public partial class Home : ComponentBase
{
    [Inject] public required NavigationManager Navigation { get; set; }
    [Inject] public required ILogger<Home> Logger { get; set; }
}

// ❌ WRONG: default! for new code
public partial class Home : ComponentBase
{
    [Inject] public NavigationManager Navigation { get; set; } = default!;
}
```

**Note:** The `required` modifier provides compile-time null safety, eliminating the need for `default!` and runtime validation.

### Services

Use primary constructor syntax:

```csharp
public class UserService(ILogger<UserService> logger, IUserRepository repository)
{
    public User GetUser(int id) => repository.GetById(id);
}
```

### Null Checks

```csharp
public class MyClass(IDependency dependency)
{
    ArgumentNullException.ThrowIfNull(dependency);
}
```

**Note:** `ArgumentNullException.ThrowIfNull` may trigger analyzer warnings. Investigate and standardize on ONE approach. Document the chosen solution.

---

## 7. Testing Standards

### Test Double Naming

All test doubles must follow the pattern: `[ClassName]_[Type]` where `[Type]` is one of:

- `Mock` - For behavior verification
- `Stub` - For state verification with predefined responses
- `Spy` - For recording interactions while maintaining real functionality
- `Fake` - For simple working implementations
- `Dummy` - For placeholders that satisfy parameter requirements

### Strategic Approach

**LightMock.Generator (External Dependencies)**

Use for 3rd party and external dependencies:

- `IHttpClientFactory`
- `ILocalStorageService`
- `ILogger<T>`
- External APIs
- Azure services

**Custom Mocks (Internal Components)**

Use for internal components and services:

- `NavigationManager`
- Internal services
- Blazor components
- Project-specific abstractions

### Examples

```csharp
// ✅ CORRECT: Custom mock for internal component
public sealed class NavigationManager_Mock : NavigationManager
{
    public string? NavigatedTo { get; private set; }
    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        NavigatedTo = uri;
    }
}

// ✅ CORRECT: LightMock for external dependency
var httpClientMock = new Mock<IHttpClientFactory>();
httpClientMock.Arrange(f => f.CreateClient(The<string>.IsAnyValue))
    .Returns(new HttpClient());

// ✅ CORRECT: Test double with proper suffix
public sealed class DelayProvider_Stub : IDelayProvider
{
    public Task DelayAsync(int milliseconds) => Task.CompletedTask;
}
```

### Disposable Pattern for Test Doubles

All test doubles that own disposable resources (e.g., `MemoryStream`, `HttpClient`, `IDisposable` fields) **must implement `IDisposable`**:

```csharp
// ✅ CORRECT: Mock with disposable resources implements IDisposable
public sealed class HttpRequestData_Mock : HttpRequestData, IDisposable
{
    private readonly MemoryStream _bodyStream = new();

    public override Stream Body => _bodyStream;

    public void Dispose()
    {
        _bodyStream.Dispose();
    }
}

// ❌ INCORRECT: Missing IDisposable causes CA2001/CA1001 warnings
public sealed class HttpRequestData_Mock : HttpRequestData
{
    private readonly MemoryStream _bodyStream = new(); // Warning: disposable field
}
```

**Why this matters:**

- Prevents CA2001 ("Call System.IDisposable.Dispose on object") warnings
- Prevents CA1001 ("Type owns disposable field(s) but is not disposable") warnings
- Ensures zero-warning builds as required by project policy
- Tests using `using var request = ...` pattern dispose correctly

### Partial Class Structure

- **Main test file**: `[TestClass].cs` - Contains only `[Test]` methods
- **Helper file**: `[TestClass].Helpers.cs` - Contains TestScope, mocks, and utilities
- **CRITICAL**: All test helpers must be in corresponding partial files, never separate helper files

```csharp
// Main test file: HomeTests.cs
[Category("Feature:Home")]
[Category("Unit")]
public sealed partial class HomeTests
{
    [Test]
    public async Task Home_ComponentStructure_HasRequiredElements()
    {
        using var scope = CreateTestScope();
        var component = scope.BUnitContext.Render<HomePage>();

        using (Assert.Multiple())
        {
            await Assert.That(component.Find("h1")).IsNotNull();
            await Assert.That(component.Find("button")).IsNotNull();
        }
    }
}

// Helper file: HomeTests.Helpers.cs
public sealed partial class HomeTests
{
    public TestScope CreateTestScope() => new();
}
```

### TestScope Architecture

All test classes must use TestScope pattern:

```csharp
public sealed class TestScope(string baseUri = "http://localhost:5000/") : IDisposable
{
    public BunitContext BUnitContext { get; } = new();
    public NavigationManager_Mock NavigationManager { get; } = new(baseUri);
    public Logger_Spy<T> Logger { get; } = new();

    public TestScope WithStandardServices() { /* setup */ return this; }
    public void Dispose() => BUnitContext?.Dispose();
}
```

### TUnit Usage

- Use `[Test]` attribute for test methods
- Use `[Category]` attribute for test organization
- Use `[Arguments]` for data-driven tests
- Use TUnit's built-in fluent assertions: `Assert.That(actual).IsNotNull()`, `Assert.That(value).IsEqualTo(expected)`
- Use `Assert.Multiple()` for grouping related assertions
- **Note**: TUnit has built-in fluent assertions - do NOT use the separate FluentAssertions package
- Prefer custom mocks over LightMock.Generator for internal components
- Use LightMock.Generator for external dependencies only

### Using Declarations

```csharp
// ✅ CORRECT: Using declaration
using var scope = CreateTestScope();
var component = scope.BUnitContext.Render<HomePage>();

// ❌ AVOID: Using statement (older pattern)
using (var scope = CreateTestScope())
{
    var component = scope.BUnitContext.Render<HomePage>();
}
```

### Fire-and-Forget Pattern

For background operations that shouldn't block:

```csharp
// Fire-and-forget with ConfigureAwait(false)
_ = Task.Run(async () => await RefreshDataInBackgroundAsync().ConfigureAwait(false));
```

### Code Quality

- **Zero build warnings policy** (except IL2111)
- `ConfigureAwait(false)` on all awaits (except at end of assert statements)
- Follow StyleCop/Meziantou analyzer rules
- Use C# 13 patterns and modern syntax

### Compliance Checklist

Before committing any test changes:

- [ ] Test double naming follows `[Class]_[Type]` convention
- [ ] Strategic approach used (LightMock vs Custom)
- [ ] TestScope architecture implemented
- [ ] Partial class organization followed
- [ ] Zero build warnings achieved
- [ ] All helpers placed in corresponding partial files
- [ ] TUnit standards followed
- [ ] ConfigureAwait(false) properly applied
- [ ] Disposable test doubles implement `IDisposable` (CA2001/CA1001 compliance)

---

## 8. Zero Warnings Policy

### Goal

Zero errors, zero warnings before commit.

### Permitted Warning

- IL2111 (Blazor WebAssembly `App_razor.g.cs` trimming) is safe to ignore

### Pragma Warnings

`#pragma warning disable` directives suppress warnings we've consciously decided to keep. Never remove or modify pragma directives without explicit user approval. Pragmas enable zero-warning builds by documenting intentional deviations from analyzer rules.

### Build Commands

```bash
# Check for warnings
dotnet build --verbosity quiet

# After C# changes
dotnet build --verbosity quiet

# After SCSS/JS changes
dotnet build -c Debug-Sass

# Before commit
dotnet test
```

### Analyzer Rules

See `.editorconfig` for complete analyzer configuration. Key rules:

- StyleCop: SA1402 (one type per file), SA1208/1210 (using order), SA1500 (brace on new line)
- Meziantou: MA0016 (use IEnumerable abstractions), MA0002/0006/0074 (StringComparison)
- Microsoft: CA1845 (AsSpan), CA1854 (TryGetValue), CA1848 (LoggerMessage), CA2016 (CancellationToken)

---

## 9. Blazor Components

### Partial Class Organization

Split into `ComponentName.razor.cs` (logic, lifecycle, properties, events) and `ComponentName.Logging.cs` (`LoggerMessage` declarations only).

```csharp
// ComponentName.razor.cs
public partial class Home : ComponentBase
{
    [Inject] public required ILogger<Home> Logger { get; set; }
    private async Task HandleClickAsync()
    {
        LogButtonClicked(Logger, null);
    }
}

// ComponentName.Logging.cs
public partial class Home
{
    private static readonly Action<ILogger, Exception?> LogButtonClicked = LoggerMessage.Define(LogLevel.Information, new EventId(5, "ButtonClicked"), "Button clicked");
}
```

**Note:** `LoggerMessage.Define` pattern may have an updated syntax. Investigate current best practice and update if needed.

### File Naming

- Components: `Home.razor.cs` (calls), `Home.Logging.cs` (declarations)
- Services: `UserService.cs` (calls), `UserService.Logging.cs` (declarations)
- Azure Functions: `FunctionName.cs` (calls), `FunctionName.Logging.cs` (declarations)
- Tests: `HomeTests.cs`, `HomeTests.Helpers.cs`
- Test files: Use `Component_Tests.cs` for new test files (e.g., `Home_Tests.cs`)

### Code Style and Structure

- Write idiomatic and efficient Blazor and C# code
- Follow .NET and Blazor conventions
- Use Razor Components appropriately for component-based UI development
- Prefer inline functions for smaller components but separate complex logic into code-behind or service classes
- Async/await should be used where applicable to ensure non-blocking UI operations

### Naming Conventions

- Follow PascalCase for component names, method names, and public members
- Use camelCase for private fields and local variables
- Prefix interface names with "I" (e.g., `IUserService`)

### Lifecycle

- Utilize Blazor's built-in features for component lifecycle (e.g., `OnInitializedAsync`, `OnParametersSetAsync`)
- Use data binding effectively with `@bind`

### Dependency Injection

- Leverage Dependency Injection for services in Blazor
- Use `[Inject]` with `required` modifier (C# 11+)
- Structure Blazor components and services following Separation of Concerns
- Always use the latest C# version (currently C# 13) features like record types, pattern matching, and global usings

### Error Handling and Validation

- Implement proper error handling for Blazor pages and API calls
- Use logging for error tracking in the backend
- Consider capturing UI-level errors in Blazor with tools like `ErrorBoundary`
- Implement validation using FluentValidation or DataAnnotations in forms

### Performance Optimization

- Optimize Razor components by reducing unnecessary renders
- Use `StateHasChanged()` efficiently
- Minimize the component render tree by avoiding re-renders unless necessary
- Use `ShouldRender()` where appropriate
- Use `EventCallbacks` for handling user interactions efficiently, passing only minimal data when triggering events

### Caching Strategies

- Implement in-memory caching for frequently used data (use `IMemoryCache` for lightweight caching)
- For Blazor WebAssembly, utilize localStorage or sessionStorage to cache application state between user sessions
- Consider Distributed Cache strategies (like Redis or SQL Server Cache) for larger applications that need shared state across multiple users or clients
- Cache API calls by storing responses to avoid redundant calls when data is unlikely to change

### State Management

- Use Blazor's built-in Cascading Parameters and EventCallbacks for basic state sharing across components
- Implement advanced state management solutions using libraries like Fluxor or BlazorState when the application grows in complexity
- For client-side state persistence in Blazor WebAssembly, consider using Blazored.LocalStorage or Blazored.SessionStorage
- For server-side Blazor, use Scoped Services and the StateContainer pattern to manage state within user sessions while minimizing re-renders

### API Design and Integration

- Use `HttpClient` or other appropriate services to communicate with external APIs or your own backend
- Implement error handling for API calls using try-catch and provide proper user feedback in the UI

### Security and Authentication

- Implement Authentication and Authorization in the Blazor app where necessary using ASP.NET Identity or JWT tokens for API authentication
- Use HTTPS for all web communication and ensure proper CORS policies are implemented

---

## 10. Azure Functions (Isolated Worker)

### Setup

- Use `Program.cs` with `AddFunctions worker` configuration
- Bindings use input/output attributes
- Use `FunctionContext` for logging and dependency injection

### Dependency Injection

Use `Startup.cs` to register services (`ILogger`, `IHttpClientFactory`) in C# Azure Functions for testability and maintainability:

```csharp
builder.Services.AddSingleton<IMyService, MyService>();
```

### Cold Start Optimization

- Minimize assembly size by reducing dependencies
- Use .NET Isolated Worker for better control over startup logic
- Avoid heavy initialization in function code

### Error Handling

- Implement retry policies with Polly for transient failures
- Use try-catch blocks to handle exceptions gracefully
- Return meaningful HTTP status codes (e.g., `400` for bad requests) for HTTP triggers

### Input Validation

Validate HTTP trigger inputs using C# model validation (e.g., `System.ComponentModel.DataAnnotations`) or custom checks to ensure security and prevent errors.

### Structured Logging

Use `ILogger` for structured logging in C#, capturing only essential data (e.g., request IDs, errors) to avoid performance overhead:

```csharp
logger.LogInformation("Processing {RequestId}", requestId);
```

### Asynchronous Programming

- Use `async`/`await` in C# functions for I/O-bound operations (e.g., HTTP calls, database queries) to improve scalability
- Avoid blocking calls like `.Result` or `.Wait()`

### Function Granularity

Write single-responsibility functions. Split complex logic into smaller, focused functions to improve maintainability and reusability. Example: Separate data retrieval and processing into distinct functions.

### Configuration Management

Access settings via environment variables using `Environment.GetEnvironmentVariable` in C#. Avoid hardcoding values to ensure flexibility across environments.

### Unit Testing

Write unit tests for function logic using TUnit. Use LightMock.Generator for external dependencies (e.g., `ILogger`, `IHttpClientFactory`) or custom mocks for internal components. See Testing Standards section.

### Idempotency

Ensure functions are idempotent, especially for event-driven triggers (e.g., Queue, Event Hub). Handle duplicate messages gracefully using unique identifiers or state checks.

### Parameter Optimization

Use strongly-typed bindings (e.g., `QueueTrigger`, `BlobInput`) in C# to reduce parsing logic and improve type safety. Avoid overusing dynamic `JObject` inputs.

### Resource Cleanup

Dispose of resources (e.g., database connections, HTTP clients) properly using `IDisposable` or `using` statements to prevent memory leaks in long-running functions.

### Code Reusability

Extract shared logic into class libraries or static methods in C#. Use NuGet packages for cross-function utilities to maintain DRY principles.

### Performance Monitoring

Instrument code with custom metrics via Application Insights SDK in C# (e.g., `TelemetryClient.TrackMetric`) to track function-specific performance indicators.

### Versioning

For HTTP-triggered functions, implement API versioning (e.g., via query parameters or headers) to support backward compatibility as function logic evolves.

### Secure Coding

Sanitize inputs and outputs to prevent injection attacks (e.g., SQL, XSS). Use libraries like `AntiXssEncoder` for output encoding in HTTP responses.

---

## 11. Architecture & Design

### SOLID Principles (ALWAYS)

- **Single Responsibility Principle (SRP)**: One reason to change per class/service
- **Open/Closed**: Extend via composition, never modify core
- **Liskov Substitution**: Subtypes replaceable without behavior change
- **Interface Segregation**: Client-specific interfaces (small, focused)
- **Dependency Inversion**: Outer layers depend inward only (Clean Architecture layers: Domain → Application → Infrastructure)

### Composition Over Inheritance (100%)

- Prefer composition over inheritance in 100% of cases unless true "is-a" specialization with no viable composition
- Model "has-a" via interfaces; never inherit for reuse
- Keep hierarchies flat (<2 levels max)
- No god classes, no anemic domain models

### Clean Architecture

```
Solution
├── Domain/ (entities, value objects, interfaces, exceptions)
├── Application/ (use cases, services, DTOs)
├── Infrastructure/ (impls, EF, external clients, DI extensions)
├── Presentation/ (API, Blazor, controllers/components)
├── Tests/ (Unit, Integration)
```

### Dependency Rule

Outer layers depend inward only. Domain has no dependencies. Application depends only on Domain. Infrastructure depends on Application and Domain.

### Design Patterns (Required)

- **Command Pattern**: Generic base classes, `ICommandHandler<TOptions>` interface, `CommandHandlerOptions` inheritance
- **Factory Pattern**: Complex object creation, service provider integration
- **Repository Pattern**: Async data access, provider abstractions
- **Provider Pattern**: External service abstractions, clear contracts, configuration handling

### TDD (Red-Green-Refactor - NON-NEGOTIABLE)

- Write failing test first (Red) → minimal code to pass (Green) → refactor
- Three laws: (1) No prod code without failing test. (2) Only enough test to fail. (3) Only enough prod to pass.
- Tests first for all business logic, use cases, domain rules
- Use TUnit with built-in fluent assertions (`Assert.That()`). **Do NOT use the separate FluentAssertions package.**
- Use LightMock.Generator for external dependencies, custom mocks for internal components
- Unit tests: isolate via interfaces/DI (mocks for deps)
- Integration tests for external boundaries only
- 80%+ coverage on domain/application; 100% on critical paths
- Tests independent, fast (<100ms), descriptive names (Should_When_Then)
- Never change passing tests except for requirement change
- Refactor only after Green; keep tests green at all times

### Trunk-Based Development (TBD)

- All work commits to trunk (main) multiple times/day
- Changes small (hours max); no long-lived branches
- Short-lived PR branches (<1 day) only for review/CI; delete after merge
- Pre-commit: full local build + all tests pass
- CI must run on every commit; trunk always green/releasable
- Hide WIP with feature flags (Config or LaunchDarkly-style) or branch-by-abstraction
- No feature branches for release artifacts
- Use TDD + feature flags to keep trunk stable

### Code Style & Quality (ENFORCED)

- C# latest (nullable enabled, records, primary constructors where clean)
- Blazor: component composition > inheritance; inject services; use @inject
- PowerShell: same DI/composition mindset when applicable
- No comments explaining code; code must be self-documenting
- Pure functions where possible; immutable by default
- Domain events for side effects
- CQRS when beneficial (MediatR or minimal APIs)
- No direct EF/DbContext in application layer (repositories only if needed; use use-case services)
- Error handling: Result<T> or exceptions with global filters (never silent fails)
- Logging: structured, injected ILogger<T>
- Performance: async/await everywhere possible; no .Result/.Wait
- Security: validate inputs, least privilege, no secrets in code

### Review Checklist

- Design Patterns: Command Handler, Factory, Provider, Repository correctly implemented?
- Architecture: Namespace conventions? Proper separation of concerns?
- .NET Best Practices: Primary constructors, async/await, ResourceManager, structured logging?
- GoF Patterns: Command, Factory, Template Method, Strategy patterns?
- SOLID Principles: Any violations?
- Performance: Async/await, resource disposal, `ConfigureAwait(false)`?
- Testability: Mockable components, async testability, AAA pattern?
- Security: Input validation, secure credential handling, parameterized queries?
- Documentation: XML docs for public APIs?

### Key Focus Areas

- Command Handlers: Validation in base class, consistent error handling
- Factories: Dependency configuration, service provider integration
- Providers: Connection management, async patterns, exception handling
- Configuration: Data annotations, validation attributes

---

## 12. Project Configuration

### Stack

| Technology      | Version     | Purpose        |
| --------------- | ----------- | -------------- |
| .NET            | 9.0         | Core framework |
| Blazor          | WebAssembly | Frontend       |
| Azure Functions | .NET 9      | Backend        |
| TUnit           | Latest      | Testing        |
| SCSS/Sass       | -           | Styling        |

### Frontend

- Blazor WebAssembly (.NET 9)
- Feature-based structure
- Build Settings: `WasmStripILAfterAOT=true`, `InvariantGlobalization=true`, `PublishTrimmed=true`
- Deployment: Azure Static Web Apps with CSP and caching in `staticwebapp.config.json`

### Backend

- Azure Functions (.NET 9)
- Isolated worker

### Dependencies

- Blazored.LocalStorage
- Markdig
- Microsoft.Azure.Functions.Worker
- TUnit
- Zurb Foundation (CDN)
- FontAwesome (CDN)
- BuildWebCompiler2022 (SCSS)
- Coverlet
- Analyzers (Roslynator, StyleCop, Meziantou, VSThreading)

### Build Commands

```bash
# Build entire solution
dotnet build

# Fast build (after restore)
dotnet build --no-restore

# Build with warnings only
dotnet build --verbosity quiet

# Clean build
dotnet clean --verbosity minimal
```

### Test Commands

```bash
# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~TestClassName"

# Run single test
dotnet test --filter "FullyQualifiedName~TestMethodName"

# List all tests
dotnet test --list-tests

# Run by category (treenode-filter)
dotnet test -- --treenode-filter "/*/*/*/*[Category=Smoke]"                 # Smoke (27, ~0.8s)
dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Home]"          # Home (52)
dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Videos]"        # Videos (10)
dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Articles]"      # Articles (17)
dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Cache]"         # Cache (31)
dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Raindrop]"      # Raindrop (24)
dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:RaindropItems]" # RaindropItems (17)
dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Core]"          # Core (13)
dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:ApiExample]"    # ApiExample (5)
```

**AOT Testing:** CI runs with AOT (`CI=true`/`GITHUB_ACTIONS=true`). Locally AOT is disabled for speed.

### Dev Modes

| Mode       | Port | Use Case             | Command                                               |
| ---------- | ---- | -------------------- | ----------------------------------------------------- |
| Normal     | 5233 | UI, mock data (99%)  | `dotnet run --project src/redmuffin.Blazor.StaticWeb` |
| Full Stack | 4280 | Real API, OAuth, E2E | `pwsh Start.ps1 -Auto`                                |

### Coverage

```powershell
# Generate coverage report
pwsh scripts/Generate-CoverageReport.ps1

# View coverage report
pwsh scripts/View-CoverageReport.ps1
```

### Development Build Scripts

- `scripts/test-build-fast.ps1` - Fast dev build (~9s, AoT disabled)
- `scripts/test-build-aot.ps1` - Production parity testing
- `scripts/DisplayWarnings.ps1` - Show all build warnings

### Package Management

- Use `scripts/Update-PackageVersions.ps1` for NuGet package updates according to Central Package Management (CPM)
- ALWAYS use `pwsh -NoProfile` for all PowerShell commands to optimize performance
- Keep every package version value centralized in the top property section of `Directory.Packages.props`
- Item groups should reference properties instead of hard-coded version literals
- After updates: `dotnet clean && dotnet build --verbosity quiet && dotnet test`

---

## 13. .NET 9 Best Practices

### Runtime Improvements

**Dynamic Adaptation for Server GC**

Server GC now adapts to application memory requirements instead of machine resources:

- Better memory management for cloud apps
- Reduced memory footprint in high-core environments
- Can configure legacy Server GC if needed

**Performance Improvements**

- **LINQ optimizations**: `Take`, `DefaultIfEmpty` up to 10x faster for empty collections
- **System.Text.Json**: >50% improvements for various operations
- **Exception handling**: 50% faster (adopted Native AOT model)
- **Dynamic PGO**: 70% faster execution for optimized code patterns

### Library Improvements

**New LINQ Methods**

```csharp
// CountBy - aggregate counts by key
var counts = items.CountBy(x => x.Category);

// AggregateBy - aggregate state by key without intermediate allocations
var aggregated = items.AggregateBy(
    x => x.Category,
    seed: 0,
    (acc, item) => acc + item.Value
);
```

**TimeSpan From Methods**

New `From*` methods that accept `int` instead of `double`:

```csharp
var timeout = TimeSpan.FromSeconds(30);  // int overload
var delay = TimeSpan.FromMilliseconds(100);  // int overload
```

**System.Text.Json Enhancements**

- Nullable reference type annotations
- JSON schema export from types
- Customizable indentation
- Multiple root-level JSON values from single stream
- `JsonMarshal.GetRawUtf8Value()` for UTF8 bytes without allocation

**PriorityQueue Updates**

New `Remove` method to update priority:

```csharp
var queue = new PriorityQueue<string, int>();
queue.Enqueue("item1", 1);
queue.Remove("item1", out var element, out var priority);
queue.Enqueue("item1", 5); // Updated priority
```

**Cryptography Additions**

- One-shot hash methods on `CryptographicOperations`
- KMAC algorithm support

**PersistedAssemblyBuilder**

New type to save emitted assemblies:

```csharp
var assemblyBuilder = new PersistedAssemblyBuilder(...);
// Can now save the assembly to disk
assemblyBuilder.Save("MyAssembly.dll");
```

### ASP.NET Core 9 Improvements

**Static File Optimization**

- Automatic fingerprinted versioning at build time
- Pre-compression with Brotli at publish time
- Content-based hash for aggressive caching

**Blazor Enhancements**

- `RendererInfo.IsInteractive` for runtime render mode detection
- Improved reconnection experience for Blazor Server
- New Hybrid and Web app templates

**OpenAPI Built-in Support**

```csharp
// Native AOT-friendly OpenAPI document generation
builder.Services.AddOpenApi();
```

**Security Improvements**

- Easier HTTPS development certificate setup on Linux
- Built-in authentication state flow to client in Blazor
- OAuth/OIDC extensibility for additional parameters
- Pushed Authorization Requests (PAR) support

### Best Practices

**Use Collection Expressions**

```csharp
// Prefer collection expressions
List<int> numbers = [1, 2, 3, 4, 5];
int[] array = [1, 2, 3];
Span<int> span = [1, 2, 3];
```

**Leverage Primary Constructors**

```csharp
// Use primary constructors for DI
public class UserService(ILogger<UserService> logger, IUserRepository repository)
{
    public User GetUser(int id) => repository.GetById(id);
}
```

**Use Span-Based APIs**

```csharp
// Prefer AsSpan() over Substring()
ReadOnlySpan<char> span = text.AsSpan(start, length);

// Use span-based LINQ operations
var count = text.AsSpan().Count(c => char.IsDigit(c));
```

**Cache JsonSerializerOptions**

```csharp
// CA1869: Cache JsonSerializerOptions instances
private static readonly JsonSerializerOptions s_options = new()
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};
```

**Use LoggerMessage Delegates**

```csharp
// CA1848: Use LoggerMessage delegates for performance
private static readonly Action<ILogger, string, Exception?> LogProcessing
    = LoggerMessage.Define<string>(LogLevel.Information, new EventId(1), "Processing {Item}");
```

---

## 14. Code Quality

### Formatting

| File Type       | Indentation   | Notes             |
| --------------- | ------------- | ----------------- |
| C#              | 4 spaces      | -                 |
| .razor, .cshtml | 4 spaces      | -                 |
| .csproj         | 2 spaces      | -                 |
| All             | Max 160 chars | Brace on new line |

### Expression-Bodied Members

Use for computed properties and simple methods:

```csharp
// Computed properties
public bool IsSuccess => Status == RaindropCacheStatus.Hit;
public bool IsExpired => Status == RaindropCacheStatus.Expired;
public double CompressionRatio => OriginalSize > 0 ? (double)CompressedSize / OriginalSize * 100 : 0;

// Simple methods
public override string ToString() => $"Result: {Status}";
```

### Record Types

Use for immutable data transfer objects:

```csharp
// Immutable record
public record RaindropItem(
    string Id,
    string Title,
    string? Excerpt
);

// Readonly record struct for value types
public readonly record struct PerformanceMetrics(
    long TotalItems,
    double AverageAccessCount
);
```

### Init-Only Properties

Use for result objects and configuration:

```csharp
public sealed class RaindropCacheResult<T>
{
    public RaindropCacheStatus Status { get; init; }
    public T? Data { get; init; }
    public RaindropCacheMetadata? Metadata { get; init; }
    public string? ErrorMessage { get; init; }
}
```

### Required Properties with DI

Use in Blazor components for dependency injection (C# 11+):

```csharp
public partial class Home : ComponentBase
{
    [Inject] public required NavigationManager Navigation { get; set; }
    [Inject] public required IHttpClientFactory HttpClientFactory { get; set; }
    [Inject] public required IDelayProvider DelayProvider { get; set; }
}
```

**Note:** The `required` modifier provides compile-time null safety, eliminating the need for `default!` and runtime validation.

### Null Pattern Matching

Use `is null` and `is not null` instead of `== null` and `!= null`:

```csharp
// ✅ CORRECT
if (request is null) return await CreateBadRequestResponseAsync(req, "Missing code.", token).ConfigureAwait(false);
if (redirectUri is not null) ProcessRedirect(redirectUri);

// ❌ AVOID
if (request == null) return ...;
if (redirectUri != null) ProcessRedirect(redirectUri);
```

### XML Documentation

All public APIs have XML documentation:

```csharp
/// <summary>
///     Represents the result of a raindrop cache operation with success/failure states and optional data.
/// </summary>
/// <typeparam name="T">The type of raindrop data being cached.</typeparam>
public sealed class RaindropCacheResult<T>
{
    /// <summary>
    ///     Gets the status of the cache operation.
    /// </summary>
    public RaindropCacheStatus Status { get; init; }
}
```

### Code Style (ENFORCED)

- C# latest (nullable enabled, records, primary constructors where clean)
- Blazor: component composition > inheritance; inject services; use @inject
- PowerShell: same DI/composition mindset when applicable
- No comments explaining code; code must be self-documenting
- Pure functions where possible; immutable by default
- Domain events for side effects
- CQRS when beneficial (MediatR or minimal APIs)
- No direct EF/DbContext in application layer (repositories only if needed; use use-case services)
- Error handling: Result<T> or exceptions with global filters (never silent fails)
- Logging: structured, injected ILogger<T>
- Performance: async/await everywhere possible; no .Result/.Wait
- Security: validate inputs, least privilege, no secrets in code

---

## Related Documents

- **Contradictions Analysis**: `csharp-standards-contradictions-2026-04-06.md`
- **Duplicates Analysis**: `csharp-standards-duplicates-analysis-2026-04-06.md`
- **Consolidation Archive**: `csharp-standards-consolidation-2026-04-06.md`
- **ce:compound Summary**: `csharp-standards-consolidation-resolution-2026-04-06.md`

## Source Files (Archived)

The following source files remain unchanged as archives:

- `.opencode/skills/redmuffin-standards/rm-csharp-standards/SKILL.md`
- `.opencode/skills/redmuffin-standards/rm-output-style/SKILL.md`
- `.opencode/skills/redmuffin-standards/rm-strict-coding-standards/SKILL.md`
- `.opencode/skills/redmuffin-standards/rm-dotnet/SKILL.md`
- `.editorconfig`
- `docs/TestingGuidelines.md`
- `.github/guides/blazor.md`
- `.github/guides/azure-functions.md`
- `AGENTS.md`

---

## Next Steps: Skill Guide Architecture

**Status:** Planning

**Problem:** This document is massive (~1,600 lines). Loading it as ONE file would be catastrophic for token efficiency. We need a system where content is loaded EXACTLY when needed.

**Proposed Solution:** Split into separate skill guides with precise trigger conditions.

### Phase 1: Investigate Skill Loading Triggers

- [ ] Analyze how OpenCode triggers skill loading
- [ ] Document trigger patterns (file type, task type, keywords)
- [ ] Identify optimal granularity for skill guides

### Phase 2: Create Separate Guide Skills

Each section becomes a separate skill with prefix `rm-guide-`:

| Section                   | Proposed Skill Name        | Trigger When...                                                   |
| ------------------------- | -------------------------- | ----------------------------------------------------------------- |
| 1. Naming Conventions     | `rm-guide-naming`          | Creating/renaming types, methods, fields, test doubles            |
| 2. C# 12/13 Features      | `rm-guide-csharp-features` | Using new C# features, modernizing code                           |
| 3. Async Programming      | `rm-guide-async`           | Writing async code, ConfigureAwait, Task patterns                 |
| 4. File-Scoped Namespaces | `rm-guide-namespaces`      | Creating new files, namespace organization                        |
| 5. Logging                | `rm-guide-logging`         | Adding logging, LoggerMessage pattern, partial class organization |
| 6. Dependency Injection   | `rm-guide-di`              | Injecting services, Blazor components, constructor injection      |
| 7. Testing Standards      | `rm-guide-testing`         | Writing tests, test doubles, TUnit, TestScope                     |
| 8. Zero Warnings Policy   | `rm-guide-warnings`        | Fixing build warnings, pragma warnings, analyzer rules            |
| 9. Blazor Components      | `rm-guide-blazor`          | Creating Blazor components, lifecycle, state management           |
| 10. Azure Functions       | `rm-guide-azure-functions` | Creating Azure Functions, isolated worker, DI                     |
| 11. Architecture & Design | `rm-guide-architecture`    | Designing services, SOLID, Clean Architecture, patterns           |
| 12. Project Configuration | `rm-guide-config`          | Build commands, test commands, dev modes, package management      |
| 13. .NET 9 Best Practices | `rm-guide-dotnet9`         | Using .NET 9 features, performance optimizations                  |
| 14. Code Quality          | `rm-guide-code-quality`    | Expression-bodied members, records, null pattern matching         |

### Phase 3: Reference in AGENTS.md

- [ ] Add all `rm-guide-*` skills to AGENTS.md
- [ ] Document trigger conditions for each skill
- [ ] Ensure AI knows what exists and when to self-trigger

### Phase 4: Reality Check - Trim Superfluous Content

- [ ] Analyze each section for content that is 100% superfluous to ANY model
- [ ] Identify content that models already know (common knowledge)
- [ ] Create foolproof system to identify trimmable content
- [ ] Document criteria for "superfluous" vs "essential"

### Phase 5: Create Skills

- [ ] Create each `rm-guide-*` skill with:
  - Precise trigger description
  - Essential content only
  - Code examples
  - Cross-references to related guides

### Success Criteria

1. Each skill loads ONLY when relevant task is performed
2. Token usage is minimized (no massive file loading)
3. All essential information is preserved
4. No duplication across skills
5. AGENTS.md provides complete map of available guides
