---
date: 2026-04-03
title: "OpenCode Instruction Architecture Overhaul (v2)"
tags: [opencode, skills, architecture, agent-instructions, namespace-isolation]
problem_type: architecture-design
---

## Problem

The OpenCode instruction architecture (`~/.config/opencode/`) had a flat, mixed-origin structure with 34 files spread across skills, snippets, agents, and commands. Custom and third-party content were intermingled, 7 snippets duplicated content already present in parent skills, 4 snippets were miscategorized (should have been commands), and the root `AGENTS.md` was 292 lines — bloated with content that belonged in skills.

## Root Cause

The architecture grew organically without a namespace convention. Custom skills had no prefix to distinguish them from vendor content. Snippets were used as a catch-all without clear purpose. AGENTS.md accumulated operational details that should have been delegated to skills.

## Solution

A 10-phase migration to namespace-isolated, lazily-loaded architecture with zero information loss:

### Target Structure

```
~/.config/opencode/
├── skills/
│   ├── rm-csharp-standards/
│   ├── rm-testing/
│   ├── rm-dotnet/
│   └── ... (13 custom skills, rm-* prefixed)
│   └── vendor/skill-creator/     (3rd-party, untouched)
├── commands/                     (flat, rm-* prefixed)
│   ├── rm-commit.md
│   ├── rm-cleanup.md
│   ├── rm-debug.md
│   ├── rm-plan.md
│   └── rm-verify.md
├── agents/
│   ├── rm-reliable-dotnet-coder.md
│   └── ... (6 custom, rm-* prefixed)
│   └── vendor/expert-dotnet.md   (3rd-party, untouched)
└── AGENTS.md                     (trimmed to ~60 lines)
```

### Key Phases

1. **Vendor isolation** — move 3rd-party content to `vendor/` subdirectories
2. **Skill renaming** — prefix all custom skills with `rm-` (13 skills)
3. **Snippet consolidation** — merge 7 snippets into parent skills (`rm-csharp-standards`, `rm-testing`, `rm-dotnet`) with deduplication
4. **Snippet-to-command conversion** — convert 4 miscategorized snippets to commands
5. **Delete consolidated snippets** — `snippet/` directory eliminated entirely
6. **Agent renaming** — prefix all custom agents with `rm-` (6 agents)
7. **Command renaming** — `commands/commit.md` → `commands/rm-commit.md`
8. **Description optimization** — every skill gets a trigger-focused description with shortcut, "what" clause, and "when" clause
9. **AGENTS.md rewrite** — trim from 292 to ~60 lines; keep only mandatory global rules, stack, structure, and skill reference table
10. **Final verification** — file count audit (26 files from original 34), no duplicate content, all `name:` fields match folder names

### Deduplication Rules

- If content exists verbatim in the target skill, do not re-add
- If content is a subset, add a cross-reference
- If content adds new detail, append to the relevant section

### Result

- 34 files reduced to 26 (7 consolidated, 1 renamed)
- All custom content instantly distinguishable from vendor by `rm-` prefix
- Zero duplicate content across instruction files
- AGENTS.md under 80 lines
- No build impact — all changes are instruction files only

## Prevention

- All new custom skills, agents, and commands must use the `rm-` prefix
- 3rd-party content lives in `vendor/` subdirectories only
- Snippets are not a permanent storage mechanism — content belongs in skills or commands
- AGENTS.md contains only global rules and references; operational details live in skills
