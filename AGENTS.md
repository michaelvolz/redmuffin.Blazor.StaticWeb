# Agent Instructions

> **Important**: Use bash-compatible commands in this CLI tool. PowerShell (`.ps1`) scripts are executed via `pwsh scripts/...`. Human-facing documentation uses PowerShell syntax — always convert to bash equivalents when using the bash tool.

## Build, Lint, and Test Commands

### Build

```bash
dotnet build                    # Build entire solution
dotnet build --no-restore       # Fast build (after restore)
```

**Zero Build Warnings Policy**: After any C# file change, run `dotnet build --verbosity quiet` and fix all warnings (except IL2111).

### Testing

#### All Tests

```bash
dotnet test                     # Run all tests (258 tests, ~1.4s)
dotnet test --list-tests       # List all tests
```

#### Category-Based Filtering (Agentic Coding)

Tests are categorized for fast, targeted execution:

| Filter                    | Command                                                                       | Tests | Duration |
| ------------------------- | ----------------------------------------------------------------------------- | ----- | -------- |
| **Smoke** (fastest)       | `dotnet test -- --treenode-filter "/*/*/*/*[Category=Smoke]"`                 | 27    | ~0.8s    |
| **Feature:Home**          | `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Home]"`          | 52    | ~0.8s    |
| **Feature:Videos**        | `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Videos]"`        | 10    | ~0.7s    |
| **Feature:Articles**      | `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Articles]"`      | 17    | ~0.7s    |
| **Feature:Cache**         | `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Cache]"`         | 31    | ~0.9s    |
| **Feature:Raindrop**      | `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Raindrop]"`      | 24    | ~0.6s    |
| **Feature:RaindropItems** | `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:RaindropItems]"` | 17    | ~0.6s    |
| **Feature:Core**          | `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Core]"`          | 13    | ~0.6s    |
| **Feature:ApiExample**    | `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:ApiExample]"`    | 5     | ~0.7s    |

**When to use:**

- **Smoke**: Fast validation after small changes (5-10 tests per feature)
- **Feature:X**: Run only tests for the feature you're working on
- **Unit**: Pure unit tests (no I/O)

**Example workflow:**

```bash
# 1. Fast smoke test (agentic coding)
dotnet test -- --treenode-filter "/*/*/*/*[Category=Smoke]"

# 2. Feature-specific tests
dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Home]"

# 3. Full test suite (before commit)
dotnet test
```

**AOT Compilation**: Tests run with AOT in CI (`CI=true` or `GITHUB_ACTIONS=true`), disabled locally for speed.

### Code Coverage

```bash
pwsh scripts/Generate-CoverageReport.ps1  # Generate coverage report
pwsh scripts/View-CoverageReport.ps1       # View unified report
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
- **Backend**: Azure Functions (.NET 9) - `src/redmuffin.Blazor.StaticWeb.Api/`
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
   - `csharp-standards`, `testing`, `ui-styling`, `dotnet`, `commits`
7. **Reference Guides**: `.github/guides/` contains detailed docs
8. **Never install anything**: ALWAYS ask first (see rule #1)

---

## Security-First Policy (CRITICAL)

This project follows a **zero-tolerance policy for secrets in files**. The repository MUST NEVER contain a single secret.

### Absolute Rules

1. **NEVER commit secrets to git**: API keys, tokens, passwords, credentials, secrets, or any sensitive data must NEVER be in any file in the repository. This includes:
   - Source code files (`.cs`, `.razor`, `.cshtml`)
   - Configuration files (`.json`, `.yml`, `.yaml`, `.xml`)
   - Docker files (`Dockerfile`, `docker-compose.yml`)
   - Scripts (`.ps1`, `.sh`, `.bash`)
   - Documentation (`.md`, `.txt`)
   - Even private repositories are not exempt

2. **NEVER hardcode secrets**: Never write patterns like `"api_key": "value"` or `Token = "secret"` in any file

3. **NEVER suggest file-based secrets**: Do not recommend `.env` files, `appsettings.json` with real values, or any file that stores secrets

### Allowed Secret Management Methods

Always use ONE of these methods for secrets:

| Method                           | Use Case                    | Syntax                                         |
| -------------------------------- | --------------------------- | ---------------------------------------------- |
| **Environment Variables**        | MCP configs, devcontainer   | `{env:VAR_NAME}` or `${env:VAR}`               |
| **VS Code DevContainer Secrets** | Devcontainer development    | Defined in `devcontainer.json` `secrets` block |
| **VS Code Copilot MCP Inputs**   | VS Code Copilot MCP servers | `${input:secret_id}` with `password: true`     |
| **GitHub Repository Secrets**    | CI/CD pipelines             | `${{ secrets.SECRET_NAME }}`                   |
| **Azure Key Vault**              | Production deployments      | `az keyvault secret show`                      |
| **User Secrets**                 | Local .NET development      | `dotnet user-secrets`                          |

### MCP Configuration Syntax

MCP configs must read secrets from environment variables:

```json
// CORRECT - reads from environment
"env": { "API_KEY": "${env:API_KEY}" }

// WRONG - hardcoded value
"env": { "API_KEY": "actual_secret_here" }
```

### If You Detect a Secret

If you find ANY secret hardcoded in a file, IMMEDIATELY:

1. Stop all work
2. Alert the user with URGENT warning
3. Do NOT continue until the exposed secret is rotated
4. Assist with cleanup (git history scrubbing if needed)

### Security Checklist

Before ANY commit, verify:

- [ ] No API keys, tokens, or secrets in changed files
- [ ] No `password`, `secret`, `token`, `key`, `credential`, `auth` with visible values
- [ ] Config files use `${env:VAR}` or `${input:VAR}` syntax only
- [ ] `.gitignore` includes sensitive file patterns

### Reference

See `.devcontainer/SECURITY.md` for detailed devcontainer secret management.

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
