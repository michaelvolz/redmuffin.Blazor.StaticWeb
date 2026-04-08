# AGENTS: Project Guide

## CRITICAL

- **ALWAYS**: `rm-commit` for commits (NO manual); `dotnet test` pre-commit.
- **ALWAYS**: Use batched commits by concern (config, agents, skills, docs) even when 'all changes' is requested.
- **ALWAYS**: `dotnet build --verbosity quiet` (C#) / `dotnet build -c Debug-Sass` (SCSS/JS) post-edit.
- **ALWAYS**: Read code before answering; `pwsh -NoProfile` for PowerShell; 80-char commit wrap.
- **ALWAYS**: `date: YYYY-MM-DD` frontmatter on new `docs/` (from filename or today).
- **NEVER**: Commit secrets; push remote; `git commit/add` without request; `git revert`.
- **NEVER**: Restore from git without asking; use `chrome-devtools_close_page` (use process-level cleanup).
- **POLICY**: Pragma warnings DELIBERATE; Goal zero warnings; Reviewers: correct subfolder/Local only.

## COMMANDS

| Command                                       | Purpose             | When                         |
| --------------------------------------------- | ------------------- | ---------------------------- |
| `dotnet test`                                 | Verify logic        | Pre-commit                   |
| `dotnet build --verbosity quiet`              | Verify C#           | Post-edit                    |
| `dotnet build -c Debug-Sass`                  | Verify UI (SCSS/JS) | Post-edit                    |
| `scripts/Update-PackageVersions.ps1`          | Update NuGet (CPM)  | Package changes              |
| `dotnet clean && dotnet build && dotnet test` | Verification        | After NuGet update           |
| `es.exe`                                      | Fast file search    | Large scale/outside solution |
| `pwsh -NoProfile`                             | Shell execution     | PowerShell tasks             |

## BOUNDARIES

### ALWAYS

- Research (Exa/web) before unfamiliar APIs.
- Reuse existing browser tabs.
- Read target file before `apply_patch`.
- Timestamp commit-message temp files.
- Single-purpose commits only (no bundling unrelated changes).
- `Directory.Packages.props`: properties for versions; items ref properties; NO hard-coding.
- `.github/` for workflows/dependabot; IGNORE `chatmodes`, `guides`, `prompts`.
- Trunk-Based Development (main); branch only for high risk.

### ASK FIRST

- Git restoration.
- `#pragma warning disable` changes.

### NEVER

- Discuss/act on sidenotes during task.
- Remove/consolidate SKILL COMMANDS tables (duplication mandatory).
- `grep` for file existence if `es.exe` is available.

## WORKFLOWS

- **Skill Loading (`strict-coding-standards`)**: ONLY for new services, architecture, or structural refactors (including structural bug fixes). NOT for config/CSS/docs.
- **Sidenotes ("sidenote:" / "/sidenote")**: Load `rm-sidenotes`; capture raw quoted text; continue task immediately. Sidenotes = backlog only.
- **Todo Tool**: Check tasks before use; clear `in_progress` on finish.
- **Everything Search**: If `es.exe` fails, STOP and report. DO NOT fallback without approval.
- **Undo Commit**: Undo last commit while keeping changes as unstaged edits.
- **Review Skills**: Check subfolders for analyzers and reviewers before starting them to have the correct name. Retry if one fails and doublecheck the name.

## PowerShell (Cross-Platform)

- You are running in PowerShell 7+ (`pwsh`).
- Always prefer native cmdlets and modules over bash-style commands.
- Use proper PowerShell quoting/escaping (backticks for special chars, `@' '@` for literals).
- Prefer structured output (`ConvertTo-Json`, `Out-String -Width 4096`).
- Handle errors with `try/catch`; use `-ErrorAction Stop`.
- Use full paths with `\` or `/` interchangeably; prefer `Join-Path`.
- Modules available: PSReadLine, Microsoft.PowerShell.Management, etc. (your profile modules load if -NoProfile is false).
- Never assume bash, sh, or Unix tools unless explicitly requested.

## NPM Global Packages (Supply Chain Security)

- **Global packages** are protected by a 7-day release age filter (`min-release-age=10080` in `.npmrc`).
- This delay protects against supply chain attacks (typosquatting, malicious releases).
- **NEVER bypass this protection** — always find the latest version older than 7 days.

### Updating Global NPM Packages

1. Check release dates: `npm view <pkg> time --json`
2. Identify versions older than 7 days from today.
3. Install safe version: `npm config delete min-release-age && npm install -g <pkg>@<safe-version> && npm config set min-release-age 10080`
4. Verify: `npm list -g --depth=0`

Example for updating prettier:

```
# 1. Check versions and dates
npm view prettier time --json

# 2. Find latest version older than 7 days
# 3.29 days ago = 3.8.1 (01/21/2026)
npm config delete min-release-age
npm install -g prettier@3.8.1
npm config set min-release-age 10080
```

### CRITICAL: Shell-Aware Command Execution

The `bash` tool's shell differs by platform. This determines how PowerShell commands must be written:

| Platform      | Shell  | Simple commands                                  | Complex scripts                    |
| ------------- | ------ | ------------------------------------------------ | ---------------------------------- |
| Windows       | `pwsh` | Direct — no wrapper                              | `pwsh -NoProfile -File script.ps1` |
| Linux/omarchy | `bash` | `pwsh -NoProfile -Command '...'` (single quotes) | `pwsh -NoProfile -File script.ps1` |

**Windows (shell = pwsh)**: The shell is already pwsh. Never wrap in `pwsh -NoProfile -Command "..."`.
The outer pwsh interpolates `$`, `@{}`, `$_`, `()` **before** the inner pwsh sees them, causing
silent data loss and parse errors.

**Linux/omarchy (shell = bash)**: You MUST use `pwsh -NoProfile -Command '...'` with **single quotes**
so bash passes `$variables` through to pwsh untouched. Double quotes let bash interpolate first.

**DO** on Windows (shell is pwsh — run directly):

```
Get-ChildItem | ForEach-Object { $_.Name }
```

**DO** on Linux/omarchy (shell is bash — single-quoted wrapper):

```
pwsh -NoProfile -Command 'Get-ChildItem | ForEach-Object { $_.Name }'
```

**DON'T** on Windows (double-evaluation destroys `$` and `@`):

```
pwsh -NoProfile -Command "Get-ChildItem | ForEach-Object { $_.Name }"
```

**For complex scripts** (both platforms): Write a `.ps1` file with the `write` tool, then execute:

```
pwsh -NoProfile -File path/to/script.ps1
```

## STACK & STRUCTURE

- **Stack**: .NET 9, Blazor WASM, Azure Functions (.NET 9), TUnit, SCSS.
- **Paths**:
  - `src/redmuffin.Blazor.StaticWeb/`: Frontend.
  - `src/redmuffin.Blazor.StaticWeb.Api/`: Backend.
  - `tests/`: Test mirror.
  - `docs/solutions/`: Knowledge store (YAML: `module`, `tags`, `problem_type`).

## SKILL REFERENCES

| Skill                         | Trigger When...                                               |
| ----------------------------- | ------------------------------------------------------------- |
| `rm-nuget-manager`            | NuGet package updates                                         |
| `rm-agent-markdown-optimizer` | "optimize for agents", "make agent-friendly"                  |
| `rm-commit`                   | Commit / Save / Checkin                                       |
| `rm-guide-naming`             | New C# types, members, namespaces, test doubles               |
| `rm-guide-csharp-features`    | C# 12/13 syntax, collection expressions, primary constructors |
| `rm-guide-async`              | Async methods, cancellation flows, Task-based APIs            |
| `rm-guide-namespaces`         | New C# files or organizing namespaces                         |
| `rm-guide-logging`            | Structured logging, LoggerMessage, partial class organization |
| `rm-guide-di`                 | Injecting dependencies, registering services, constructors    |
| `rm-guide-testing`            | TUnit tests, test doubles, TestScope helpers                  |
| `rm-guide-warnings`           | Analyzer warnings, pragma directives, zero-warning build      |
| `rm-guide-blazor`             | Blazor components, lifecycle, render behavior                 |
| `rm-guide-azure-functions`    | Azure Functions isolated worker code                          |
| `rm-guide-architecture`       | Designing services, boundaries, patterns, C# changes          |
| `rm-guide-config`             | Build commands, dev modes, package management, config         |
| `rm-guide-dotnet9`            | .NET 9 APIs or current runtime best practices                 |
| `rm-guide-code-quality`       | Style, readability, null handling, records, code quality      |
