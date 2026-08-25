---
date: 2026-06-18
title: AGENTS Project Guide (v2)
tags: [agent, rules, blazor, critical-policies, context-management, dotnet10]
description: Project-specific rules for the redmuffin.Blazor.StaticWeb repo. Cross-harness global rules are in ~/.claude/CLAUDE.md. Build and repo conventions are in rm-build-config. Commit rules are in rm-commit.
---

# AGENTS: Project Guide

> **Universal rules:** `~/.claude/CLAUDE.md` (Karpathy, commit discipline, safety).
> **Harness rules:** Grok `~/.grok/AGENTS.md` · OpenCode `~/.config/opencode/AGENTS.md` · Cursor `~/.cursor/AGENTS.md`.
> **Harness skills:** `rm-grok-build` (Grok) · `rm-opencode` (OpenCode).
> **Commit rules:** `rm-commit` skill.
> **Build & repo conventions:** `rm-build-config` skill.
> **Repo LSP config (Grok):** `.grok/lsp.json` when Grok Roslyn LSP is enabled.
> **AGENTS.md maintenance:** `rm-instruction-standards` skill.

## STRUCTURAL CHANGE GATE (READ FIRST — STOP HERE)

Before implementing ANY change that affects the build pipeline, toolchain, project structure, deployment, SCSS compilation, or any system spanning dev and production — you MUST answer three questions in writing:

1. **What constraints am I aware of?** — List every known constraint that applies (AGENTS.md rules, user directives, architectural decisions, platform requirements, tool policies, build requirements, Omarchy rules, npm policies, package manager rules, etc.).

2. **What do I NOT know?** — List gaps. Unknowns about the existing system. Side effects you cannot predict. User preferences you are assuming. Assumptions you're making without verification.

3. **What conflicts could this create?** — Map how the change interacts with every constraint from question 1. If any interaction is unclear or risky, you do not proceed.

If any answer is incomplete, if you are guessing about a constraint the user holds, if you are unsure about a side effect, or if the solution might collide with something else the user is balancing — **STOP AND ASK.** Do not implement. Do not edit files. Do not run commands.

### Azure Functions deployment boundary (non-negotiable)

`src/redmuffin.Blazor.StaticWeb.Api/` is a **separate deployment unit**
(Azure Functions). It is not a RiverBooks module.

- **Never** move code **from** the Api project into `Modules/`, the WASM host,
  Common, or any other project as part of modularization or cleanup.
- **Never** move module/host client code **into** Api to “share” types across
  the deploy boundary.
- Frontend modules may **HTTP-call** `/api/...` only. Dual-consumed DTOs go in
  `Common` when deliberate — not by extracting Functions source.
- Full rule: ADR `docs/adr/0013-riverbooks-modular-layout-and-result.md` and
  `docs/modular-monolith-module-guide-2026-08-03.md` § Hard constraint.
- Modularization roadmap (destination, Mediator, sequencing):
  `docs/specs/2026-08-03-riverbooks-modularization-roadmap-spec.md`

## PRE-COMMIT VERIFICATION

Never commit when a change can break `dotnet build` or any test without
running build and tests first. LSP diagnostics are per-file only — they
cannot detect cross-file reference breakage, test failures, or runtime
errors. LSP is a fast pre-check, not a substitute for the build+test gate.

**Impact test** — ask whether the change can affect compilation, test
outcomes, or published output. Matching a file extension is not enough.

| Category      | Extensions                                 | Rationale                                           |
| ------------- | ------------------------------------------ | --------------------------------------------------- |
| C# source     | `.cs`                                      | Compilation, test logic, analyzers                  |
| Razor markup  | `.razor`                                   | Compilation, bUnit selectors, rendering             |
| Project/build | `.csproj`, `.props`, `.targets`            | Compilation, package resolution                     |
| Solution      | `.slnx`                                    | Project discovery                                   |
| SCSS          | `.scss`                                    | CSS output affects bUnit DOM assertions             |
| NuGet         | `Directory.Packages.props`, `nuget.config` | Package resolution                                  |
| Workflow      | `.github/workflows/*.yml`                  | CI pipeline — validate with `act push`, never build |

