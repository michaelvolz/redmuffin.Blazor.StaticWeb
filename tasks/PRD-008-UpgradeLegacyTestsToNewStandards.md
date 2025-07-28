# PRD-008: Upgrade Legacy Tests to New Standards

## Introduction/Overview

This PRD outlines the systematic upgrade of all legacy test files located outside the `NewTests` folders to align with current TUnit testing standards, modern C# 13 patterns, and established testing principles. The goal is to achieve consistency, maintainability, and adherence to the project's zero build warnings policy while ensuring all tests validate behavior rather than implementation.

## Goals

1. **Standardize Testing Architecture**: Migrate all legacy tests to use the TestScope pattern with partial class organization
2. **Improve Test Quality**: Ensure tests focus on behavior validation rather than implementation details
3. **Achieve Zero Build Warnings**: Eliminate all build warnings across all test files
4. **Modernize Code Patterns**: Apply C# 13 features and TUnit best practices throughout the test suite
5. **Maintain Test Coverage**: Preserve or improve existing test coverage during the upgrade process
6. **Establish Consistent Mocking Strategy**: Use custom mocks for internal dependencies and LightMock.Generator only for external dependencies

## User Stories

- **As a developer**, I want all tests to follow consistent patterns so that I can easily understand and maintain them
- **As a developer**, I want tests to validate behavior rather than implementation so that refactoring doesn't break tests unnecessarily
- **As a team member**, I want zero build warnings in tests so that real issues are not hidden by noise
- **As a maintainer**, I want clear separation between test logic and test infrastructure so that changes are easier to implement
- **As a contributor**, I want modern C# patterns in tests so that the codebase stays current with best practices

## Functional Requirements

1. **Legacy Test Identification**
   - The system must identify all test files outside `NewTests` folders in both test projects
   - Legacy test files include:
     - `tests/redmuffin.Blazor.StaticWeb.Tests/CodeQuality/BlazorCodeBehindEnforcementTests.cs`
     - `tests/redmuffin.Blazor.StaticWeb.Tests/CodeQuality/BlazorCodeBehindEnforcementTests.Helpers.cs`
     - `tests/redmuffin.Blazor.StaticWeb.Tests/Core/StringExtensionsTests.cs`
     - `tests/redmuffin.Blazor.StaticWeb.Api.Tests/Functions/ArticlesApiVerification_Tests.cs`
     - `tests/redmuffin.Blazor.StaticWeb.Api.Tests/Functions/RaindropListArticles_Tests.cs`
     - `tests/redmuffin.Blazor.StaticWeb.Api.Tests/Functions/RaindropListVideos_Tests.cs`
     - `tests/redmuffin.Blazor.StaticWeb.Api.Tests/TestDeserialization.cs`
     - All helper files in `tests/redmuffin.Blazor.StaticWeb.Api.Tests/Helpers/`

2. **Test Upgrade Process**
   - Each test file must be processed individually until fully upgraded before moving to the next
   - Tests must be analyzed to ensure they validate behavior, not implementation
   - Tests focused on private/protected methods must be removed or refactored to test public interfaces
   - New behavior-focused tests must be added if coverage gaps are identified
   - Upgraded tests must be placed in appropriate `NewTests` folder maintaining existing hierarchy

3. **Architecture Compliance**
   - All upgraded tests must use the TestScope pattern with C# 13 primary constructors
   - Tests must follow partial class organization: main file for `[Test]` methods, `.Helpers.cs` for infrastructure
   - Custom mocks must be used for internal dependencies (NavigationManager, internal services)
   - LightMock.Generator must only be used for external dependencies (IHttpClientFactory, ILogger, external APIs)
   - TUnit fluent chaining must be used for related assertions on the same object
   - `Assert.Multiple()` must be used for unrelated concerns

4. **Code Quality Standards**
   - All async calls must use `ConfigureAwait(false)` except on Assert statements
   - Test naming must follow `Component_Behavior_ExpectedOutcome` pattern
   - Clear Arrange-Act-Assert structure with comments
   - Zero build warnings compliance
   - Resource disposal via using statements
   - Single responsibility principle per test method

