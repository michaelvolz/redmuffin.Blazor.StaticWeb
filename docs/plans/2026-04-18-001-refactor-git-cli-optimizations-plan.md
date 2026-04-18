---
title: refactor: Add Git CLI Optimizations to AGENTS.md and rm-commit
type: refactor
status: active
date: 2026-04-18
origin: docs/solutions/best-practices/git-cli-optimizations-for-ai-agents-2026-04-18.md
---

# refactor: Add Git CLI Optimizations to AGENTS.md and rm-commit

## Overview

Add Git CLI optimizations to both AGENTS.md (general) and rm-commit (commit-specific) while preserving all existing rm-commit information. The split ensures Git optimizations benefit all operations (research, history, review) while keeping commit-focused guidance in rm-commit.

**Origin:** [docs/solutions/best-practices/git-cli-optimizations-for-ai-agents-2026-04-18.md](../solutions/best-practices/git-cli-optimizations-for-ai-agents-2026-04-18.md)

## Problem Frame

AI agents running Git commands need machine-readable, stable output that doesn't depend on user configuration or Git version. The current rm-commit has commit MESSAGE guidance but lacks Git CLI optimization for general operations. AGENTS.md has no Git CLI guidance.

## Requirements Trace

- R1. Preserve ALL existing rm-commit skill content (243 lines)
- R2. Add Git CLI optimizations table to AGENTS.md (~15 lines)
- R3. Add commit-relevant Git improvements to rm-commit (~20 lines)
- R4. Split ensures Git optimizations benefit research/history/review, not just commits

## Scope Boundaries

- **In scope**: AGENTS.md Git table, rm-commit additions
- **NOT in scope**: Removing or restructuring existing rm-commit sections
- **NOT in scope**: Other skills or agent files

## Context & Research

### Relevant Code and Patterns

- `.opencode/skills/rm-commit/SKILL.md` — Current commit skill (243 lines, commit MESSAGE focus)
- `AGENTS.md` — Current agent rules (166 lines)
- `docs/solutions/best-practices/git-cli-optimizations-for-ai-agents-2026-04-18.md` — Research source

### Institutional Learnings

- The split agreed with user: AGENTS.md for general Git, rm-commit for commit-specific
- Priority 1: Preserve ALL existing rm-commit information

### External References

- Git `--porcelain` documentation: stable output for scripting
- Git `--numstat`: machine-readable line counts
- Git `--no-optional-locks`: prevents index lock contention

## Key Technical Decisions

- **AGENTS.md gets global Git table**: Because optimizations benefit ALL Git operations, not just commits
- **rm-commit gets minimal add**: Only commit-relevant improvements (status porcelain, error handling)
- **Preserve verbatim**: Existing rm-commit sections stay unchanged — additions only

## Implementation Units

- [ ] **Unit 1: Add Git CLI table to AGENTS.md**

**Goal:** Add optimized Git commands table for general agent use

**Dependencies:** None

**Files:**

- Modify: `AGENTS.md`

**Approach:**

- Add new "Git CLI Optimizations" section after existing COMMANDS table
- Use table format matching existing AGENTS.md style
- Include: porcelain status, diff --numstat, plumbing branch, --no-optional-locks

**Test scenarios:**

- Test expectation: none — documentation update only

**Verification:**

- New section visible in AGENTS.md
- Table format matches existing style

- [ ] **Unit 2: Add commit-relevant Git improvements to rm-commit**

**Goal:** Add Git CLI optimizations relevant to commit workflow

**Dependencies:** Unit 1 (can be done in parallel)

**Files:**

- Modify: `.opencode/skills/rm-commit/SKILL.md`

**Approach:**

- Add to CRITICAL section: Use `--no-optional-locks` for background checks
- Update COMMANDS table: Add porcelain status, diff --numstat
- Keep existing here-string and message formatting as-is
- Add minimal new patterns section with commit-specific optimizations

**Test scenarios:**

- Test expectation: none — skill documentation update only

**Verification:**

- All existing content preserved
- New optimizations present but not dominant

## System-Wide Impact

- **Documentation**: Two files updated, no code changes
- **Workflow impact**: Agents gain Git CLI optimization guidance
- **No breaking changes**: Pure documentation additions

## Risks & Dependencies

| Risk                                    | Mitigation                           |
| --------------------------------------- | ------------------------------------ |
| Accidentally removing rm-commit content | Preserving verbatim - additions only |
| AGENTS.md bloat                         | Keep table compact (~15 lines)       |

## Documentation / Operational Notes

- AGENTS.md will have new "Git CLI Optimizations" section
- rm-commit will have additional commit-relevant Git guidance
- Reference doc exists in docs/solutions/ for detailed guidance

## Sources & References

- **Origin document:** [docs/solutions/best-practices/git-cli-optimizations-for-ai-agents-2026-04-18.md](../solutions/best-practices/git-cli-optimizations-for-ai-agents-2026-04-18.md)
- Related code: `.opencode/skills/rm-commit/SKILL.md`, `.opencode/AGENTS.md`
