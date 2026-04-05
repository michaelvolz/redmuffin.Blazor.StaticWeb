---
title: "feat: Fast PowerShell script for sidenotes list"
type: feat
status: completed
date: 2026-04-05
origin: docs/brainstorms/2026-04-04-sidenote-skill-improvements-requirements.md
---

# Fast PowerShell Script for Sidenotes List

## Overview

Replace the slow agent-tool-based `sidenotes list` flow (glob + read each file individually) with a single PowerShell script that reads frontmatter from all sidenote files in one pass. The script completes in milliseconds and outputs a clean numbered list of pending sidenotes. The `rm-sidenotes` skill is updated to call this script instead of using agent tools for listing.

## Problem Frame

The current `sidenotes list` implementation in the `rm-sidenotes` skill requires the agent to:

1. Glob `docs/sidenotes/SN-*.md`
2. Read every single file to extract frontmatter `title:` and `status:`
3. Filter to pending status
4. Format and display

This is incredibly slow because each `read` tool call has overhead, and with 11+ sidenote files, the cumulative latency is several seconds. The user explicitly flagged this as a performance problem. (see origin: docs/brainstorms/2026-04-04-sidenote-skill-improvements-requirements.md)

The repo already has a proven pattern for this: `Cleanup-DevEnv.ps1` replaced a slow multi-agent cleanup skill with a fast script that completes in ~1 second.

## Requirements Trace

- R1. Sidenote list must complete in under 1 second (derived from R1 origin: instantaneous feel)
- R2. List must read frontmatter `title:` and `status:` from each sidenote file
- R3. List must filter to only `pending` status sidenotes
- R4. List must output a clean numbered format matching the current display style
- R5. The `rm-sidenotes` skill must be updated to use the script instead of agent tools
- R6. Script must handle malformed files gracefully (skip with marker)
- R7. Script must handle empty directory (no pending sidenotes) gracefully
- R8. Script must emit a soft warning when a sidenote title exceeds 110 characters (target ~100 chars)

## Scope Boundaries

- NOT changing the capture, conversion, or dismissal flows
- NOT changing the sidenote file format
- Title length warning is additive feedback, not a blocking error
- Script is the only list mechanism — no fallback

## Context & Research

### Relevant Code and Patterns

- `scripts/Cleanup-DevEnv.ps1` — proven pattern: fast script replacing slow multi-agent skill. Uses `Get-CimInstance` for single-shot data collection, minimal output, exits cleanly.
- `.opencode/skills/rm-sidenotes/SKILL.md` — current skill file; the "Retrieval" section (Phase 2) needs updating to reference the script.
- `docs/sidenotes/SN-*.md` — existing sidenote corpus with YAML frontmatter containing `id:`, `date:`, `title:` (optional), `status:` fields.
- `docs/plans/2026-04-04-009-refactor-sidenote-skill-improvements-plan.md` — existing plan for sidenote improvements; this new plan is a focused addition for the list performance issue.

### Key Technical Decisions

- **Single script, no modules**: The list operation is simple enough to be a single self-contained script. No need for the module pattern used by `Update-Changelog.ps1`.
- **Regex-based frontmatter extraction**: PowerShell's `Select-String` or regex matching on file content is fast and avoids YAML parsing dependencies. Each file is small (<20 lines), so reading all content and extracting `title:` and `status:` via regex is efficient.
- **Script location**: `scripts/List-Sidenotes.ps1` — follows the existing `scripts/` convention and naming pattern (`Verb-Noun.ps1`).
- **Skill update**: The skill's retrieval section is updated to instruct the agent to run the script via `bash`. No fallback — the script is the only mechanism.

## Open Questions

### Resolved During Planning

- None

### Deferred to Implementation

- Whether to add a `-All` switch to show dismissed/converted sidenotes (out of scope for initial version, but easy to add later)

## Implementation Units

- [ ] **Unit 1: Create `scripts/List-Sidenotes.ps1`**

**Goal:** Fast PowerShell script that lists pending sidenotes by reading frontmatter from all `SN-*.md` files.

**Requirements:** R1, R2, R3, R4, R6, R7, R8

**Dependencies:** None

**Files:**

- Create: `scripts/List-Sidenotes.ps1`

**Approach:**

