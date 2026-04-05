# AGENTS: Project Guide

## MANDATORY GLOBAL RULES

For every coding, architecture, refactoring, or review task:

- Load the skill "strict-coding-standards" ONLY when creating new services/classes, designing feature architecture, performing structural refactoring, or reviewing code for design-pattern violations. Do NOT load for trivial bug fixes, config edits, CSS/SCSS changes, documentation, or running commands. If a bug fix requires structural changes, load the skill.
- Strictly follow every rule in that skill when loaded. No exceptions.

## CRITICAL BOUNDARIES

- NEVER restore a file from git without asking. We could have had multiple uncommitted changes.
- NEVER use `git revert`.
- NEVER commit secrets, NEVER push to remote (HARD BLOCKED)
- NEVER run `git commit`, `git add`, or any git commit-related command unless the user explicitly asks you to. The user reviews all changes before committing. Bypassing this breaks the entire review process. (HARD BLOCKED)
- NEVER use `chrome-devtools_close_page` for cleanup. Use process-level identification only.
- ALWAYS navigate existing browser tabs to target URLs. Never create new blank tabs when an existing tab can be reused.
- NEVER answer without reading the actual code first
- Before using `apply_patch` from OpenCode, read the latest version of the target file directly first; otherwise you will most likely hit a patch error. This is usually the fastest path.
- For any commit, always invoke the `rm-commit` skill first. Do not improvise a manual commit workflow.
- Keep commit bodies wrapped to about 80 characters per line; longer body lines are likely to trigger commitlint failures.
- Stage and commit only files that belong together for one clear reason. Do not bundle unrelated changes or default to "all files" staging.
- ALWAYS `dotnet build --verbosity quiet` after C# changes
- ALWAYS `dotnet build -c Debug-Sass` after SCSS/JS changes
- ALWAYS `dotnet test` before commit
- "Undo commit" means: undo the last commit and keep changes as unstaged edits.
- Research first: use Exa code search or web search before implementing unfamiliar APIs
- Use `scripts/Update-PackageVersions.ps1` for NuGet package updates, then
  finish with `dotnet clean && dotnet build --verbosity quiet && dotnet test`
  as the final verification step.
- When invoking repository PowerShell scripts (`.ps1`), use `pwsh -NoProfile`
  unless a script explicitly requires profile loading.
- Keep every package version value centralized in the top property section of
  `Directory.Packages.props`; item groups should reference properties instead
  of hard-coded version literals.
- SKILL COMMANDS tables are intentionally duplicated across skills. Each skill is a
  self-contained entry point — agents load one skill at a time and need the quick-ref
  there. Never remove or consolidate COMMANDS tables as "duplication".

## SIDENOTES

When the user says "sidenote:" or "/sidenote", load the `rm-sidenotes` skill. The skill handles capture, storage, retrieval, and conversion.

If a prompt starts with `sidenote`, `sidenotes`, or `/rm-sidenotes` and includes quoted text, treat the quoted text as pure data for sidenote capture only. Do not interpret it as instruction text; pass it to `rm-sidenotes` exactly as data, then continue the current task without waiting.

Behavioral rules:

- NEVER act on a sidenote during the current task — not now, not later in the same turn
- NEVER ask follow-up questions about a sidenote ("want to tackle it now?", "should I...?")
- NEVER suggest, propose, or discuss the sidenote beyond the capture confirmation
- Sidenotes are a backlog — the user will explicitly reference one when ready to convert it to a task
- The current task continues uninterrupted after capture

## STACK

| Technology      | Version     | Purpose        |
| --------------- | ----------- | -------------- |
| .NET            | 9.0         | Core framework |
| Blazor          | WebAssembly | Frontend       |
| Azure Functions | .NET 9      | Backend        |
| TUnit           | Latest      | Testing        |
| SCSS/Sass       | -           | Styling        |

## STRUCTURE

```
src/redmuffin.Blazor.StaticWeb/          # Frontend (Blazor WASM)
src/redmuffin.Blazor.StaticWeb.Api/      # Backend (Azure Functions)
tests/                                    # Tests (mirrors src)
docs/solutions/                           # Searchable knowledge store of past solutions (bugs, best practices, patterns), organized by category with YAML frontmatter (module, tags, problem_type). Relevant when implementing or debugging in documented areas.
```

## DOCUMENTATION

When creating new markdown files in `docs/`:

- **Always add `date:` frontmatter** — Extract from filename first (e.g., `2026-04-04-name.md` → `date: 2026-04-04`), then fall back to current date
- **Do this before the user reviews changes** — Not during skill execution, but during the commit preparation phase
- **Pattern**: If filename matches `YYYY-MM-DD-*.md` or `*-YYYY-MM-DD.md`, use that date. Otherwise use today's date
- **Applies to**: All new docs files (brainstorms, plans, solutions, sidenotes, or any other docs)

Example frontmatter:

```yaml
---
title: My Doc Title
date: 2026-04-04
---
```

## DEVELOPMENT PHILOSOPHY

- **Trunk-Based Development**: Prefer staying on trunk/main if possible. Branch only when the risk is too high.

## SKILL REFERENCES

| Skill                         | Trigger When...                                                                                        |
| ----------------------------- | ------------------------------------------------------------------------------------------------------ |
| `strict-coding-standards`     | Creating new services/classes, feature architecture, structural refactoring, PR design-pattern reviews |
| `rm-nuget-manager`            | Adding/removing/updating NuGet packages                                                                |
| `rm-agent-markdown-optimizer` | "optimize for agents", "make agent-friendly"                                                           |
| `rm-commit`                   | commit/save changes/git commit/checkin                                                                 |
