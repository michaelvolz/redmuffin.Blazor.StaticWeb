---
date: 2026-04-20
title: AGENTS Project Guide (OpenCode-Optimized v2)
tags: [agent, rules, blazor, critical-policies, context-management, dotnet9]
description: Project-specific rules for the redmuffin.Blazor.StaticWeb repo. System-wide rules are in ~/.config/opencode/AGENTS.md. Build and repo conventions are in rm-guide-config. Commit rules are in rm-commit.
---

# AGENTS: Project Guide

> **System-wide rules**: See `~/.config/opencode/AGENTS.md` for communication protocol, safety blocks, API rate limits, Git rules, PowerShell patterns, NPM policies, and global workflows.
> **Commit rules**: See `rm-commit` skill.
> **Build & repo conventions**: See `rm-guide-config` skill.
> **Lock files**: Every `packages.lock.json` change must be committed alongside the change that caused it. Never ignore lock file drift.
> **AGENTS.md maintenance**: See `rm-agents` skill.

## STRUCTURAL CHANGE GATE (READ FIRST — STOP HERE)

**This is the highest-priority rule in this document. Violating it breaks everything else.**

Before implementing ANY change that affects the build pipeline, toolchain, project structure, deployment, SCSS compilation, or any system spanning dev and production — you MUST answer three questions in writing:

1. **What constraints am I aware of?** — List every known constraint that applies (AGENTS.md rules, user directives, architectural decisions, platform requirements, tool policies, build requirements, Omarchy rules, npm policies, package manager rules, etc.).

2. **What do I NOT know?** — List gaps. Unknowns about the existing system. Side effects you cannot predict. User preferences you are assuming. Assumptions you're making without verification.

3. **What conflicts could this create?** — Map how the change interacts with every constraint from question 1. If any interaction is unclear or risky, you do not proceed.

If any answer is incomplete, if you are guessing about a constraint the user holds, if you are unsure about a side effect, or if the solution might collide with something else the user is balancing — **STOP AND ASK.** Do not implement. Do not edit files. Do not run commands.

Structural changes are not routine edits. They touch multiple systems. The user balances a dozen constraints that you cannot see. Only through this gate can we converge safely.

## COMMANDS

| Command                                                                                       | Purpose                                            | When                                        |
| --------------------------------------------------------------------------------------------- | -------------------------------------------------- | ------------------------------------------- |
| `dotnet run --project tests/redmuffin.Blazor.StaticWeb.Tests`                                 | Verify logic & prevent regressions                 | Pre-commit (mandatory)                      |
| `dotnet build --verbosity quiet`                                                              | Verify C# compilation                              | Immediately after any C# edit               |
| `sass --watch scss:wwwroot/css`                                                               | Auto-compile SCSS on save (background)             | Start of dev session                        |
| `sass --style=compressed --no-source-map scss/app.scss:wwwroot/css/app.min.css`               | Production SCSS build (one-shot)                   | Before publish                              |
| `scripts/Update-PackageVersions.ps1`                                                          | Update NuGet packages (Central Package Management) | After any package change                    |
| `dotnet run --project tests/redmuffin.Tools.QualityGates.Tests`                               | Run quality gates tool tests (+ build)             | After any tools/ code change                |
| `dotnet format [<solution-path>]`                                                             | Auto-fix ~75% of StyleCop/Roslyn violations        | Before manually fixing analyzer warnings    |
| `dotnet clean && dotnet build && dotnet run --project tests/redmuffin.Blazor.StaticWeb.Tests` | Full verification cycle                            | After NuGet updates or repeated failures    |
| `es.exe`                                                                                      | Ultra-fast file search                             | Large solutions or searches outside project |
| `pwsh -NoProfile`                                                                             | Cross-platform PowerShell execution                | Any PowerShell task                         |

## WORKFLOWS

- **Chrome DevTools MCP**: Available but disabled by default in `opencode.jsonc`. When the agent needs Lighthouse audits, performance tracing, screenshots, or browser-based testing, it will ask for them to be enabled. Enable on demand.
- **Quality Gates — Recursive Loop**: Gates are not one-shot. Run → fix worst violations → re-run → repeat until zero violations across all gates. See `rm-gates-cleanup` §0 for the full principle.

## STACK & STRUCTURE

- **Technology Stack**: .NET 10 SDK (builds net9.0 projects for Azure SWA), Blazor WebAssembly (.NET 9), Azure Functions (isolated worker, .NET 9), TUnit testing framework, SCSS.
- **SDK vs Target**: All projects target `net9.0` for Azure SWA compatibility. The .NET 10 SDK provides build tooling, Roslyn, and MSBuild — it does not require changing target frameworks. When SWA adds .NET 10 Functions support, updating targets is a one-line change per `.csproj`.
- **Knowledge Base**: `docs/solutions/` — searchable archive of past solutions, bugs, best practices, and workflow patterns. All entries use YAML frontmatter with `module`, `tags`, and `problem_type` fields.
- **Key Paths**:
  - `src/redmuffin.Blazor.StaticWeb/` — Frontend application
  - `src/redmuffin.Blazor.StaticWeb.Api/` — Backend API
  - `tests/` — Test project mirror
  - `docs/solutions/` — Persistent knowledge store
  - `tools/` — Quality Gates toolchain (CRAP, SCRAP, Architecture, Mutation). See `tools/README.md`.
