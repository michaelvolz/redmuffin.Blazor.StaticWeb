# Agent Instructions

> **Important**: This repo runs on Windows 11 with PowerShell 7. Always use PowerShell commands (e.g., `dotnet build`, `.\scripts\...`), NOT Unix commands like `bash`, `sh`, or `/bin/*`.

## Build, Lint, and Test Commands

### Build

```powershell
dotnet build                    # Build entire solution
dotnet build --no-restore       # Fast build (after restore)
```

**Zero Build Warnings Policy**: After any C# file change, run `dotnet build --verbosity quiet` and fix all warnings (except IL2111).

### Testing

```powershell
dotnet test                     # Run all tests
dotnet test --filter "FullyQualifiedName~TestClassName"  # Run specific test class
dotnet test --filter "FullyQualifiedName~TestMethodName" # Run single test
dotnet test --list-tests       # List all tests
```

**AOT Compilation**: Tests run with AOT in CI (`CI=true` or `GITHUB_ACTIONS=true`), disabled locally for speed.

### Code Coverage

```powershell
.\scripts\Generate-CoverageReport.ps1  # Generate coverage report
.\scripts\View-CoverageReport.ps1       # View unified report
```

### Development Build Scripts

- `scripts/test-build-fast.ps1` - Fast dev build (~9s, AoT disabled)
- `scripts/test-build-aot.ps1` - Production parity testing
- `scripts/DisplayWarnings.ps1` - Show all build warnings

---

## Code Style Guidelines

### Formatting (.editorconfig)

- **C# files**: Tab indentation (4 tabs = 4 spaces)
- **Web files** (`.razor`, `.cshtml`): 4-space indentation
- **Project files** (`.csproj`): 2-space indentation
- Max line length: 160 characters
- Opening brace on new line

### Naming Conventions

- **Types/Namespaces**: PascalCase (e.g., `HomePage`, `UserService`)
- **Methods/Properties**: PascalCase
- **Private fields**: camelCase (e.g., `_userService`)
- **Static readonly fields**: `UpperCamelCase_underscore` (e.g., `LogEvent`)
- **Interfaces**: Prefix with "I" (e.g., `IUserService`)
- **Test doubles**: `[ClassName]_[Type]` (e.g., `NavigationManager_Mock`, `HttpClient_Stub`)

### Imports

- File-scoped namespace declarations
- Single-line using directives
- System.\* imports first, then alphabetical

### C# 12/13 Features

- Primary constructors
- Collection expressions (`[1, 2, 3]`)
- `ref readonly` parameters
- Pattern matching in switch expressions
- Use `nameof` instead of string literals

### Nullable Reference Types

- Declare variables non-nullable
- Check for `null` at entry points
- Use `is null` or `is not null` (NOT `== null`)

### Error Handling

- Use `LoggerMessage` delegates (NEVER `Logger.LogError()`)
- Throw specific exceptions with meaningful messages
- Test error scenarios in `.EdgeCases.cs` files

---

## Partial Class Organization

### Blazor Components

```
Features/Home/
  Home.razor.cs      # Logic, lifecycle, properties, events
  Home.Logging.cs   # LoggerMessage delegates
```

### Services

```
Core/Services/
  UserService.cs           # Logic, methods, properties
  UserService.Logging.cs   # LoggerMessage delegates
```

### Tests (NEVER separate helper files)

```
Core/HomeTests/
  HomeTests.cs              # [Test] methods
  HomeTests.Helpers.cs      # TestScope, mocks, utilities
  HomeTests.EdgeCases.cs    # Error handling, edge cases
  HomeTests.Infrastructure.cs  # Lifecycle, logging, DI
  HomeTests.Behavior.cs     # User interactions, workflows
```

---

## Testing Standards

### Framework

- **TUnit** with `[Test]` and `[Arguments]` (NEVER xUnit/NUnit/MSTest)
- **LightMock.Generator** for external dependencies, **Custom mocks** for internal

### Test Quality

- Use `ConfigureAwait(false)` on async calls (except asserts)
- Follow AAA structure, use `using` for disposal
- Test edge cases, zero build warnings

