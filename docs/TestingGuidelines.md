# Testing Guidelines

## Overview

This document provides comprehensive testing guidelines for the redmuffin.Blazor.StaticWeb project, including test double standards, naming conventions, and best practices.

## Test Double Standards

### Naming Convention

All test doubles must follow the pattern: `[ClassName]_[Type]` where `[Type]` is one of:
- `Mock` - For behavior verification
- `Stub` - For state verification with predefined responses
- `Spy` - For recording interactions while maintaining real functionality
- `Fake` - For simple working implementations
- `Dummy` - For placeholders that satisfy parameter requirements

### Strategic Approach

#### LightMock.Generator (External Dependencies)
Use for 3rd party and external dependencies:
- `IHttpClientFactory`
- `ILocalStorageService`
- `ILogger<T>`
- External APIs
- Azure services

#### Custom Mocks (Internal Components)
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

## Organizational Standards

### Partial Class Structure
- **Main test file**: `[TestClass].cs` - Contains only `[Test]` methods
- **Helper file**: `[TestClass].Helpers.cs` - Contains TestScope, mocks, and utilities
- **CRITICAL**: All test helpers must be in corresponding partial files, never separate helper files

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

## Framework Standards

### TUnit Usage
- Use `[Test]` attribute for test methods
- Use `[Arguments]` for data-driven tests
- Follow TUnit fluent chaining for related assertions
- Use `Assert.Multiple` for unrelated concerns

### Code Quality
- **Zero build warnings policy** (except IL2111)
- `ConfigureAwait(false)` on all awaits (except at end of assert statements)
- Follow StyleCop/Meziantou analyzer rules
- Use C# 13 patterns and modern syntax

## Compliance Checklist

Before committing any test changes:
- [ ] Test double naming follows `[Class]_[Type]` convention
- [ ] Strategic approach used (LightMock vs Custom)
- [ ] TestScope architecture implemented
- [ ] Partial class organization followed
- [ ] Zero build warnings achieved
- [ ] All helpers placed in corresponding partial files
- [ ] TUnit standards followed
- [ ] ConfigureAwait(false) properly applied

## References

- [Test Double Best Practices](TestDoubleBestPractices.md)
- [PRD-011 Standardizing Test Doubles](../tasks/PRD-011-StandardizingTestDoubles.md)