5. **Post-Upgrade Actions**
   - Original legacy files must be renamed with `.outdated` suffix to exclude from test system
   - All tests must pass (green status) before proceeding to next file
   - Build warnings must be resolved before proceeding to next file

## Non-Goals (Out of Scope)

- Creating new test abstractions or base classes
- Modifying test project configurations (already fully configured)
- Adding new testing frameworks or dependencies
- Changing existing test project structure or organization
- Modifying code outside of test projects
- Creating integration tests for untested functionality
- Performance optimization of test execution

## Design Considerations

### TestScope Pattern Implementation

All upgraded tests must implement the TestScope pattern following the established HomeTests examples:

```csharp
public sealed class TestScope(string baseUri = "http://localhost:5000/") : IDisposable
{
    public BunitContext BUnitContext { get; } = new();
    public NavigationManagerMock NavigationManager { get; } = new(baseUri);
    public TestLogger<T> Logger { get; } = new();

    public TestScope WithStandardServices() { /* setup */ return this; }
    public TestScope WithFailingHttpClient() { /* setup */ return this; }

    public void Dispose() => BUnitContext?.Dispose();
}
```

### Partial Class Organization

- **Main Test File**: Contains only `[Test]` methods with clear AAA structure
- **Helpers File**: Contains TestScope, custom mocks, factory methods, and utilities
- **Naming Convention**: `TestClassName.cs` and `TestClassName.Helpers.cs`

### Mocking Strategy

- **Custom Mocks**: For NavigationManager, internal services, Blazor components
- **LightMock.Generator**: Only for IHttpClientFactory, ILogger, external APIs
- **Mock Naming**: Use `Mock` suffix for all mock objects

## Technical Considerations

### Blazor WebAssembly .NET 9 Integration

- Tests must be compatible with WebAssembly build optimizations
- Component tests must use BUnit with proper service registration
- Navigation tests must use custom NavigationManagerMock
- HTTP client tests must use TestHttpClientFactory patterns

### TUnit Framework Requirements

- Use `[Test]` and `[Arguments]` attributes (NOT xUnit/NUnit/MSTest)
- Implement TUnit fluent chaining for related assertions
- Use `Assert.Multiple()` for unrelated concerns
- Follow functional style with no state or startup/teardown methods

### Azure Functions Testing

- API tests must use TestFunctionContext and related helpers
- HTTP request/response testing must use TestHttpRequestData/TestHttpResponseData
- Function isolation worker patterns must be maintained

### Build and Quality Standards

- Zero build warnings policy enforcement
- StyleCop/Meziantou analyzer compliance
- Modern C# 13 pattern usage
- Proper async/await patterns with ConfigureAwait(false)

## Success Metrics

1. **Test Migration Completion**: 100% of legacy test files successfully upgraded and moved to NewTests folders
2. **Build Quality**: Zero build warnings across all test projects
3. **Test Execution**: All upgraded tests pass consistently
4. **Code Coverage**: Maintain or improve existing test coverage percentages
5. **Pattern Compliance**: 100% adherence to TestScope and partial class patterns
6. **Mocking Strategy**: Correct usage of custom mocks vs LightMock.Generator based on dependency type

## Testing Guidelines from copilot-instructions.md

### MANDATORY: Partial Class Organization Standards

#### TEST CLASSES: Split by Concern

**Pattern**: `TestClassName.cs` ([Test] methods ONLY) + `TestClassName.Helpers.cs` (TestScope, mocks, utilities)