- Accept an optional `-SidenotesPath` parameter (defaults to `docs/sidenotes` relative to repo root)
- Use `Get-ChildItem` to find all `SN-*.md` files in one call
- For each file, read content with `Get-Content -Raw` (single read per file, fast)
- Extract `status:` and `title:` from frontmatter using regex (no YAML parser dependency)
- Filter to `status: pending`
- Sort by ID (natural sort on SN-NNNN)
- Output numbered list in format: `N. SN-NNNN (YYYY-MM-DD) — Title text`
- If title is missing, use first 149 chars of body as fallback
- If file is malformed (no frontmatter), output `[malformed sidenote] SN-NNNN`
- If no pending sidenotes, output `No pending sidenotes.`
- After the numbered list, emit soft warnings for any titles exceeding 110 characters: `⚠ SN-NNNN title is N chars (target ~100, max 110)`
- Warnings are informational only — they do not affect exit code or list output
- Exit code 0 on success, 1 on error

**Patterns to follow:**

- `scripts/Cleanup-DevEnv.ps1` — fast, single-pass, minimal output, `$ErrorActionPreference = 'SilentlyContinue'`
- PowerShell 7.4+ best practices: use `-Raw` for file reads, regex for extraction

**Test scenarios:**

- Happy path — script lists all pending sidenotes with ID, date, and title in numbered format
- Happy path — script outputs `No pending sidenotes.` when all are converted/dismissed
- Edge case — script handles sidenote files missing `title:` frontmatter (uses body fallback)
- Edge case — script handles empty sidenotes directory gracefully
- Error path — script skips malformed files with `[malformed sidenote]` marker
- Error path — script handles missing sidenotes directory with clear error message
- Edge case — script emits soft warning for titles exceeding 110 characters without breaking the list

**Verification:**

- Script runs in under 1 second with 11+ sidenote files
- Output matches the current agent-displayed format
- No YAML parser dependency required

- [ ] **Unit 2: Update `rm-sidenotes` skill to use the script**

**Goal:** Update the skill's retrieval/listing instructions to call the PowerShell script instead of using agent tools.

**Requirements:** R5

**Dependencies:** Unit 1

**Files:**

- Modify: `.opencode/skills/rm-sidenotes/SKILL.md`

**Approach:**

- Update the "Retrieval (show sidenotes / list sidenotes)" section to instruct the agent to run `pwsh scripts/List-Sidenotes.ps1` via bash
- Remove the existing glob+read logic entirely — the script is the only mechanism
- Update the skill's FLOW section to reference the script as the list mechanism
- Add guidance: when the script emits a title-length warning, the AI should shorten the sidenote's `title:` frontmatter to ~100 chars (max 110)
- Ensure the skill still handles the "No pending sidenotes." case correctly

**Patterns to follow:**

- Current `rm-sidenotes` skill structure and command table format
- `rm-cleanup` skill pattern of delegating to a fast script

**Test scenarios:**

- Happy path — agent runs the script and displays the numbered list
- Error path — agent handles script failure with clear error message

**Verification:**

- Skill file references `scripts/List-Sidenotes.ps1` as the only list mechanism
- No fallback logic remains in the skill
- Command table is updated if needed

## System-Wide Impact

- **Interaction graph:** User types `sidenotes list` → agent runs `pwsh scripts/List-Sidenotes.ps1` → script reads all sidenote files → outputs formatted list → agent displays result
- **Error propagation:** Script failure surfaces a clear error; malformed files are skipped with markers
- **State lifecycle risks:** None — script is read-only, does not modify any files
- **API surface parity:** User-facing command (`sidenotes list`) behavior is unchanged; only the implementation mechanism changes
- **Unchanged invariants:** Capture, conversion, dismissal, and verification flows are untouched

## Risks & Dependencies

| Risk                               | Likelihood | Impact | Mitigation                                                                                       |
| ---------------------------------- | ---------- | ------ | ------------------------------------------------------------------------------------------------ |
| Script not found                   | Low        | High   | Script is created in this plan; if missing, skill instruction fails clearly — no silent fallback |
| Regex fails on unusual frontmatter | Low        | Low    | Malformed marker for unparseable files                                                           |

## Documentation / Operational Notes

- Script follows the same pattern as `Cleanup-DevEnv.ps1` — fast, deterministic, no dependencies
- No rollout or monitoring needed; this is a developer tooling improvement
- Future enhancement: `-All` switch to show all sidenotes regardless of status

## Sources & References

- **Origin document:** `docs/brainstorms/2026-04-04-sidenote-skill-improvements-requirements.md`
- Related code: `.opencode/skills/rm-sidenotes/SKILL.md`
- Pattern reference: `scripts/Cleanup-DevEnv.ps1`
- Related plan: `docs/plans/2026-04-04-009-refactor-sidenote-skill-improvements-plan.md`
