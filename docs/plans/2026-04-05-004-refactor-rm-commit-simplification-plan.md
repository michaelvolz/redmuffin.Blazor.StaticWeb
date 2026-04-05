---
title: refactor: Simplify rm-commit instructions
type: refactor
status: active
date: 2026-04-05
origin: docs/brainstorms/2026-04-03-rm-commits-skill-optimization-requirements.md
---

# refactor: Simplify rm-commit instructions

## Overview

Keep the working commit behavior, but make `rm-commit` as small and direct as
possible. The only behavior change is switching to a unique temp file so the
message file does not need manual cleanup. Remove stale-lock handling and trim
obsolete examples and wording.

## Problem Frame

`rm-commit` already produces valid conventional commits, but it still carries
extra instruction weight:

- stale-lock handling is superfluous
- temp-file cleanup is unnecessary once the file name is unique

The goal is to keep the exact commit output rules that already work!

## Requirements Trace

- R1. Keep the working commit-message format and commitlint compatibility AS IS.
- R2. Use a unique temp file so manual cleanup is no longer needed.
- R3. Remove stale-lock handling.
- R4. Reduce onsolete examples and wording only.

## Scope Boundaries

- Do not change commitlint rules, allowed types/scopes, or the `rm-` namespace.
- Do not change the commit message format.
- Do not change line-length protection, footer-only `Refs: #NNN` handling, or
  any other message-format safeguard.
- Do not change the text or content of the existing commit message examples; at
  most, they may be rearranged.
- Treat examples about stale-lock handling, manual temp cleanup, or verification
  steps as obsolete if those behaviors are removed.
- Do not make any changes outside the items explicitly described in this plan.
- Do not add cleanup steps.
- Do not add verification steps.
- Do not change push policy or branch policy.

## Context & Research

### Relevant Code and Patterns

- `.opencode/skills/rm-commit/SKILL.md` — target skill to simplify.
- `docs/solutions/developer-experience/fix-rm-commit-parser-errors-with-write-tool-and-git-commit-f-2026-04-05.md` — working temp-file commit pattern.
- `docs/solutions/developer-experience/fix-rm-commit-heredoc-syntax-and-line-length-violations-2026-04-03.md` — commit-message constraints and body formatting.
- `docs/solutions/integration-issues/opencode-instruction-architecture-pattern-2026-04-03.md` — `rm-` namespace and skill structure.
- `docs/solutions/integration-issues/opencode-instruction-management-lessons-2026-04-03.md` — trigger density and instruction simplicity.

### Institutional Learnings

- `docs/solutions/developer-experience/fix-rm-commit-commands-table-parser-error-2026-04-05.md` — instruction shape matters; keep the skill structurally simple.

## Key Technical Decisions

- Switch from a fixed temp file to a unique temp file per attempt.
- Remove stale-lock handling entirely.
- Preserve the existing commit message contract.

## Open Questions

### Resolved During Planning

- Should temp cleanup remain explicit? No — the temp file name becomes unique.
- Should stale-lock handling stay? No — remove it.

## Implementation Units

- [ ] **Unit 1: Switch to unique temp file**

**Goal:** Replace the fixed temp file with a unique file so the message file no
longer needs manual cleanup.

**Requirements:** R1, R2

**Dependencies:** None

**Files:**

- Modify: `.opencode/skills/rm-commit/SKILL.md`

**Approach:**

- Keep the temp-file commit-message pattern intact.
- Change the message-file lifecycle to a unique name per commit attempt.
- Keep the commit message content and footer rules unchanged.

**Patterns to follow:**

- `docs/solutions/developer-experience/fix-rm-commit-parser-errors-with-write-tool-and-git-commit-f-2026-04-05.md`

**Test scenarios:**

- Happy path: a normal commit still uses the same message body and footer shape.
- Edge case: repeated commits each get a fresh unique temp file.
- Integration: the written message still produces the same commit content.

- [ ] **Unit 2: Remove stale-lock handling**

**Goal:** Delete the stale-lock instructions and remove only obsolete examples
where necessary.

**Requirements:** R3, R4

**Dependencies:** Unit 1

**Files:**

- Modify: `.opencode/skills/rm-commit/SKILL.md`

**Approach:**

- Remove the stale-lock section entirely.
- Keep all commit examples that are not obsolete.
- Remove only examples that describe stale-lock handling, manual temp cleanup,
  or verification steps.

**Patterns to follow:**

- `.opencode/skills/rm-commit/SKILL.md` current structure
- `docs/solutions/integration-issues/opencode-instruction-management-lessons-2026-04-03.md`

**Test scenarios:**

- Happy path: the remaining example still shows a valid conventional commit.
- Edge case: apostrophes, `$`, or backticks still work in the retained example.
- Edge case: `Refs: #NNN` appears only in the footer, never in the body.
- Integration: the shortened skill still preserves the commitlint-facing rules.

## System-Wide Impact

- **Interaction graph:** `rm-commit` still handles commit requests, but with less
  instruction overhead.
- **State lifecycle risks:** unique temp files must not change commit content.
- **Unchanged invariants:** commitlint rules, body requirement, allowed
  types/scopes, and no-push posture remain intact.

## Risks & Dependencies

| Risk                                                        | Mitigation                                                                                   |
| ----------------------------------------------------------- | -------------------------------------------------------------------------------------------- |
| Unique temp files accidentally change commit content        | Keep the message body and footer rules unchanged while only changing the file name strategy. |
| Removing stale-lock handling deletes something still useful | Accept the simplification as requested; do not retain fallback lock logic.                   |

## Sources & References

- **Origin document:** `docs/brainstorms/2026-04-03-rm-commits-skill-optimization-requirements.md`
- Related code: `.opencode/skills/rm-commit/SKILL.md`
- Related docs: `docs/solutions/developer-experience/fix-rm-commit-parser-errors-with-write-tool-and-git-commit-f-2026-04-05.md`
- Related docs: `docs/solutions/developer-experience/fix-rm-commit-heredoc-syntax-and-line-length-violations-2026-04-03.md`
- Related docs: `docs/solutions/integration-issues/opencode-instruction-architecture-pattern-2026-04-03.md`
- Related docs: `docs/solutions/integration-issues/opencode-instruction-management-lessons-2026-04-03.md`
