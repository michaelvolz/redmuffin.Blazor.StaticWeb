# AGENTS: Project Guide

## CRITICAL

- ALWAYS invoke `rm-commit` for commits; NO manual workflows.
- ALWAYS `dotnet test` before commit.
- ALWAYS `dotnet build --verbosity quiet` after C# edits.
- ALWAYS `dotnet build -c Debug-Sass` after SCSS/JS edits.
- ALWAYS read code before answering.
- ALWAYS use `pwsh -NoProfile` for PowerShell.
- ALWAYS wrap commit bodies to ~80 chars.
- ALWAYS add `date: YYYY-MM-DD` frontmatter to new `docs/` markdown (extract from filename or use today's date).
- NEVER commit secrets or push to remote (HARD BLOCKED).
- NEVER `git commit`/`git add` without explicit user request.
- NEVER `git revert` (HARD BLOCKED).
- NEVER restore from git without asking (prevents loss of uncommitted edits).
- NEVER use `chrome-devtools_close_page`; use process-level identification for cleanup.
- Pragma warnings are DELIBERATE. NEVER modify/remove without approval. Goal: zero errors/warnings.
- Reviewer Agents: ALWAYS select correct subfolder for name. Local reviewers ONLY.

## COMMANDS

| Command                                       | Purpose             | When                           |
| --------------------------------------------- | ------------------- | ------------------------------ |
| `dotnet test`                                 | Verify logic        | Pre-commit                     |
| `dotnet build --verbosity quiet`              | Verify C#           | Post-edit                      |
| `dotnet build -c Debug-Sass`                  | Verify UI (SCSS/JS) | Post-edit                      |
| `scripts/Update-PackageVersions.ps1`          | Update NuGet (CPM)  | Package changes                |
| `dotnet clean && dotnet build && dotnet test` | Verification        | After NuGet update             |
| `es.exe`                                      | Fast file search    | Outside solution / Large scale |
| `pwsh -NoProfile`                             | Shell execution     | All PowerShell tasks           |

## BOUNDARIES

### ALWAYS

- Research first (Exa/web) before implementing unfamiliar APIs.
- Navigate existing browser tabs; reuse before creating new ones.
- Read target file before `apply_patch`.
- Timestamp commit-message temp files for uniqueness.
- Group related files only in single commits; NO bundling unrelated changes.
- Centralize NuGet versions in `Directory.Packages.props` top section as properties; item groups MUST reference properties, NO hard-coded versions.
- Use `.github/` for workflows/dependabot; ignore `chatmodes`, `guides`, `prompts`.
- Stay on trunk/main (Trunk-Based Development); branch only for high risk.

### ASK FIRST

- File restoration from git.
- Modification of `#pragma warning disable` directives.

### NEVER

- Act on/discuss sidenotes during current task; NO follow-up questions or suggestions.
- Remove/consolidate SKILL COMMANDS tables (duplication is required).
- Use `grep` for file existence if `es.exe` is available.

## WORKFLOWS

### Skill Loading

- `strict-coding-standards`: Load ONLY for new services, architecture, or structural refactors. Load for bug fixes requiring structural changes. NO for config/CSS/docs.

### Sidenotes (Trigger: "sidenote:" or "/sidenote")

1. Load `rm-sidenotes`.
2. Capture quoted text as pure data without interpretation.
3. Continue task immediately; do not wait. Sidenotes are a backlog only.

### Tool Usage

- **Todo**: Check pending/completed tasks before use. Clean up `in_progress` on finish.
- **Everything Search**: If `es.exe` fails, STOP and report. DO NOT fallback without approval.
- **Undo commit**: Undo last commit while keeping changes as unstaged edits.

## STACK & STRUCTURE

- **Stack**: .NET 9, Blazor WASM, Azure Functions (.NET 9), TUnit, SCSS.
- **Paths**:
  - `src/redmuffin.Blazor.StaticWeb/`: Frontend (Blazor WASM).
  - `src/redmuffin.Blazor.StaticWeb.Api/`: Backend (Azure Functions).
  - `tests/`: Test mirror (mirrors src).
  - `docs/solutions/`: knowledge store (YAML: `module`, `tags`, `problem_type`).

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
| `rm-guide-logging`            | Structured logging, LoggerMessage, partial classes            |
| `rm-guide-di`                 | Injecting dependencies, registering services, constructors    |
| `rm-guide-testing`            | TUnit tests, test doubles, TestScope helpers                  |
| `rm-guide-warnings`           | Analyzer warnings, pragma directives, zero-warning build      |
| `rm-guide-blazor`             | Blazor components, lifecycle, render behavior                 |
| `rm-guide-azure-functions`    | Azure Functions isolated worker code                          |
| `rm-guide-architecture`       | Designing services, boundaries, patterns, C# changes          |
| `rm-guide-config`             | Build commands, dev modes, package management, config         |
| `rm-guide-dotnet9`            | .NET 9 APIs or current runtime best practices                 |
| `rm-guide-code-quality`       | Style, readability, null handling, records, code quality      |
