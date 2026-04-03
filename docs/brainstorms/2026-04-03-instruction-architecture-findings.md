---
date: 2026-04-03
topic: instruction-architecture-findings
---

# Instruction Architecture — Complete Findings & Final Recommendations

## Executive Summary

After auditing all 34 instruction files in the repo, we found:

- **2 files are 3rd-party** (`skill-creator`, `expert-dotnet`) — isolate in `vendor/` folders
- **7 snippets were misclassified** — their content belongs in skills, not snippets
- **4 snippets were misclassified** — they are actually commands, not text macros
- **The `snippet/` directory should be eliminated entirely** — every file was either consolidated into a skill or converted to a command
- **AGENTS.md is 4.8x too large** (292 lines vs ~60 line target) — trim to global rules only

## OpenCode Mechanism Reference (Verified 2026-04-03, v1.3.3)

| Mechanism     | Purpose                                            | Loading                                                     | Best For                                                             |
| ------------- | -------------------------------------------------- | ----------------------------------------------------------- | -------------------------------------------------------------------- |
| **AGENTS.md** | Root project guide                                 | Eager (startup)                                             | Global rules, stack overview, skill reference table                  |
| **Skills**    | Full workflow instructions                         | Lazy (metadata at startup, full on-demand via `skill` tool) | Domain-specific coding standards, testing patterns, build workflows  |
| **Commands**  | Action triggers (typed by user as `/command-name`) | On-demand (user invokes)                                    | Operational workflows: cleanup, debug, verify, commit                |
| **Agents**    | Specialized AI personas                            | On-demand (user selects via Tab or @mention)                | Accessibility review, Azure architecture, beast mode problem solving |
| **Snippets**  | Text macros/expansions                             | On-demand (user types alias)                                | Quick text insertion — NOT for instructions or workflows             |

## Subfolder Support Verification

| Type         | Subfolder Support? | Evidence                                                                                                                                                                  | Verdict                                       |
| ------------ | ------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------- |
| **Skills**   | YES                | Issue #10964 "Auto-discovery of nested skills" — closed/completed. Docs confirm `.opencode/skills/<name>/SKILL.md` pattern.                                               | ✅ Can use `vendor/` subfolder                |
| **Agents**   | YES                | PR #1999 "make markdown agent files in subfolder discoverable" — merged. Issue #2369 "subdirectory subagent definitions not being picked up" — closed/completed.          | ✅ Can use `vendor/` subfolder                |
| **Commands** | UNCERTAIN          | Docs say "Create markdown files in the `commands/` directory" but do NOT explicitly mention subfolder support. No GitHub issue found confirming nested command discovery. | ⚠️ Keep flat, use `rm-` prefix for separation |

## Skill Name Regex Constraint

Official docs state skill `name` must match: `^[a-z0-9]+(-[a-z0-9]+)*$`

This regex does NOT allow colons. However, the global `ce:plan` skill at `~/.config/opencode/skills/ce-plan/SKILL.md` uses `name: ce:plan` in its frontmatter. This works in practice but is undocumented.

**Decision**: Use hyphen-only names for safety (`rm-csharp-standards`). Put conversational shortcuts in the description field (`Shortcut: rm:cs`).

## Current State Audit

### File Inventory (34 total)

| Type      | Count | Custom | 3rd Party                            |
| --------- | ----- | ------ | ------------------------------------ |
| AGENTS.md | 1     | 1      | 0                                    |
| Skills    | 15    | 13     | 2 (`skill-creator`, `expert-dotnet`) |
| Snippets  | 11    | 11     | 0                                    |
| Agents    | 7     | 6      | 1 (`expert-dotnet`)                  |
| Commands  | 1     | 1      | 0                                    |

### Overlap Analysis (7 snippets duplicate skill content)

| Snippet                               | Duplicates                | Unique Content to Preserve                                                                                                               |
| ------------------------------------- | ------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------- |
| `csharp-standards.md` (31 lines)      | `skills/csharp-standards` | Tab indentation, 160 char max, static readonly naming, primary constructors, collection expressions, ref readonly                        |
| `dotnet-best-practices.md` (38 lines) | `skills/dotnet`           | Primary constructor DI, Command Handler pattern, namespace structure, service lifetimes, Task.WhenAll, ResourceManager/.resx             |
| `async.md` (36 lines)                 | Not in any skill          | Entire async section: naming, return types, pitfalls, IAsyncEnumerable, TAP                                                              |
| `tunit.md` (34 lines)                 | `skills/testing`          | Test naming convention, lifecycle hooks, assertion chaining, advanced attributes ([Repeat], [Retry], [Skip], [NotInParallel], [Timeout]) |
| `testscope.md` (76 lines)             | `skills/testing`          | Full TestScope code, TestHttpClientFactory variants, TestLogger implementation, fluent chaining rules                                    |
| `dotnet-build.md` (25 lines)          | `skills/dotnet`           | AOT testing note (`CI=true`)                                                                                                             |
| `design-patterns.md` (29 lines)       | Not in any skill          | Required patterns, review checklist, key focus areas                                                                                     |

