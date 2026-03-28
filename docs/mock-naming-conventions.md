# Mock Naming Conventions

## Overview

This document establishes the naming conventions for mock objects in unit tests across the project. Following consistent naming patterns improves code readability and maintainability.

## Standard Pattern: `Mock` Suffix

### For Mock Objects

Use the `Mock` suffix for all mock wrapper objects:

```csharp
// ✅ RECOMMENDED - Standard pattern
private readonly Mock<IUserService> _userServiceMock;
private readonly Mock<ILogger<UserService>> _loggerMock;
private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
private readonly Mock<ICacheService> _cacheServiceMock;
```

### For Mock Object Instances

When accessing the actual mock object, use the base interface name:

```csharp
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;

    // The actual service under test
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _loggerMock = new Mock<ILogger<UserService>>();

        // Pass the .Object to the service constructor
        _userService = new UserService(
            _userRepositoryMock.Object,
            _loggerMock.Object
        );
    }
}
```

## Complete Example

```csharp
public class ImageValidationServiceTests : IDisposable
{
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<ImageValidationService>> _loggerMock;
    private readonly ImageValidationService _service;

    public ImageValidationServiceTests()
    {
        _cacheServiceMock = new Mock<ICacheService>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<ImageValidationService>>();

        _service = new ImageValidationService(
            _httpClientFactoryMock.Object,
            _cacheServiceMock.Object,
            _loggerMock.Object
        );
    }

    [Test]
    public async Task ClearValidationCacheAsync_ClearsBothCaches()
    {
        // Act
        await _service.ClearValidationCacheAsync();

        // Assert
        _cacheServiceMock.Verify(x => x.ClearNamespaceAsync("image_validation", It.IsAny<CancellationToken>()), Times.Once);
    }

    public void Dispose()
    {
        _service?.Dispose();
        GC.SuppressFinalize(this);
    }
}
```

## Benefits of This Pattern

1. **Industry Standard**: Most widely adopted in professional .NET development
2. **Clear Intent**: Immediately obvious that the variable is a mock
3. **IntelliSense Friendly**: Groups all mocks together when typing
4. **Consistent**: Follows .NET naming conventions (PascalCase with descriptive suffixes)
5. **Maintainable**: Easy to identify and refactor mock-related code

## What to Avoid

```csharp
// ❌ AVOID - Unclear what these are
private readonly Mock<IUserService> _userService;
private readonly Mock<ILogger> _logger;

// ❌ AVOID - Inconsistent naming
private readonly Mock<IUserService> _fakeUserService;
private readonly Mock<IUserService> _stubUserService;
private readonly Mock<IUserService> _mockUserService; // prefix instead of suffix

// ❌ AVOID - Generic names
private readonly Mock<IUserService> _mock1;
private readonly Mock<ILogger> _mock2;
```

## Framework-Specific Patterns

### NSubstitute

```csharp
private readonly IUserService _userServiceMock = Substitute.For<IUserService>();
private readonly ILogger<UserService> _loggerMock = Substitute.For<ILogger<UserService>>();
```

### LightMock.Generator

```csharp
private readonly Mock<IUserService> _userServiceMock = new Mock<IUserService>();
private readonly Mock<ILogger<UserService>> _loggerMock = new Mock<ILogger<UserService>>();
```

## Enforcement

- **Code Reviews**: All mock objects should follow the `Mock` suffix pattern
- **Pull Requests**: Reviewers should check for consistent mock naming
- **Documentation**: Update this document when patterns evolve
- **Team Standards**: All team members should follow these conventions

## Notes

- This is a **temporary document** until patterns are established
- May be moved to a more permanent location in the future
- Should be updated as the project's mocking strategy evolves
- Consider adding to coding standards documentation
