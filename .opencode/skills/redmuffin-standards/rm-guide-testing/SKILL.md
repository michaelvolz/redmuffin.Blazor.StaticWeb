---
date: 2026-05-10
last_updated: 2026-05-10
---

# rm-guide-testing

Comprehensive testing standard for this .NET 9 Blazor WASM project.
Single source of truth for test patterns, test doubles, and test
project conventions.

## What Belongs in This File

- **Viewpoint**: Developer writing tests for any part of the codebase.
  You know C# and TUnit but need the project's specific conventions.
- **What belongs**: Test double taxonomy and naming. File structure
  conventions. Test scope patterns. Pure function testing. Fake
  implementation patterns. Static state handling. When to use LightMock.
  Author-aligned testing philosophy.
- **What does NOT belong**: General TDD workflow (use `rm-tdd`).
  CRAP/SCRAP/Architecture cleanup workflows (use `rm-gates-cleanup`).
  Code quality principles (use `rm-guide-cleanup`). Naming conventions
  (use `rm-guide-naming`).

---

## Philosophy

Every test should spark joy. If a test is messy, the production code
needs refactoring — not the test.

**Authors we follow:**

| Author               | Principle                                                                                       |
| -------------------- | ----------------------------------------------------------------------------------------------- |
| **Uncle Bob**        | AAA pattern. Tests as documentation. One logical assertion per test.                            |
| **Michael Feathers** | Characterization tests for untested code. Extract pure functions before mocking.                |
| **Freeman & Pryce**  | Mock only at architectural boundaries. Prefer state verification over interaction verification. |
| **Kent Beck**        | Test-first. Tests drive design, not the other way around.                                       |
| **John Ousterhout**  | Minimize complexity in test code too. Simple tests = simple design.                             |

## Decision Tree: How to Test

```
Can the logic be tested as a pure function (zero dependencies)?
  → Yes: Extract to public static. Test directly. ZERO mocks.
  → No: Does the dependency have ≤3 methods you need to fake?
    → Yes: Hand-rolled fake ([Class]_Fake) in .Helpers.cs
    → No: LightMock.Generator source-gen mock
          (But first: can we redesign to reduce the dependency surface?)
```

## Test Double Taxonomy

| Name    | Purpose                                         | Example                      |
| ------- | ----------------------------------------------- | ---------------------------- |
| `_Fake` | Working implementation with simplified behavior | `BrowserStorageService_Fake` |
| `_Stub` | Returns canned answers, no logic                | `JSRuntime_Stub`             |
| `_Mock` | Records calls for verification                  | `HttpMessageHandler_Mock`    |
| `_Spy`  | Records calls + passes through or logs          | `Logger_Spy<T>`              |

Naming: `[Class]_[Type]` where Class is the interface/abstract class
being implemented and Type is from the taxonomy above.

## File Structure

Each test class lives in a `partial class` spread across concern files:

```
Tests/[Feature]/[Class]Tests.cs              ← Happy path, core behavior
Tests/[Feature]/[Class]Tests.Helpers.cs       ← TestScope, fakes, stubs, factory methods
Tests/[Feature]/[Class]Tests.EdgeCases.cs     ← Error states, nulls, edge conditions
Tests/[Feature]/[Class]Tests.Infrastructure.cs ← DI-wired integration tests
Tests/[Feature]/[Class]Tests.Behavior.cs      ← Behavioral tests (state transitions)
```

**Rules:**

- `[Class]Tests.cs` is always `public sealed partial class`
- `.Helpers.cs` contains `TestScope`, test doubles, factory methods, setup helpers
- `.EdgeCases.cs` tests null inputs, exceptions, boundary values
- `.Infrastructure.cs` tests the class through its real DI container
- Not every class needs all files — start with `.cs` + `.Helpers.cs`
- Each file gets `[Category("Feature:XXX")]` matching its feature area

## Test Scope Patterns

### Pattern 1: Direct (for pure functions)

No scope. No DI. Just call the method.

```csharp
[Test]
public async Task IsLocalhostHost_ReturnsTrue_ForLocalhost()
{
    var result = PageLoadSpeedConfig.IsLocalhostHost("localhost");
    await Assert.That(result).IsTrue();
}
```

### Pattern 2: Simple TestScope (for 1-2 dependencies)

Hand-rolled fake. `new` up the service directly. No DI container.

