---
name: rm-guide-warnings
description: "Shortcut: rm:guide-warnings. Use when fixing analyzer warnings, pragma directives, or zero-warning build issues."
---

# rm-guide-warnings

## CRITICAL

- Target zero build warnings.
- Do not change `#pragma warning disable` lines without explicit approval.
- Prefer one consistent fix for each warning class.

## WHEN TO LOAD

- Cleaning analyzer output or build warnings.
- Reviewing code that already contains pragmas.

## GUIDANCE

- Fix root causes before suppressing warnings.
- Keep warning suppressions documented and intentional.

## NEVER

- Do not remove deliberate pragmas to silence a warning temporarily.
