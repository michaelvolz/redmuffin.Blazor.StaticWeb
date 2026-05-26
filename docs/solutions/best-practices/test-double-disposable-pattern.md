---
title: Test Double Disposable Pattern for CA2001 Compliance
date: 2026-04-06
category: best-practices
module: Api.Tests
problem_type: best_practice
component: testing_framework
severity: medium
applies_when:
  - Creating test doubles that own disposable resources (MemoryStream, HttpClient, etc.)
  - Building mock classes for Azure Functions testing
  - Implementing TestScope pattern with disposable fields
tags:
  - testing
  - tunit
  - azure-functions
  - code-quality
  - ca2001
---

# Test Double Disposable Pattern for CA2001 Compliance

## Context

When creating test doubles (mocks, stubs, fakes) for Azure Functions testing, the mock classes often own disposable resources like `MemoryStream` for request/response bodies. Without implementing `IDisposable`, these classes trigger CA2001 ("Call System.IDisposable.Dispose on object") and CA1001 ("Type owns disposable field(s) but is not disposable") warnings, violating the project's zero-warning policy.

## Guidance

All test doubles that own disposable resources **must implement `IDisposable`**:

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

**Usage pattern in tests:**

```csharp
// ✅ CORRECT: Using statement ensures disposal
using var request = TestScope.CreateHttpRequestData(functionContext, requestBody);

// Test code here...

// request.Dispose() called automatically at end of scope
```

## Why This Matters

1. **Zero-warning policy**: The project requires zero build warnings. CA2001/CA1001 violations break this policy.
2. **Resource management**: Proper disposal prevents memory leaks in test suites that create many mock objects.
3. **Pattern consistency**: All test doubles follow the same disposable pattern, making the codebase predictable.
4. **Analyzer compliance**: StyleCop/Meziantou analyzers enforce these rules.

## When to Apply

- Any test double class that has a `IDisposable` field (e.g., `MemoryStream`, `HttpClient`, `Timer`)
- Mock classes for Azure Functions `HttpRequestData` and `HttpResponseData`
- TestScope classes that create disposable resources
- Any class in a `.Helpers.cs` file that owns disposable fields

## Examples

### Before: Missing IDisposable

```csharp
public sealed class HttpRequestData_Mock : HttpRequestData
{
    private readonly MemoryStream _bodyStream = new();

    public HttpRequestData_Mock(FunctionContext functionContext, object? body = null) : base(functionContext)
    {
        // ... initialization
    }

    public override Stream Body => _bodyStream;
    // CA2001: Call System.IDisposable.Dispose on object
    // CA1001: Type owns disposable field(s) but is not disposable
}
```

### After: Proper IDisposable Implementation

```csharp
public sealed class HttpRequestData_Mock : HttpRequestData, IDisposable
{
    private readonly MemoryStream _bodyStream;

    public HttpRequestData_Mock(FunctionContext functionContext, object? body = null) : base(functionContext)
    {
        _bodyStream = body != null
            ? new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(body)))
            : new MemoryStream();
    }

    public override Stream Body => _bodyStream;

    public void Dispose()
    {
        _bodyStream.Dispose();
    }
}
```

### Test Usage

```csharp
[Category("Feature:Api")]
public sealed partial class ExchangeRaindropCodeFunction_Tests
{
    [Test]
    public async Task Should_Return_BadRequest_When_Code_Is_Missing()
    {
        // Arrange
        using var scope = CreateTestScope();
        var functionContext = TestScope.CreateFunctionContext(nameof(ExchangeRaindropCodeFunction));
        var requestBody = new ExchangeRequest { Code = "", RedirectUri = "http://localhost" };
        using var request = TestScope.CreateHttpRequestData(functionContext, requestBody);

        // Act & Assert
        // ... test code ...
    }
}
```

## Related

- `docs/TestingGuidelines.md` - Central testing guidelines
- `docs/TestDoubleBestPractices.md` - Test double naming conventions
- `tests/redmuffin.Blazor.StaticWeb.Api.Tests/Functions/ExchangeRaindropCodeFunction_Tests.Helpers.cs` - Reference implementation
