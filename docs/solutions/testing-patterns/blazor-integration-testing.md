---
date: 2026-04-03
title: "Blazor Integration Testing with TUnit, bUnit, and TestScope Pattern"
tags: [blazor, integration-testing, tunit, bunit, testscope, mocking]
problem_type: testing-pattern
---

## Problem

The Blazor project needed exemplary integration and unit tests that demonstrate best practices: TDD, proper mocking, code-behind enforcement, bUnit component testing, AoT compatibility, and a reusable TestScope architecture. Existing tests were ad-hoc with inconsistent patterns.

## Root Cause

No standardized testing pattern existed. Test infrastructure (mocks, service registrations, resource disposal) was duplicated across test methods. There was no enforcement of code-behind over inline `@code` blocks.

## Solution

Implemented a comprehensive testing architecture centered on the **TestScope pattern**:

### TestScope Architecture

A sealed, IDisposable inner class using C# 13 primary constructors, encapsulating all test resources:

```csharp
public sealed class TestScope(string baseUri = "http://localhost:5000/") : IDisposable
{
    public TestContext Context { get; } = new();
    public NavigationManagerMock NavigationManager { get; } = new(baseUri);
    public TestLogger<HomePage> Logger { get; } = new();
}
```

- **Fluent configuration**: `WithStandardServices()`, `WithFailingHttpClient()`, `WithThrowingNavigation()`, `WithJSInterop(mode)`
- **Factory methods**: `CreateTestScope()`, `CreateFailingHttpTestScope()`
- **Automatic disposal**: `using var scope` pattern ensures cleanup

### Key Testing Patterns

1. **TUnit fluent chaining** — chain related assertions on the same object with `.And`; use `Assert.Multiple()` only for unrelated concerns
2. **Mock implementations** — dedicated mock classes for `NavigationManager`, `IHttpClientFactory` (with Mock/Failing/Timeout variants), and `ILogger<T>` with structured `LogEntry` tracking
3. **Cascading parameters** — always use lambda functions for `AddCascadingValue`, never direct values; use separate `TestScope` instances for different scenarios
4. **Authorization testing** — `MockClaimsIdentity` with proper `ClaimsIdentity` inheritance; wrap authentication state awaits in try-catch to prevent `VSTHRD003` warnings
5. **Code-behind enforcement** — `BlazorCodeBehindEnforcementTests` scans all `.razor` files for inline `@code` blocks using regex patterns

### Conditional AoT Configuration

AoT is controlled by `RunAOTCompilation` conditions in `tests/redmuffin.Blazor.StaticWeb.Tests.csproj`. It is enabled in CI/CD (`CI=true` or `GITHUB_ACTIONS=true`) and disabled locally by default. Set `$env:AOT_TESTS='true'` before build for optional local AoT parity.

Run tests with TUnit's native host (same as CI):

```powershell
dotnet build -c Release
dotnet run --project tests/redmuffin.Blazor.StaticWeb.Tests -c Release --no-build
dotnet run --project tests/redmuffin.Blazor.StaticWeb.Api.Tests -c Release --no-build
```

### Test File Organization

```
Tests:
├── TestClassName.cs          // [Test] methods ONLY
└── TestClassName.Helpers.cs  // TestScope, mocks, utilities, factory methods
```

## Prevention

- All new test classes must follow the TestScope pattern
- `ConfigureAwait(false)` on all async calls (zero warnings compliance)
- Descriptive test names: `Component_Behavior_ExpectedOutcome`
- Clear Arrange-Act-Assert structure with comments
- Run `BlazorCodeBehindEnforcementTests` in CI/CD to prevent inline `@code` blocks
