---
name: rm-testing
description: "Shortcut: rm:test. TUnit testing patterns, TestScope architecture, test categorization, mocking strategies, and TUnit analyzer rules. Use when writing tests, creating TestScopes, mocking with LightMock, or categorizing test files."
---

# Testing

## Framework

- TUnit with `[Test]` and `[Arguments]`
- NEVER use xUnit, NUnit, or MSTest
- Name tests: `Component_Behavior_ExpectedOutcome`

## TUnit Attributes

### Lifecycle Hooks

- `[Before(Test)]` / `[After(Test)]` for per-test setup/teardown
- `[Before(Class)]` / `[After(Class)]` for class-level setup/teardown

### Data-Driven Tests

- `[Arguments]` = inline data
- `[MethodData]` = method-based data
- `[ClassData]` = class-based data

### Advanced Attributes

- `[Repeat(n)]` — run test n times
- `[Retry(n)]` — retry up to n times on failure
- `[Skip("reason")]` — skip test with explanation
- `[NotInParallel]` — disable parallel execution for this test
- `[Timeout(milliseconds)]` — set test timeout

## Assertions

- Use: `await Assert.That(value).IsEqualTo(expected)`
- Chain related assertions: `.And`, `.Or`, `.Within(tolerance)`
- All assertions are async — must await
- **Correct**: `await Assert.That(component.Markup).IsNotNull().And.Contains("expected")`
- **Wrong**: Separate assertions on same object with `Assert.Multiple()`

## Test Quality Checklist

- Use `ConfigureAwait(false)` on async calls (except asserts)
- Use TestScope with fluent configuration
- Follow AAA structure
- Ensure single responsibility
- Comply with zero build warnings
- Use `using` for resource disposal
- Test error scenarios
- Follow partial class structure

## Minimal Component Tests

Write only essential TUnit tests for simple components (e.g., buttons). Avoid overengineering with excessive or redundant tests.

## Test Categorization Rules

Organize TUnit tests into partial class files for each test class (e.g., `HomeTests`) to enhance maintainability.

### File Structure

| File                            | Purpose                             |
| ------------------------------- | ----------------------------------- |
| `[TestClass].cs`                | Basic functionality tests           |
| `[TestClass].EdgeCases.cs`      | Error handling and edge case tests  |
| `[TestClass].Infrastructure.cs` | Framework and system-level tests    |
| `[TestClass].Behavior.cs`       | User interaction and workflow tests |
| `[TestClass].Helpers.cs`        | TestScope, mocks, utilities         |

### Decision Flow

1. **[TestClass].EdgeCases.cs**:
   - Test name includes: `Error`, `Exception`, `Fail`, `Invalid`, `Null`, `Empty`, `Timeout`, `Malformed`, `Corrupt`
   - Uses: `Assert.Throws`, `ThrowsAsync`, `SetException`, `HttpRequestException`, `InvalidOperationException`
   - Setup includes: `CreateFailing*`, `WithFailing*`, `SetupFailure`, `SetupException`, `SetupThrows`
   - Validates: Error messages, exception handling, fallback behavior, graceful degradation
   - Inputs: Null values, empty collections, invalid data, extreme values

2. **[TestClass].Infrastructure.cs**:
   - Test name includes: `Lifecycle`, `Logging`, `Cache`, `Auth`, `DI`, `JSInterop`, `Serializ`, `Disposal`, `Memory`, `Event`
   - Validates: `OnInitialized`, `OnParametersSet`, `OnAfterRender`, `StateHasChanged`, `Dispose`
   - Checks: Log entries, event IDs, authentication state, dependency injection, JS calls
   - Uses: `CascadingValue`, `AuthenticationState`, `JSInterop`, `LocalStorage`, cache services

3. **[TestClass].Behavior.cs**:
   - Test name includes: `Click`, `Submit`, `Change`, `Interaction`, `Workflow`, `Concurrent`, `Multiple`, `Rapid`
   - Uses: `ClickAsync`, `ChangeAsync`, `TriggerEventAsync`, `MouseEventArgs`, `ChangeEventArgs`
   - Performs: User interactions, form submissions, button clicks, input changes
   - Validates: State transitions, user workflows, interactive behavior

