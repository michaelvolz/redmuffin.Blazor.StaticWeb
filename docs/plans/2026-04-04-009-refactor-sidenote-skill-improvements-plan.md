---
title: refactor: Sidenote skill improvements
type: refactor
status: active
date: 2026-04-04
origin: docs/brainstorms/2026-04-04-sidenote-skill-improvements-requirements.md
---

# Sidenote Skill Improvements — Implementation Plan

## Overview

Update `rm-sidenotes` so capture feels immediate, titles are stored in one canonical place, and existing sidenote files are normalized to the same format. The work keeps the skill fast and quiet during active coding while making list display and future maintenance simpler.
Capture should end with one short filename confirmation, and that line is the user-visible verification that the note was created.

## Problem Frame

The current sidenote skill still carries two sources of friction: capture feels slow because it relies on subagent startup, and sidenote files store the title in more than one place. The agreed target is a direct capture path on the main flow, async verification that does not block the user, and frontmatter-only titles that `sidenotes list` can read consistently. (see origin: docs/brainstorms/2026-04-04-sidenote-skill-improvements-requirements.md)

## Requirements Trace

**Capture Flow**

- R1. Capture must feel instantaneous and avoid blocking on subagent startup
- R2. Capture must write directly without using a subagent

**Verification**

- R3. Verification/retry must run asynchronously after the note is written, and exhausted failures must be recorded for follow-up

**File Format**

- R4. Each sidenote file must have exactly one title representation
- R5. `sidenotes list` must read the frontmatter title for display

**Migration**

- R6. Existing sidenote files must be normalized to the new format

**Documentation**

- R7. The `rm-sidenotes` skill file must document the new capture mechanism and title rules

## Scope Boundaries

- NOT rewriting the entire skill or changing conversion/dismissal behavior
- Retrieval/list display may change only as needed to read frontmatter titles
- NOT introducing a new persistence format or a separate migration framework

## Context & Research

### Relevant Code and Patterns

- `.opencode/skills/rm-sidenotes/SKILL.md` — current skill behavior, command surface, and lifecycle rules
- `docs/sidenotes/SN-*.md` — existing sidenote corpus, including mixed title formats that need normalization
- `docs/plans/2026-04-04-008-feat-sidenote-capture-skill-plan.md` — prior, broader sidenote plan to avoid reintroducing old assumptions
- `docs/brainstorms/2026-04-04-sidenote-skill-improvements-requirements.md` — current source of truth for this refactor

### Institutional Learnings

- `docs/solutions/integration-issues/opencode-instruction-architecture-pattern-2026-04-03.md` — keep skill frontmatter machine-focused and avoid redundant metadata
- `docs/solutions/developer-experience/replace-multi-agent-cleanup-with-fast-script-2026-04-04.md` — prefer a direct deterministic normalization step over orchestration-heavy cleanup for simple migrations
- `docs/solutions/developer-experience/commitlint-rejects-body-when-hash-in-body-2026-04-04.md` — use plain `SN-0001` references; avoid `#SN-0001` in docs or examples

### External References

- None required for this plan; the repo already has enough local pattern guidance for a docs/skill refactor

## Key Technical Decisions

- **Direct capture path:** the main flow writes the sidenote immediately instead of delegating capture to a subagent. This is the main latency win.
- **Async verification only:** verification/retry stays, but as a non-blocking follow-up step. If retries are exhausted, the sidenote is marked with `verification-status: failed` and a short `verification-error` summary so the failure stays attached to the exact file instead of disappearing.
- **Frontmatter title as source of truth:** the `title:` field becomes canonical, and `sidenotes list` reads from it.
- **Inline normalization:** existing sidenote files are normalized as part of this update so the directory ends up with one consistent title shape.
- **One-line capture receipt:** capture returns a single filename line and nothing else; that line is the confirmation and the verify signal for the user.

## Open Questions

### Resolved During Planning

- None

### Deferred to Implementation

- Exact wording of the `verification-error` summary, if the first implementation path needs a small wording adjustment after seeing the real skill/runtime behavior

## Implementation Units

- [ ] **Unit 1: Rewrite `rm-sidenotes` capture and listing behavior**

**Goal:** Make sidenote capture direct, fast, and canonicalize the title source for listing.

**Requirements:** R1, R2, R3, R4, R5, R7

**Dependencies:** None

**Files:**

- Modify: `.opencode/skills/rm-sidenotes/SKILL.md`
- Test: `docs/sidenotes/SN-0010.md`, `docs/sidenotes/SN-0011.md`

**Approach:**

- Replace the capture flow so it writes directly from the main path instead of spawning a capture subagent
- Keep a lightweight verification/retry step after the write, with a clear failure-follow-up path that marks the sidenote when retries are exhausted
- Make `title:` in YAML frontmatter the only title source used by `sidenotes list`
- Keep the confirmation to one short filename-based line and no extra chatter
- If verification retries are exhausted, update the written sidenote with `verification-status: failed` and `verification-error` so the failure is durable and traceable

**Patterns to follow:**

- `.opencode/skills/rm-commit/SKILL.md` for concise `rm-*` skill structure
- Current `rm-sidenotes` command/flow structure for naming and lifecycle consistency

**Test scenarios:**

