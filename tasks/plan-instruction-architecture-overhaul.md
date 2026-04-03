# Plan: Instruction Architecture Overhaul (v2 — Final)

## Overview

Migrate the entire instruction architecture from a flat, mixed-origin structure to a namespace-isolated, lazily-loaded system with zero information loss. All 34 files accounted for, overlapping snippets consolidated into parent skills, 4 misclassified snippets converted to commands, custom items prefixed with `rm-`, 3rd-party items moved to `vendor/` subfolders, `snippet/` directory eliminated, and root AGENTS.md trimmed to ~60 lines.

## Target Architecture

```
.opencode/
├── skills/
│   ├── rm-csharp-standards/       # Custom (128 lines) + async + design patterns + formatting details
│   ├── rm-testing/                # Custom (146 lines) + TUnit details + TestScope code + mocks
│   ├── rm-dotnet/                 # Custom (121 lines) + build commands + best practices + AOT note
│   ├── rm-commits/                # Custom (129 lines)
│   ├── rm-dev-workflows/          # Custom (37 lines)
│   ├── rm-output-style/           # Custom (51 lines)
│   ├── rm-security-secrets/       # Custom (36 lines)
│   ├── rm-generate-tasks/         # Custom (93 lines)
│   ├── rm-create-prd/             # Custom (103 lines)
│   ├── rm-ui-styling/             # Custom (111 lines)
│   ├── rm-markdown/               # Custom (87 lines)
│   ├── rm-nuget-manager/          # Custom (81 lines)
│   ├── rm-agent-markdown-optimizer/ # Custom (286 lines)
│   └── vendor/
│       └── skill-creator/         # 3rd party (no content changes)
│
├── commands/                      # FLAT — subfolder support unverified
│   ├── rm-commit.md               # Custom (7 lines) — renamed from commands/commit.md
│   ├── rm-cleanup.md              # Custom (17 lines) — converted from snippet/cleanup-devserver.md
│   ├── rm-debug.md                # Custom (10 lines) — converted from snippet/find-bug.md
│   ├── rm-plan.md                 # Custom (14 lines) — converted from snippet/verify-plan.md
│   └── rm-verify.md               # Custom (7 lines)  — converted from snippet/verify-work-so-far.md
│
├── agents/
│   ├── rm-reliable-dotnet-coder.md  # Custom (66 lines)
│   ├── rm-accessibility.md          # Custom (86 lines)
│   ├── rm-azure-architect.md        # Custom (66 lines)
│   ├── rm-beastmode.md              # Custom (138 lines)
│   ├── rm-debug.md                  # Custom (90 lines)
│   ├── rm-janitor.md                # Custom (85 lines)
│   └── vendor/
│       └── expert-dotnet.md         # 3rd party (no content changes)
│
├── snippet/                       # EMPTY — eliminated entirely
│
└── AGENTS.md                      # Trimmed from 292 → ~60 lines
```

## Consolidation Map (7 snippets → parent skills)

| Snippet                                       | Merged Into                           | Content to Merge                                                                                                                         |
| --------------------------------------------- | ------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| `snippet/csharp-standards.md` (31 lines)      | `skills/rm-csharp-standards/SKILL.md` | Tab indentation, 160 char max, static readonly naming, primary constructors, collection expressions, ref readonly                        |
| `snippet/dotnet-best-practices.md` (38 lines) | `skills/rm-dotnet/SKILL.md`           | Primary constructor DI, Command Handler pattern, namespace structure, service lifetimes, Task.WhenAll, ResourceManager/.resx             |
| `snippet/async.md` (36 lines)                 | `skills/rm-csharp-standards/SKILL.md` | Naming, return types, exception handling, performance, pitfalls, patterns (new section)                                                  |
| `snippet/tunit.md` (34 lines)                 | `skills/rm-testing/SKILL.md`          | Test naming convention, lifecycle hooks, assertion chaining, advanced attributes ([Repeat], [Retry], [Skip], [NotInParallel], [Timeout]) |
| `snippet/testscope.md` (76 lines)             | `skills/rm-testing/SKILL.md`          | Full TestScope code, TestHttpClientFactory variants, TestLogger implementation, fluent chaining rules                                    |
| `snippet/dotnet-build.md` (25 lines)          | `skills/rm-dotnet/SKILL.md`           | AOT testing note (`CI=true`)                                                                                                             |
| `snippet/design-patterns.md` (29 lines)       | `skills/rm-csharp-standards/SKILL.md` | Required patterns, review checklist, key focus areas (new section)                                                                       |