```csharp
// In .Helpers.cs:
public sealed class TestScope : IDisposable
{
    public BrowserStorageService_Fake Storage { get; } = new();
    public Logger_Spy<MyService> Logger { get; } = new();
    public MyService Service { get; }

    public TestScope()
    {
        Service = new MyService(Storage, Logger);
    }

    public void Dispose() { }
}

public sealed class BrowserStorageService_Fake : IBrowserStorageService
{
    private readonly Queue<StorageStats> _responses = [];
    public Exception? Exception { get; set; }

    public void QueueResponse(StorageStats stats) => _responses.Enqueue(stats);

    public Task<StorageStats> GetStatsAsync(CancellationToken ct = default)
    {
        if (Exception is not null) return Task.FromException<StorageStats>(Exception);
        if (_responses.Count > 0) return Task.FromResult(_responses.Dequeue());
        return Task.FromResult(new StorageStats());
    }

    // Unused members throw NotSupportedException to signal test gaps
    public Task SetAsync<T>(string key, T value, CancellationToken ct = default)
        => throw new NotSupportedException();
}

// In .cs:
[Test]
public async Task GetMetrics_ReturnsStats_WhenStorageAvailable()
{
    using var scope = new TestScope();
    scope.Storage.QueueResponse(new StorageStats { TotalItems = 42 });
    var result = await scope.Service.GetMetricsAsync();
    await Assert.That(result.TotalItems).IsEqualTo(42);
}
```

### Pattern 3: DI TestScope (for classes with many dependencies)

Real `ServiceCollection` with stub registrations for boundaries.

```csharp
// In .Helpers.cs:
public sealed class TestScope : IDisposable
{
    private readonly ServiceCollection _services = new();
    public ServiceProvider ServiceProvider { get; private set; } = default!;

    public TestScope WithServices()
    {
        _services.AddSingleton<ILogger<MyService>>(new Logger_Spy<MyService>());
        _services.AddSingleton<IJSRuntime>(new JSRuntime_Stub());
        _services.AddSingleton<IMyService, MyService>();
        ServiceProvider = _services.BuildServiceProvider();
        return this;
    }

    public void Dispose() => ServiceProvider?.Dispose();
}
```

### Pattern 4: Exclusive Scope (for static mutable state)

SemaphoreSlim to serialize tests that mutate shared static state.
Snapshot on entry, restore on dispose.

```csharp
// In .Helpers.cs:
private static readonly SemaphoreSlim _gate = new(1, 1);

private static async Task<ConfigScope> EnterExclusiveScopeAsync()
{
    await _gate.WaitAsync().ConfigureAwait(false);
    return new ConfigScope(MyConfig.Value); // snapshot current state
}

public sealed class ConfigScope : IDisposable
{
    private readonly int _originalValue;

    public ConfigScope(int original) { _originalValue = original; }

    public void Dispose()
    {
        MyConfig.Value = _originalValue;
        _gate.Release();
    }
}

// In .cs:
[Test]
public async Task ShouldDisplayComponent_ReturnsFalse_WhenDisabled()
{
    using var scope = await EnterExclusiveScopeAsync();
    MyConfig.IsEnabled = false;
    var result = MyConfig.ShouldDisplayComponent("https://example.com");
    await Assert.That(result).IsFalse();
}
```

## Hand-Rolled Fake Conventions

1. **Name**: `[Interface]_Fake` (e.g., `IBrowserStorageService_Fake`)
2. **Location**: In the `[Class]Tests.Helpers.cs` file
3. **Unused members**: Throw `NotSupportedException` — signals test gaps
4. **Queue pattern**: For methods called multiple times with different
   responses
5. **Exception injection**: `public Exception? MethodNameException { get; set; }`
6. **Capture pattern**: Properties like `LastEvictionTargetSize` for
   verifying arguments

### HttpClient Fake Pattern

When testing Azure Functions or any code that uses `IHttpClientFactory`,
use a `ControlledHttpHandler_Fake` that injects deterministic HTTP
responses with zero real HTTP calls:

```csharp
public sealed class ControlledHttpHandler_Fake : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;

    public ControlledHttpHandler_Fake(
        Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _handler(request);
    }
}

// Wire through IHttpClientFactory:
private sealed class HttpClientFactory_Fake(HttpMessageHandler handler)
    : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
        => new(handler, disposeHandler: false);
}
```

**Key properties:**

