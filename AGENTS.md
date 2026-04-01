# AGENTS: Project Guide

## CRITICAL

- NEVER commit secrets to git
- NEVER install tools/packages without explicit permission
- ALWAYS run `dotnet build --verbosity quiet` after C# changes
- ALWAYS run `dotnet build -c Debug-Sass` after modifying SCSS or JS files (see compilerconfig.json)
- ALWAYS run `dotnet test` before commit
- ALWAYS use `question` tool before installing anything
- ALWAYS use `skill name="commits"` before git commit
- NEVER push to remote (HARD BLOCKED)
- **Research-First**: 15 minutes research BEFORE any code changes
- **Stop Immediately**: 2+ edits without testing → use `brave_web_search`
- **Stop Immediately**: Words like "maybe", "try", "probably" → use `brave_web_search`

## COMMANDS

| Command                                                                       | Purpose                             | When                                                                               |
| ----------------------------------------------------------------------------- | ----------------------------------- | ---------------------------------------------------------------------------------- |
| `dotnet build`                                                                | Build entire solution               | After any C# file change                                                           |
| `dotnet build -c Debug-Sass`                                                  | Compile SCSS and minify JS          | After modifying .scss or .js files that need compilation (see compilerconfig.json) |
| `dotnet build --no-restore`                                                   | Fast build (after restore)          | Subsequent builds                                                                  |
| `dotnet build --verbosity quiet`                                              | Build + show warnings               | Verify zero warnings (except IL2111)                                               |
| `dotnet test`                                                                 | Run all 258 tests (~1.4s)           | Before commit                                                                      |
| `dotnet test -- --treenode-filter "/*/*/*/*[Category=Smoke]"`                 | Smoke tests (27, ~0.8s)             | Fast validation                                                                    |
| `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Home]"`          | Home feature tests (52)             | Feature-specific validation                                                        |
| `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Videos]"`        | Videos tests (10)                   | Feature-specific validation                                                        |
| `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Articles]"`      | Articles tests (17)                 | Feature-specific validation                                                        |
| `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Cache]"`         | Cache tests (31)                    | Feature-specific validation                                                        |
| `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Raindrop]"`      | Raindrop tests (24)                 | Feature-specific validation                                                        |
| `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:RaindropItems]"` | RaindropItems tests (17)            | Feature-specific validation                                                        |
| `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Core]"`          | Core tests (13)                     | Feature-specific validation                                                        |
| `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:ApiExample]"`    | ApiExample tests (5)                | Feature-specific validation                                                        |
| `pwsh scripts/Generate-CoverageReport.ps1`                                    | Generate coverage report            | Check coverage                                                                     |
| `pwsh scripts/View-CoverageReport.ps1`                                        | View coverage report                | Review coverage                                                                    |
| `pwsh scripts/test-build-fast.ps1`                                            | Fast dev build (~9s, AoT disabled)  | Local development                                                                  |
| `pwsh scripts/test-build-aot.ps1`                                             | Production parity testing           | Pre-deployment check                                                               |
| `pwsh scripts/DisplayWarnings.ps1`                                            | Show all build warnings             | Debug warnings                                                                     |
| `dotnet run --project src/redmuffin.Blazor.StaticWeb`                         | Start frontend only (port 5233)     | Normal development (99% of time)                                                   |
| `dotnet run --project src/redmuffin.Blazor.StaticWeb > logs/dotnet.log 2>&1`  | Start with logging to file          | Debug mode - captures all output                                                   |
| `pwsh Start.ps1`                                                              | Start full stack - interactive mode | Manual debugging                                                                   |
| `pwsh Start.ps1 -Auto`                                                        | Start full stack - automated mode   | Agent workflows                                                                    |
| `pwsh Stop.ps1`                                                               | Stop full stack processes           | Cleanup after `Start.ps1 -Auto`                                                    |

## STACK

