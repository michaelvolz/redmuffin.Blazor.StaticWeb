---
title: "fix: Restore fast sidenote list flow"
type: fix
status: active
date: 2026-04-05
origin: docs/brainstorms/2026-04-04-sidenote-skill-improvements-requirements.md
---

# Restore Fast Sidenote List Flow

## Overview

`rm-sidenotes` now uses PowerShell for listing, but the current invocation adds extra steps through a temp file bridge and follow-up reads. The goal is to keep the fast script-based listing path while removing avoidable orchestration overhead and tightening the script contract where needed so `sidenotes list` feels like a single quick action again.

## Problem Frame

The list flow was moved away from per-file agent reads to a PowerShell script, but the skill still shells through a multi-step capture-and-read sequence. That adds latency and makes a simple list feel heavier than the old path, even though the underlying script is already fast. The fix is to keep the script, simplify the skill wrapper around it, and make any small script-output hardening explicit rather than implied. (see origin: docs/brainstorms/2026-04-04-sidenote-skill-improvements-requirements.md)

## Requirements Trace

- R1. Listing must feel fast and avoid unnecessary blocking on orchestration overhead
- R2. Listing must continue using the PowerShell script as the source of truth for sidenote files
- R3. The user-facing list output must remain stable and readable
- R4. The skill file must describe the simplified list flow clearly
- R5. Any script hardening must preserve the existing output contract for direct agent consumption

## Scope Boundaries

- NOT changing capture, conversion, or dismissal behavior
- NOT changing the sidenote file format or title canonicalization rules
- NOT reintroducing glob-plus-read listing in the agent flow
- NOT adding new list modes or filters
- NOT inventing a new listing framework or output format

## Context & Research

### Relevant Code and Patterns

- `.opencode/skills/rm-sidenotes/SKILL.md` — current list flow still uses a temp file handoff
- `scripts/List-Sidenotes.ps1` — fast single-pass listing script and current output contract
- `scripts/List-Sidenotes.Tests.ps1` — expected place for PowerShell regression coverage if the script gains test scaffolding
- `docs/plans/2026-04-05-001-feat-fast-sidenotes-list-script-plan.md` — prior work that established the script as the list mechanism
- `docs/plans/2026-04-04-009-refactor-sidenote-skill-improvements-plan.md` — earlier refactor plan that already moved title sourcing to frontmatter

### Institutional Learnings

- `docs/solutions/developer-experience/replace-multi-agent-cleanup-with-fast-script-2026-04-04.md` — when orchestration dominates, a direct deterministic script is the right primitive
- `docs/solutions/integration-issues/opencode-instruction-architecture-pattern-2026-04-03.md` — keep skill instructions concise and avoid accidental orchestration bloat

### External References

- None required; the repo already contains enough evidence for a skill-only fix

## Key Technical Decisions

- **Keep the script, remove the bridge:** the PowerShell script remains the list implementation, but the skill should invoke and consume it directly rather than writing to a temp file and reading it back.
- **Preserve the output contract:** the script’s numbered list, empty-state message, and warnings remain the user-visible contract; only the invocation path changes.
- **Prefer one-step listing semantics:** the skill should present listing as a single command/result cycle so the UX matches the intent of the fast script.
- **Name the test approach:** if script-level regression coverage is added, use the repo’s existing PowerShell script-test pattern rather than an ad hoc harness.

## Open Questions

### Resolved During Planning

- Should listing fall back to old agent-tool enumeration if the script is slow or unavailable? No — the script remains the only list mechanism, and failures should be surfaced clearly.

### Deferred to Implementation

- Whether the skill should read the script’s stdout directly or use a minimal capture helper around stdout/stderr: the plan commits to eliminating the temp-file round trip, but the exact plumbing is left to implementation.
- Whether any stdout/stderr capture helper is still needed after the wrapper simplification: the plan intentionally leaves that to implementation, but the direct temp-file round trip is out.

## Implementation Units

- [ ] **Unit 1: Simplify `rm-sidenotes` list invocation**

**Goal:** Remove the temp-file bridge and make `sidenotes list` a direct one-step script invocation.

**Requirements:** R1, R2, R3, R4

**Dependencies:** None

**Files:**

- Modify: `.opencode/skills/rm-sidenotes/SKILL.md`
- Test: `scripts/List-Sidenotes.Tests.ps1`
- Test fixture: `docs/sidenotes/SN-0010.md`
- Test fixture: `docs/sidenotes/SN-0013.md`

**Approach:**

