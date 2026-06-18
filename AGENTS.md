---
date: 2026-06-18
title: AGENTS Project Guide (v2)
tags: [agent, rules, blazor, critical-policies, context-management, dotnet10]
description: Project-specific rules for the redmuffin.Blazor.StaticWeb repo. Cross-harness global rules are in ~/.claude/CLAUDE.md. Build and repo conventions are in rm-build-config. Commit rules are in rm-commit.
---

# AGENTS: Project Guide

> **System-wide rules**: See `~/.claude/CLAUDE.md` for communication protocol, safety blocks, API rate limits, Git rules, PowerShell patterns, NPM policies, and global workflows.
> **Commit rules**: See `rm-commit` skill.
> **Build & repo conventions**: See `rm-build-config` skill.
> **LSP tool over grep (OpenCode)**: The `lsp` tool (`findReferences`, `goToDefinition`, `hover`, `goToImplementation`, `documentSymbol`, `workspaceSymbol`, `incomingCalls`, `outgoingCalls`) is available when `OPENCODE_EXPERIMENTAL_LSP_TOOL=true`. Never use `grep`/`glob`/`read` for a code structure question when the corresponding LSP operation handles it — semantic matching eliminates false positives, sub-second vs multi-step. See `rm-opencode` for operation usage.
> **Pre-commit verification**: See §PRE-COMMIT VERIFICATION below. Never commit after a code file change without running build and tests first.
> **AGENTS.md maintenance**: See `rm-agents` skill.

## STRUCTURAL CHANGE GATE (READ FIRST — STOP HERE)

Before implementing ANY change that affects the build pipeline, toolchain, project structure, deployment, SCSS compilation, or any system spanning dev and production — you MUST answer three questions in writing:

1. **What constraints am I aware of?** — List every known constraint that applies (AGENTS.md rules, user directives, architectural decisions, platform requirements, tool policies, build requirements, Omarchy rules, npm policies, package manager rules, etc.).

2. **What do I NOT know?** — List gaps. Unknowns about the existing system. Side effects you cannot predict. User preferences you are assuming. Assumptions you're making without verification.

3. **What conflicts could this create?** — Map how the change interacts with every constraint from question 1. If any interaction is unclear or risky, you do not proceed.

If any answer is incomplete, if you are guessing about a constraint the user holds, if you are unsure about a side effect, or if the solution might collide with something else the user is balancing — **STOP AND ASK.** Do not implement. Do not edit files. Do not run commands.

## PRE-COMMIT VERIFICATION

**Never commit after a code file change without running build and tests first.**
This rule has no exceptions. LSP diagnostics are per-file only — they cannot
detect cross-file reference breakage, test failures, or runtime errors. LSP is
a fast pre-check, not a substitute for the build+test gate.

**Code files** are any file whose change can break the build or any test:

| Category      | Extensions                                 | Rationale                                           |
| ------------- | ------------------------------------------ | --------------------------------------------------- |
| C# source     | `.cs`                                      | Compilation, test logic, analyzers                  |
| Razor markup  | `.razor`                                   | Compilation, bUnit selectors, rendering             |
| Project/build | `.csproj`, `.props`, `.targets`            | Compilation, package resolution                     |
| Solution      | `.slnx`                                    | Project discovery                                   |
| SCSS          | `.scss`                                    | CSS output affects bUnit DOM assertions             |
| NuGet         | `Directory.Packages.props`, `nuget.config` | Package resolution                                  |
| PowerShell    | `.ps1`                                     | Build scripts, package tooling                      |
| Workflow      | `.github/workflows/*.yml`                  | CI pipeline — validate with `act push`, never build |

**SCSS-only changes**: CSS output affects bUnit DOM assertions. Never skip
the full build+test chain, even when only SCSS files changed — a stale test
binary from a prior build can silently pass against old C# code.

**SCSS production output**: Every `.scss` change must be accompanied by a
recompiled `app.min.css`. Run `sass --style=compressed --no-source-map
scss/app.scss:wwwroot/css/app.min.css` before committing. The sass watcher
only handles the dev CSS — production minified CSS is your responsibility.
Commit the recompiled `app.min.css` alongside the `.scss` change that
produced it.

**LSP diagnostics**: After every `edit` or `write`, the tool output includes
`<diagnostics>` tags with per-file errors. Zero diagnostics means the file
itself is clean. It does NOT mean the build passes or tests pass. Use LSP
diagnostics as an edit confirmation, never as a build replacement.

**Workflow**: edit → LSP confirms zero diagnostics →
verify (see decision tree below) → commit. Never skip a step.
Never use `dotnet test` for TUnit test runs — a stale binary from
`dotnet test` silently passes tests against old source. Use
`dotnet clean && dotnet build && dotnet run` for the full
verification cycle. Never batch-commit multiple changes without
re-running the full build+test chain.