| Technology          | Version     | Purpose                     |
| ------------------- | ----------- | --------------------------- |
| .NET                | 9.0         | Core framework              |
| Blazor              | WebAssembly | Frontend                    |
| Azure Functions     | .NET 9      | Backend                     |
| TUnit               | Latest      | Testing framework           |
| LightMock.Generator | Latest      | External dependency mocking |
| SCSS/Sass           | -           | Styling                     |

## STRUCTURE

```
src/redmuffin.Blazor.StaticWeb/          # Frontend (Blazor WASM)
src/redmuffin.Blazor.StaticWeb.Api/      # Backend (Azure Functions)
tests/                                    # Tests (mirrors src structure)
src/[Project]/Features/                   # Feature folders
tasks/PRD-XXX-*.md                        # PRD documents
skills/                                   # Skill definitions
csharp-standards, testing, ui-styling, dotnet, commits
.github/guides/                           # Detailed reference docs
```

### Partial Class Organization

**Blazor Components:**

```
Features/Home/
  Home.razor.cs           # Logic, lifecycle, properties, events
  Home.Logging.cs         # LoggerMessage delegates
```

**Services:**

```
Core/Services/
  UserService.cs          # Logic, methods, properties
  UserService.Logging.cs  # LoggerMessage delegates
```

**Tests:**

```
Core/HomeTests/
  HomeTests.cs              # [Test] methods
  HomeTests.Helpers.cs      # TestScope, mocks, utilities
  HomeTests.EdgeCases.cs    # Error handling, edge cases
  HomeTests.Infrastructure.cs  # Lifecycle, logging, DI
  HomeTests.Behavior.cs     # User interactions, workflows
```

## WORKFLOWS

### Research-First Protocol

**15-Minute Rule:**

1. **First 15 minutes:** Research ONLY. Zero code changes.
2. **After 15 minutes:** Implement ONLY if authoritative guidance found
3. **If no source found:** Escalate with research summary

**STOP Conditions:**

- 2+ file edits without testing → use `brave_web_search`
- About to "try" something uncertain → use `brave_web_search`
- Words: "maybe", "perhaps", "let's try", "I think", "probably" → use `brave_web_search`
- Same error twice → escalate to user
- Modifying without docs → use Context7
- Stuck/unsure → escalate to user

**Before Implementation, Answer:**

1. "What is the canonical way?" → find official docs
2. "Has someone else solved this?" → search GitHub/SO
3. "What does official docs say?" → Context7

**Verification Checklist:**

- [ ] Consulted authoritative source
- [ ] Can cite documentation used
- [ ] Didn't "experiment" or "guess"
- [ ] Know where to look next if fails

### Infrastructure Error Protocol (SCOPED)

**Applies to:** SCSS/Sass errors, NuGet failures, missing tools, build config issues

**Steps:**

1. Halt work on feature
2. Analyze error message thoroughly
3. Check system state (Node.js, NuGet, etc.)
4. Inspect config (`.csproj`, `compilerconfig.json`)
5. Research error using web search

**Then:**

- Explain root cause
- Recommend specific fix
- Explain why it works
- WAIT for user approval

**Does NOT Apply to:** C# syntax errors (fix immediately), test failures (debug and fix), logic errors (research and fix)

### Frontend Debugging Protocol

**When:** Site running on port 5233 shows errors in browser devtools or isn't working

**Prerequisites:** Start with `dotnet run --project src/redmuffin.Blazor.StaticWeb > logs/dotnet.log 2>&1`

**Steps:**

1. **Check dotnet logs FIRST** - Read `logs/dotnet.log`
   - Look for exceptions, build errors, 404s
   - Note warning messages
   - Do NOT guess - use actual log output

2. **Investigate based on findings** - Only after reading logs

### Web Search Decision Tree

| Query Type            | Tool                                           | Use Case                                |
| --------------------- | ---------------------------------------------- | --------------------------------------- |
| Library/framework API | Context7 (`resolve-library-id` → `query-docs`) | .NET, Blazor, NuGet, JS frameworks      |
| Keyword-specific      | `brave_web_search`                             | Errors, "how to", topics                |
| Vague/conceptual      | `websearch`                                    | "find library like X", deep exploration |
| Complex reasoning     | `sequentialthinking`                           | Architecture, debugging, refactoring    |

