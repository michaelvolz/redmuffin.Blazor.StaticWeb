---
title: feat: Add sidenote capture skill
type: feat
status: completed
date: 2026-04-04
origin: docs/brainstorms/2026-04-04-sidenote-capture-skill-requirements.md
---

# Sidenote Capture Skill — Implementation Plan

## Overview

A new `rm-sidenotes` skill that captures tangential ideas mid-conversation, stores them as structured markdown files in `docs/sidenotes/`, and provides retrieval/conversion commands. Replaces the current flat `docs/sidenotes.md` checklist.

## Problem Frame

During active work sessions, users have ideas that must be captured immediately but should not derail the current task. The manual approach (appending to `docs/sidenotes.md`) lacks metadata, structure, and a conversion path. This is a recognized pain point across AI coding agents (Claude Code issue #26376, marked duplicate). (see origin: docs/brainstorms/2026-04-04-sidenote-capture-skill-requirements.md)

## Requirements Trace

- R1. Trigger on inline keyword "sidenote:" or "/sidenote" command
- R2. Capture includes timestamp, source context, and sidenote text
- R3. Brief one-line confirmation, no conversation derailment
- R4. Both capture methods supported: inline keyword (sidenote:) and slash command (/sidenote)
- R5. One markdown file per sidenote in `docs/sidenotes/`, named by ID (e.g., `SN-0001.md`)
- R6. Unique sequential ID (SN-0001, SN-0002, ...) with 4-digit zero-padding
- R7. Frontmatter: id, date, status, source-context, tags. Additional fields added by lifecycle: converted-to (on conversion), dismissed-reason (on dismissal)
- R8. "show sidenotes" / "list sidenotes" displays pending items
- R9. "convert sidenote SN-XXX" converts to todo/brainstorm/task
- R10. Converted sidenotes marked with status and linked to artifact
- R11. "dismiss sidenote SN-XXX" marks as dismissed

## Scope Boundaries

- NOT a full project management tool
- NOT cross-session memory (not auto-loaded into context)
- NOT auto-suggestions (user explicitly requests retrieval)
- Migrates existing `docs/sidenotes.md` entries into the new structure

## Context & Research

### Relevant Code and Patterns

- **`rm-*` skill convention**: All custom skills live in `.opencode/skills/rm-<name>/SKILL.md`. Flat directory, single file. No subdirectories needed for this skill.
- **Skill frontmatter**: Exactly two fields — `name` and `description`. Description includes shortcut alias and trigger phrases.
- **Skill body structure**: `## CRITICAL`, `## FLOW`, `## COMMANDS`, `## PATTERNS`, `## BOUNDARIES`, `## CONTEXT`.
- **Todo system**: `.context/compound-engineering/todos/` uses sequential `issue_id` with zero-padded 3-digit format. Sidenotes will use similar global sequential (SN-001) rather than per-day counter, since sidenotes span sessions.
- **Brainstorm system**: `docs/brainstorms/` uses date-based naming with per-day counter.
- **Plan system**: `docs/plans/` uses `YYYY-MM-DD-NNN` per-day sequential.

### Institutional Learnings

- No existing solutions in `docs/solutions/` for sidenote or scratchpad patterns.

### External References

- Claude Code issue #26376 — non-interrupting todo queue/scratchpad (closed as duplicate, high demand signal).

## Key Technical Decisions

- **Global sequential IDs (SN-001, SN-002, ...)** over date-based: Resolves deferred question from origin doc. Sidenotes span sessions and days, so a global monotonic counter is simpler to reference ("tackle SN-007") than date-based IDs. The counter is computed by scanning existing `docs/sidenotes/SN-*.md` files and taking the highest number + 1.
- **Conversion approach**: Resolves second deferred question. Rather than a subjective heuristic, the skill always presents 3 options (todo, brainstorm, plan) and lets the user choose. This eliminates classification ambiguity and the cross-skill invocation problem.
  - todo → creates file in `.context/compound-engineering/todos/` via todo-create pattern
  - brainstorm → creates file in `docs/brainstorms/` following date-based naming
  - plan → creates file in `docs/plans/` following plan naming, then reference ce:work for execution
  - Once converted, a sidenote cannot be re-converted to a different type.
- **Skill file structure**: Single SKILL.md with one reference file (`references/conversion.md`) to stay within ~800 lines. Pre-designed split avoids reactive panic later.
- **AGENTS.md integration**: The SIDENOTES section in AGENTS.md will be updated to instruct the agent to load the `rm-sidenotes` skill when the user says "sidenote:" or "/sidenote". This ensures reliable triggering since skills depend on LLM semantic matching, not keyword detection.
- **Migration of existing entries**: The two entries in `docs/sidenotes.md` will be migrated to `docs/sidenotes/SN-001.md` and `docs/sidenotes/SN-002.md` as part of this plan. The old file will be deleted.

## Open Questions

### Resolved During Planning

- **ID format**: Global sequential (SN-0001) with 4-digit padding chosen over date-based. Rationale: sidenotes span sessions, easier to reference, 4-digit padding eliminates SN-999 ceiling.
- **Conversion approach**: User chooses target type (todo/brainstorm/plan) — no subjective heuristic.

### Deferred to Implementation

- **None** — all questions from the origin document are resolved.

## High-Level Technical Design

> _This illustrates the intended approach and is directional guidance for review, not implementation specification. The implementing agent should treat it as context, not code to reproduce._

**Capture flow:**

```
User types "sidenote: <text>" or "/sidenote <text>"
  → Skill detects trigger
  → Compute next ID (scan docs/sidenotes/SN-*.md, max N + 1)
  → Write docs/sidenotes/SN-NNN.md with frontmatter + content
  → Confirm: "Sidenote SN-NNN captured."
  → Continue conversation (no further action)
```

**Retrieval flow:**

```
User types "show sidenotes" or "list sidenotes"
  → Glob docs/sidenotes/SN-*.md
  → Filter status:pending
  → Display numbered list with ID, date, text preview
```

**Conversion flow:**

```
User types "convert sidenote SN-NNN" or "tackle sidenote SN-NNN"
  → Read SN-NNN.md
  → Check status: if already converted, return "Already converted to <path>."
  → Present 3 options: todo, brainstorm, plan
  → User selects target type
  → Create artifact in appropriate location
  → Update SN-NNN.md status to "converted", add converted-to field
```

**Dismissal flow:**

```
User types "dismiss sidenote SN-NNN" or "dismiss sidenote SN-NNN - <reason>"
  → Read SN-NNN.md
  → Update status to "dismissed"
  → Add dismissed-reason field if reason provided
  → Confirm: "Sidenote SN-NNN dismissed."
```

## Implementation Units

- [x] **Unit 0: Update AGENTS.md SIDENOTES section to reference rm-sidenotes skill**

**Goal:** Update the existing SIDENOTES section in AGENTS.md to instruct the agent to load the `rm-sidenotes` skill, ensuring reliable triggering since skills depend on LLM semantic matching, not keyword detection.

**Requirements:** R1, R3, R4

**Dependencies:** None

**Files:**

- Modify: `AGENTS.md`

**Approach:**

- Update the SIDENOTES section to replace the inline capture instruction with a skill-loading instruction
- New text: "When the user says 'sidenote:' or '/sidenote', load the `rm-sidenotes` skill. The skill handles capture, storage, retrieval, and conversion."
- Keep the behavioral rules: NEVER act on a sidenote during the current task, NEVER ask follow-up questions, task continues uninterrupted after capture

**Test scenarios:**

- Happy path: AGENTS.md SIDENOTES section references rm-sidenotes skill by name
- Happy path: Behavioral rules (never act, never ask, capture confirm) are preserved

**Verification:**

- AGENTS.md SIDENOTES section instructs loading rm-sidenotes skill
- Behavioral rules are preserved

- [x] **Unit 1: Create docs/sidenotes/ directory and migrate existing entries**

**Goal:** Set up the storage directory and migrate the two existing checklist items from `docs/sidenotes.md` into structured sidenote files.

**Requirements:** R5, R6, R7

**Dependencies:** `docs/sidenotes.md` must exist for migration. If absent, create directory with no initial files and skip migration step.

**Files:**

- Create: `docs/sidenotes/SN-001.md`
- Create: `docs/sidenotes/SN-002.md`
- Delete: `docs/sidenotes.md`

**Approach:**

- Create `docs/sidenotes/` directory
- Convert the two checklist items from `docs/sidenotes.md` into structured markdown files with YAML frontmatter (id, date, status: pending, source-context: "migrated from docs/sidenotes.md", tags)
- Delete the old `docs/sidenotes.md` file
- SN-001: "Research a good sidenote solution and maybe create skills for this feature"
- SN-002: "Prevent many open about:blank tabs in dev Chrome browser"

**Test scenarios:**

- Happy path: Both migrated files exist with valid frontmatter and correct IDs

**Verification:**

- `docs/sidenotes/` contains exactly two files
- Each file has valid YAML frontmatter with id, date, status, source-context
- `docs/sidenotes.md` no longer exists

- [x] **Unit 2: Write the rm-sidenotes skill (capture flow)**

**Goal:** Create the skill file with capture logic for both inline keyword ("sidenote:") and slash command ("/sidenote") triggers.

**Requirements:** R1, R2, R3, R4, R5, R6, R7

**Dependencies:** Unit 1

**Files:**

- Create: `.opencode/skills/rm-sidenotes/SKILL.md`

**Approach:**

- Follow `rm-*` skill conventions: frontmatter with `name: rm-sidenotes` and description including shortcut alias `rm:sn` and trigger phrases
- Skill body sections: `## CRITICAL`, `## FLOW`, `## COMMANDS`, `## PATTERNS`, `## BOUNDARIES`, `## CONTEXT`
- Capture flow: detect trigger → compute next ID → write file → one-line confirmation
- ID computation: glob `docs/sidenotes/SN-*.md`, extract numeric suffix, take max + 1, zero-pad to 4 digits (SN-0001)
- Frontmatter template: id, date (YYYY-MM-DD), status (pending), source-context, tags (empty array by default)
- Sidenote body: brief title derived from first few words of the text, followed by the full captured text

**Patterns to follow:**

- `.opencode/skills/rm-commit/SKILL.md` — structure, frontmatter style, BOUNDARIES section
- `.opencode/skills/rm-cleanup/SKILL.md` — concise flow instructions

**Test scenarios:**

- Happy path: "sidenote: add dark mode toggle" creates SN-003.md with correct frontmatter
- Happy path: "/sidenote we should add rate limiting" creates next sequential file
- Edge case: No existing sidenotes (empty directory) — ID starts at SN-001
- Edge case: Gaps in sequence (SN-001, SN-003 exist) — next ID is SN-004 (max + 1, not fill gaps)

**Verification:**

- Skill file exists at `.opencode/skills/rm-sidenotes/SKILL.md`
- Skill is under ~800 lines total (SKILL.md + references/conversion.md)
- Frontmatter matches `rm-*` convention (name + description only)
- Capture flow instructions are clear and complete
- Trigger detection uses precise syntactic rule: "sidenote:" must be first non-whitespace token on a line, or "/sidenote" must be at message start

- [x] **Unit 3: Add retrieval flow to the skill**

**Goal:** Add "show sidenotes" / "list sidenotes" command to the skill that displays pending items.

**Requirements:** R8

**Dependencies:** Unit 2

**Files:**

- Modify: `.opencode/skills/rm-sidenotes/SKILL.md`

**Approach:**

- Add retrieval flow to the skill's FLOW section
- Glob `docs/sidenotes/SN-*.md`, filter by `status: pending` in frontmatter
- Display as numbered list: ID, date, truncated text preview
- If no pending sidenotes, respond with "No pending sidenotes."

**Test scenarios:**

- Happy path: "show sidenotes" lists all pending items with IDs and previews
- Edge case: No pending sidenotes — returns "No pending sidenotes."
- Edge case: All sidenotes dismissed/converted — returns "No pending sidenotes."

**Verification:**

- Retrieval instructions are clear in the skill file
- Display format is consistent and scannable

- [x] **Unit 4: Add conversion flow to the skill**

**Goal:** Add "convert sidenote SN-XXX" command with classification heuristic and user confirmation.

**Requirements:** R9, R10

**Dependencies:** Unit 3

**Files:**

- Modify: `.opencode/skills/rm-sidenotes/SKILL.md`

**Approach:**

- Add conversion flow to the skill's FLOW section
- Present 3 options to user: todo, brainstorm, plan — user selects target type
- No subjective classification heuristic — user always chooses
- On selection:
  - todo → Create file in `.context/compound-engineering/todos/` using todo-create naming convention. Populate Problem Statement from sidenote text, set priority p3, leave Findings/Proposed Solutions as TBD placeholders.
  - brainstorm → Create file in `docs/brainstorms/` following `YYYY-MM-DD-<topic>-requirements.md` convention.
  - plan → Create file in `docs/plans/` following `YYYY-MM-DD-NNN-<type>-<name>-plan.md` convention.
- Update sidenote status to "converted", add `converted-to` field with artifact path
- Once converted, sidenote cannot be re-converted. If user attempts, return "Sidenote SN-XXX was already converted to <path>."
- Conversion details (artifact creation patterns, field mappings) documented in `references/conversion.md`

**Test scenarios:**

- Happy path: "convert sidenote SN-001" → user selects "brainstorm" → creates requirements doc in docs/brainstorms/
- Happy path: "convert sidenote SN-002" → user selects "todo" → creates todo file with p3 priority
- Happy path: "tackle sidenote SN-001" (alias trigger) → same behavior as "convert"
- Error path: Invalid ID (SN-9999 doesn't exist) — returns "Sidenote SN-9999 not found."
- Error path: Already converted sidenote — returns "Sidenote SN-XXX was already converted to <path>."
- Edge case: Prose containing word "sidenote" but not as trigger — no capture occurs

**Verification:**

- Conversion flow handles all three artifact types
- Status update and linking works correctly
- Error cases are handled gracefully

- [x] **Unit 5: Add dismissal flow to the skill**

**Goal:** Add "dismiss sidenote SN-XXX" command with optional reason.

**Requirements:** R11

**Dependencies:** Unit 2

**Files:**

- Modify: `.opencode/skills/rm-sidenotes/SKILL.md`

**Approach:**

- Add dismissal flow to the skill's FLOW section
- Update sidenote status to "dismissed", add `dismissed-reason` field if provided
- Confirm: "Sidenote SN-XXX dismissed."
- If sidenote already dismissed, return "Sidenote SN-XXX is already dismissed."

**Test scenarios:**

- Happy path: "dismiss sidenote SN-002" marks as dismissed
- Happy path: "dismiss sidenote SN-002 - not relevant anymore" includes reason
- Error path: Invalid ID — returns "Sidenote SN-XXX not found."
- Error path: Already dismissed — returns "Sidenote SN-XXX is already dismissed."

**Verification:**

- Dismissal flow is documented
- Status and optional reason are persisted correctly

- [x] **Unit 6: Add COMMANDS reference table and BOUNDARIES section**

**Goal:** Complete the skill with reference table and boundary definitions following `rm-*` conventions.

**Requirements:** R4

**Dependencies:** Units 2-5

**Files:**

- Modify: `.opencode/skills/rm-sidenotes/SKILL.md`

**Approach:**

- Add `## COMMANDS` table mapping trigger phrases to actions
- Add `## BOUNDARIES` section with ALWAYS / ASK FIRST / NEVER tiers
- Add `## CONTEXT` closing paragraph

**Commands table:**

| Command                                              | Purpose             | When                                |
| ---------------------------------------------------- | ------------------- | ----------------------------------- |
| `sidenote: <text>`                                   | Capture inline      | Mid-conversation tangential thought |
| `/sidenote <text>`                                   | Capture via command | Explicit capture                    |
| `show sidenotes` / `list sidenotes`                  | List pending        | Ready to review backlog             |
| `convert sidenote SN-XXX` / `tackle sidenote SN-XXX` | Convert to task     | Ready to act on specific item       |
| `dismiss sidenote SN-XXX`                            | Dismiss item        | No longer relevant                  |

**Boundaries:**

- ALWAYS: Create `docs/sidenotes/` if missing, use 4-digit sequential IDs, confirm capture in one line, re-scan directory before each write
- ASK FIRST: Conversion target type (always — user selects from 3 options)
- NEVER: Act on a sidenote immediately after capture, auto-suggest sidenotes, modify existing sidenote text (only status changes), re-convert an already-converted sidenote

**Test scenarios:**

- Happy path: Skill file is complete, under ~800 lines total, follows all `rm-*` conventions

**Verification:**

- Skill file is complete and self-contained
- Follows `rm-*` structural conventions exactly

## System-Wide Impact

- **Interaction graph:** The skill integrates with the existing `todo-create` and `ce:brainstorm` workflows when converting sidenotes. No existing code paths are modified. AGENTS.md SIDENOTES section will be updated to reference the new skill by name.
- **Error propagation:** File writes can fail (permissions, disk full, directory locked). Glob operations fail on missing directories. YAML frontmatter parsing fails on malformed content. Skill instructs: if directory creation fails → report error and abort; if file write fails → retry once then report; if ID scan finds no files → default to SN-0001; if frontmatter read fails → report "malformed sidenote file".
- **State lifecycle risks:** ID generation uses scan → max + 1 → write. Agent may lose track of last assigned ID within a long session — re-scan before each write mitigates this. If all sidenote files are deleted, ID counter resets — acceptable risk for single-user tool, documented in BOUNDARIES.
- **Unchanged invariants:** Existing `docs/brainstorms/`, `docs/plans/`, and `.context/compound-engineering/todos/` systems are not modified. The sidenote system is additive.

## Risks & Dependencies

| Risk                                                  | Mitigation                                                                                                                                                            |
| ----------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Skill does not trigger on simple "sidenote:" messages | AGENTS.md SIDENOTES section instructs agent to load rm-sidenotes skill. Skill description is "pushy" — enumerates trigger phrases, includes negative examples         |
| False-positive trigger on prose containing "sidenote" | Precise syntactic rule: "sidenote:" must be first non-whitespace token on a line, or "/sidenote" at message start. Test scenario for prose containing word "sidenote" |
| ID collision within long session                      | Re-scan directory before each write; if ID exists, increment and retry                                                                                                |
| Skill grows beyond 800 lines                          | Pre-designed split: SKILL.md + references/conversion.md. Conversion details isolated to keep main file lean                                                           |
| Migration loses existing sidenote data                | Read `docs/sidenotes.md` before deletion, verify full text of each checklist item preserved in new files including parenthetical guidance                             |
| Conversion creates artifact that already exists       | User selects target type — artifact creation follows existing naming conventions which include per-day counters or sequential IDs                                     |

## Documentation / Operational Notes

- The old `docs/sidenotes.md` will be deleted after migration. Any references to it in other docs should be updated to point to `docs/sidenotes/`.
- The skill follows the existing `rm-*` naming convention and will appear in the available skills list alongside other custom skills.

## Sources & References

- **Origin document:** [docs/brainstorms/2026-04-04-sidenote-capture-skill-requirements.md](docs/brainstorms/2026-04-04-sidenote-capture-skill-requirements.md)
- Related code: `.opencode/skills/rm-commit/SKILL.md`, `.opencode/skills/rm-cleanup/SKILL.md`
- Related skill: `.opencode/skills/vendor/todo-create/SKILL.md`
- Claude Code issue: https://github.com/anthropics/claude-code/issues/26376