4. **[TestClass].cs** (Default):
   - Covers: Basic rendering, simple property validation, "happy path" scenarios, structure verification, default state validation

### Override Rule

If a test fits multiple categories, prioritize: 1. EdgeCases, 2. Infrastructure, 3. Behavior, 4. Main

### Code Structure

```csharp
namespace Tests.[ProjectName].Features;
partial class HomeTests
{
    // Test methods for specific category
}
```

### Examples

- `HomeTests.EdgeCases.cs`: `Should_Handle_Null_Input_Gracefully`, `Should_Throw_ArgumentException_When_Invalid`
- `HomeTests.Infrastructure.cs`: `Should_Log_Initialization_Events`, `Should_Dispose_Resources_Properly`
- `HomeTests.Behavior.cs`: `Should_Submit_Form_When_Button_Clicked`, `Should_Handle_Concurrent_Operations`
- `HomeTests.cs`: `Should_Render_Successfully`, `Should_Display_Correct_Title`

## TestScope Architecture

Use `TestScope` with primary constructor, fluent methods, and `IDisposable`

```csharp
public sealed class TestScope(string baseUri = "http://localhost:5233/") : IDisposable
{
    public TestContext Context { get; } = new();
    public NavigationManagerMock NavigationManager { get; } = new(baseUri);
    public TestLogger<T> Logger { get; } = new();

    public TestScope WithStandardServices()
    {
        Context.Services.AddSingleton<NavigationManager>(NavigationManager);
        Context.Services.AddSingleton<ILogger<T>>(Logger);
        Context.Services.AddSingleton<IHttpClientFactory>(TestHttpClientFactory.Mock);
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        return this;
    }

    public void Dispose() => Context?.Dispose();
}

// Usage:
using var scope = new TestScope("http://localhost:3000/").WithStandardServices();
```

### Required Mock Implementations

```csharp
// HTTP Client Factory — three variants for different test scenarios
public sealed class TestHttpClientFactory(Func<HttpMessageHandler> handlerFactory) : IHttpClientFactory
{
    public static TestHttpClientFactory Mock { get; } = new(() => new HttpMessageHandlerMock());
    public static TestHttpClientFactory Failing { get; } = new(() => new FailingHttpMessageHandler());
    public static TestHttpClientFactory Timeout { get; } = new(() => new TimeoutHttpMessageHandler());
}

// Test Logger — captures log entries for assertion
public class TestLogger<T> : ILogger<T>
{
    public List<LogEntry> LogEntries { get; } = [];
    public class LogEntry { public LogLevel LogLevel; public EventId EventId; public string Message; public Exception? Exception; }
}
```

- Use TUnit's `TestContext` for debug output in tests to ensure visibility in test output

## TUnit Analyzer (CRITICAL)

TUnit has a built-in analyzer that catches the #1 pitfall: **missing `await` on assertions**. Without `await`, assertions pass silently without executing.

```csharp
// WRONG - Test passes but assertion never runs!
// TUnit.Analyzer warning: "Await the assertion"
Assert.That(result, Is.EqualTo(expected));

// CORRECT - Assertion executes properly
await Assert.That(result).Is.EqualTo(expected);
```

Enable in test project:

```xml
<ItemGroup>
  <PackageReference Include="TUnit.Analyzer" PrivateAssets="all" />
</ItemGroup>
```

## Mocking Strategy

### LightMock.Generator

For external dependencies. Use `Mock` suffix, setup with `.Arrange()`, pass `.Object`

```csharp
var httpClientMock = new Mock<IHttpClientFactory>();
httpClientMock.Arrange(f => f.CreateClient(The<string>.IsAnyValue)).Returns(new HttpClient());
```

### Custom Mocks

For internal components. Use sealed classes with primary constructors

```csharp
public sealed class NavigationManagerMock(string baseUri) : NavigationManager
{
    public string? NavigatedTo { get; private set; }
    protected override void NavigateToCore(string uri, NavigationOptions options) => NavigatedTo = uri;
}
```

### Optional Parameters

Specify all parameters explicitly (e.g., `CancellationToken.None`, `null`, `The<T>.IsAnyValue`)
