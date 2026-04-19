---
date: 2026-04-19
title: AGENTS Project Guide (Optimized for OpenCode)
tags: [agent, rules, blazor, critical-policies, context-management]
description: Compacted single-file version. ALL original information preserved verbatim in rules/tables/commands/versions/examples. Size reduced ~30% via deduplication of phrasing, merged overlapping sections, tighter bullets. No info lost. Use this for OpenCode/agent contexts to avoid ignore/truncation on large MD (>10-15KB common limit).
---

# AGENTS: Project Guide (OpenCode-Optimized)

## CRITICAL (Merged with Boundaries)

**ALWAYS**:

- `rm-commit` for commits (NO manual); `dotnet test` pre-commit.
- Use batched commits by concern (config, agents, skills, docs) even when 'all changes' requested.
- `dotnet build --verbosity quiet` (C#) / `dotnet build -c Debug-Sass` (SCSS/JS) post-edit.
- If `dotnet test` or `dotnet build` fails repeatedly → run `dotnet clean` first.
- Read code before answering; `pwsh -NoProfile` for PowerShell; 80-char commit wrap.
- `date: YYYY-MM-DD` frontmatter on new `docs/` (from filename or today).
- Research (Exa/web) before unfamiliar APIs. Timestamp commit-message temp files. Single-purpose commits only. `Directory.Packages.props`: properties for versions; items ref properties; NO hard-coding. `.github/` for workflows/dependabot; Trunk-Based Development (main); branch only for high risk.

**NEVER**:

- Commit secrets; push remote; `git commit/add` without request; `git revert`.
- Restore from git without asking.
- Discuss/act on sidenotes during task. Remove/consolidate SKILL COMMANDS tables (duplication mandatory). `grep` for file existence if `es.exe` is available.
- Bypass NPM 7-day release age filter (`min-release-age=7` in `.npmrc`).

**POLICY**: Pragma warnings DELIBERATE; Goal zero warnings; Reviewers: correct subfolder/Local only.

**ASK FIRST**: Git restoration. `#pragma warning disable` changes.

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

## Git CLI Optimizations

| Command                                                                  | Purpose                     | When                       |
| ------------------------------------------------------------------------ | --------------------------- | -------------------------- |
| `git status --porcelain=v2 --branch`                                     | Machine-readable status     | Scripted workflows         |
| `git diff --numstat`                                                     | Line counts (tab-separated) | Code review, analysis      |
| `git diff --name-status`                                                 | File status (M/A/D/R)       | Change categorization      |
| `git for-each-ref --format='%(refname:short)' --merged HEAD refs/heads/` | Safe branch list            | Cleanup scripts            |
| `git --no-optional-locks status`                                         | No index lock               | Background/parallel agents |
| `git log --format='%H\|%an\|%s'`                                         | Custom log fields           | Release notes, analysis    |
| `git remote get-url origin 2>/dev/null`                                  | Get remote URL              | Automation                 |
| `gh pr list --json number,title`                                         | GitHub PR list              | PR workflows               |

**Why:** Porcelain output stable across Git versions/user configs. Use `--no-optional-locks` to prevent index lock in parallel agents.

## WORKFLOWS

- **Sidenotes ("sidenote:" / "/sidenote")**: Load `rm-sidenotes`; capture raw quoted text; continue task immediately. Sidenotes = backlog only.
- **Everything Search**: If `es.exe` fails, STOP and report. DO NOT fallback without approval.
- **Undo Commit**: Undo last commit while keeping changes as unstaged edits.
- **Review Skills**: Check subfolders for analyzers/reviewers agenst/skills before starting them to have correct name. Retry if one fails and doublecheck the name.

## PowerShell (Cross-Platform)

- Running in PowerShell 7+ (`pwsh`).
- Prefer native cmdlets/modules over bash-style. Proper quoting/escaping (backticks for special chars, `@' '@` for literals). Structured output (`ConvertTo-Json`, `Out-String -Width 4096`). Errors with `try/catch`; `-ErrorAction Stop`. Full paths with `\` or `/`; prefer `Join-Path`.
- **Platform shell differences**:
  - Windows (shell=pwsh): Direct — no wrapper. `Get-ChildItem | ForEach-Object { $_.Name }`
  - Linux/omarchy (shell=bash): MUST use `pwsh -NoProfile -Command '...' ` (single quotes) so bash passes `$variables` untouched. Double quotes let bash interpolate first.
  - Complex scripts (both): Write `.ps1` file with `write` tool, then `pwsh -NoProfile -File path/to/script.ps1`

## NPM Global Packages (Supply Chain Security)

- **Global packages** protected by 7-day release age filter (`min-release-age=7` in `.npmrc`). Protects against supply chain attacks (typosquatting, malicious releases). **NEVER bypass**.
- **Updating**:
  1. Check release dates: `npm view <pkg> time --json`
  2. Identify versions older than 7 days from today.
  3. Install safe version: `npm config delete min-release-age && npm install -g <pkg>@<safe-version> && npm config set min-release-age 10080`
  4. Verify: `npm list -g --depth=0`
- Example (prettier): Check `npm view prettier time --json` → find latest >7 days old (e.g. 3.8.1) → install as above.

## STACK & STRUCTURE

- **Stack**: .NET 9, Blazor WASM, Azure Functions (.NET 9), TUnit, SCSS.
- **Knowledge base**: `docs/solutions/` — documented solutions to past problems (bugs, best practices, workflow patterns), searchable by category/tags. YAML: `module`, `tags`, `problem_type`.
- **Paths**:
  - `src/redmuffin.Blazor.StaticWeb/`: Frontend.
  - `src/redmuffin.Blazor.StaticWeb.Api/`: Backend.
  - `tests/`: Test mirror.
  - `docs/solutions/`: Knowledge store.

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

## context-mode — MANDATORY routing rules (Preserved verbatim for correctness)

You have context-mode MCP tools available. These rules are NOT optional — they protect your context window from flooding. A single unrouted command can dump 56 KB into context and waste the entire session.

**Think in Code — MANDATORY**: When you need to analyze, count, filter, compare, search, parse, transform, or process data: **write code** that does the work via `context-mode_ctx_execute(language, code)` and `console.log()` only the answer. Do NOT read raw data into context to process mentally. Your role is to PROGRAM the analysis, not to COMPUTE it. Write robust, pure JavaScript — no npm dependencies, only Node.js built-ins (`fs`, `path`, `child_process`). Always use `try/catch`, handle `null`/`undefined`, and ensure compatibility with both Node.js and Bun. One script replaces ten tool calls and saves 100x context.

**BLOCKED commands — do NOT attempt these**

- **curl / wget — BLOCKED**: Any shell command containing `curl` or `wget` will be intercepted and blocked by the context-mode plugin. Do NOT retry. Instead use: `context-mode_ctx_fetch_and_index(url, source)` or `context-mode_ctx_execute(language: "javascript", code: "const r = await fetch(...)")`
- **Inline HTTP — BLOCKED**: Any shell command containing `fetch('http`, `requests.get(`, `requests.post(`, `http.get(`, or `http.request(` will be intercepted and blocked. Do NOT retry with shell. Instead use sandbox equivalent.
- **Direct web fetching — BLOCKED**: Do NOT use any direct URL fetching tool. Use the sandbox equivalent.

**REDIRECTED tools — use sandbox equivalents**

- **Shell (>20 lines output)**: Shell is ONLY for: `git`, `mkdir`, `rm`, `mv`, `cd`, `ls`, `npm install`, `pip install`, and other short-output commands. For everything else, use: `context-mode_ctx_batch_execute(commands, queries)` — run multiple commands + search in ONE call (each command: `{label: "descriptive header", command: "..."}`. Label becomes FTS5 chunk title). Or `context-mode_ctx_execute(language: "shell", code: "...")`
- **File reading (for analysis)**: If reading to **edit** → reading is correct. If reading to **analyze, explore, or summarize** → use `context-mode_ctx_execute_file(path, language, code)` instead. Only your printed summary enters context.
- **grep / search (large results)**: Search results can flood context. Use `context-mode_ctx_execute(language: "shell", code: "grep ...")` to run searches in sandbox. Only your printed summary enters context.

**Tool selection hierarchy**

1. **GATHER**: `context-mode_ctx_batch_execute(commands, queries)` — Primary tool. Runs all commands, auto-indexes output, returns search results. ONE call replaces 30+ individual calls.
2. **FOLLOW-UP**: `context-mode_ctx_search(queries: ["q1", "q2", ...])` — Query indexed content. Pass ALL questions as array in ONE call.
3. **PROCESSING**: `context-mode_ctx_execute(language, code)` | `context-mode_ctx_execute_file(path, language, code)` — Sandbox execution. Only stdout enters context.
4. **WEB**: `context-mode_ctx_fetch_and_index(url, source)` then `context-mode_ctx_search(queries)` — Fetch, chunk, index, query. Raw HTML never enters context.
5. **INDEX**: `context-mode_ctx_index(content, source)` — Store content in FTS5 knowledge base for later search.

**Output constraints**

- Keep responses under 500 words.
- Write artifacts (code, configs, PRDs) to FILES — never return them as inline text. Return only: file path + 1-line description.
- When indexing content, use descriptive source labels so others can `search(source: "label")` later.

**ctx commands**
| Command | Action |
|---------|--------|
| `ctx stats` | Call the `stats` MCP tool and display the full output verbatim |
| `ctx doctor` | Call the `doctor` MCP tool, run the returned shell command, display as checklist |
| `ctx upgrade` | Call the `upgrade` MCP tool, run the returned shell command, display as checklist |
| `ctx purge` | Call the `purge` MCP tool with confirm: true. Warns before wiping the knowledge base. |

After /clear or /compact: knowledge base and session stats are preserved. Use `ctx purge` if you want to start fresh.