- Replace the current list instructions so the agent invokes `scripts/List-Sidenotes.ps1` directly and consumes the result inline
- Remove the temp-file dance and the extra read step from the documented flow
- Keep warnings and empty-state handling intact, but treat them as output from the script rather than a separate post-processing pass
- If test scaffolding is added, use the repo’s existing PowerShell script-test pattern to exercise the output contract against representative sidenote fixtures

**Patterns to follow:**

- The direct-script pattern established in `scripts/List-Sidenotes.ps1`
- The concise command documentation style already used in `.opencode/skills/rm-sidenotes/SKILL.md`

**Test scenarios:**

- Happy path — listing returns the numbered pending-sidenote output in a single invocation path
- Happy path — the empty-state message still reaches the user without extra follow-up steps
- Edge case — title-length warnings remain visible in the same list response when present
- Error path — a script failure is surfaced clearly instead of being hidden behind a silent temp-file read
- Integration — the documented list flow no longer mentions temp-file creation or a separate read step
- Integration — representative sidenote fixtures (`SN-0010.md`, `SN-0013.md`) still produce the expected list output when run through the script test harness

**Verification:**

- The skill instructions describe one direct list invocation and no secondary read phase
- The list output contract remains unchanged from the user’s perspective

- [ ] **Unit 2: Tighten list-script output expectations**

**Goal:** Make the script contract explicit enough that the simplified skill invocation remains stable.

**Requirements:** R2, R3

**Dependencies:** Unit 1

**Files:**

- Modify: `scripts/List-Sidenotes.ps1`
- Test: `scripts/List-Sidenotes.Tests.ps1`

**Approach:**

- Review the script’s emitted output so it is safe to consume directly without a temp-file intermediate
- Keep the numbered list, empty-state text, malformed-file markers, and warnings consistent
- If small formatting changes are needed, keep them conservative so the skill and user-facing behavior do not drift
- Validate the script through the repo’s existing PowerShell script-test pattern so stdout/stderr behavior stays intentional

**Patterns to follow:**

- `scripts/Cleanup-DevEnv.ps1` for direct deterministic script output
- `scripts/Update-Changelog.Tests.ps1` for script-level test organization if a test file is introduced

**Test scenarios:**

- Happy path — pending sidenotes still render in numeric order with ID, date, and title
- Edge case — no pending sidenotes still produces the exact empty-state message
- Edge case — malformed files remain skipped or clearly marked without breaking the list
- Edge case — long titles still emit warnings without affecting list completion
- Integration — stdout/stderr behavior remains safe for direct agent consumption
- Integration — the script test harness confirms the current output contract with representative sidenote fixtures

**Verification:**

- The script’s output is stable enough to be used directly by the skill without intermediary file plumbing
- Any output changes are limited to clarity, not semantics

## System-Wide Impact

- **Interaction graph:** user command → `rm-sidenotes` list instructions → `scripts/List-Sidenotes.ps1` → user-visible list output
- **Error propagation:** script failures should surface immediately; no temp-file layer should swallow the root cause
- **State lifecycle risks:** none beyond read-only file enumeration and output formatting
- **API surface parity:** `sidenotes list` should remain behaviorally compatible while becoming faster and simpler
- **Integration coverage:** the list flow should be verified with representative sidenote files and the empty-directory case
- **Unchanged invariants:** capture, conversion, dismissal, and title normalization are outside this fix

## Risks & Dependencies

| Risk                                                                 | Mitigation                                                                                  |
| -------------------------------------------------------------------- | ------------------------------------------------------------------------------------------- |
| The simplified invocation still leaves hidden orchestration overhead | Make the skill’s list path one direct script call and avoid any intermediate temp-file read |
| Output formatting changes break the user’s mental model              | Keep the script output contract stable and treat formatting changes as last resort          |
| A direct invocation behaves differently across shells                | Keep the invocation aligned with the existing PowerShell script and repo conventions        |

## Documentation / Operational Notes

- Update the `rm-sidenotes` skill language so the fast path is obvious to future maintainers
- Keep the list contract text concise; this is an implementation simplification, not a feature expansion

## Sources & References

- **Origin document:** `docs/brainstorms/2026-04-04-sidenote-skill-improvements-requirements.md`
- Related code: `.opencode/skills/rm-sidenotes/SKILL.md`
- Related code: `scripts/List-Sidenotes.ps1`
- Related plan: `docs/plans/2026-04-05-001-feat-fast-sidenotes-list-script-plan.md`
- Related plan: `docs/plans/2026-04-04-009-refactor-sidenote-skill-improvements-plan.md`
- Related learning: `docs/solutions/developer-experience/replace-multi-agent-cleanup-with-fast-script-2026-04-04.md`