## PATTERNS

### Formatting

| File Type       | Indentation             | Notes                     |
| --------------- | ----------------------- | ------------------------- |
| C#              | Tab (4 tabs = 4 spaces) | -                         |
| .razor, .cshtml | 4 spaces                | -                         |
| .csproj         | 2 spaces                | -                         |
| All             | Max 160 chars           | Opening brace on new line |

### Naming Conventions

| Type               | Convention                | Example                   |
| ------------------ | ------------------------- | ------------------------- |
| Types/Namespaces   | PascalCase                | `HomePage`, `UserService` |
| Methods/Properties | PascalCase                | `GetUser()`               |
| Private fields     | camelCase                 | `_userService`            |
| Static readonly    | UpperCamelCase_underscore | `LogEvent`                |
| Interfaces         | Prefix "I"                | `IUserService`            |
| Test doubles       | `[ClassName]_[Type]`      | `NavigationManager_Mock`  |

### C# 12/13 Features

- Primary constructors
- Collection expressions: `[1, 2, 3]`
- `ref readonly` parameters
- Pattern matching in switch expressions
- `nameof` instead of string literals

### Nullable Reference Types

- Declare variables non-nullable
- Check `null` at entry points
- Use `is null` or `is not null` (NOT `== null`)

### Error Handling

- Use `LoggerMessage` delegates
- NEVER `Logger.LogError()`
- Throw specific exceptions with meaningful messages
- Test errors in `.EdgeCases.cs` files

### File-Scoped Namespaces

```csharp
namespace MyNamespace;
// Single-line using directives
// System.* first, then alphabetical
```

### Mocking Patterns

**LightMock (external dependencies):**

```csharp
var httpMock = new Mock<IHttpClientFactory>();
httpMock.Arrange(f => f.CreateClient(The<string>.IsAnyValue)).Returns(new HttpClient());
```

**Custom Mock (internal components):**

```csharp
public sealed class NavigationManager_Mock : NavigationManager
{
    public string? NavigatedTo { get; private set; }
    protected override void NavigateToCore(string uri, NavigationOptions options)
        => NavigatedTo = uri;
}
```

**Critical:** Always specify ALL parameters explicitly:

```csharp
_mock.Arrange(f => f.GetAsync("key", CancellationToken.None))
```

### Test Quality

- Use `ConfigureAwait(false)` on async calls (except asserts)
- Follow AAA structure
- Use `using` for disposal
- Test edge cases
- Zero build warnings

### Secret Management

| Method                       | Use Case                  | Syntax                                     |
| ---------------------------- | ------------------------- | ------------------------------------------ |
| Environment Variables        | MCP configs, devcontainer | `{env:VAR_NAME}` or `${env:VAR}`           |
| VS Code DevContainer Secrets | Devcontainer              | `devcontainer.json` `secrets` block        |
| VS Code Copilot MCP Inputs   | VS Code Copilot MCP       | `${input:secret_id}` with `password: true` |
| GitHub Repository Secrets    | CI/CD                     | `${{ secrets.SECRET_NAME }}`               |
| Azure Key Vault              | Production                | `az keyvault secret show`                  |
| User Secrets                 | Local .NET                | `dotnet user-secrets`                      |

**MCP Configuration:**

```json
// CORRECT
"env": { "API_KEY": "${env:API_KEY}" }

// WRONG - never hardcode
"env": { "API_KEY": "actual_secret_here" }
```

### Development Modes

| Mode       | Port | Use Case                         | Command                                               |
| ---------- | ---- | -------------------------------- | ----------------------------------------------------- |
| Normal     | 5233 | UI work, mock data (99% of time) | `dotnet run --project src/redmuffin.Blazor.StaticWeb` |
| Full Stack | 4280 | Real API, OAuth, E2E (sparingly) | `pwsh Start.ps1 -Auto`                                |

### Managing Dotnet Processes with Visual Studio Running