- Happy path — capture a new sidenote and confirm it writes immediately with one short filename-based acknowledgement
- Happy path — `sidenotes list` reads the frontmatter title for a note that no longer has an H1
- Edge case — a malformed or manually edited sidenote still yields a usable list entry or a clear failure path
- Error path — verification exhaustion marks the note with `verification-status: failed` and a short error summary instead of silently losing the note

**Verification:**

- Capture instructions no longer mention a capture subagent
- Listing instructions clearly treat frontmatter `title:` as authoritative
- The confirmation text remains short and consistent

- [ ] **Unit 2: Normalize existing sidenote files to the new title format**

**Goal:** Bring the current sidenote corpus into a single title format without losing content.

**Requirements:** R4, R5, R6

**Dependencies:** Unit 1

**Files:**

- Modify: `docs/sidenotes/SN-0001.md`
- Modify: `docs/sidenotes/SN-0002.md`
- Modify: `docs/sidenotes/SN-0003.md`
- Modify: `docs/sidenotes/SN-0004.md`
- Modify: `docs/sidenotes/SN-0005.md`
- Modify: `docs/sidenotes/SN-0006.md`
- Modify: `docs/sidenotes/SN-0007.md`
- Modify: `docs/sidenotes/SN-0008.md`
- Modify: `docs/sidenotes/SN-0009.md`
- Modify: `docs/sidenotes/SN-0010.md`
- Modify: `docs/sidenotes/SN-0011.md`

**Approach:**

- Add frontmatter `title:` where it is missing
- If both frontmatter and H1 exist, keep the frontmatter title and remove the H1
- If only an H1 exists, promote it into frontmatter `title:` and remove the H1
- If frontmatter and H1 disagree, frontmatter wins and the H1 is removed
- Preserve frontmatter-only notes as-is
- Keep file content otherwise unchanged so historical context stays intact
- Normalize the corpus in one reviewable pass so the end state is consistent and easy to verify
- Skip malformed notes with a clear failure marker and leave them for manual follow-up rather than rewriting them blindly

**Patterns to follow:**

- Existing sidenote file structure in `docs/sidenotes/*.md`
- The frontmatter/title conventions already used in `SN-0010.md` and `SN-0011.md`

**Test scenarios:**

- Happy path — H1-only notes gain a frontmatter title and lose the H1
- Happy path — notes with both title forms keep the frontmatter title and drop the H1
- Edge case — frontmatter-only notes remain unchanged
- Error path — a malformed note is skipped with a clear failure marker instead of being rewritten blindly
- Edge case — list display remains stable after normalization across all sample files

**Verification:**

- No sidenote file contains both a frontmatter title and an H1 title
- All sidenote files are readable by the updated listing behavior
- The corpus is internally consistent after normalization

**Recovery note:**

- If normalization reveals an unexpected malformed file, skip it with a failure marker and rerun the pass after the file-shape rule is fixed.

## System-Wide Impact

- **Interaction graph:** user text → `rm-sidenotes` capture flow → file write → async verification; listing reads the same frontmatter title source that capture writes
- **Error propagation:** write failures should surface immediately; verification failures should be retained for follow-up instead of being dropped
- **State lifecycle risks:** mixed-format titles during migration, duplicate titles, or missing frontmatter in legacy files
- **State lifecycle risks:** mixed-format titles during migration, duplicate titles, missing frontmatter in legacy files, or a failed verification state that must remain tied to the written note
- **API surface parity:** the user-facing commands (`sidenote:`, `/sidenote`, list, convert, dismiss) should remain stable; only title sourcing and capture flow change
- **Integration coverage:** sample sidenote files should cover H1-only, both-title, and frontmatter-only cases
- **Unchanged invariants:** conversion and dismissal behavior remain intact; the change is about capture speed, title canonicalization, and consistency

## Risks & Dependencies

| Risk                                                             | Mitigation                                                                          |
| ---------------------------------------------------------------- | ----------------------------------------------------------------------------------- |
| Direct write path behaves differently than the old subagent flow | Keep the plan bounded to the skill file and normalize around existing file patterns |
| Async verification failure becomes silent data loss              | Mark the note with `verification-status: failed` and a short error summary          |
| Legacy sidenote files remain inconsistent after the update       | Normalize all existing sidenotes in one pass and verify the corpus state afterward  |
| Frontmatter parsing fails on manually edited files               | Keep the listing behavior simple and skip malformed files with a clear marker       |

## Documentation / Operational Notes

- Keep `SN-xxxx` references plain in prose and examples
- Preserve the historical content in sidenote files while removing redundant title duplication
- Keep normalization and skill updates as separate, reviewable changes even if they land in the same branch

## Sources & References

- **Origin document:** `docs/brainstorms/2026-04-04-sidenote-skill-improvements-requirements.md`
- Related code: `.opencode/skills/rm-sidenotes/SKILL.md`
- Related plan: `docs/plans/2026-04-04-008-feat-sidenote-capture-skill-plan.md`
- Related learning: `docs/solutions/integration-issues/opencode-instruction-architecture-pattern-2026-04-03.md`
- Related learning: `docs/solutions/developer-experience/replace-multi-agent-cleanup-with-fast-script-2026-04-04.md`
- Related learning: `docs/solutions/developer-experience/commitlint-rejects-body-when-hash-in-body-2026-04-04.md`
