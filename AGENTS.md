# AGENTS: Project Guide

## CRITICAL

- **ALWAYS**: `rm-commit` for commits (NO manual); `dotnet test` pre-commit.
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
