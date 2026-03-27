---
aliases: [dotnet]
description: .NET/C# best practices for this project
---
.NET/C# Best Practices for this project:

## Architecture & Patterns
- Use primary constructor syntax for DI: `public class MyClass(IDependency dependency)`
- Prefix interfaces with 'I' (e.g., IUserService)
- Use Command Handler pattern with generic base classes
- Follow namespace structure: {Core|Console|App|Service}.{Feature}

## Dependency Injection
- Constructor DI with null checks: `ArgumentNullException.ThrowIfNull(dependency)`
- Register services with appropriate lifetimes: Singleton, Scoped, Transient
- Use Microsoft.Extensions.DependencyInjection

## Async/Await
- Return Task<T> for value, Task for void
- Use ConfigureAwait(false) where appropriate
- NEVER use .Wait(), .Result, or .GetAwaiter().GetResult()
- Use Task.WhenAll() for parallel execution

## Error Handling & Logging
- Structured logging with Microsoft.Extensions.Logging
- Throw specific exceptions with descriptive messages
- Use try-catch for expected failure scenarios

## Resource Management
- Use ResourceManager for localized messages
- Separate LogMessages and ErrorMessages .resx files
- Implement proper disposal patterns

## Code Quality
- Ensure SOLID principles
- Comprehensive XML documentation for public APIs
- C# 12+ features (.NET 8)
- Meaningful names reflecting domain concepts