**Verification decision tree**: Not every change needs `dotnet build`.
Start at Q1. Stop at the first match.

```
Q1: Did the change include any file in the "Code files" table above
    EXCEPT the Workflow row?
    ├─ YES → Run `dotnet build && dotnet run --project tests/...`
    │        (or SCSS-only: also `sass --style=compressed`).
    └─ NO → Q2

Q2: Did the change include only Workflow files AND non-code files
    (docs, .md, .gitignore, pipeline-neutral)?
    ├─ YES → Never run dotnet build — it cannot detect workflow
    │        issues. Validate with:
    │        `wrkflw validate .github/workflows/<file>.yml`
    │        (syntax gate) then `act push` (full pipeline gate).
    │        Then commit.
    └─ NO → This branch should not be reached. Re-trace Q1.
```

## COMMANDS

| Command                                                                                       | Purpose                                         | When                                                                                    |
| --------------------------------------------------------------------------------------------- | ----------------------------------------------- | --------------------------------------------------------------------------------------- |
| `dotnet build && dotnet run --project tests/redmuffin.Blazor.StaticWeb.Tests`                 | Verify logic & prevent regressions              | Pre-commit (mandatory — unless only workflow/non-code files changed, see decision tree) |
| `dotnet build --verbosity quiet`                                                              | Verify C# compilation                           | Immediately after any C# edit                                                           |
| `dotnet build && dotnet run --project tests/redmuffin.Tools.QualityGates.Tests`               | Run quality gates tool tests                    | After any tools/ code change                                                            |
| `sass --style=compressed --no-source-map scss/app.scss:wwwroot/css/app.min.css`               | Production SCSS build (one-shot, commit output) | Pre-commit (mandatory — see §PRE-COMMIT VERIFICATION)                                   |
| `dotnet format [<solution-path>]`                                                             | Auto-fix ~75% of StyleCop/Roslyn violations     | Before manually fixing analyzer warnings                                                |
| `dotnet clean && dotnet build && dotnet run --project tests/redmuffin.Blazor.StaticWeb.Tests` | Full verification cycle                         | After NuGet updates or repeated failures                                                |

## WORKFLOWS

- **Chrome DevTools MCP**: Configured per harness (OpenCode: `opencode.jsonc`; Grok: `~/.grok/config.toml`). Never assume it is available — ask the user to enable it when a task requires Lighthouse audits, performance tracing, screenshots, or browser-based testing. See `rm-dev-environment` for the full workflow.
- **Local workflow testing (`act`)**: Never push a workflow change without running the full pipeline locally first. `act push -W .github/workflows/azure-static-web-apps-lively-cliff-0945be603.yml -P ubuntu-latest=dotnet-sdk-node:10.0 --pull=false`. Full procedure in `rm-github-workflows` skill.
- **Quality Gates — Recursive Loop**: Gates are not one-shot. Run → fix worst violations → re-run → repeat until zero violations across all gates. See `rm-cleanup-session` §0 for the full principle.
- **Cleanup Sessions**: Load `rm-cleanup-session` to activate all 7 cleanup skills in one call.
- **Code Knowledge Graph (better-code-review-graph)**: Use for `file_summary`, `children_of`, and `large_functions` queries only. Never rely on `callers_of`, `callees_of`, `tests_for`, `inheritors_of`, `importers_of`, `impact`, or `security scan` for C# — the Tree-sitter C# parser produces incomplete semantic edges (no IMPLEMENTS, no TESTED_BY, no reliable cross-file calls). Use LSP tools for call graphs and references, quality gates for security, and `dotnet test` for test discovery.

## STACK & STRUCTURE

- **Technology Stack**: .NET 10 SDK, Blazor WebAssembly (.NET 10), Azure Functions (isolated worker, .NET 9), TUnit testing framework, SCSS.
- **SDK vs Target**: The API project targets `net9.0` for Azure SWA Functions compatibility. The Blazor WASM frontend and SwaLauncher target `net10.0`. The .NET 10 SDK provides build tooling, Roslyn, and MSBuild for both frameworks. When SWA adds .NET 10 Functions support, updating the API target is a one-line change per `.csproj`.
- **Knowledge Base**: `docs/solutions/` — searchable archive of past solutions, bugs, best practices, and workflow patterns. All entries use YAML frontmatter with `module`, `tags`, and `problem_type` fields.
- **Key Paths**:
  - `src/` — Application projects (Blazor frontend, Azure Functions API)
  - `tests/` — Test project mirror
  - `docs/solutions/` — Persistent knowledge store (YAML frontmatter: `module`, `tags`, `problem_type`)
  - `tools/` — Quality Gates toolchain (CRAP, SCRAP, Architecture, Depth, Mutation, Dupes). See `tools/README.md`.
