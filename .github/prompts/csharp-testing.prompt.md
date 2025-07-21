---
mode: 'agent'
tools: ['changes', 'codebase', 'editFiles', 'problems', 'search']
description: 'Combined best practices for TUnit unit testing and LightMock mocking with TestScope architecture'
---

# TUnit + LightMock Testing

## Setup
- Test project: `[ProjectName].Tests`
- Packages: TUnit, TUnit.Assertions, LightMock.Generator, bUnit (for Blazor)
- Test classes match tested classes: `CalculatorTests` for `Calculator`
- Requires .NET 8+

## Test Structure
- `[Test]` attribute (not `[Fact]`)
- Arrange-Act-Assert pattern
- Naming: `Component_Behavior_ExpectedOutcome`
- Lifecycle: `[Before(Test)]`/`[After(Test)]`, `[Before(Class)]`/`[After(Class)]`

## Core Features
- Single behavior per test, independent/idempotent
- Data-driven: `[Arguments]`, `[MethodData]`, `[ClassData]`
- Fluent assertions: `await Assert.That(value).IsEqualTo(expected)`
- Chaining: `.And`, `.Or`, `.Within(tolerance)`
- Advanced: `[Repeat(n)]`, `[Retry(n)]`, `[Skip("reason")]`, `[Timeout(ms)]`
- Parallel by default, use `[NotInParallel]` to disable

## Key Assertions
- Equality: `await Assert.That(value).IsEqualTo(expected)`
- Booleans: `await Assert.That(value).IsTrue()`
- Collections: `await Assert.That(collection).Contains(item)`
- Exceptions: `await Assert.That(action).Throws<TException>()`
- All assertions are async and must be awaited

## MANDATORY: TUnit Fluent Chaining Patterns

### Golden Rules
**✅ USE CHAINING FOR**: Same object/property assertions, logically sequential validations
**⚠️ USE Assert.Multiple FOR**: Different objects, unrelated concerns, multiple failure reporting

### Prime Examples (COPY THESE)
```csharp
// ✅ OPTIMAL: Chain related assertions on same object
await Assert.That(component.Markup).IsNotNull().And.Contains("expected").And.Contains("more");

// ✅ OPTIMAL: Use Assert.Multiple for unrelated concerns
using (Assert.Multiple())
{
    await Assert.That(component.Find("h1")).IsNotNull();  // DOM structure
    await Assert.That(scope.Logger.LogEntries.Any(entry => entry.Message.Contains("logged"))).IsTrue();  // Logging
}
```

### Anti-Patterns (NEVER DO)
```csharp
// ❌ DON'T: Separate assertions on same object
using (Assert.Multiple())
{
    await Assert.That(component.Markup).IsNotNull();
    await Assert.That(component.Markup).Contains("text");
}
```

## MANDATORY: TestScope Architecture Pattern

### Required Structure (ALL Test Classes)
```csharp
/// <summary>
///     Modern test scope that encapsulates all test resources with automatic disposal.
///     Uses C# 13 primary constructor pattern for clean, professional resource management.
/// </summary>
public sealed class TestScope(string baseUri = "http://localhost:5000/") : IDisposable
{
    public TestContext Context { get; } = new();
    public NavigationManagerMock NavigationManager { get; } = new(baseUri);
    public TestLogger<T> Logger { get; } = new();

    /// <summary>
    ///     Configures the test context with standard services for normal testing scenarios.
    /// </summary>
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

// Factory methods for common scenarios
private static TestScope CreateTestScope() => new TestScope().WithStandardServices();
```

### Required Mock Implementations
```csharp
// HTTP Client Factory with multiple scenarios
public sealed class TestHttpClientFactory(Func<HttpMessageHandler> handlerFactory) : IHttpClientFactory
{
    public static TestHttpClientFactory Mock { get; } = new(() => new HttpMessageHandlerMock());
    public static TestHttpClientFactory Failing { get; } = new(() => new FailingHttpMessageHandler());
    public static TestHttpClientFactory Timeout { get; } = new(() => new TimeoutHttpMessageHandler());
}

// Test Logger with structured logging
public class TestLogger<T> : ILogger<T>
{
    public List<LogEntry> LogEntries { get; } = [];
    
    public class LogEntry
    {
        public LogLevel LogLevel { get; set; }
        public EventId EventId { get; set; }
        public string Message { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
    }
}
```

## LightMock Integration
### Naming Convention
```csharp
// ✅ Use Mock suffix
private readonly Mock<IUserService> _userServiceMock;
private readonly Mock<ILogger<UserService>> _loggerMock;

// ❌ Avoid
private readonly Mock<IUserService> _userService; // unclear
private readonly Mock<IUserService> _fakeUserService; // inconsistent
```

### Critical: Optional Parameters (CS0854 Fix)
**ALWAYS specify ALL parameters explicitly for interfaces with optional parameters:**
```csharp
// ❌ FAILS: CS0854 error
_mock.Arrange(f => f.GetAsync("key")).Returns(Task.FromResult(data));

// ✅ WORKS: Explicit parameters
_mock.Arrange(f => f.GetAsync("key", CancellationToken.None)).Returns(Task.FromResult(data));
_mock.Arrange(f => f.SetAsync("key", data, null, CancellationToken.None)).Returns(Task.CompletedTask);
```
**Pattern**: Use `CancellationToken.None`, `null`, `The<T>.IsAnyValue` for optional params

## MANDATORY: Quality Standards

### Zero Warnings Compliance
- `ConfigureAwait(false)` on ALL async calls
- Comprehensive XML documentation
- Modern C# 13 patterns (primary constructors, collection expressions)
- Professional test naming: `Component_Behavior_ExpectedOutcome`

### Test Implementation Checklist
**Before committing ANY test:**
- [ ] ConfigureAwait(false) on all async calls
- [ ] TestScope pattern with fluent configuration
- [ ] TUnit chaining for related assertions
- [ ] Clear AAA structure with comments
- [ ] Single responsibility principle
- [ ] Zero build warnings compliance
- [ ] Resource disposal via using statements

## Usage Pattern
```csharp
[Test]
public async Task Component_Behavior_ExpectedOutcome()
{
    // Arrange
    using var scope = new TestScope("http://localhost:3000/")
        .WithStandardServices()
        .WithFailingHttpClient();
    
    // Act
    var component = scope.Context.RenderComponent<MyComponent>();
    await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);
    
    // Assert - Use chaining for related assertions
    await Assert.That(component.Markup).IsNotNull().And.Contains("expected");
}
```

## xUnit Migration
- `[Fact]` → `[Test]`
- `[Theory]` → `[Test]` + `[Arguments]`
- `[InlineData]` → `[Arguments]`
- `Assert.Equal` → `await Assert.That(actual).IsEqualTo(expected)`
- `Assert.True` → `await Assert.That(condition).IsTrue()`
- Constructor/IDisposable → TestScope pattern

**Benefits**: TUnit provides async assertions, refined lifecycle hooks, parallel execution. TestScope provides clean resource management, flexible service configuration, and comprehensive error testing capabilities.