**Problem:** Visual Studio runs its own dotnet processes. Agents cannot use Ctrl+C, so processes must be killed explicitly. Killing all dotnet tasks terminates VS.

**Solution:**

Find the specific process using port 5233, then kill only that process by PID:

```powershell
netstat -ano | findstr :5233
taskkill /PID <PID> /F
```

**When running full stack mode:** Use `pwsh Stop.ps1` (tracks PIDs in `.dev-session.pids`)

## BOUNDARIES

### ALWAYS

- Use bash-compatible commands (PowerShell scripts via `pwsh scripts/...`)
- Use winget for package management (NOT Chocolatey)
- Run `dotnet build --verbosity quiet` after C# changes
- Run `dotnet build -c Debug-Sass` after modifying SCSS or JS files (see compilerconfig.json)
- Run `dotnet test` before commit
- Use `skill name="commits"` before `git commit`
- Use `ConfigureAwait(false)` on async calls (except asserts)
- Use `nameof` instead of string literals
- Use `is null` / `is not null` (NOT `== null`)
- Use `LoggerMessage` delegates (NEVER `Logger.LogError()`)
- Specify ALL parameters explicitly in mocks
- File-scoped namespaces
- Tab indentation for C# (4 tabs = 4 spaces)
- 4-space indentation for .razor/.cshtml
- 2-space indentation for .csproj
- Max 160 character line length
- Opening brace on new line
- PascalCase for types/namespaces/methods/properties
- camelCase for private fields
- UpperCamelCase_underscore for static readonly
- Prefix interfaces with "I"
- Test double naming: `[ClassName]_[Type]`

### ASK FIRST

- Install ANY tool/package/extension/dependency: 100% of the time ask permission
- Infrastructure/toolchain error fixes: propose solution, wait for approval

### NEVER

- Commit secrets to git - API keys, tokens, passwords in ANY file
- Hardcode secrets: `"api_key": "value"` or `Token = "secret"`
- Suggest file-based secrets (.env, appsettings.json with real values)
- Push to remote (enforced by plugin)
- Auto-commit (only after explicit command)
- Use `Logger.LogError()` (use `LoggerMessage` instead)
- Use `== null` (use `is null`)
- Use Chocolatey (use winget instead)
- Create ALL CAPS filenames (except AGENTS.md, README.md)
- Edit multiple files without testing
- Skip testing on infrastructure errors (investigate root cause)

## OUTPUT STYLE

**Vertical Density Rules:**
| Rule | Constraint |
|------|------------|
| Line width | Max 160 chars |
| Empty lines | Minimize - use only between major sections |
| Paragraphs | Avoid - use bullets/tables instead |
| Recapitulation | Never - state once, move on |
| Voice | Active, imperative |

**Format Priority:** Tables > bullets > single-line > minimal prose. ALL information preserved, only verbosity removed.

## CONTEXT

- **AOT Compilation**: Tests run with AOT in CI (`CI=true` or `GITHUB_ACTIONS=true`), disabled locally for speed
- **Security**: Zero-tolerance for secrets. If detected: stop work, alert user, rotate secret, assist with cleanup
- **Naming**: Use lowercase or PascalCase for new files (not ALL_CAPS except AGENTS.md, README.md)
- **Start.ps1 -Auto**: Creates `.dev-session.pids` for automated cleanup via `Stop.ps1`
- **Test Categories**: Smoke (fastest), Feature:X (targeted), Unit (pure, no I/O)
- **Secrets Reference**: See `.devcontainer/SECURITY.md`

## CRITICAL VERIFICATION

Before ANY solution, confirm:

- [ ] Consulted authoritative source
- [ ] Can cite documentation
- [ ] Didn't experiment/guess
- [ ] Know where to look next (not guess)

Before ANY commit, verify:

- [ ] No secrets in changed files
- [ ] No visible `password`, `secret`, `token`, `key`, `credential`, `auth` values
- [ ] Config uses `${env:VAR}` or `${input:VAR}` only
- [ ] `.gitignore` includes sensitive patterns