## Conversion Map (4 snippets → commands)

| Snippet                                   | New Command File         | Notes                                         |
| ----------------------------------------- | ------------------------ | --------------------------------------------- |
| `snippet/cleanup-devserver.md` (17 lines) | `commands/rm-cleanup.md` | Convert from snippet format to command format |
| `snippet/find-bug.md` (10 lines)          | `commands/rm-debug.md`   | Convert from snippet format to command format |
| `snippet/verify-plan.md` (14 lines)       | `commands/rm-plan.md`    | Convert from snippet format to command format |
| `snippet/verify-work-so-far.md` (7 lines) | `commands/rm-verify.md`  | Convert from snippet format to command format |

## Phase 1: Create Vendor Folders and Move 3rd-Party Items

**Goal**: Isolate 3rd-party content so it can be updated/reinstalled without touching custom content.

1. Create `.opencode/skills/vendor/` directory
2. Move `.opencode/skills/skill-creator/` → `.opencode/skills/vendor/skill-creator/` (no content changes)
3. Create `.opencode/agents/vendor/` directory
4. Move `.opencode/agents/expert-dotnet.md` → `.opencode/agents/vendor/expert-dotnet.md` (no content changes)

**Verification**: `ls .opencode/skills/vendor/` and `ls .opencode/agents/vendor/` show expected files.

## Phase 2: Rename Custom Skills with `rm-` Prefix

**Goal**: Visual namespace isolation — custom vs 3rd-party instantly distinguishable.

For each skill, rename the folder and update the `name:` field in the YAML frontmatter:

| Old Folder                         | New Folder                            | Frontmatter `name:` Update          |
| ---------------------------------- | ------------------------------------- | ----------------------------------- |
| `skills/commits/`                  | `skills/rm-commits/`                  | `name: rm-commits`                  |
| `skills/dev-workflows/`            | `skills/rm-dev-workflows/`            | `name: rm-dev-workflows`            |
| `skills/dotnet/`                   | `skills/rm-dotnet/`                   | `name: rm-dotnet`                   |
| `skills/output-style/`             | `skills/rm-output-style/`             | `name: rm-output-style`             |
| `skills/security-secrets/`         | `skills/rm-security-secrets/`         | `name: rm-security-secrets`         |
| `skills/generate-tasks/`           | `skills/rm-generate-tasks/`           | `name: rm-generate-tasks`           |
| `skills/create-prd/`               | `skills/rm-create-prd/`               | `name: rm-create-prd`               |
| `skills/csharp-standards/`         | `skills/rm-csharp-standards/`         | `name: rm-csharp-standards`         |
| `skills/testing/`                  | `skills/rm-testing/`                  | `name: rm-testing`                  |
| `skills/ui-styling/`               | `skills/rm-ui-styling/`               | `name: rm-ui-styling`               |
| `skills/markdown/`                 | `skills/rm-markdown/`                 | `name: rm-markdown`                 |
| `skills/nuget-manager/`            | `skills/rm-nuget-manager/`            | `name: rm-nuget-manager`            |
| `skills/agent-markdown-optimizer/` | `skills/rm-agent-markdown-optimizer/` | `name: rm-agent-markdown-optimizer` |

**Verification**: `ls .opencode/skills/` shows all 13 custom (`rm-*`) + 1 `vendor/` folder. All SKILL.md frontmatter `name:` fields match folder names.

## Phase 3: Consolidate Overlapping Snippets into Parent Skills

**Goal**: Eliminate duplicate content. 7 snippets merge into 3 parent skills.

### 3a. Merge into `skills/rm-csharp-standards/SKILL.md`

Append three new sections to the existing skill:

