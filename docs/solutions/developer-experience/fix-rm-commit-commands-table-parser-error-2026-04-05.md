---
title: Fix rm-commit COMMANDS table here-string parser error
date: 2026-04-05
category: developer-experience
module: agent-tooling
problem_type: developer_experience
component: development_workflow
severity: medium
symptoms:
  - "ParserError: No characters are allowed after a here-string header but before the end of the line"
  - AI agent copies broken one-liner from COMMANDS quick-reference table
root_cause: inadequate_documentation
resolution_type: documentation_update
tags:
  - git-commit
  - skill-optimization
  - powershell
  - here-string
  - commands-table
---

# Fix rm-commit COMMANDS Table Here-String Parser Error

## Context

The `rm-commit` skill's COMMANDS quick-reference table (line 39) contained a one-liner using PowerShell here-string syntax:

```powershell
$msg = [System.IO.Path]::GetTempFileName(); @"..."@ | Set-Content $msg -Encoding utf8NoBOM; git commit -F $msg; Remove-Item $msg -Force
```

This is syntactically invalid in PowerShell. The `@"` here-string header **must** be followed immediately by a newline — no other code can precede it on the same line. The AI agent copies this one-liner from the COMMANDS table (the quick-reference it scans first), producing a `ParserError` on every commit attempt.

The skill's WORKFLOWS section already contains correct multi-line here-string templates. The bug was isolated to the COMMANDS table's attempt to compress a multi-line construct into a single table cell.

## Symptoms

- `ParserError: No characters are allowed after a here-string header but before the end of the line`
- Error occurs consistently when the AI uses the COMMANDS table entry
- The WORKFLOWS section templates work correctly — only the quick-reference table is broken

## What Didn't Work

- **`git commit -m "subject" -m "body"`** — Initially tried replacing the here-string with `-m` flags in the COMMANDS table. This works syntactically but changes behavior: `-m` flags lack the mechanical line-length enforcement and template structure that the here-string approach provides. The team explicitly rejected this approach after prior experience showed it produces commitlint violations.

## Solution

Replace the broken one-liner in the COMMANDS table with a cross-reference to the working WORKFLOWS section:

```markdown
| See WORKFLOWS → Commit (here-string via temp file) | Commit with Conventional Commits | After staging |
```

This is the only viable fix because:

1. A PowerShell here-string **cannot** exist on one line — it's a language constraint
2. The COMMANDS table is a single-line-per-row markdown table
3. You physically cannot put valid here-string syntax in a table cell
4. The WORKFLOWS section already contains the correct, complete templates

The FLOW step 6 was also verified to remain consistent: "Commit with Conventional Commits using PowerShell here-string written to a temp file, then `git commit -F $file`."

## Why This Works

The root cause was **inadequate documentation** — the COMMANDS table tried to compress a multi-line PowerShell construct into a single-line table cell, which is impossible. The fix removes the broken inline code and points the AI to the authoritative templates in the WORKFLOWS section.

The skill has defensive redundancy: the commit pattern appears in FLOW (summary), WORKFLOWS (detailed walkthrough with retry logic), and PATTERNS (standalone snippets). An agent can find working code from any entry point.

## Prevention

- **Never put multi-line constructs in single-line table cells** — If a code pattern requires line breaks (here-strings, heredocs, multi-line strings), reference the section where the full template lives instead of trying to inline it
- **Test skill table entries as executable commands** — Every code snippet in a COMMANDS table should be independently valid and copy-pasteable. If it can't be, use a reference instead
- **Verify cross-references after skill edits** — When updating a skill's WORKFLOWS section, check that the COMMANDS table still points to the correct location

## Related

- `docs/solutions/developer-experience/fix-rm-commit-heredoc-syntax-and-line-length-violations-2026-04-03.md` — Original heredoc-to-here-string migration for the rm-commit skill (moderate overlap — same file, same root cause category, but different solution)
- `docs/solutions/developer-experience/commitlint-rejects-body-when-hash-in-body-2026-04-04.md` — Related commitlint issue with `#` + identifier in commit body
- `docs/solutions/developer-experience/stale-git-lock-recovery-for-interrupted-sessions-2026-04-04.md` — Git lock recovery pattern added to the same skill