### Misclassification Analysis (4 snippets are actually commands)

| Snippet                 | Why it's a command, not a snippet                                                |
| ----------------------- | -------------------------------------------------------------------------------- |
| `cleanup-devserver.md`  | Executes a sequence of operations (kill processes, close browsers, verify ports) |
| `find-bug.md`           | Triggers an evidence-based debugging workflow                                    |
| `verify-plan.md`        | Conducts an interview before building                                            |
| `verify-work-so-far.md` | Triggers a retrospective review of completed work                                |

**Snippets are text macros** — they expand typed aliases into text blocks. None of these 4 files are text macros. They are all action instructions. They belong in `commands/`.

## Final Recommendations

### 1. Namespace Strategy

| Item Type                    | Folder Structure                            | Naming Convention                                  | Rationale                                                                     |
| ---------------------------- | ------------------------------------------- | -------------------------------------------------- | ----------------------------------------------------------------------------- |
| **Custom Skills**            | `.opencode/skills/rm-*/`                    | `rm-csharp-standards` (hyphen-only, matches regex) | Safe, documented, matches official regex                                      |
| **Custom Skills (shortcut)** | In description field                        | `Shortcut: rm:cs`                                  | Conversational reference, not in `name:` field                                |
| **3rd-Party Skills**         | `.opencode/skills/vendor/`                  | Original name (e.g., `skill-creator`)              | Physical isolation, zero naming changes                                       |
| **Custom Commands**          | `.opencode/commands/` (flat, NOT subfolder) | `rm-cleanup`, `rm-debug`, `rm-plan`, `rm-verify`   | Commands typed manually — short names critical. Subfolder support unverified. |
| **Custom Agents**            | `.opencode/agents/rm-*/`                    | `rm-reliable-dotnet-coder`                         | Subfolder support confirmed. `rm-` prefix for visual separation.              |
| **3rd-Party Agents**         | `.opencode/agents/vendor/`                  | Original name (e.g., `expert-dotnet`)              | Physical isolation, subfolder support confirmed                               |

### 2. Content Migration Map

| Source                             | Destination                           | Action                                                 |
| ---------------------------------- | ------------------------------------- | ------------------------------------------------------ |
| `snippet/csharp-standards.md`      | `skills/rm-csharp-standards/SKILL.md` | Merge unique content, delete snippet                   |
| `snippet/dotnet-best-practices.md` | `skills/rm-dotnet/SKILL.md`           | Merge unique content, delete snippet                   |
| `snippet/async.md`                 | `skills/rm-csharp-standards/SKILL.md` | Add new `## Async Programming` section, delete snippet |
| `snippet/tunit.md`                 | `skills/rm-testing/SKILL.md`          | Merge unique content, delete snippet                   |
| `snippet/testscope.md`             | `skills/rm-testing/SKILL.md`          | Merge unique content, delete snippet                   |
| `snippet/dotnet-build.md`          | `skills/rm-dotnet/SKILL.md`           | Add AOT note, delete snippet                           |
| `snippet/design-patterns.md`       | `skills/rm-csharp-standards/SKILL.md` | Add new `## Design Patterns` section, delete snippet   |
| `snippet/cleanup-devserver.md`     | `commands/rm-cleanup.md`              | Convert to command, rename                             |
| `snippet/find-bug.md`              | `commands/rm-debug.md`                | Convert to command, rename                             |
| `snippet/verify-plan.md`           | `commands/rm-plan.md`                 | Convert to command, rename                             |
| `snippet/verify-work-so-far.md`    | `commands/rm-verify.md`               | Convert to command, rename                             |
| `skills/skill-creator/`            | `skills/vendor/skill-creator/`        | Move, no content changes                               |
| `agents/expert-dotnet.md`          | `agents/vendor/expert-dotnet.md`      | Move, no content changes                               |
| `commands/commit.md`               | `commands/rm-commit.md`               | Rename                                                 |
| `AGENTS.md`                        | `AGENTS.md` (rewritten)               | Trim from 292 → ~60 lines                              |

### 3. Target Structure

