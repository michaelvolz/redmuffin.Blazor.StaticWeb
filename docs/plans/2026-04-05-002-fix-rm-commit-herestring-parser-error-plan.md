---
title: "fix: rm-commit skill here-string parser error in COMMANDS table"
type: fix
status: active
date: 2026-04-05
---

# fix: rm-commit skill here-string parser error in COMMANDS table

## Overview

The `rm-commit` skill's COMMANDS quick-reference table shows a one-liner PowerShell pattern using `@"..."@` (here-string), which is syntactically invalid — PowerShell requires `@"` to be followed immediately by a newline. The AI agent copies this one-liner from the table, producing `ParserError: No characters are allowed after a here-string header but before the end of the line.`

## Problem Frame

The skill has two conflicting patterns:

1. **COMMANDS table (line 39):** Shows a condensed one-liner with `@"..."@` — **invalid PowerShell**
2. **WORKFLOWS section (lines 132-192):** Shows correct multi-line here-string patterns — **valid PowerShell**

The AI agent prefers the COMMANDS table for quick commits because it's the quick-reference. This produces the parser error consistently.

## Requirements Trace

- R1. COMMANDS table must show syntactically valid PowerShell that works on one line
- R2. Multi-line here-string patterns in WORKFLOWS section must remain correct and complete
- R3. The fix must not break the skill's existing workflow or retry logic
- R4. The COMMANDS table entry must be copy-pasteable without modification

## Scope Boundaries

- Only `.opencode/skills/rm-commit/SKILL.md` is modified
- No changes to git hooks, commitlint, or other tooling
- No changes to the multi-line here-string examples in WORKFLOWS (they are correct)

## Context & Research

### Relevant Code and Patterns

- `.opencode/skills/rm-commit/SKILL.md` — line 39 has the broken one-liner
- `commitlint.config.js` — enforces body-max-line-length: 100, body-empty: never
- Previous plan `docs/plans/2026-04-03-007-rm-commit-skill-optimization-plan.md` recommended `git commit -m "subject" -m "body"` but was not fully implemented

### Key Technical Decision

**Use `git commit -m "subject" -m "body"` in the COMMANDS table** instead of here-strings. This is:

- Valid one-liner PowerShell
- Cross-shell compatible (bash, pwsh, cmd)
- Simple enough for the quick-reference table
- The multi-line here-string pattern remains in WORKFLOWS for complex messages

## Implementation Units

- [ ] **Unit 1: Fix COMMANDS table one-liner**

**Goal:** Replace the invalid here-string one-liner with a valid `git commit -m` pattern

**Requirements:** R1, R4

**Dependencies:** None

**Files:**

- Modify: `.opencode/skills/rm-commit/SKILL.md`

**Approach:**

- Replace line 39's COMMANDS table entry from the broken here-string one-liner to `git commit -m "type(scope): subject" -m "Body explaining why."`
- Update the Purpose column to clarify this is for simple commits; complex messages use the WORKFLOWS pattern
- The COMMANDS table should show the `-m` approach as the quick-reference, with a note pointing to WORKFLOWS for multi-line messages

**Patterns to follow:**

- Existing COMMANDS table format (pipe-delimited markdown table)
- Keep the entry concise — the table is a quick-reference, not a tutorial

**Test scenarios:**

- Test expectation: none — documentation-only change, but verify the resulting PowerShell command is syntactically valid

**Verification:**

- The COMMANDS table entry is valid PowerShell that can be executed as a one-liner
- No `@"` appears in any one-liner context in the skill file

## System-Wide Impact

- **Interaction graph:** Affects every commit created by the AI agent using this skill
- **Unchanged invariants:** The multi-line here-string patterns in WORKFLOWS remain the recommended approach for complex commit messages with footers, references, etc.

## Risks & Dependencies

| Risk                                                               | Mitigation                                                                                                                          |
| ------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------------------- |
| `-m` flags may be harder to use for multi-line bodies with footers | The WORKFLOWS section still provides the full here-string template for complex messages; COMMANDS table is only for quick reference |
| Body line length still needs manual enforcement                    | The skill already documents the ≤100 char rule; `-m` flags don't change this                                                        |

## Documentation / Operational Notes

- This is a targeted fix to one line in the skill file
- No rollout or monitoring needed — the fix takes effect immediately on next skill load

## Sources & References

- **Origin:** User bug report with exact parser error
- **Related code:** `.opencode/skills/rm-commit/SKILL.md` (line 39)
- **Related plan:** `docs/plans/2026-04-03-007-rm-commit-skill-optimization-plan.md` (recommended `-m` approach, not fully implemented)
- **External docs:** [PowerShell here-strings](https://learn.microsoft.com/en-us/powershell/module/microsoft.powershell.core/about/about_quoting_rules#here-strings) — `@"` must be followed by newline