```csharp
// HomeTests.cs - [Test] methods ONLY
public partial class HomeTests
{
    [Test]
    public async Task Home_ButtonClick_LogsExpectedEvent()
    {
        // Arrange
        using var scope = CreateTestScope();
        var component = scope.Context.RenderComponent<HomePage>();

        // Act & Assert
        await button.ClickAsync(new MouseEventArgs()).ConfigureAwait(false);
        await Assert.That(scope.Logger.LogEntries.Any(entry =>
            entry.Message.Contains("Button clicked"))).IsTrue();
    }
}

// HomeTests.Helpers.cs - Infrastructure ONLY
public partial class HomeTests
{
    public sealed class TestScope(string baseUri = "http://localhost:5000/") : IDisposable
    {
        public TestContext Context { get; } = new();
        public NavigationManagerMock NavigationManager { get; } = new(baseUri);
        public TestLogger<HomePage> Logger { get; } = new();
        // TestScope infrastructure...
    }

    private static TestScope CreateTestScope() => new TestScope().WithStandardServices();
    // Helper methods, mocks, utilities...
}
```

### MANDATORY: TestScope Architecture (ALL Test Classes)

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

    // Fluent builder methods for service configuration
    public TestScope WithStandardServices() { /* setup */ return this; }
    public TestScope WithFailingHttpClient() { /* setup */ return this; }
    public TestScope WithJSInterop(JSRuntimeMode mode = JSRuntimeMode.Strict) { /* setup */ return this; }

    public void Dispose() => Context?.Dispose();
}

// Factory methods for common scenarios
private static TestScope CreateTestScope() => new TestScope().WithStandardServices();
```

### MANDATORY: TUnit Fluent Chaining

**✅ USE CHAINING FOR**: Same object/property assertions, logically sequential validations
**⚠️ USE Assert.Multiple FOR**: Different objects, unrelated concerns

```csharp
// ✅ OPTIMAL: Chain related assertions on same object
await Assert.That(component.Markup).IsNotNull().And.Contains("expected").And.Contains("more");

// ✅ OPTIMAL: Use Assert.Multiple for unrelated concerns
using (Assert.Multiple())
{
    await Assert.That(component.Find("h1")).IsNotNull();  // DOM structure
    await Assert.That(scope.Logger.LogEntries.Any(entry => entry.Message.Contains("logged"))).IsTrue();  // Logging
}

// ❌ NEVER: Separate assertions on same object
using (Assert.Multiple())
{
    await Assert.That(component.Markup).IsNotNull();
    await Assert.That(component.Markup).Contains("text");  // WRONG
}
```

### MANDATORY: Test Quality Checklist

**Before committing ANY test:**

- [ ] ConfigureAwait(false) on all async calls
- [ ] **NEVER put ConfigureAwait(false) at the end of assert statements**
- [ ] TestScope pattern with fluent configuration
- [ ] TUnit chaining for related assertions
- [ ] Clear AAA structure with comments
- [ ] Single responsibility principle
- [ ] Zero build warnings compliance
- [ ] Resource disposal via using statements
- [ ] Comprehensive error scenario testing
- [ ] Partial class structure: Tests in main, helpers in .Helpers.cs
- [ ] **CRITICAL**: ALL new test code MUST be placed in `NewTests/` folders within test projects - NEVER touch or reference testcode outside NewTests folders

### Mocking Strategy

**STRATEGIC APPROACH**: Use appropriate mocking based on dependency type

#### LightMock.Generator - For 3rd Party/External Dependencies ONLY

**USE FOR**: `IHttpClientFactory`, `ILocalStorageService`, `ILogger<T>`, external APIs, Azure services
**Benefits**: Compile-time generation, zero runtime overhead, AOT compatible
**Mock naming**: Use `Mock` suffix: `var httpClientMock = new Mock<IHttpClientFactory>();`
**Usage**: `new Mock<IInterface>()` → setup → pass `.Object` to constructor

**🔧 CRITICAL: Optional Parameters Solution**
**CS0854 Fix**: ALWAYS specify ALL parameters explicitly in `Arrange()`/`Assert()` calls:

```csharp
// ❌ FAILS: _mock.Arrange(f => f.GetAsync("key"))
// ✅ WORKS: _mock.Arrange(f => f.GetAsync("key", CancellationToken.None))
// ✅ WORKS: _mock.Arrange(f => f.SetAsync("key", value, null, CancellationToken.None))
```

**Pattern**: `CancellationToken.None`, `null`, `The<T>.IsAnyValue` for optional params

#### Custom Mocks - For Internal Components/Services

**USE FOR**: `NavigationManager`, internal services, Blazor components, project-specific abstractions
**Benefits**: Full control, tailored behavior, easier debugging, no external dependencies
**Pattern**: Follow HomeTests.Helpers.cs examples with sealed classes and primary constructors

```csharp
// ✅ CUSTOM MOCK: Internal NavigationManager
public sealed class NavigationManagerMock(string baseUri) : NavigationManager
{
    public string? NavigatedTo { get; private set; }
    protected override void NavigateToCore(string uri, NavigationOptions options)
    {
        NavigatedTo = uri;
    }
}