1. **`## Async Programming`** — from `snippet/async.md`
   - Naming conventions, return types, exception handling, performance, pitfalls, patterns
   - Remove generic content already covered by the skill (e.g., `ConfigureAwait(false)` already mentioned)

2. **`## Design Patterns`** — from `snippet/design-patterns.md`
   - Required patterns (Command, Factory, DI, Repository, Provider)
   - Review checklist
   - Key focus areas

3. **`## Formatting Quick Reference`** — from `snippet/csharp-standards.md`
   - Formatting table (tab indentation, 160 char max, brace on new line)
   - Naming conventions table (already partially duplicated — merge, don't duplicate)
   - C# 12/13 features (already in `rm-output-style` skill — cross-reference, don't duplicate)
   - Nullable rules (already in skill — merge)
   - Error handling (LoggerMessage — already in skill — reference)

**Deduplication rules**:

- If content exists verbatim in the target skill, do NOT re-add it
- If content is a subset, add a cross-reference: "See [section] above"
- If content adds new detail, append it to the relevant section

### 3b. Merge into `skills/rm-testing/SKILL.md`

1. **`## TUnit Quick Reference`** — from `snippet/tunit.md`
   - Framework attributes, lifecycle hooks, assertions, data-driven, advanced features
   - Most content already exists in the skill — deduplicate, keep unique items (e.g., `[Repeat(n)]`, `[Retry(n)]`, `[Skip("reason")]`, `[NotInParallel]`, `[Timeout]`)

2. **`## TestScope Reference`** — from `snippet/testscope.md`
   - Required structure, mock implementations, LightMock optional params, fluent chaining
   - The skill already has TestScope architecture — merge the more detailed mock implementations (TestHttpClientFactory variants, TestLogger) and fluent chaining rules

### 3c. Merge into `skills/rm-dotnet/SKILL.md`

1. **`## Build Commands Reference`** — from `snippet/dotnet-build.md`
   - Already largely duplicated in the skill's Build Commands and Test Commands sections
   - Add unique content: AOT testing note (`CI=true`), coverage script paths

2. **`## Best Practices`** — from `snippet/dotnet-best-practices.md`
   - Architecture & patterns (primary constructor DI, Command Handler pattern, namespace structure)
   - DI section (already partially covered — merge `ArgumentNullException.ThrowIfNull`, service lifetimes)
   - Async/await (already partially covered — merge `Task.WhenAll`, never `.Wait()/.Result`)
   - Resource management (ResourceManager, .resx files, disposal patterns — NEW content, add)
   - Code quality (SOLID, XML docs, C# 12+, meaningful names — cross-reference to csharp-standards)

**Verification**: After each merge, read the target file and confirm: (a) all unique content from the snippet is present, (b) no verbatim duplicates exist, (c) the file still reads coherently.

## Phase 4: Convert 4 Snippets to Commands and Delete Consolidated Ones

**Goal**: 4 snippets become commands. 7 consolidated snippets are deleted (content preserved in parent skills). `snippet/` directory becomes empty.

### 4a. Convert snippets to commands

| Old Snippet File                | New Command File         | Notes                                         |
| ------------------------------- | ------------------------ | --------------------------------------------- |
| `snippet/cleanup-devserver.md`  | `commands/rm-cleanup.md` | Convert snippet frontmatter to command format |
| `snippet/find-bug.md`           | `commands/rm-debug.md`   | Convert snippet frontmatter to command format |
| `snippet/verify-plan.md`        | `commands/rm-plan.md`    | Convert snippet frontmatter to command format |
| `snippet/verify-work-so-far.md` | `commands/rm-verify.md`  | Convert snippet frontmatter to command format |

Command format:

```yaml
---
description: <short description of what the command does>
---
<instruction content>
```

### 4b. Delete consolidated snippets (content preserved in Phase 3)

Delete these 7 files (their content was merged into parent skills in Phase 3):

- `snippet/csharp-standards.md`
- `snippet/dotnet-best-practices.md`
- `snippet/async.md`
- `snippet/tunit.md`
- `snippet/testscope.md`
- `snippet/dotnet-build.md`
- `snippet/design-patterns.md`

**Verification**: `ls .opencode/snippet/` shows empty directory (or directory can be deleted entirely). `ls .opencode/commands/` shows exactly 5 files (all `rm-*` prefixed).

## Phase 5: Rename Custom Agents with `rm-` Prefix

**Goal**: Agent namespace isolation matching skills/commands.

| Old File                          | New File                             |
| --------------------------------- | ------------------------------------ |
| `agents/reliable-dotnet-coder.md` | `agents/rm-reliable-dotnet-coder.md` |
| `agents/accessibility.md`         | `agents/rm-accessibility.md`         |
| `agents/azure-architect.md`       | `agents/rm-azure-architect.md`       |
| `agents/beastmode.md`             | `agents/rm-beastmode.md`             |
| `agents/debug.md`                 | `agents/rm-debug.md`                 |
| `agents/janitor.md`               | `agents/rm-janitor.md`               |

No content changes to agent files — only rename.

**Verification**: `ls .opencode/agents/` shows 6 `rm-*` files + 1 `vendor/` folder.

## Phase 6: Rename Custom Command

| Old File             | New File                |
| -------------------- | ----------------------- |
| `commands/commit.md` | `commands/rm-commit.md` |

Update the content to reference the renamed skill: change `Run commits skill` → `Run rm-commits skill`.

**Verification**: `ls .opencode/commands/` shows exactly 5 files (all `rm-*` prefixed). Content of `rm-commit.md` references `rm-commits`.

## Phase 7: Optimize Skill Descriptions for Triggering

**Goal**: Every custom skill has a description that reliably triggers when needed and does NOT trigger when not needed.

Update the YAML `description:` field in each skill's frontmatter to include:

1. What the skill does (1 line)
2. Specific trigger contexts: user phrases, file types, workflows
3. Shortcut reference (e.g., `Shortcut: rm:cs`)

| Skill                         | New Description                                                                                                                                                                                                                                                                                     |
| ----------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `rm-commits`                  | "Shortcut: rm:commit. Conventional commit message generation. Use when committing, writing commit messages, preparing to commit, or asked about commit format. Trigger on 'commit', 'commit this', 'save changes', 'create a commit'."                                                              |
| `rm-dev-workflows`            | "Shortcut: rm:devops. Dotnet process management (VS vs agent-owned), port 5233 management, and web search tool selection. Use when managing processes, checking ports, identifying VS-owned processes, or deciding which search tool to use."                                                       |
| `rm-dotnet`                   | "Shortcut: rm:dotnet. .NET 9 project config, DI, build commands, Azure Functions, and repo best practices. Use when working with .csproj files, dependency injection, build/test commands, Azure Functions, or coverage reports."                                                                   |
| `rm-output-style`             | "Shortcut: rm:style. C# formatting, naming conventions, C# 12/13 features, and nullable reference types. Use when writing or reviewing C# code, formatting files, naming types/variables, or handling nullable types."                                                                              |
| `rm-security-secrets`         | "Shortcut: rm:sec. Secret management, MCP env vars, and zero-tolerance security rules. Use when handling API keys, tokens, passwords, MCP secrets, environment variables, or any security-sensitive configuration."                                                                                 |
| `rm-generate-tasks`           | "Shortcut: rm:tasks. Task list generation from PRD documents. Use when asked to create tasks, todo lists, task breakdowns, or implementation plans from a PRD."                                                                                                                                     |
| `rm-create-prd`               | "Shortcut: rm:prd. Product Requirements Document (PRD) generation. Use when asked to generate a PRD, write requirements, create a feature spec, or mentions PRD/requirements document."                                                                                                             |
| `rm-csharp-standards`         | "Shortcut: rm:cs. C# coding standards, analyzer rules (StyleCop/Meziantou/Microsoft), LoggerMessage patterns, async programming, design patterns, and partial class organization. Use when writing C# code, fixing analyzer warnings, creating LoggerMessage delegates, or reviewing C# standards." |
| `rm-testing`                  | "Shortcut: rm:test. TUnit testing patterns, TestScope architecture, test categorization, mocking strategies, and TUnit analyzer rules. Use when writing tests, creating TestScopes, mocking with LightMock, or categorizing test files."                                                            |
| `rm-ui-styling`               | "Shortcut: rm:ui. Foundation CSS framework, SCSS compilation, Blazor styling, and WCAG 2.1 AA accessibility. Use when writing SCSS, using Foundation CSS classes, styling Blazor components, or implementing accessibility."                                                                        |
| `rm-markdown`                 | "Shortcut: rm:md. Markdown content standards, MarkdownLint rules (MD001-MD059), and formatting guidelines. Use when writing markdown files, fixing MarkdownLint errors, or formatting documentation."                                                                                               |
| `rm-nuget-manager`            | "Shortcut: rm:nuget. NuGet package management via dotnet CLI. Use when adding, removing, or updating NuGet package versions in .NET projects."                                                                                                                                                      |
| `rm-agent-markdown-optimizer` | "Shortcut: rm:opt. Transforms markdown into AI agent-optimized format. Use ONLY when explicitly asked to 'optimize for agents', 'make agent-friendly', 'compress for AI agents', or 'transform to agent format'. Only processes .md/.mdc files."                                                    |

**Verification**: Each description contains a shortcut, a "what" clause, and a "when" clause. No two skills have overlapping trigger phrases.

## Phase 8: Rewrite AGENTS.md (~60 lines)

**Goal**: Trim from 292 lines to ~60 lines. Contains ONLY: mandatory global rules, project overview, critical workflows, skill reference table.

### What stays (condensed):

1. Mandatory global rules (load `strict-coding-standards`, research-first, read code first)
2. Critical boundaries (no secrets, no push, build/test before commit, port 5233 protocol)
3. Stack table (6 technologies)
4. Structure overview (7 paths)
5. Skill reference table (13 custom skills with triggers)

### What moves out:

- **COMMANDS table** (27 lines) → already in `skills/rm-dotnet/SKILL.md`
- **EVERYTHING SEARCH** section (15 lines) → moved to `skills/rm-dev-workflows/SKILL.md`
- **WORKFLOWS** section (~75 lines) → moved to `skills/rm-dev-workflows/SKILL.md`
- **BOUNDARIES** section (~50 lines) → split: security rules to `rm-security-secrets`, coding rules to `rm-csharp-standards`, operational rules to `rm-dev-workflows`
- **CONTEXT** section (7 lines) → split: AOT to `rm-dotnet`, security ref to `rm-security-secrets`, test categories to `rm-testing`, naming to `rm-output-style`
- **Partial Classes** section (15 lines) → already in `rm-csharp-standards`
- **TOOL SELECTION** section (4 lines) → moved to `skills/rm-dev-workflows/SKILL.md`
- **SKILL REFERENCES** table (10 lines) → replaced with updated table using new `rm-*` names

### New AGENTS.md structure (~60 lines):

```markdown
# AGENTS: Project Guide

## MANDATORY GLOBAL RULES

For every coding, architecture, refactoring, or review task:

- Immediately load the skill "strict-coding-standards" via the skill tool
- Strictly follow every rule in that skill. No exceptions.
- NEVER answer without reading the actual code first
- Research first: use Exa code search or web search before implementing unfamiliar APIs

## CRITICAL BOUNDARIES

- NEVER commit secrets, NEVER push to remote (HARD BLOCKED)
- ALWAYS `dotnet build --verbosity quiet` after C# changes
- ALWAYS `dotnet build -c Debug-Sass` after SCSS/JS changes
- ALWAYS `dotnet test` before commit
- ALWAYS check port 5233 free before `dotnet run`, redirect output to `logs/dotnet.log`
- NEVER kill dotnet processes without verifying they are not VS-owned

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

src/redmuffin.Blazor.StaticWeb/ # Frontend (Blazor WASM)
src/redmuffin.Blazor.StaticWeb.Api/ # Backend (Azure Functions)
tests/ # Tests (mirrors src)
.opencode/skills/ # Custom skills (rm-_ prefix) + vendor/
.opencode/commands/ # Custom commands (rm-_ prefix)
.opencode/agents/ # Custom agents (rm-\* prefix) + vendor/
docs/solutions/ # Documented solutions

```

## SKILL REFERENCES

| Skill | Trigger When... |
|-------|----------------|
| `rm-csharp-standards` | Writing C# code, analyzer rules, LoggerMessage, async, design patterns |
| `rm-testing` | Writing tests, TUnit patterns, TestScope, mocking |
| `rm-dotnet` | .csproj, DI, build/test commands, Azure Functions, coverage |
| `rm-dev-workflows` | Process management, port 5233, search tool selection, Everything CLI |
| `rm-ui-styling` | Foundation CSS, SCSS, accessibility (WCAG 2.1 AA) |
| `rm-commits` | Committing, commit messages, conventional commits |
| `rm-security-secrets` | API keys, tokens, passwords, MCP env vars, security config |
| `rm-output-style` | C# formatting, naming, C# 12/13, nullable types |
| `rm-markdown` | Writing markdown, MarkdownLint errors, documentation |
| `rm-nuget-manager` | Adding/removing/updating NuGet packages |
| `rm-create-prd` | Generating PRDs, requirements documents |
| `rm-generate-tasks` | Task lists from PRDs, implementation plans |
| `rm-agent-markdown-optimizer` | "optimize for agents", "make agent-friendly" |
```

**Verification**: Count lines — target is 50-80. Confirm no content loss: every rule/command/workflow from the original exists somewhere in the new structure (either in AGENTS.md or in a skill).

## Phase 9: Update `opencode.json` if Present

Check if `opencode.json` exists and references any of the old skill/agent/snippet/command paths. Update all references to new `rm-*` prefixed names and `vendor/` paths.

**Verification**: `cat opencode.json` (if exists) — all paths resolve correctly.

## Phase 10: Final Verification

1. **File count audit**:
   - Skills: 13 custom (`rm-*`) + 1 vendor (`skill-creator`) = 14 total
   - Snippets: 0 (directory eliminated)
   - Agents: 6 custom (`rm-*`) + 1 vendor (`expert-dotnet`) = 7 total
   - Commands: 5 custom (`rm-*`) = 5 total
   - Total: 26 files (34 original - 7 consolidated - 1 command renamed = 26, correct)

2. **No duplicate content**: Verify that the 7 consolidated snippets' unique content exists in their parent skills

3. **AGENTS.md line count**: Must be under 80 lines

4. **All `name:` fields match folder names**: Every SKILL.md frontmatter `name:` matches its directory

5. **Build and test**: `dotnet build --verbosity quiet` and `dotnet test` to confirm no regressions (instruction architecture changes should not affect build, but verify)

## Execution Order and Dependencies

```
Phase 1 (vendor folders) ──────────────────────────────────────────────────────────┐
Phase 2 (rename skills) ───────────────────────────────────────────────────────────┤
Phase 3 (consolidate snippets) ────────────────────────────────────────────────────┤── Can run in parallel
Phase 4a (convert snippets to commands) ───────────────────────────────────────────┤
Phase 4b (delete consolidated snippets) ──── depends on Phase 3 ───────────────────┤
Phase 5 (rename agents) ───────────────────────────────────────────────────────────┤
Phase 6 (rename command) ──────────────────────────────────────────────────────────┤
Phase 7 (optimize descriptions) ──── depends on Phase 2 ───────────────────────────┤
Phase 8 (rewrite AGENTS.md) ──── depends on all above ─────────────────────────────┤
Phase 9 (update opencode.json) ──── depends on all above ──────────────────────────┤
Phase 10 (final verification) ──── depends on all above ───────────────────────────┘
```

**Parallel execution**: Phases 1-6 are independent file operations and can be batched. Phase 7 depends on Phase 2 (skill names must exist before descriptions reference them). Phases 8-10 are sequential.

## Risk Mitigation

- **No information loss**: Every consolidation step includes a deduplication check — content is merged, not deleted, until the merge is verified
- **Atomic rollback**: All changes are file renames and content edits tracked by git — a single `git checkout -- .opencode/ AGENTS.md` reverts everything
- **No build impact**: Instruction files are not compiled — build/test in Phase 10 is a sanity check only
- **Vendor isolation**: 3rd-party files are moved, not modified — zero risk of breaking external content
- **Command subfolder risk avoided**: Commands stay flat in `commands/` — no reliance on unverified subfolder discovery
