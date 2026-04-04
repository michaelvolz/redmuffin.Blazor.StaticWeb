---
title: "feat: Add timestamp frontmatter to all docs files"
type: feat
status: completed
date: 2026-04-04
completed: 2026-04-04
---

# Add Timestamp Frontmatter to All Docs Files

## Overview

Add `date:` frontmatter to every markdown file in `docs/` that lacks it, using git history to find the absolute first creation date for each file. This ensures consistent timestamp metadata across all documentation.

## Problem Frame

The docs folder contains markdown files with inconsistent timestamp handling:

- Sidenotes, brainstorms, plans, and solutions already have `date:` frontmatter
- Many root-level docs (`docs/*.md`) lack frontmatter entirely
- Some files have embedded dates in content but not frontmatter
- Files were deleted and restored (Apr 3, 2026), requiring git history analysis to find true creation dates

## Requirements Trace

- R1. Add `date:` frontmatter to every docs/\*.md file missing it
- R2. Use git history to find absolute first creation date (earliest commit where file was added)
- R3. Handle deleted/restored files by finding earliest add across full history
- R4. Preserve existing frontmatter dates where already present
- R5. Extract embedded content dates where they exist (footer dates, inline dates)

## Scope Boundaries

- **INCLUDED**: All `docs/*.md` files (root level)
- **EXCLUDED**: docs/brainstorms/, docs/plans/, docs/solutions/, docs/sidenotes/ (already have dates)
- **EXCLUDED**: docs/testing/ (legacy test artifacts, may delete)
- **EXCLUDED**: docs/ideation/ (user ideation, not project documentation)

## Context & Research

### Git History Findings

The docs folder experienced a delete/restore cycle:

- **2026-04-03 17:46:54**: `chore: remove deprecated documentation and task files` — deleted 48 docs files
- **2026-04-03 18:26:44**: `docs: restore documentation and task files` — restored all files

For deleted/restored files, git history shows TWO add entries:

1. Original creation date (e.g., `2025-08-02` for TestingGuidelines.md)
2. Restore date (2026-04-03)

The **first date is the true creation date**.

### Files Missing Frontmatter Date

Root-level docs files needing dates:

| File                              | First Git Add (True Creation)     |
| --------------------------------- | --------------------------------- |
| TestingGuidelines.md              | 2025-08-02                        |
| TestDoubleBestPractices.md        | 2025-08-02                        |
| CodeCoverage.md                   | 2025-07-12                        |
| mock-naming-conventions.md        | 2025-07-17                        |
| useless-rules-analysis.md         | unknown (check)                   |
| SCSS_VALIDATION_SUMMARY.md        | 2025-07-16                        |
| start-script.md                   | 2025-07-19                        |
| SECRETS-SETUP.md                  | 2026-03-30                        |
| SSH-AGENT-SETUP.md                | 2026-03-30                        |
| TRIMMING-WARNINGS.md              | 2025-07-13                        |
| MODERNIZATION-SUMMARY.md          | 2025-07-16                        |
| DEVCOTAINER-PLAN.md               | 2026-03-30                        |
| accessibility-animations-guide.md | ✅ DONE (2024-12-01 from content) |

### Embedded Date Sources

Some files have dates in content footer:

- `accessibility-animations-guide.md`: `_Last updated: December 2024_` → used 2024-12-01
- `SCSS_VALIDATION_SUMMARY.md`: `_Validation completed on: January 16, 2025_` → used 2025-01-16

## Key Technical Decisions

- **Git-first approach**: Query git for absolute first add date per file, not restoration date
- **Scripted process**: Use PowerShell/git commands to automate date lookup for all files
- **Preserve existing**: Do not modify files that already have frontmatter date

## Open Questions

### Resolved During Planning

- **How to find true creation date for restored files?** — Use `git log --follow --diff-filter=A --pretty=format:"%ai" -- <filename>` and take the earliest date
- **What about files with embedded dates?** — Extract from content first (more accurate), fallback to git

### Deferred to Implementation

- **Should we add dates to subfolders?** — No, they already have dates. Confirm via spot-check.

## Implementation Units

- [ ] **Unit 1: Verify all root-level docs files missing dates**

**Goal:** Confirm complete list of files needing frontmatter date

**Dependencies:** None

**Files:**

- Modify: All `docs/*.md` files without frontmatter `date:`

**Approach:**

- Run glob to get all `docs/*.md` files
- Check each for existing frontmatter `date:` field
- Confirm final list matches research findings

**Test scenarios:**

- Happy path: Glob returns correct file count
- Edge case: New files added since research

**Verification:**

- All files accounted for in final list

---

- [ ] **Unit 2: Query git for first add dates**

**Goal:** Get absolute first creation date for each file needing date

**Dependencies:** Unit 1

**Files:**

- (No files created/modified)

**Approach:**

```powershell
# For each file missing date, run:
git log --all --follow --diff-filter=A --pretty=format:"%ai" -- docs/<filename> | tail -1
# tail -1 gets the EARLIEST date (first add)
```

**Test scenarios:**

- Happy path: Returns date like "2025-08-02"
- Edge case: File never committed (should not happen)

**Verification:**

- All dates captured in lookup table

---

- [ ] **Unit 3: Update frontmatter for each file**

**Goal:** Add `date:` frontmatter to all files missing it

**Dependencies:** Unit 2

**Files:**

- Modify: All `docs/*.md` files missing frontmatter date

**Approach:**

- For each file, add YAML frontmatter with `date:` field
- Preserve any existing frontmatter fields
- Format: `date: YYYY-MM-DD`

**Test scenarios:**

- Happy path: File now has `date:` in frontmatter
- Edge case: File already has other frontmatter fields

**Verification:**

- Run glob again to confirm no root-level docs files missing date

---

- [ ] **Unit 4: Validate consistency**

**Goal:** Ensure all docs files now have timestamp frontmatter

**Dependencies:** Unit 3

**Files:**

- (No files created/modified)

**Approach:**

- Scan all `docs/*.md` files
- Confirm each has `date:` frontmatter
- Report any exceptions

**Test scenarios:**

- Happy path: All files have date
- Error path: Identify any remaining missing dates

**Verification:**

- 100% of root-level docs/\*.md have frontmatter date

## System-Wide Impact

- **Interaction graph:** No code or workflow changes
- **Error propagation:** N/A
- **State lifecycle risks:** None — metadata only
- **Unchanged invariants:** File content unchanged, only frontmatter added

## Risks & Dependencies

| Risk                                    | Mitigation                             |
| --------------------------------------- | -------------------------------------- |
| Git history unavailable for some files  | Fallback to content inspection         |
| Script fails on Windows path edge cases | Manual fallback for problematic files  |
| New files added during work             | Re-run validation after implementation |

## Documentation / Operational Notes

- This is a one-time normalization task
- Future docs should follow frontmatter date convention
- Consider adding this to docs style guide or AGENTS.md

## Sources & References

- Git history analysis: `git log --all --follow --diff-filter=A --pretty=format:"%ai" -- docs/<filename>`
- Restore commit: `8719e2c` (2026-04-03)
- Delete commit: `c68747a` (2026-04-03 17:46:54)
