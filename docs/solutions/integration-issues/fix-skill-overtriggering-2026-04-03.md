---
title: "Fix skill over-triggering due to overly broad trigger descriptions"
problem_type: knowledge
category: integration-issues
date: 2026-04-03
track: knowledge
component: skills/strict-coding-standards
module: instruction-architecture
tags:
  - skill-configuration
  - trigger-optimization
  - strict-coding-standards
  - agents-md
  - over-triggering
applies_when: >
  A skill fires on tasks outside its intended scope — config edits, CSS changes,
  documentation, or running commands — because its trigger description uses absolute
  language like "EVERY", "ALL", or "immediately."
---

# Fix Skill Over-Triggering Due to Overly Broad Trigger Descriptions

## Context

The `strict-coding-standards` skill was triggering "very often for the strangest reasons" — firing on config edits, CSS/SCSS changes, documentation updates, and running commands, not just on architectural or structural code work. The skill was supposed to enforce SOLID principles, composition over inheritance, and TDD patterns, but it was loading for literally every task.

## Guidance

Skill trigger descriptions must use **precise, conditional language** with three parts:

1. **Positive triggers** — what the skill IS for (specific scenarios)
2. **Explicit exclusions** — what the skill is NOT for (specific scenarios)
3. **Escape hatch** — edge cases where excluded scenarios should still trigger

### The Three-Part Pattern

```
Use ONLY when [specific positive scenarios].
Do NOT load for [specific excluded scenarios].
If [excluded scenario] requires [structural/architectural change], load the skill.
```

### Applied to strict-coding-standards

**Before** (three contradictory locations, all using absolute language):

```
SKILL.md description: "...rules on EVERY code change or review."
AGENTS.md: "Immediately load the skill 'strict-coding-standards'"
SKILL.md activation: "This skill applies to ALL architecture, refactoring, feature, bugfix, or review tasks."
```

**After** (consistent across all three locations):

```
SKILL.md description: "...rules. Use ONLY when creating new services/classes,
  designing feature architecture, performing structural refactoring, or reviewing
  code for design-pattern violations. Do NOT load for trivial bug fixes, config
  edits, CSS/SCSS changes, documentation, or running commands. If a bug fix
  requires structural changes, load the skill."

AGENTS.md: "Load the skill 'strict-coding-standards' ONLY when creating new
  services/classes, designing feature architecture, performing structural
  refactoring, or reviewing code for design-pattern violations. Do NOT load
  for trivial bug fixes, config edits, CSS/SCSS changes, documentation, or
  running commands. If a bug fix requires structural changes, load the skill."

SKILL.md activation: "This skill applies to new services/classes, feature
  architecture, structural refactoring, and code reviews. It does NOT apply
  to trivial bug fixes, config edits, CSS/SCSS changes, documentation, or
  running commands. If a bug fix requires structural changes, load the skill."
```

### Files That Must Be Updated

All trigger definitions for a skill must be consistent across:

| Location                                    | What to check                                            |
| ------------------------------------------- | -------------------------------------------------------- |
| `SKILL.md` description (frontmatter)        | Primary trigger — the skill-matching system reads this   |
| `AGENTS.md` MANDATORY GLOBAL RULES          | Overrides skill matching — must match the narrowed scope |
| `AGENTS.md` SKILL REFERENCES table          | Reference for agents — should match the other two        |
| `SKILL.md` activation line (bottom of file) | Hidden contradiction risk — often forgotten              |

## Why This Matters

Overly broad trigger language causes the skill-matching system to fire on literally any task. This has three costs:

1. **Token waste** — loading a 92-line architecture skill for a CSS color change burns tokens
2. **Constraint pollution** — applying SOLID/Clean Architecture rules to a config edit produces confused, over-engineered output
3. **Latency** — unnecessary skill loading adds round-trip time to every interaction

The escape hatch ("If a bug fix requires structural changes, load the skill") is critical. Without it, the exclusion list becomes too rigid — a bug caused by a captive dependency (scoped service injected into singleton) _does_ need architectural enforcement, even though it's technically a "bug fix."

## When to Apply

- When a skill is observed firing on tasks outside its intended scope
- When creating or updating any skill's trigger definitions
- When multiple files define triggers for the same skill — all must be consistent
- When writing skill descriptions, avoid absolute language ("EVERY", "ALL", "immediately") unless the skill genuinely applies to every single task

## Examples

### Words That Cause Over-Triggering

| Word/Phrase                  | Problem                    | Replace With                                            |
| ---------------------------- | -------------------------- | ------------------------------------------------------- |
| "EVERY code change"          | Fires on all code edits    | "creating new services/classes, structural refactoring" |
| "ALL tasks"                  | Fires on everything        | List specific scenarios                                 |
| "Immediately"                | Implies no judgment needed | "when [specific condition]"                             |
| "any commit-related request" | Catch-all is too broad     | List specific commit phrases                            |
| "writing C# code"            | Fires on every C# edit     | "creating new services, designing architecture"         |

### Words That Prevent Over-Triggering

| Word/Phrase                       | Effect                                  |
| --------------------------------- | --------------------------------------- |
| "ONLY when"                       | Explicitly constrains scope             |
| "Do NOT load for"                 | Explicit exclusions                     |
| "trivial"                         | Qualifier that leaves room for judgment |
| "If X requires Y, load the skill" | Escape hatch for edge cases             |

### Related Docs

- `docs/solutions/integration-issues/opencode-instruction-management-lessons-2026-04-03.md` — Section 4 covers the **opposite** problem (under-triggering / weak triggers). Together, these two docs cover both directions of trigger optimization.
- `docs/solutions/integration-issues/opencode-instruction-architecture-pattern-2026-04-03.md` — Main architecture pattern for skill descriptions
- `docs/brainstorms/2026-04-03-instruction-architecture-overhaul-requirements.md` — R6: Optimal Skill Triggers requirement
