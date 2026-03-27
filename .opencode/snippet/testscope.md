---
aliases: [csharp-testing, testscope]
description: TestScope architecture and TUnit + LightMock testing patterns
---
MANDATORY: TUnit + LightMock TestScope Architecture Pattern

## Required Structure (ALL Test Classes)
```csharp
public sealed class TestScope(string baseUri = "http://localhost:5000/") : IDisposable
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

## Required Mock Implementations
```csharp
// HTTP Client Factory
public sealed class TestHttpClientFactory(Func<HttpMessageHandler> handlerFactory) : IHttpClientFactory
{
    public static TestHttpClientFactory Mock { get; } = new(() => new HttpMessageHandlerMock());
    public static TestHttpClientFactory Failing { get; } = new(() => new FailingHttpMessageHandler());
    public static TestHttpClientFactory Timeout { get; } = new(() => new TimeoutHttpMessageHandler());
}

// Test Logger
public class TestLogger<T> : ILogger<T>
{
    public List<LogEntry> LogEntries { get; } = [];
    public class LogEntry { LogLevel LogLevel; EventId EventId; string Message; Exception? Exception; }
}
```

## LightMock Critical: Optional Parameters
ALWAYS specify ALL parameters explicitly:
```csharp
// ❌ FAILS: CS0854
_mock.Arrange(f => f.GetAsync("key")).Returns(...);

// ✅ WORKS
_mock.Arrange(f => f.GetAsync("key", CancellationToken.None)).Returns(...);
```

## TUnit Fluent Chaining Rules
```csharp
// ✅ CORRECT: Chain related assertions
await Assert.That(component.Markup).IsNotNull().And.Contains("expected").And.Contains("more");

// ❌ WRONG: Separate assertions on same object
using (Assert.Multiple())
{
    await Assert.That(component.Markup).IsNotNull();
    await Assert.That(component.Markup).Contains("text");
}
```

## Quality Standards
- ConfigureAwait(false) on ALL async calls
- AAA pattern with comments
- Zero build warnings compliance
- Resource disposal via using statements