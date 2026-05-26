---
title: "OpenCode Instruction Architecture: Namespace-Isolated, Lazy-Loaded Pattern"
problem_type: architecture_pattern
category: integration-issues
date: 2026-04-03
track: knowledge
component: opencode
module: instruction-architecture
tags:
  [opencode, skills, commands, agents, context-management, progressive-loading]
applies_when: "Setting up or reorganizing OpenCode instruction files (skills, commands, agents) for a .NET/Blazor project"
---

# OpenCode Instruction Architecture: Namespace-Isolated, Lazy-Loaded Pattern

## Context

The repo had a single 292-line `AGENTS.md` at root with no subfolder instructions, 15 skills, 11 snippets, 7 agents, and 1 command — all mixed together with no namespace, no folder organization, and no clear separation between custom repo-specific rules and 3rd-party skill content. OpenCode does NOT support subfolder `AGENTS.md` auto-discovery (issue #6316 is open). The `instructions` array in `opencode.json` eagerly loads everything at startup, causing context bloat.

> **2026-05-24 update:** Skills grew to ~22 (including the `redmuffin-standards/` sub-folder with 22 sub-skills).
> Agents expanded from ~7 to 15+ (including 5 new author-specific C# reviewers and primary/verifier agents).
> Commands were consolidated to a single `rm-forward.md`. Snippets were eliminated.
> The architectural principles below remain valid — only the counts have changed.

## Guidance

### Mechanism Selection

| Mechanism     | Loading                                    | Best For                                                            |
| ------------- | ------------------------------------------ | ------------------------------------------------------------------- |
| **AGENTS.md** | Eager (startup)                            | Global rules, stack overview, skill reference table only            |
| **Skills**    | Lazy (metadata at startup, full on-demand) | Domain-specific coding standards, testing patterns, build workflows |
| **Commands**  | On-demand (user invokes)                   | Operational workflows: cleanup, debug, verify, commit               |
| **Agents**    | On-demand (user selects)                   | Specialized personas: accessibility, Azure architecture, debug      |
| **Snippets**  | On-demand (text macro)                     | Quick text insertion — NOT for instructions or workflows            |

### Namespace Strategy

| Item Type                    | Folder                                | Naming                            | Rationale                                                                                                  |
| ---------------------------- | ------------------------------------- | --------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| **Custom Skills**            | `~/.config/opencode/skills/rm-*/`     | `rm-guide-csharp-features`        | `rm-` prefix, hyphen-only (matches OpenCode regex). Standards now under `redmuffin-standards/` sub-folder. |
| **Custom Skills (shortcut)** | Description field                     | Removed (May 2026)                | Shortcut prefixes stripped — descriptions now match folder names exactly                                   |
| **3rd-Party Skills**         | `~/.config/opencode/skills/vendor/`   | Original name                     | Physical isolation, zero naming changes. Now `cursor/` and `skill-creator/`                                |
| **Custom Commands**          | `~/.config/opencode/commands/` (flat) | `rm-forward` (only one remaining) | Commands converted to skills — `rm-commit`, `rm-cleanup`, `rm-debug` are now skills                        |
| **Custom Agents**            | `~/.config/opencode/agents/rm-*.md`   | `rm-uncle-bob-csharp-reviewer`    | `rm-` prefix for visual separation. Expanded to 15+ including 6 author reviewers                           |
| **3rd-Party Agents**         | `~/.config/opencode/agents/vendor/`   | Original name                     | Physical isolation                                                                                         |

### Key Constraints (Verified 2026-04-03, OpenCode 1.3.3)

- **Subfolder AGENTS.md**: NOT supported (issue #6316 open)
- **`@file` references in AGENTS.md**: NOT auto-resolved (issue #2225 open)
- **Skill `name:` regex**: `^[a-z0-9]+(-[a-z0-9]+)*$` — hyphens only, no colons
- **Agent subfolders**: Supported (PR #1999, issue #2369 closed)
- **Skill subfolders**: Supported (issue #10964 closed)
- **Command subfolders**: Unverified — keep flat

### Skill Description Pattern

Every skill description must include:

1. Shortcut reference (e.g., `Shortcut: rm:cs`)
2. What it does (1 line)
3. When to use it (trigger phrases, file types, workflows)

```yaml
---
name: rm-csharp-standards
description:
  "Shortcut: rm:cs. C# coding standards, analyzer rules (StyleCop/Meziantou/Microsoft),
  LoggerMessage patterns, async programming, design patterns, and partial class organization.
  Use when writing C# code, fixing analyzer warnings, creating LoggerMessage delegates,
  or reviewing C# standards."
---
```

### AGENTS.md Target (~60 lines)

Contains ONLY:

- Mandatory global rules (load `strict-coding-standards`, research-first, read code first)
- Critical boundaries (no secrets, no push, build/test before commit, port 5233 protocol)
- Stack table (5-6 technologies)
- Structure overview (7 paths)
- Skill reference table (13 custom skills with triggers)

### Snippets: Eliminate or Convert

Snippets are text macros, not instruction files. Common misclassifications:

- **Workflow instructions** → convert to commands
- **Domain knowledge** → merge into skills
- **Quick text expansions** → keep as snippets (rare)

## Why This Matters

Without this architecture:

- **Context bloat**: 292-line AGENTS.md loads on every session, wasting tokens
- **No isolation**: 3rd-party updates can overwrite custom content
- **Duplicate content**: Same rules in AGENTS.md, skills, and snippets drift apart
- **Poor triggers**: Skills without optimized descriptions get skipped by the agent
- **Lost knowledge**: No clear separation between standard best practices and repo-specific conventions

With this architecture:

- **~80% reduction** in startup context (292 → 59 lines in AGENTS.md)
- **Zero information loss**: All content preserved, deduplicated, and organized
- **Update safety**: 3rd-party content in `vendor/` folders, never modified
- **Optimal triggers**: Every skill has a description that reliably fires when needed
- **Clear provenance**: `rm-` prefix instantly identifies custom vs 3rd-party content

## When to Apply

- Setting up OpenCode for a new project
- Reorganizing an existing instruction architecture that has grown messy
- Migrating from Claude Code (`.claude/`) to OpenCode (`.opencode/`)
- Adding 3rd-party skills to a project with existing custom skills
- Consolidating overlapping instruction files

## Examples

### Before (flat, mixed, bloated)

```
~/.config/opencode/
├── skills/commits/
├── skills/dev-workflows/
├── skills/skill-creator/          # 3rd party mixed with custom
├── snippet/csharp-standards.md    # Misclassified — should be in skill
├── snippet/cleanup-devserver.md   # Misclassified — should be command
├── agents/reliable-dotnet-coder.md
├── agents/expert-dotnet.md        # 3rd party mixed with custom
└── AGENTS.md                      # 292 lines
```

### After (namespace-isolated, lazy-loaded)

```
~/.config/opencode/
├── skills/
│   ├── rm-csharp-standards/       # Custom: rm- prefix
│   ├── rm-testing/
│   ├── rm-dotnet/
│   ├── ... (10 more rm-* skills)
│   └── vendor/
│       └── skill-creator/         # 3rd party: isolated
│
├── commands/                      # Flat — subfolder unverified
│   └── rm-forward.md
│
├── agents/
│   ├── rm-reliable-dotnet-coder.md
│   ├── ... (5 more rm-* agents)
│   └── vendor/
│       └── expert-dotnet.md       # 3rd party: isolated
│
├── snippet/                       # Empty — eliminated
└── AGENTS.md                      # 59 lines
```

### Related Docs

- `docs/brainstorms/2026-04-03-instruction-architecture-overhaul-requirements.md` — Requirements document
- `docs/brainstorms/2026-04-03-instruction-architecture-findings.md` — Complete findings and recommendations
- `tasks/plan-instruction-architecture-overhaul.md` — Implementation plan
