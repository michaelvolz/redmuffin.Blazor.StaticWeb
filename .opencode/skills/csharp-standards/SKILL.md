---
name: csharp-standards
description: C# coding standards, analyzer rules, logging patterns, and partial class organization for Blazor components and services.
invocable: false
---

# C# Coding Standards

## Analyzer Rules (Zero Tolerance)

### StyleCop
- SA1402: One type per file
- SA1208/1210: Order usings (System first, alphabetical)
- SA1201-1214: Enforce member order
- SA1413: Trailing commas in multi-line initializers
- SA1028: No trailing whitespace
- SA1500: Opening brace on new line
- SA1507: No multiple blank lines
- SA1508: No blank line before closing brace

### Meziantou
- MA0016: Use `IEnumerable<T>`, `IList<T>` abstractions
- MA0002/0006/0074: Specify `StringComparison.OrdinalIgnoreCase`
- MA0048: File name must match type name
- MA0051: Methods <60 lines
- MA0053: Make class sealed when possible

### Microsoft
- CA1845: Use `AsSpan()` instead of `Substring()`
- CA1854: Use `TryGetValue` for Dictionary
- CA1869: Cache `JsonSerializerOptions` instances
- CA1848: Use `LoggerMessage` delegates
- CA2016: Forward `CancellationToken` parameters
- CA1805: Remove explicit default initialization
- CA1822: Mark members static when possible

### Permitted Warning
- IL2111 (Blazor WebAssembly `App_razor.g.cs` trimming) is safe to ignore

### Documentation Warnings
- Fix SA1623/SA1615 in Visual Studio if documented; skip if undocumented

## Logging

- Use `LoggerMessage` delegates, NEVER `Logger.LogError()`

```csharp
private static readonly Action<ILogger, Exception?> LogEvent = LoggerMessage.Define(LogLevel.Information, new EventId(1, "Event"), "Event occurred");
```

## Partial Class Organization

### Blazor Components
Split into `ComponentName.razor.cs` (logic, lifecycle, properties, events) and `ComponentName.Logging.cs` (`LoggerMessage` delegates)

```csharp
// ComponentName.razor.cs
public partial class Home : ComponentBase
{
    [Inject] public required ILogger<Home> Logger { get; set; }
    private async Task HandleClickAsync()
    {
        LogButtonClicked(Logger, null);
    }
}

// ComponentName.Logging.cs
public partial class Home
{
    private static readonly Action<ILogger, Exception?> LogButtonClicked = LoggerMessage.Define(LogLevel.Information, new EventId(5, "ButtonClicked"), "Button clicked");
}
```

### Services
Split into `ServiceName.cs` (logic, methods, properties) and `ServiceName.Logging.cs` (`LoggerMessage` delegates)

### Tests
Split into `TestClassName.cs` (`[Test]` methods) and `TestClassName.Helpers.cs` (TestScope, mocks, utilities in same partial class). NEVER create separate helper files.

### File Naming
- Components: `Home.razor.cs`, `Home.Logging.cs`
- Services: `UserService.cs`, `UserService.Logging.cs`
- Tests: `HomeTests.cs`, `HomeTests.Helpers.cs`
- Test files: Use `Component_Tests.cs` for new test files (e.g., `Home_Tests.cs`)

## Naming Conventions

- Follow PascalCase for component names, method names, and public members
- Use camelCase for private fields and local variables
- Prefix interface names with "I" (e.g., IUserService)

## Formatting

- Apply code-formatting style defined in `.editorconfig`
- Prefer file-scoped namespace declarations and single-line using directives
- Insert a newline before the opening curly brace of any code block
- Use pattern matching and switch expressions wherever possible
- Use `nameof` instead of string literals

## Nullable Reference Types

- Declare variables non-nullable, and check for `null` at entry points
- Always use `is null` or `is not null` instead of `== null` or `!= null`

## LightMock.Generator with Optional Parameters

**CRITICAL**: Always specify ALL parameters explicitly for interfaces with optional parameters:

```csharp
// FAILS: _mock.Arrange(f => f.GetAsync("key"))
// WORKS: _mock.Arrange(f => f.GetAsync("key", CancellationToken.None))
```

Use `CancellationToken.None`, `null`, `The<T>.IsAnyValue` for optional params in both Arrange() and Assert() calls.