**Never run `dotnet build` for `scripts/**` changes.** CI and MSBuild
never invoke `scripts/` (pipeline-neutral). Local helper scripts only.

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

**Verification decision tree**: Start at Q1. Stop at the first match.

```
Q1: Can this change break dotnet build, test outcomes, or published output?
    ├─ YES → Run `dotnet build && dotnet run --project tests/...`
    │        (SCSS-only: also `sass --style=compressed`).
    └─ NO  → Q2

    NO examples — never run dotnet build:
    - `scripts/**` only (add, edit, delete)
    - docs, comments, .gitignore, pipeline-neutral paths only

Q2: Did the change include workflow files?
    ├─ YES → Never run dotnet build. Validate with:
    │        `wrkflw validate .github/workflows/<file>.yml`
    │        then `act push`. Then commit.
    └─ NO  → Commit.
```

## COMMANDS

| Command                                                                                       | Purpose                                         | When                                                  |
| --------------------------------------------------------------------------------------------- | ----------------------------------------------- | ----------------------------------------------------- |
| `dotnet build && dotnet run --project tests/redmuffin.Blazor.StaticWeb.Tests`                 | Verify logic & prevent regressions              | Pre-commit when Q1 is YES (see decision tree)         |
| `dotnet build --verbosity quiet`                                                              | Verify C# compilation                           | Immediately after any C# edit                         |
| `dotnet build && dotnet run --project tests/redmuffin.Tools.QualityGates.Tests`               | Run quality gates tool tests                    | After any tools/ code change                          |
| `sass --style=compressed --no-source-map scss/app.scss:wwwroot/css/app.min.css`               | Production SCSS build (one-shot, commit output) | Pre-commit (mandatory — see §PRE-COMMIT VERIFICATION) |
| `dotnet format [<solution-path>]`                                                             | Auto-fix ~75% of StyleCop/Roslyn violations     | Before manually fixing analyzer warnings              |
| `dotnet clean && dotnet build && dotnet run --project tests/redmuffin.Blazor.StaticWeb.Tests` | Full verification cycle                         | After NuGet updates or repeated failures              |

## WORKFLOWS

- **Code intelligence:** LSP routing and harness tool names — see the active
  harness `AGENTS.md`. Never use `grep`/`glob`/`read` for semantic symbol
  queries when the active harness exposes `lsp`. Never use `grep` for
  AST-structure queries — load `ast-grep` and `rm-structural-search`.
- **Browser automation**: Load `rm-agent-browser-companion` (co-loads upstream `agent-browser`) for live-site QA, snapshots, screenshots, navigation, network, vitals, and a11y checks on redmuffin.net or local dev. Never use bUnit for live-app QA. See `rm-dev-environment` for site startup; `rm-dev-shutdown` for cleanup.
- **Structural code search**: Load `rm-structural-search` (co-loads `ast-grep`) for syntax-shape queries across `.cs` files.
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
  - `CONCEPTS.md` — Shared domain vocabulary (entities, named processes, and status concepts with project-specific meaning)
  - `tools/` — Quality Gates toolchain (CRAP, SCRAP, Architecture, Depth, Mutation, Dupes). See `tools/README.md`.

## DIRECTORY & NAMESPACE STRUCTURE

Folder names map 1:1 to namespace segments. A file at
`Features/Raindrop/Cache/RaindropItemsCache.cs` has namespace
`redmuffin.Blazor.StaticWeb.Features.Raindrop.Cache`. Module IO lives under
`src/redmuffin.Blazor.StaticWeb.Modules/Raindrop*/` (not host `Features/…/Services`).

**Feature folders (top-level):** every page, domain, and shared construct
lives under `Features/`.

| Pattern                     | Example                          | Contains                                                                                |
| --------------------------- | -------------------------------- | --------------------------------------------------------------------------------------- |
| `Features/{FeatureName}/`   | `Features/Raindrop/`             | Host domain leftovers: `Cache/`, `Presentation/`, `Models/` (IO is `Modules/Raindrop*`) |
| `Features/{PageName}/`      | `Features/HomePage/`             | Single-page feature: `.razor` + `.razor.cs` + optional `Components/`                    |
| `Features/{PageName}/`      | `Features/DebugPage/`            | Multi-page feature: sub-pages, `Services/`, `Models/`, `Components/`                    |
| `Features/Common/`          | `Features/Common/Components/`    | Shared reusable components used by 2+ features                                          |
| `Features/Common/{Domain}/` | `Features/Common/PageLoadSpeed/` | Cross-cutting domain: `Services/`, `Models/`, `Components/`                             |

**Core (app infrastructure):** `Core/` holds application-level infrastructure
shared across features but not feature-specific.

| Folder                   | Purpose                                                           |
| ------------------------ | ----------------------------------------------------------------- |
| `Core/Layout/`           | Layout components (`MainLayout`, `NavMenu`)                       |
| `Core/Services/`         | Cross-cutting services (`WarmupService`, `BrowserStorageService`) |
| `Core/ImagePlaceholder/` | Cross-cutting feature: `Abstractions/`, `Models/`, `Services/`    |
| `Core/Abstractions/`     | Truly app-wide interfaces (`IDelayProvider`)                      |

**Never:**

- Never nest pages under `Features/Pages/` — the `Pages/` level adds zero
  signal. Flat: `Features/HomePage/`.
- Never create `Services/` at the project root. Services belong in
  `Core/Services/` or `Features/{Domain}/Services/`.
- Never create generic `Models/` folders at the root or in `Core/`. Models
  belong with their consumer.
- Never add block-scoped namespaces in new code. File-scoped only:
  `namespace A.B.C;`.
- Never let a namespace drift from the file's responsibility to match its
  historical origin — keep namespaces predictable.

## QUALITYGATES ASSET NAMING

All QualityGates configuration files and generated artifacts follow a strict
no-abbreviation convention. The filename must describe exactly what it is
without requiring the reader to open it. No abbreviations: `architecture` not
`arch`, `duplicates` not `dupes`. Industry-standard acronyms (CRAP, SCRAP) are
preserved — they are more recognizable than their expanded forms.

**Directory:** `quality-gates/` — all QualityGates configuration lives here,
placed at the solution root (main: `REPO_ROOT/quality-gates/`, tools:
`REPO_ROOT/tools/quality-gates/`). Directory name matches tool name.

**Configuration files:**

| File                       | Purpose                                      |
| -------------------------- | -------------------------------------------- |
| `architecture-rules.yml`   | Component dependency rules                   |
| `exclusions.yml`           | (Future) Methods/files excluded from gates   |
| `quality-gates-config.yml` | (Future) Master config: thresholds, defaults |

**Generated artifacts** go to `/tmp/` and are never committed:

| File                     | Purpose                                  |
| ------------------------ | ---------------------------------------- |
| `/tmp/coverage-data.xml` | Cobertura coverage XML for CRAP analysis |

**CLI subcommands:**

| Subcommand     | Purpose                                            |
| -------------- | -------------------------------------------------- |
| `crap`         | Complexity Risk Analysis (kept — industry acronym) |
| `scrap`        | Structural Code Analysis (kept — industry acronym) |
| `architecture` | Dependency architecture validation                 |
| `mutation`     | Mutation testing                                   |
| `duplicates`   | Duplicate code detection                           |
| `all`          | Run all gates with defaults                        |

**CLI flags:**

| Flag                    | Purpose                          |
| ----------------------- | -------------------------------- |
| `--architecture-config` | Path to `architecture-rules.yml` |
| `--mutation-source`     | Source file for mutation testing |
| `--mutation-scan`       | Scan-only mutation mode          |
| `--duplicates`          | Enable duplicate detection gate  |

Generated artifact names describe the data, not the tool that produced it
(`coverage-data.xml` not `quality-gates-coverage.xml`).