// ✅ LIGHTMOCK: External dependency
var httpClientMock = new Mock<IHttpClientFactory>();
httpClientMock.Arrange(f => f.CreateClient(The<string>.IsAnyValue))
    .Returns(new HttpClient());
```

## Implementation Notes

### Blazor Component Testing

- Use BUnit for component rendering and interaction testing
- Implement proper service registration in TestScope
- Test component behavior through public interfaces only
- Use custom NavigationManagerMock for navigation testing
- Validate DOM structure and user interactions

### Azure Function Testing

- Maintain existing TestFunctionContext and helper patterns
- Use TestHttpRequestData/TestHttpResponseData for HTTP testing
- Test function behavior through public interfaces
- Validate response status codes and content
- Use LightMock.Generator for external HTTP dependencies

### Performance Considerations

- Use TestDelayProvider to eliminate delays in tests
- Implement fast-executing test scopes
- Avoid unnecessary async operations in test setup
- Use JSRuntimeMode.Loose for faster JavaScript interop testing

### Error Handling

- Test both success and failure scenarios
- Validate proper exception handling
- Test timeout scenarios with appropriate mocks
- Ensure resource disposal in error conditions

## Open Questions

1. Should we maintain the existing test data files (like Videos.json) or create new test data following modern patterns?
2. Are there specific performance benchmarks we should maintain during the upgrade process?
3. Should we create additional behavior tests for areas that currently only have implementation tests?
4. How should we handle tests that currently depend on external APIs - should they be mocked or maintained as integration tests?
5. Are there specific accessibility testing patterns we should implement during the upgrade?

## File Migration Plan

### Phase 1: Core Infrastructure Tests

1. `StringExtensionsTests.cs` → `NewTests/Core/StringExtensionsTests.cs` + `.Helpers.cs`
2. `BlazorCodeBehindEnforcementTests.cs` → `NewTests/CodeQuality/BlazorCodeBehindEnforcementTests.cs` + `.Helpers.cs`

### Phase 2: API Function Tests

1. `TestDeserialization.cs` → `NewTests/Functions/TestDeserialization.cs` + `.Helpers.cs`
2. `ArticlesApiVerification_Tests.cs` → `NewTests/Functions/ArticlesApiVerification_Tests.cs` + `.Helpers.cs`
3. `RaindropListArticles_Tests.cs` → `NewTests/Functions/RaindropListArticles_Tests.cs` + `.Helpers.cs`
4. `RaindropListVideos_Tests.cs` → `NewTests/Functions/RaindropListVideos_Tests.cs` + `.Helpers.cs`

### Phase 3: Helper Infrastructure

1. Migrate and modernize all helper classes in `Helpers/` directory
2. Ensure compatibility with new TestScope patterns
3. Update to use modern C# 13 patterns
4. In the end all old Helper files must be integrated into our new structure and be obsolete and can be removed.

### Phase 4: Validation and Cleanup

1. Verify all tests pass with zero build warnings
2. Rename original files with `.outdated` suffix
3. Update any remaining references
4. Generate final test coverage report