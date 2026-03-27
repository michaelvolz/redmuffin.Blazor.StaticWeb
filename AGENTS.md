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
- System.* imports first, then alphabetical

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

1. **NEVER commit or push** without explicit user permission
2. **File Editing**: Edit one file at a time, track progress
3. **Large Changes**: Outline plan, get approval, make incremental edits
4. **Skills**: See `skills/` folder for detailed rules (loaded automatically):
   - `csharp-standards`, `testing`, `ui-styling`, `dotnet`, `powershell`, `commits`
5. **Reference Guides**: `.github/guides/` contains detailed docs

## Commit Message Format

All commits MUST follow this format:

```
<type>(<scope>): <description> (max 112 chars)

- Detailed explanation of what was changed
- Why it was changed
- Any breaking changes or migration notes
```

### Requirements
- **Title**: `<type>(<scope>): <description>` (max 112 chars)
- **Body**: Required for ALL commits except:
  - Dependency bumps (e.g., `chore(deps): bump X from 1.0 to 2.0`)
  - Merge commits
  - Simple refactoring with self-evident changes
- **Type/scope**: Use conventional commits (feat, fix, docs, refactor, etc.)

### When Body is Required
If the commit changes behavior, adds features, fixes bugs, or requires explanation, include a body with 2-3 sentences explaining:
- What was changed
- Why it was changed
- Any breaking changes or migration notes

### Examples

**Good (with body):**
```
feat(blazor): add new navigation component

Added NavMenu component with responsive behavior. This improves
mobile navigation and provides better UX. No breaking changes.
```

**Good (minimal - dependency bump):**
```
chore(deps): bump Meziantou.Analyzer from 2.0.161 to 2.0.163
```

**Bad (missing body for significant change):**
```
refactor: clean up service code
```

## Development Modes

| Mode | Port | Use Case |
|------|------|----------|
| Simplified | 5233 | UI work, uses mock data when API unavailable |
| Full Stack | 4280 | API integration, OAuth, E2E testing |
