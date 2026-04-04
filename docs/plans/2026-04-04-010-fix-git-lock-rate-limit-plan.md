---
title: Fix git lock errors from rate-limit-induced session kills
type: fix
status: completed
date: 2026-04-04
---

# Fix git lock errors from rate-limit-induced session kills

## Overview

Replace the pipe-based commit pattern (`@"..."@ | git commit -F -`) with a temp file approach and add a single retry wrapper. This shrinks the window where API rate limits can orphan `.git/index.lock` and makes the commit operation self-healing on the first retry.

## Problem Frame

OpenCode's bash tool enforces a hard timeout and kills the shell session when API rate limits trigger a retry. If this happens while git holds `.git/index.lock` (during commit, especially while the commit-msg hook runs commitlint), the lock file persists and blocks all subsequent git operations. The stale lock recovery script handles the symptom but doesn't prevent the cause.

## Requirements Trace

- R1. Commits must survive a single rate-limit-induced shell kill without orphaning locks
- R2. The here-string message format and line-length enforcement must be preserved
- R3. The commit workflow must remain a single atomic operation from the agent's perspective
- R4. No regression in commit message quality (subject, body, footer formatting)

## Scope Boundaries

- Only changes `rm-commit/SKILL.md` — no changes to git hooks, commitlint, or other tooling
- Does not change the commit message format, quoting rules, or line-length guidance
- Does not change staging behavior or commit splitting logic

## Context & Research

### Relevant Code and Patterns

- `.opencode/skills/rm-commit/SKILL.md` — the skill to modify
- `.githooks/commit-msg` — runs `commitlint --edit "$1"`, adds ~1-2s to every commit
- Existing stale lock recovery script (1-minute threshold) — keep as-is

### Institutional Learnings

- `docs/solutions/developer-experience/stale-git-lock-recovery-for-interrupted-sessions-2026-04-04.md` — documents the lock recovery pattern
- `docs/solutions/developer-experience/fix-rm-commit-heredoc-syntax-and-line-length-violations-2026-04-03.md` — documents why here-string format was adopted (must not regress)
- `docs/solutions/developer-experience/bash-timeout-kills-long-running-dotnet-processes-2026-04-04.md` — same root cause class (bash tool kills processes)

## Key Technical Decisions

- **Temp file over pipe**: The here-string is written to a temp file first, then git reads from the file. This is faster (no pipe setup, no stdin streaming) and more atomic (message fully materialized before git starts). The here-string still provides the visual template and line-length discipline.
- **Single retry, not a loop**: One retry after stale lock removal. No bounded loops — if it fails twice, something else is wrong and the agent should surface it.
- **Keep stale lock recovery**: The existing 1-minute threshold script stays as the first line of defense. The retry wrapper is a second layer.

## Implementation Units

- [ ] **Unit 1: Switch commit pattern from pipe to temp file**

**Goal:** Replace `@"..."@ | git commit -F -` with temp file approach in all examples and the COMMANDS table.

**Requirements:** R1, R2, R3, R4

**Dependencies:** None

**Files:**

- Modify: `.opencode/skills/rm-commit/SKILL.md`

**Approach:**

- Update the COMMANDS table entry to show the temp file pattern
- Update all code examples in WORKFLOWS (section 5) and PATTERNS sections
- The pattern: create temp file → write here-string to it → `git commit -F $file` → delete temp file
- Use `[System.IO.Path]::GetTempFileName()` for the temp file
- Use `Set-Content -Path $msg -Encoding UTF8` to write (no `-NoNewline` — git expects a trailing newline)
- Use `Remove-Item $msg -Force` in a `finally`-style cleanup (or just after commit succeeds)

**Patterns to follow:**

- Keep the same here-string visual template structure (subject, blank line, body, footer)
- Keep all quoting rules (double-quoted vs single-quoted here-strings)
- Keep all line-length enforcement guidance

**Test scenarios:**

- Happy path: Commit with subject + body + footer works identically to the pipe pattern
- Edge case: Commit message with `$` and backticks uses single-quoted here-string correctly
- Error path: If temp file creation fails, the error is surfaced (not silently swallowed)

**Verification:**

- `git status` shows clean working tree after commit
- `git log -1` shows correct subject, body, and footer
- commitlint passes (no line-length violations)

- [ ] **Unit 2: Add retry wrapper around git commit**

**Goal:** Wrap the commit command with a single retry that handles stale lock errors.

**Requirements:** R1, R3

**Dependencies:** Unit 1

**Files:**

- Modify: `.opencode/skills/rm-commit/SKILL.md`

**Approach:**

- Add a retry block in the Commit workflow section (section 5)
- Pattern: try commit → if lock error AND lock is stale → remove lock → retry once → if still fails, surface error
- The retry logic reuses the existing stale lock recovery script
- Update the FLOW section to mention the retry behavior
- Keep the pre-flight lock check (existing) — the retry is a fallback for locks acquired during the commit itself

**Patterns to follow:**

- PowerShell `try/catch` or manual error checking with `$LASTEXITCODE`
- Keep error messages actionable

**Test scenarios:**

- Happy path: Normal commit succeeds on first attempt (no retry triggered)
- Error path: Stale lock during commit → removed → retry succeeds
- Error path: Two consecutive failures → error surfaced to agent (no infinite loop)

**Verification:**

- Normal commits work without retry overhead
- A simulated lock file (created before commit) is removed and commit retries successfully

- [ ] **Unit 3: Update STALE LOCK RECOVERY documentation**

**Goal:** Update the root cause explanation to include rate limiting as a cause, not just session interruption.

**Requirements:** R1

**Dependencies:** None (can be done in parallel with Unit 1)

**Files:**

- Modify: `.opencode/skills/rm-commit/SKILL.md`

**Approach:**

- Update the "Root cause" paragraph in STALE LOCK RECOVERY to mention API rate limiting killing the bash tool's shell session
- Clarify that the pipe pattern is not the cause, but the temp file approach reduces the vulnerable window
- Add a note about the retry wrapper as a second layer of defense

**Verification:**

- Documentation accurately describes both causes (Esc interrupt + rate limit)
- Documentation explains the layered defense (pre-flight check + retry + temp file)

## Risks & Dependencies

| Risk                                                   | Mitigation                                                                                     |
| ------------------------------------------------------ | ---------------------------------------------------------------------------------------------- |
| Temp file left behind if commit crashes                | Use `Remove-Item` after commit; temp files are in system temp dir, cleaned up by OS eventually |
| Race condition: two commits simultaneously             | Not applicable — agent runs commits sequentially                                               |
| Regression in line-length enforcement                  | The here-string template is unchanged; what you type is what git reads from the file           |
| commitlint still slow, still vulnerable to rate limits | The temp file approach shrinks the window; the retry wrapper catches the remaining cases       |

## Sources & References

- Related solution: `docs/solutions/developer-experience/stale-git-lock-recovery-for-interrupted-sessions-2026-04-04.md`
- Related solution: `docs/solutions/developer-experience/fix-rm-commit-heredoc-syntax-and-line-length-violations-2026-04-03.md`
- Related solution: `docs/solutions/developer-experience/bash-timeout-kills-long-running-dotnet-processes-2026-04-04.md`