```
.opencode/
├── skills/
│   ├── rm-csharp-standards/       # + async, design patterns, formatting details
│   ├── rm-testing/                # + TUnit details, TestScope code, mock implementations
│   ├── rm-dotnet/                 # + build commands, best practices, AOT note
│   ├── rm-commits/
│   ├── rm-dev-workflows/
│   ├── rm-output-style/
│   ├── rm-security-secrets/
│   ├── rm-generate-tasks/
│   ├── rm-create-prd/
│   ├── rm-ui-styling/
│   ├── rm-markdown/
│   ├── rm-nuget-manager/
│   ├── rm-agent-markdown-optimizer/
│   └── vendor/
│       └── skill-creator/
│
├── commands/                      # Flat — subfolder support unverified
│   ├── rm-commit.md
│   ├── rm-cleanup.md
│   ├── rm-debug.md
│   ├── rm-plan.md
│   └── rm-verify.md
│
├── agents/
│   ├── rm-reliable-dotnet-coder.md
│   ├── rm-accessibility.md
│   ├── rm-azure-architect.md
│   ├── rm-beastmode.md
│   ├── rm-debug.md
│   ├── rm-janitor.md
│   └── vendor/
│       └── expert-dotnet.md
│
├── snippet/                       # EMPTY — eliminated
│
└── AGENTS.md                      # ~60 lines: global rules + skill reference table
```

### 4. Skill Description Optimization

Every custom skill description must include:

1. **What** it does (1 line)
2. **When** to use it (trigger phrases, file types, workflows)
3. **Shortcut** reference (e.g., `Shortcut: rm:cs`)

Example:

```yaml
---
name: rm-csharp-standards
description: >
  Shortcut: rm:cs. C# coding standards, analyzer rules (StyleCop/Meziantou/Microsoft),
  LoggerMessage patterns, async programming, design patterns, and partial class organization.
  Use when writing C# code, fixing analyzer warnings, creating LoggerMessage delegates,
  or reviewing C# standards.
---
```

### 5. AGENTS.md Target (~60 lines)

Contains ONLY:

- Mandatory global rules (load `strict-coding-standards`, research-first, read code first)
- Critical boundaries (no secrets, no push, build/test before commit, port 5233 protocol)
- Stack table (6 technologies)
- Structure overview (7 paths)
- Skill reference table (13 custom skills with triggers)

Everything else moves to skills:

- COMMANDS table → `rm-dotnet` skill
- EVERYTHING SEARCH → `rm-dev-workflows` skill
- WORKFLOWS → `rm-dev-workflows` skill
- BOUNDARIES → split across `rm-security-secrets`, `rm-csharp-standards`, `rm-dev-workflows`
- CONTEXT → split across relevant skills
- Partial Classes → already in `rm-csharp-standards`
- TOOL SELECTION → `rm-dev-workflows` skill

### 6. Zero Information Loss Guarantee

- Every piece of content from all 34 files is preserved
- Overlapping content is deduplicated (not duplicated)
- Unique content from snippets is merged into parent skills
- 3rd-party files are moved, not modified
- Git tracks all changes — single `git checkout -- .opencode/ AGENTS.md` reverts everything

### 7. What We Learned (Lessons for Future)

1. **Snippets are for text macros, not instructions.** We misused them for workflow instructions. Commands are the right mechanism for action triggers.
2. **Skills are the primary lazy-loading mechanism.** OpenCode doesn't support subfolder AGENTS.md or `@file` references. Skills are the only way to load context on-demand.
3. **Namespace isolation requires both folder structure AND naming.** `vendor/` folders + `rm-` prefixes give maximum transparency.
4. **Command names must be short.** Users type them manually without autocomplete. `rm-cleanup` (10 chars) beats `redmuffin-cleanup-devserver` (28 chars).
5. **Skill names can be descriptive.** Skills are loaded via autocomplete/shortcuts, so `rm-csharp-standards` is fine.
6. **Colons work in skill names in practice** (verified: `ce:plan` uses `name: ce:plan`) but are NOT in the official regex. Use hyphens for safety, put shortcuts in descriptions.
7. **Agent subfolders work** (PR #1999, issue #2369 both closed/completed).
8. **Skill subfolders work** (issue #10964 closed/completed).
9. **Command subfolders are unverified** — keep commands flat in `.opencode/commands/`.

## Related Documents

- [Instruction Architecture Overhaul Requirements](2026-04-03-instruction-architecture-overhaul-requirements.md) — Requirements doc for the full restructure
- [rm-commits Skill Optimization](2026-04-03-rm-commits-skill-optimization-requirements.md) — Focused optimization of the commit skill
