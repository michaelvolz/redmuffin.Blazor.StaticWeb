# AGENTS: Project Guide

# MANDATORY GLOBAL RULES (always loaded)

For every coding, architecture, refactoring, or review task:

- Immediately load the skill "strict-coding-standards" via the skill tool.
- Strictly follow every rule in that skill. No exceptions.
- If you ever violate any rule, regenerate the entire output.

CRITICAL: Never Answer Without Reading Code First
Always ground your answers in the actual code. Read the files, trace the paths, verify the behavior – then speak with confidence because you've seen it firsthand. This is what makes you reliable: every answer backed by evidence you just read.
If asked "does X do Y?" – read X before answering.
If asked "why does Z happen?" – read the code path before answering.
If asked about a design decision – read the implementation before claiming what it does.
NEVER answer questions about the codebase, architecture, or design without READING THE ACTUAL CODE FIRST. Do not speculate, assume, or guess based on naming conventions, memory, or what "makes sense." No exceptions. Getting it wrong and stating it confidently is worse than saying "let me check."

Code Philosophy

- CRITICAL – Research first: use Exa code search or web search before implementing unfamiliar APIs

## CRITICAL

- ALWAYS use PascalCase for newly created docs and scripts (except if convention exists like AGENTS.md and README.md or instructed otherwise)
- BEFORE you do any work mention how you could verify that work
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
- ALWAYS check port 5233 is free before starting dev server
- ALWAYS redirect dev server output to `logs/dotnet.log`
- NEVER kill dotnet processes without verifying they are not VS-owned
- NEVER use `pwsh -Command` for ad-hoc commands — use the Bash tool instead

## TOOL SELECTION

- **Bash tool**: All ad-hoc commands (file ops, registry queries, process management, git, netstat, etc.)
- **PowerShell (`pwsh`)**: Only for running existing scripts (`pwsh scripts/...`) or writing new `.ps1` files
- **NEVER** use `pwsh -Command` for ad-hoc one-liners — the Bash tool is faster and more reliable

## EVERYTHING SEARCH

`es.exe` (voidtools Everything CLI) must be in the system PATH for all contributors. Use it for **instant filesystem searches** when you don't know where to look or need to search outside the workspace.

**Syntax:** `es.exe <search> [options]`

| Example                                            | Purpose                                     |
| -------------------------------------------------- | ------------------------------------------- |
| `es.exe "ext:cs redmuffin" -p`                     | Find .cs files matching "redmuffin" in path |
| `es.exe "ext:razor" -sort dm -n 10`                | 10 most recently modified .razor files      |
| `es.exe "filename:AGENTS.md" -p`                   | Find specific file by name anywhere         |
| `es.exe "path:redmuffin.Blazor.StaticWeb ext:sln"` | Find solution files in project              |

**When to use:** Prefer `glob`/`grep` for workspace-relative searches. Use `es.exe` when searching the entire filesystem, locating files outside the workspace, or when glob patterns are too slow for broad searches.

**Key options:** `-p` (match full path), `-n <num>` (limit results), `-sort dm` (sort by date modified), `-sort size` (sort by size), `/a-d` (files only), `/ad` (folders only)

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
docs/solutions/                            # documented solutions to past problems, organized by category with YAML frontmatter; useful when debugging recurring issues or reviewing established patterns
.opencode/skills/                         # Skills: csharp-standards, testing, ui-styling, dotnet, commits, markdown, nuget-manager, agent-markdown-optimizer, create-prd, generate-tasks, skill-creator, output-style, security-secrets, dev-workflows
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

### Dev Server Startup Protocol

**Before starting:**

1. Check port: `netstat -ano | findstr :5233`
2. If occupied → identify owner PID (see Process Management below)
3. Kill only agent-owned PIDs: `taskkill /PID <PID> /F`
4. Verify port free: `netstat -ano | findstr :5233` (should return nothing)

**Starting:**

```bash
nohup dotnet run --project src/redmuffin.Blazor.StaticWeb > logs/dotnet.log 2>&1 &
```

**After starting:**

1. Wait + verify: `sleep 12 && netstat -ano | findstr :5233`
2. Read `logs/dotnet.log` for errors (build failures, port conflicts, WASM corruption)
3. Only open browser after clean log confirmation

**Diagnosing failures from log:**
| Log symptom | Root cause | Fix |
| -------------------------------- | ----------------------- | ----------------------------- |
| "address already in use" | Port still occupied | Kill remaining process |
| "Unexpected end of JSON input" | Corrupt blazor.boot.json| Clean rebuild |
| Build errors/warnings | Compilation failure | Fix errors, rebuild |
| No "Now listening on" line | Server never started | Check build output, restart |
| 404 on `_framework/*` | Missing wwwroot files | Full rebuild (`dotnet build`) |

## BOUNDARIES

### ALWAYS

- Bash tool for ad-hoc commands; `pwsh scripts/...` for PowerShell scripts
- `winget`
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
- Check port 5233 free before `dotnet run`
- Redirect all `dotnet run` output to `logs/dotnet.log`
- Verify server started via log before opening browser
- Ignore BuildWebCompiler2022 lock file drift — it is conditionally included (Debug-Sass only, Windows-only) and always dropped by CI/CD restores. If BuildWebCompiler2022 is the only change in packages.lock.json, do NOT commit it.

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

- ALL CAPS filenames (except AGENTS.md, README.md)
- Edit multiple files without testing
- Skip testing on infrastructure errors
- Start dev server without logging to `logs/dotnet.log`
- Kill dotnet processes without verifying ownership
- Assume port is free without checking
- NEVER circumvent git hooks (--no-verify etc.). Implemented to avoid agent mistakes. Critical

## CONTEXT

- **AOT**: CI runs AOT (`CI=true`/`GITHUB_ACTIONS=true`), disabled locally
- **Security**: Zero-tolerance. Detected secrets → stop, alert, rotate, cleanup
- **Naming**: Lowercase/PascalCase new files (not ALL_CAPS except AGENTS.md, README.md)
- **Start.ps1 -Auto**: Creates `.dev-session.pids` for `Stop.ps1` cleanup
- **Test Categories**: Smoke (fastest), Feature:X (targeted), Unit (pure, no I/O)
- **Secrets Reference**: `.devcontainer/SECURITY.md`

## SKILL REFERENCES

| Skill              | Trigger When...                                                      |
| ------------------ | -------------------------------------------------------------------- |
| `csharp-standards` | Writing C# code, analyzer rules, LoggerMessage, partial classes      |
| `testing`          | Writing tests, TUnit patterns, TestScope, mocking                    |
| `dotnet`           | .NET config, DI, build commands, Azure Functions                     |
| `ui-styling`       | Foundation CSS, SCSS, accessibility (WCAG 2.1 AA)                    |
| `commits`          | Creating commits, conventional commit messages                       |
| `markdown`         | Markdown formatting, MarkdownLint rules                              |
| `nuget-manager`    | Adding/removing NuGet packages                                       |
| `output-style`     | C# formatting, naming conventions, C# 12/13 features, nullable types |
| `security-secrets` | Secret management, MCP env vars, security rules                      |
| `dev-workflows`    | Process management, port handling, web search tool selection         |
