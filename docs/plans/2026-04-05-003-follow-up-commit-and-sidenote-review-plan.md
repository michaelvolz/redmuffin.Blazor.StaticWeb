---
title: "follow-up: commit workflow and sidenote list hardening"
type: fix
status: active
date: 2026-04-05
---

# Follow-up: commit workflow and sidenote list hardening

## Overview

This follow-up keeps the current deterministic commit-message approach and the
new sidenote list script, then hardens the remaining review items in a test
branch so we can iterate without polluting the main branch.

## What remains

- `rm-commit` still needs a final decision on the user-facing shortcut entrypoint
- `rm-sidenotes` still needs the capture order tightened so the acknowledgement
  only happens after the file is confirmed on disk
- `List-Sidenotes.ps1` still needs regression tests for malformed IDs and the
  output contract

## Recommended order

1. Keep the current commit-message implementation as-is for now.
2. Open a temporary test branch for any remaining commit-workflow experiments.
3. Tighten the `rm-sidenotes` capture acknowledgement ordering.
4. Add Pester coverage for `List-Sidenotes.ps1` on the test branch.

## Test branch approach

- Create a throwaway branch dedicated to tests and experiments
- Commit as often as needed there
- Discard the branch once the tests and final wording are settled

## Notes

- The existing `docs/plans/2026-04-05-001-feat-fast-sidenotes-list-script-plan.md`
  remains the implementation plan for the sidenote list script
- This plan is only a short follow-up to capture the remaining review items