- Inject any HTTP response (200, 401, 500) with arbitrary JSON body
- Inject exceptions (`HttpRequestException`, `OperationCanceledException`)
  directly via the handler lambda — no need for exception properties
- Zero external dependencies: no real HTTP, no mock frameworks
- Tests run deterministically even when Raindrop API is unavailable

### Azure Functions Test Project Requirements

API test projects (e.g., `redmuffin.Blazor.StaticWeb.Api.Tests`) **must NOT**
reference `Microsoft.Azure.Functions.Worker.Sdk`. The SDK's
`_FunctionsComputeRunArguments` MSBuild target replaces `dotnet run` with
`func start`, blocking TUnit test execution entirely.

Azure Functions Core Tools (`yay -S azure-functions-core-tools-bin`) must be
installed separately on the development machine for integration tests that
require a real Functions host. Unit tests use `ControlledHttpHandler_Fake` and
do not require Core Tools.

## When to Use LightMock.Generator

Use LightMock when:

- The interface has >3 methods you need to fake with non-trivial behavior
- You need argument matching (`The<string>.IsAnyValue`)
- You need call count verification

```csharp
// LightMock example (use sparingly)
using var mock = new Mock<IService>();
mock.Arrange(f => f.GetItemAsync<T>("namespace", The<string>.IsAnyValue, CancellationToken.None))
    .Returns(Task.FromResult<T?>(default));
```

**Prefer hand-rolled over LightMock** whenever practical. The hand-rolled
fake is debuggable, explicit, and documents the interface contract.
LightMock is a fallback for large interfaces.

## JS Interop Testing

Blazor `IJSRuntime` cannot be called in a headless test runner. Always
stub it:

```csharp
public class JSRuntime_Stub : IJSRuntime
{
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        => ValueTask.FromResult(default(TValue)!);

    public ValueTask<TValue> InvokeAsync<TValue>(
        string identifier, CancellationToken ct, object?[]? args)
        => ValueTask.FromResult(default(TValue)!);
}
```

For tests that need actual JS interop behavior, use `bUnit` integration
tests (not covered here).

## Logger in Tests

Use `Logger_Spy<T>` to capture log output when testing logging behavior.
When the test doesn't verify logging, a no-op stub is fine.

```csharp
public class Logger_Spy<T> : ILogger<T>
{
    public List<LogEntry> LogEntries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId,
        TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        LogEntries.Add(new LogEntry(logLevel, eventId,
            formatter(state, exception), exception));
    }
}

public record LogEntry(LogLevel Level, EventId EventId,
    string Message, Exception? Exception);
```

## Test Quality Checklist

Before any test is complete, verify:

- [ ] AAA pattern: Arrange, Act, Assert clearly separated
- [ ] One logical concept per test (may have multiple assertions for same concept)
- [ ] No mocking of internal collaborators (only boundaries)
- [ ] Pure functions tested directly (no doubles)
- [ ] Test double in `.Helpers.cs`, not inlined in test
- [ ] AAA comments (`// Arrange`, `// Act`, `// Assert`)
- [ ] Test name follows `Method_Scenario_ExpectedBehavior` pattern
- [ ] No magic values — use factory methods for test data

## What NOT to Do

- Do NOT use Moq or NSubstitute (they don't work under WASM/AOT)
- Do NOT put test doubles in standalone files
- Do NOT mock interfaces that exist only for testing
- Do NOT write coverage-only tests with no assertions
- Do NOT use FluentAssertions (use TUnit built-in assertions)
- Do NOT use `InternalsVisibleTo` to test private methods
  (extract to public static instead)
- Do NOT reference `Microsoft.Azure.Functions.Worker.Sdk` in API test projects
  (it hijacks `dotnet run` and blocks test execution)
- Do NOT assume single-test-project coverage — when the solution has both
  frontend and API test projects, coverage must be generated per-project and
  merged via `CoberturaMerger` before CRAP analysis

## References

- `.opencode/skills/redmuffin-standards/rm-guide-naming/SKILL.md` —
  naming conventions for test doubles
- `.opencode/skills/redmuffin-standards/rm-guide-cleanup/SKILL.md` —
  characterization tests and code quality standards
- `.opencode/skills/redmuffin-standards/rm-gates-cleanup/SKILL.md` —
  mutation testing workflow
- `.opencode/skills/rm-tdd/SKILL.md` — TDD red-green-refactor workflow
- `docs/pure-function-extraction-testing-guide-2026-05-10.md` —
  pure function extraction pattern
