# AGENTS: Project Guide

## CRITICAL

- NEVER commit secrets to git
- NEVER install tools/packages without explicit permission (use `question` tool)
- NEVER push to remote (HARD BLOCKED)
- ALWAYS run `dotnet build --verbosity quiet` after C# changes
- ALWAYS run `dotnet build -c Debug-Sass` after SCSS/JS changes (see compilerconfig.json)
- ALWAYS run `dotnet test` before commit
- ALWAYS use `skill name="commits"` before git commit
- **Research-First**: 15 min research BEFORE any code changes
- **Stop**: 2+ edits without testing → `brave_web_search`
- **Stop**: "maybe", "try", "probably" → `brave_web_search`

## COMMANDS

| Command                                                                       | Purpose                       | When                                 |
| ----------------------------------------------------------------------------- | ----------------------------- | ------------------------------------ |
| `dotnet build`                                                                | Build solution                | After C# changes                     |
| `dotnet build -c Debug-Sass`                                                  | Compile SCSS, minify JS       | After .scss/.js changes              |
| `dotnet build --no-restore`                                                   | Fast build (post-restore)     | Subsequent builds                    |
| `dotnet build --verbosity quiet`                                              | Build + warnings              | Verify zero warnings (except IL2111) |
| `dotnet test`                                                                 | All 258 tests (~1.4s)         | Before commit                        |
| `dotnet test -- --treenode-filter "/*/*/*/*[Category=Smoke]"`                 | Smoke (27, ~0.8s)             | Fast validation                      |
| `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Home]"`          | Home (52)                     | Feature validation                   |
| `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Videos]"`        | Videos (10)                   | Feature validation                   |
| `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Articles]"`      | Articles (17)                 | Feature validation                   |
| `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Cache]"`         | Cache (31)                    | Feature validation                   |
| `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Raindrop]"`      | Raindrop (24)                 | Feature validation                   |
| `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:RaindropItems]"` | RaindropItems (17)            | Feature validation                   |
| `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Core]"`          | Core (13)                     | Feature validation                   |
| `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:ApiExample]"`    | ApiExample (5)                | Feature validation                   |
| `pwsh scripts/Generate-CoverageReport.ps1`                                    | Generate coverage             | Check coverage                       |
| `pwsh scripts/View-CoverageReport.ps1`                                        | View coverage                 | Review coverage                      |
| `pwsh scripts/test-build-fast.ps1`                                            | Fast dev build (~9s, AoT off) | Local development                    |
| `pwsh scripts/test-build-aot.ps1`                                             | Production parity             | Pre-deployment                       |
| `pwsh scripts/DisplayWarnings.ps1`                                            | Show build warnings           | Debug warnings                       |
| `dotnet run --project src/redmuffin.Blazor.StaticWeb`                         | Frontend only (5233)          | Normal dev (99%)                     |
| `dotnet run --project src/redmuffin.Blazor.StaticWeb > logs/dotnet.log 2>&1`  | Frontend + logging            | Debug mode                           |
| `pwsh Start.ps1`                                                              | Full stack interactive        | Manual debugging                     |
| `pwsh Start.ps1 -Auto`                                                        | Full stack automated          | Agent workflows                      |
| `pwsh Stop.ps1`                                                               | Stop full stack               | Cleanup after Start -Auto            |

## STACK

| Technology          | Version     | Purpose          |
| ------------------- | ----------- | ---------------- |
| .NET                | 9.0         | Core framework   |
| Blazor              | WebAssembly | Frontend         |
| Azure Functions     | .NET 9      | Backend          |
| TUnit               | Latest      | Testing          |
| LightMock.Generator | Latest      | External mocking |
| SCSS/Sass           | -           | Styling          |

## STRUCTURE

```
src/redmuffin.Blazor.StaticWeb/          # Frontend (Blazor WASM)
src/redmuffin.Blazor.StaticWeb.Api/      # Backend (Azure Functions)
tests/                                    # Tests (mirrors src)
src/[Project]/Features/                   # Feature folders
tasks/PRD-XXX-*.md                        # PRD documents
skills/                                   # Skills: csharp-standards, testing, ui-styling, dotnet, commits
.github/guides/                           # Reference docs
```

### Partial Classes

