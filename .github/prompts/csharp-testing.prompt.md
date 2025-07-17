---
mode: 'agent'
tools: ['changes', 'codebase', 'editFiles', 'problems', 'search']
description: 'Combined best practices for TUnit unit testing and LightMock mocking'
---

# TUnit + LightMock Testing

## Setup
- Test project: `[ProjectName].Tests`
- Packages: TUnit, TUnit.Assertions, LightMock.Generator
- Test classes match tested classes: `CalculatorTests` for `Calculator`
- Requires .NET 8+

## Test Structure
- `[Test]` attribute (not `[Fact]`)
- Arrange-Act-Assert pattern
- Naming: `MethodName_Scenario_ExpectedBehavior`
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

### Usage Pattern
```csharp
public class ServiceTests : IDisposable
{
    private readonly Mock<IRepository> _repositoryMock;
    private readonly Service _service;
    
    public ServiceTests()
    {
        _repositoryMock = new Mock<IRepository>();
        _service = new Service(_repositoryMock.Object);
    }
    
    [Test]
    public async Task Method_Scenario_ExpectedBehavior()
    {
        // Arrange
        _repositoryMock.Setup(x => x.GetAsync("id")).Returns(Task.FromResult(data));
        
        // Act
        var result = await _service.ProcessAsync("id");
        
        // Assert
        await Assert.That(result).IsNotNull();
        _repositoryMock.Assert(x => x.GetAsync("id"), Times.Once);
    }
    
    public void Dispose() => _service?.Dispose();
}
```

## xUnit Migration
- `[Fact]` → `[Test]`
- `[Theory]` → `[Test]` + `[Arguments]`
- `[InlineData]` → `[Arguments]`
- `Assert.Equal` → `await Assert.That(actual).IsEqualTo(expected)`
- `Assert.True` → `await Assert.That(condition).IsTrue()`
- Constructor/IDisposable → `[Before(Test)]`/`[After(Test)]`

**Benefits**: TUnit provides async assertions, refined lifecycle hooks, parallel execution. LightMock offers compile-time generation, performance, type safety.