### Mocking Patterns

```csharp
// LightMock for external dependencies
var httpMock = new Mock<IHttpClientFactory>();
httpMock.Arrange(f => f.CreateClient(The<string>.IsAnyValue)).Returns(new HttpClient());

// Custom mock for internal components
public sealed class NavigationManager_Mock : NavigationManager
{
    public string? NavigatedTo { get; private set; }
    protected override void NavigateToCore(string uri, NavigationOptions options)
        => NavigatedTo = uri;
}
```

### Critical: Optional Parameters

Always specify ALL parameters explicitly: `_mock.Arrange(f => f.GetAsync("key", CancellationToken.None))`

---

## Project Structure

- **Frontend**: Blazor WebAssembly (.NET 9) - `src/redmuffin.Blazor.StaticWeb/`
- **Backend**: Azure Functions (.NET 8) - `src/redmuffin.Blazor.StaticWeb.Api/`
- **Tests**: `tests/` mirroring source structure
- **Features**: `src/[Project]/Features/`
- **PRDs**: `tasks/PRD-XXX-*.md`

---

## Critical Rules

1. **Ask Before Installing**: ALWAYS ask for explicit permission BEFORE installing ANY tool, package, extension, or dependency. Never install anything without user approval — ask 100% of the time.

2. **Commit**: ONLY after user's explicit command (never auto-commit)
3. **Push**: HARD BLOCKED - NEVER allow under any circumstances (enforced by plugin)
4. **File Editing**: Edit one file at a time, track progress
5. **Large Changes**: Outline plan, get approval, make incremental edits
6. **Skills**: See `skills/` folder for detailed rules (loaded automatically):
   - `csharp-standards`, `testing`, `ui-styling`, `dotnet`, `powershell`, `commits`
7. **Reference Guides**: `.github/guides/` contains detailed docs
8. **Never install anything**: ALWAYS ask first (see rule #1)
9. **NEVER hardcode secrets**: Never recommend putting API keys, tokens, passwords, or any secrets directly into code or config files. Always use one of:
   - Environment variables (`{env:VAR_NAME}` in opencode.json, `$env:VAR` in PowerShell)
   - User secrets (`dotnet user-secrets`) for .NET development
   - Azure Key Vault for production
   - `.env` files (gitignored) for local development
     If you detect a secret in a file, immediately warn the user and suggest the correct approach.

## Web Search Strategy

This project has four search/discovery tools. Use the right one for the job:

### Sequential Thinking (MCP)

For complex problems that require careful reasoning, multi-step planning, or exploring alternate approaches, use the `sequentialthinking` tool to break down the problem step-by-step. This MCP server provides structured, iterative reasoning with revision capabilities.

- Use for: Architectural decisions, debugging complex issues, multi-step refactoring
- Prompt: "use sequential thinking to solve this" or include in your reasoning request

### Context7 → Always use first for library/framework code

- Any time you're writing code that uses an external library or framework
- .NET, Blazor, NuGet packages, JavaScript frameworks
- Fetches version-specific API docs and code examples — prevents hallucinated APIs
- Tools: `resolve-library-id` → `get-library-docs`

### Brave (`brave_web_search`) → Default for general web search

- Stack Overflow answers, error messages, "how to do X"
- Current events, version changelogs, blog posts, tutorials
- When you need factual information from the web

### Exa (`websearch`) → Semantic/discovery search

- Vague/conceptual queries: "find a library that does X", "similar to this"
- Finding "hidden gem" content that keyword search misses
- Deep topic exploration where you don't know the exact keywords
- Fallback when Brave results aren't sufficient

### Decision Rule

1. Is it about a library/framework API? → **Context7**
2. Is it a keyword-specific query (error, "how to", specific topic)? → **Brave**
3. Is it vague/conceptual? → **Exa**

---

## Development Modes

| Mode       | Port | Use Case                                     |
| ---------- | ---- | -------------------------------------------- |
| Simplified | 5233 | UI work, uses mock data when API unavailable |
| Full Stack | 4280 | API integration, OAuth, E2E testing          |