```
Features/Home/
  Home.razor.cs           # Logic, lifecycle, properties, events
  Home.Logging.cs         # LoggerMessage delegates

Core/Services/
  UserService.cs          # Logic, methods, properties
  UserService.Logging.cs  # LoggerMessage delegates

Core/HomeTests/
  HomeTests.cs              # [Test] methods
  HomeTests.Helpers.cs      # TestScope, mocks, utilities
  HomeTests.EdgeCases.cs    # Error handling, edge cases
  HomeTests.Infrastructure.cs  # Lifecycle, logging, DI
  HomeTests.Behavior.cs     # User interactions, workflows
```

## WORKFLOWS

### Research-First Protocol

1. **0-15 min:** Research ONLY. Zero code changes.
2. **After 15 min:** Implement ONLY if authoritative guidance found
3. **No source found:** Escalate with research summary

**STOP triggers:**

- 2+ edits without testing → `brave_web_search`
- "maybe", "perhaps", "let's try", "I think", "probably" → `brave_web_search`
- Same error twice → escalate
- No docs → Context7
- Stuck → escalate

**Pre-implementation:**

1. Canonical way? → official docs
2. Solved before? → GitHub/SO
3. Official docs say? → Context7

**Checklist:**

- [ ] Consulted authoritative source
- [ ] Can cite documentation
- [ ] No guessing
- [ ] Know next step if fails

### Infrastructure Error Protocol (SCOPED)

**Scope:** SCSS/Sass, NuGet, missing tools, build config

1. Halt feature work
2. Analyze error
3. Check system state (Node.js, NuGet)
4. Inspect config (`.csproj`, `compilerconfig.json`)
5. Web search
6. Explain root cause + fix + why → WAIT for approval

**Excluded:** C# syntax (fix immediately), test failures (debug), logic errors (research)

### Frontend Debugging Protocol

**When:** Port 5233 errors or broken site

1. Read `logs/dotnet.log` FIRST (exceptions, 404s, warnings)
2. Investigate based on findings only - NO guessing

### Web Search Decision Tree

| Query Type            | Tool                                           | Use Case                |
| --------------------- | ---------------------------------------------- | ----------------------- |
| Library/framework API | Context7 (`resolve-library-id` → `query-docs`) | .NET, Blazor, NuGet, JS |
| Keyword-specific      | `brave_web_search`                             | Errors, "how to"        |
| Vague/conceptual      | `websearch`                                    | "find library like X"   |
| Complex reasoning     | `sequentialthinking`                           | Architecture, debugging |

## PATTERNS

### Formatting

| File Type       | Indentation    | Notes             |
| --------------- | -------------- | ----------------- |
| C#              | Tab (4 spaces) | -                 |
| .razor, .cshtml | 4 spaces       | -                 |
| .csproj         | 2 spaces       | -                 |
| All             | Max 160 chars  | Brace on new line |

### Naming

| Type               | Convention       | Example                   |
| ------------------ | ---------------- | ------------------------- |
| Types/Namespaces   | PascalCase       | `HomePage`, `UserService` |
| Methods/Properties | PascalCase       | `GetUser()`               |
| Private fields     | camelCase        | `_userService`            |
| Static readonly    | UpperCamelCase\_ | `LogEvent`                |
| Interfaces         | Prefix "I"       | `IUserService`            |
| Test doubles       | `[Class]_[Type]` | `NavigationManager_Mock`  |

### C# 12/13

- Primary constructors
- Collection expressions: `[1, 2, 3]`
- `ref readonly` parameters
- Pattern matching in switch expressions
- `nameof` not string literals

### Nullable Reference Types

- Declare non-nullable
- Check `null` at entry points
- `is null` / `is not null` (NOT `== null`)

### Error Handling

- `LoggerMessage` delegates (NEVER `Logger.LogError()`)
- Specific exceptions with messages
- Test errors in `.EdgeCases.cs`

### File-Scoped Namespaces

```csharp
namespace MyNamespace;
// Single-line using directives
// System.* first, then alphabetical
```

### Mocking

**LightMock (external):**

```csharp
var httpMock = new Mock<IHttpClientFactory>();
httpMock.Arrange(f => f.CreateClient(The<string>.IsAnyValue)).Returns(new HttpClient());
```

**Custom (internal):**

```csharp
public sealed class NavigationManager_Mock : NavigationManager
{
    public string? NavigatedTo { get; private set; }
    protected override void NavigateToCore(string uri, NavigationOptions options)
        => NavigatedTo = uri;
}
```

**Critical:** ALL parameters explicit:

