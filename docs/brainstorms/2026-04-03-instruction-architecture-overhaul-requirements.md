---
date: 2026-04-03
topic: instruction-architecture-overhaul
---

# Instruction Architecture Overhaul

## Problem Frame

The repo has a single 292-line `AGENTS.md` at root with no subfolder instructions, 15 skills, 11 snippets, 7 agents, and 1 command — all mixed together with no namespace, no folder organization, and no clear separation between **custom repo-specific rules** and **3rd-party skill content**. OpenCode does NOT support subfolder `AGENTS.md` auto-discovery (issue #6316 is open). The `instructions` array in `opencode.json` eagerly loads everything at startup, causing context bloat. Skills are the only true lazy-loading mechanism, but they lack namespace isolation from 3rd-party skills and have inconsistent trigger quality.

## Current State Inventory

### What Exists Today

| Type               | Count         | Location              | Origin            |
| ------------------ | ------------- | --------------------- | ----------------- |
| **AGENTS.md**      | 1 (292 lines) | Root                  | Custom            |
| **Skills**         | 15            | `.opencode/skills/`   | Mixed (see below) |
| **Snippets**       | 11            | `.opencode/snippet/`  | Custom            |
| **Agents**         | 7             | `.opencode/agents/`   | Mixed (see below) |
| **Commands**       | 1             | `.opencode/commands/` | Custom            |
| **Reference docs** | 8             | `.github/guides/`     | Custom            |

### Classification: Custom vs 3rd Party

**CUSTOM (authored for this repo, should be maintained):**

| File                              | Type      | Purpose                                         | Lines  |
| --------------------------------- | --------- | ----------------------------------------------- | ------ |
| `AGENTS.md`                       | Root      | Project guide, commands, workflows, boundaries  | 292    |
| `skills/commits`                  | Skill     | Conventional commit guidance                    | 129    |
| `skills/dev-workflows`            | Skill     | Process management, port, search tool selection | 37     |
| `skills/dotnet`                   | Skill     | .NET project config, DI, build commands         | 121    |
| `skills/output-style`             | Skill     | C# formatting, naming, C# 12/13 features        | 51     |
| `skills/security-secrets`         | Skill     | Secret management, zero-tolerance rules         | 36     |
| `skills/generate-tasks`           | Skill     | Task list generation from PRD                   | 93     |
| `skills/create-prd`               | Skill     | PRD generation workflow                         | 103    |
| `skills/csharp-standards`         | Skill     | C# coding standards, analyzer rules, logging    | 128    |
| `skills/testing`                  | Skill     | TUnit patterns, TestScope, mocking              | 146    |
| `skills/ui-styling`               | Skill     | Foundation CSS, SCSS, accessibility             | 111    |
| `skills/markdown`                 | Skill     | Markdown content standards, MarkdownLint        | 87     |
| `skills/nuget-manager`            | Skill     | NuGet package management procedures             | 81     |
| `skills/agent-markdown-optimizer` | Skill     | Transform markdown for agent optimization       | 286    |
| `snippet/verify-work-so-far`      | Snippet   | Verify all work                                 | 7      |
| `snippet/verify-plan`             | Snippet   | Interview before building                       | 14     |
| `snippet/cleanup-devserver`       | Snippet   | Kill dev server, close DevTools                 | 17     |
| `snippet/find-bug`                | Snippet   | Evidence-based bug troubleshooting              | 10     |
| `snippet/dotnet-build`            | Snippet   | .NET build commands quick ref                   | 25     |
| `snippet/tunit`                   | Snippet   | TUnit framework patterns                        | 34     |
| `snippet/testscope`               | Snippet   | TestScope architecture pattern                  | 76     |
| `snippet/dotnet-best-practices`   | Snippet   | .NET/C# best practices                          | 38     |
| `snippet/design-patterns`         | Snippet   | C#/.NET design pattern checklist                | 29     |
| `snippet/csharp-standards`        | Snippet   | C# coding standards quick ref                   | 31     |
| `snippet/async`                   | Snippet   | C# async best practices                         | 36     |
| `agents/reliable-dotnet-coder`    | Agent     | Primary agent (default)                         | 66     |
| `agents/accessibility`            | Agent     | WCAG 2.1 subagent                               | 86     |
| `agents/azure-architect`          | Agent     | Azure WAF subagent                              | 66     |
| `agents/beastmode`                | Agent     | Iterative problem solver subagent               | 138    |
| `agents/debug`                    | Agent     | Debug mode subagent                             | 90     |
| `agents/janitor`                  | Agent     | C# janitor primary agent                        | 85     |
| `commands/commit`                 | Command   | Conventional commit command                     | 7      |
| `.github/guides/*` (8 files)      | Reference | Blazor, Azure Functions, REST, etc.             | varies |

**3RD PARTY (installed from external sources, leave as-is):**

| File                   | Type  | Origin                              | Purpose                         |
| ---------------------- | ----- | ----------------------------------- | ------------------------------- |
| `skills/skill-creator` | Skill | Anthropic/Claude Code skill-creator | Skill creation + eval framework |
| `agents/expert-dotnet` | Agent | Generic .NET expert template        | .NET design patterns guidance   |

### Overlap & Redundancy Analysis

| Overlap         | Files Involved                                                                                                             | Issue                                             |
| --------------- | -------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------- |
| C# standards    | `skills/csharp-standards` (128 lines) + `snippet/csharp-standards` (31 lines) + `snippet/dotnet-best-practices` (38 lines) | Same content in 3 places, different detail levels |
| Testing         | `skills/testing` (146 lines) + `snippet/tunit` (34 lines) + `snippet/testscope` (76 lines)                                 | Skill covers everything snippets do               |
| Build commands  | `skills/dotnet` (121 lines) + `snippet/dotnet-build` (25 lines) + AGENTS.md COMMANDS table                                 | Same commands in 3 places                         |
| Async patterns  | `snippet/async` (36 lines) + `skills/csharp-standards` mentions async                                                      | Minor overlap                                     |
| Design patterns | `snippet/design-patterns` (29 lines) + `agents/expert-dotnet` covers patterns                                              | Minor overlap                                     |

## Requirements

**R1. Namespace Isolation**
All custom skills, agents, snippets, and commands must be visually and structurally distinguishable from 3rd-party ones. The `redmuffin:` prefix (or equivalent) should appear in skill/agent names and descriptions so the agent and humans can instantly tell origin.

**R2. Folder Organization**
Custom instructions must be organized into subfolders by domain/purpose within `.opencode/`. 3rd-party skills must remain in their own subfolder or be clearly separated. The folder structure itself should communicate purpose.

**R3. Progressive/Lazy Loading**
The system must use OpenCode's native lazy-loading mechanisms (skills loaded on-demand via `skill` tool, snippets loaded on-demand) rather than eager-loading via `opencode.json` `instructions` array. Root `AGENTS.md` must be trimmed to only truly global, always-applicable rules.

**R4. No Information Loss**
Every piece of information currently in any instruction file must be preserved in the new structure. Nothing is deleted without being migrated. Obsolete content is moved to an archive, not deleted.

**R5. Best Practices as Base Layer**
Standard best practices for .NET 9, Blazor WASM, Azure Functions, TUnit, and SCSS should come from well-maintained 3rd-party skills or official docs (via Context7/websearch), NOT from custom files. Custom files should ONLY contain repo-specific deviations, additions, or conventions.

**R6. Optimal Skill Triggers**
Every custom skill must have a description that reliably triggers when needed and does NOT trigger when not needed. Descriptions must include both what the skill does AND specific trigger contexts (user phrases, file types, workflows).

**R7. Small AGENTS.md**
The root `AGENTS.md` must be reduced to approximately 50-80 lines containing only: mandatory global rules, project overview (stack, structure), critical workflows, and a skill reference table.

**R8. 3rd Party Update Safety**
3rd-party skills/agents must be in a location where they can be updated/reinstalled without overwriting custom content. Custom content must never be in the same directory as 3rd-party content.

## Success Criteria

- Root `AGENTS.md` is under 80 lines
- All 15 skills + 11 snippets + 7 agents + 1 command are accounted for and migrated
- Custom vs 3rd-party separation is visually obvious in folder structure and naming
- No duplicate content exists (overlaps resolved)
- Every custom skill has a trigger-optimized description
- The folder structure communicates purpose without reading file contents
- A new team member can understand the instruction architecture in under 5 minutes

## Scope Boundaries

- **In scope**: Skills, snippets, agents, commands, AGENTS.md, folder structure, naming conventions, trigger optimization
- **Out of scope**: Changing OpenCode source code, implementing subfolder AGENTS.md auto-discovery (not supported), modifying 3rd-party skill internals, global OpenCode config (`~/.config/opencode/`)
- **Deferred**: Implementing a custom plugin for auto-discovery (too complex, OpenCode may add this natively)

## Key Decisions

**Decision: Use Skills as primary lazy-loading mechanism, not `instructions` array**
Rationale: OpenCode's `instructions` array eagerly loads all files at startup. Skills only load metadata (~50 tokens) at startup and full content on-demand. This is the only native progressive-loading mechanism OpenCode provides.

**Decision: Do NOT create subfolder AGENTS.md files**
Rationale: OpenCode issue #6316 is open — subfolder AGENTS.md auto-discovery is not implemented. Creating them would be dead weight.

**Decision: Use `redmuffin-` prefix for custom skill names (not `redmuffin:`)**
Rationale: OpenCode skill names must match the regex `^[a-z0-9]+(-[a-z0-9]+)*$`. Colons are not allowed. Hyphens are the standard separator.

**Decision: Consolidate overlapping snippets into their parent skills**
Rationale: `snippet/csharp-standards`, `snippet/dotnet-best-practices`, and `snippet/async` are subsets of `skills/csharp-standards` and `skills/dotnet`. `snippet/tunit` and `snippet/testscope` are subsets of `skills/testing`. Keeping both creates maintenance burden and risks drift.

**Decision: Keep snippets for operational/quick-reference use, not domain knowledge**
Rationale: Snippets are best for short, frequently-used operational references (cleanup-devserver, find-bug, verify-plan). Domain knowledge belongs in skills.

## Dependencies / Assumptions

- OpenCode version supports skills, snippets, agents, and commands as documented
- OpenCode does NOT support subfolder AGENTS.md auto-discovery (verified via GitHub issue #6316)
- OpenCode does NOT auto-resolve `@file` references in AGENTS.md (verified via GitHub issue #2225)
- The `skill-creator` skill is 3rd-party (from Anthropic/Claude ecosystem) and should not be modified

## Outstanding Questions

### Resolve Before Planning

- [Affects R2][User decision] Do you want 3rd-party skills moved to a `.opencode/skills/vendor/` subfolder, or kept flat but with a clear naming convention (e.g., they keep their names, yours get `redmuffin-` prefix)?
- [Affects R5][User decision] For the "best practices as base layer" requirement — do you want to keep your current custom best-practice snippets as a safety net, or fully trust Context7 + websearch for standard best practices and only keep truly repo-specific deviations?

### Deferred to Planning

- [Affects R1][Technical] Should agent names also get the `redmuffin-` prefix, or only skills/snippets? (Agents are defined in `.opencode/agents/` and loaded differently.)
- [Affects R4][Technical] What is the exact migration path — do we create new files alongside old ones, verify they work, then delete old ones? Or do we do it in one atomic change?
- [Affects R6][Needs research] What is the optimal description length for skill triggering in OpenCode? The skill-creator has an optimization loop — should we run it for all 15 custom skills?

## Visual Aid: Proposed Architecture

```
.opencode/
├── skills/                          # ALL skills (OpenCode loads metadata at startup)
│   ├── redmuffin-commits/           # Custom: prefixed with redmuffin-
│   ├── redmuffin-dev-workflows/
│   ├── redmuffin-dotnet/
│   ├── redmuffin-output-style/
│   ├── redmuffin-security-secrets/
│   ├── redmuffin-generate-tasks/
│   ├── redmuffin-create-prd/
│   ├── redmuffin-csharp-standards/
│   ├── redmuffin-testing/
│   ├── redmuffin-ui-styling/
│   ├── redmuffin-markdown/
│   ├── redmuffin-nuget-manager/
│   ├── redmuffin-agent-markdown-optimizer/
│   ├── skill-creator/               # 3rd party: no prefix
│   └── (future 3rd party skills)
│
├── snippet/                         # Quick operational references
│   ├── redmuffin-cleanup-devserver/
│   ├── redmuffin-find-bug/
│   ├── redmuffin-verify-plan/
│   ├── redmuffin-verify-work-so-far/
│   └── (consolidated: tunit, testscope → skills/testing)
│       (consolidated: dotnet-build → skills/dotnet)
│       (consolidated: csharp-standards → skills/csharp-standards)
│       (consolidated: dotnet-best-practices → skills/dotnet)
│       (consolidated: async → skills/csharp-standards)
│       (consolidated: design-patterns → skills/csharp-standards)
│
├── agents/                          # Specialized personas
│   ├── redmuffin-reliable-dotnet-coder/
│   ├── redmuffin-accessibility/
│   ├── redmuffin-azure-architect/
│   ├── redmuffin-beastmode/
│   ├── redmuffin-debug/
│   ├── redmuffin-janitor/
│   └── expert-dotnet/               # 3rd party: no prefix
│
├── commands/
│   └── redmuffin-commit/
│
└── AGENTS.md                        # Trimmed to ~60 lines
```

## Alternatives Considered

| Approach                                  | Pros                                  | Cons                                       | Verdict         |
| ----------------------------------------- | ------------------------------------- | ------------------------------------------ | --------------- |
| **Subfolder AGENTS.md**                   | Auto-discovers context                | Not supported by OpenCode (#6316 open)     | Rejected        |
| **`instructions` array in opencode.json** | Simple config                         | Eager-loads everything, context bloat      | Rejected        |
| **`@file` references in AGENTS.md**       | Manual lazy loading                   | Not auto-resolved by OpenCode (#2225 open) | Rejected        |
| **Skills + snippets (proposed)**          | Native lazy loading, clear separation | Requires renaming, migration effort        | **Recommended** |
| **Custom plugin for auto-discovery**      | Would solve the problem perfectly     | High complexity, OpenCode may add natively | Deferred        |
| **Keep everything as-is**                 | Zero migration cost                   | Context bloat, no organization, overlaps   | Rejected        |

## Next Steps

→ Resume `/ce:brainstorm` to resolve the two blocking questions above, then → `/ce:plan` for structured implementation planning.

## Related Documents

- [rm-commits Skill Optimization](2026-04-03-rm-commits-skill-optimization-requirements.md) — Focused optimization of the commit skill (body length fix, trunk-based workflow, linear flow)
- [Instruction Architecture Findings](2026-04-03-instruction-architecture-findings.md) — Complete audit results and migration map