```csharp
_mock.Arrange(f => f.GetAsync("key", CancellationToken.None))
```

### Test Quality

- `ConfigureAwait(false)` on async (except asserts)
- AAA structure
- `using` for disposal
- Edge cases
- Zero build warnings

### Secret Management

| Method                | Use Case          | Syntax                                |
| --------------------- | ----------------- | ------------------------------------- |
| Environment Variables | MCP, devcontainer | `{env:VAR}` or `${env:VAR}`           |
| VS Code DevContainer  | Devcontainer      | `devcontainer.json` secrets block     |
| VS Code Copilot MCP   | Copilot           | `${input:secret_id}` `password: true` |
| GitHub Secrets        | CI/CD             | `${{ secrets.NAME }}`                 |
| Azure Key Vault       | Production        | `az keyvault secret show`             |
| User Secrets          | Local .NET        | `dotnet user-secrets`                 |

**MCP:**

```json
"env": { "API_KEY": "${env:API_KEY}" }  // CORRECT
"env": { "API_KEY": "actual_secret" }   // WRONG
```

### Dev Modes

| Mode       | Port | Use Case             | Command                                               |
| ---------- | ---- | -------------------- | ----------------------------------------------------- |
| Normal     | 5233 | UI, mock data (99%)  | `dotnet run --project src/redmuffin.Blazor.StaticWeb` |
| Full Stack | 4280 | Real API, OAuth, E2E | `pwsh Start.ps1 -Auto`                                |

### Dotnet Process Management (VS Running)

VS runs own dotnet processes. Cannot Ctrl+C. Kill specific PID only:

```powershell
netstat -ano | findstr :5233
taskkill /PID <PID> /F
```

Full stack: `pwsh Stop.ps1` (tracks PIDs in `.dev-session.pids`)

## BOUNDARIES

### ALWAYS

- Bash-compatible commands (`pwsh scripts/...`)
- `winget` (NOT Chocolatey)
- `dotnet build --verbosity quiet` after C# changes
- `dotnet build -c Debug-Sass` after SCSS/JS
- `dotnet test` before commit
- `skill name="commits"` before git commit
- `ConfigureAwait(false)` on async (except asserts)
- `nameof` not string literals
- `is null` / `is not null` (NOT `== null`)
- `LoggerMessage` (NEVER `Logger.LogError()`)
- ALL parameters explicit in mocks
- File-scoped namespaces
- Tab indent C# (4 spaces)
- 4-space .razor/.cshtml
- 2-space .csproj
- Max 160 chars line length
- Brace on new line
- PascalCase: types/namespaces/methods/properties
- camelCase: private fields
- UpperCamelCase\_: static readonly
- "I" prefix: interfaces
- `[Class]_[Type]`: test doubles

### ASK FIRST

- Install ANY tool/package/extension/dependency
- Infrastructure/toolchain error fixes

### NEVER

- Commit secrets (API keys, tokens, passwords)
- Hardcode secrets (`"api_key": "value"`)
- Suggest file-based secrets (.env, appsettings.json real values)
- Push to remote (plugin blocked)
- Auto-commit (explicit command only)
- `Logger.LogError()` (use `LoggerMessage`)
- `== null` (use `is null`)
- Chocolatey (use `winget`)
- ALL CAPS filenames (except AGENTS.md, README.md)
- Edit multiple files without testing
- Skip testing on infrastructure errors

## OUTPUT STYLE

| Rule           | Constraint                     |
| -------------- | ------------------------------ |
| Line width     | Max 160 chars                  |
| Empty lines    | Minimize - major sections only |
| Paragraphs     | Avoid - bullets/tables         |
| Recapitulation | Never - state once             |
| Voice          | Active, imperative             |

**Priority:** Tables > bullets > single-line > prose. ALL info preserved, verbosity removed.

## CONTEXT

- **AOT**: CI runs AOT (`CI=true`/`GITHUB_ACTIONS=true`), disabled locally
- **Security**: Zero-tolerance. Detected secrets → stop, alert, rotate, cleanup
- **Naming**: Lowercase/PascalCase new files (not ALL_CAPS except AGENTS.md, README.md)
- **Start.ps1 -Auto**: Creates `.dev-session.pids` for `Stop.ps1` cleanup
- **Test Categories**: Smoke (fastest), Feature:X (targeted), Unit (pure, no I/O)
- **Secrets Reference**: `.devcontainer/SECURITY.md`
