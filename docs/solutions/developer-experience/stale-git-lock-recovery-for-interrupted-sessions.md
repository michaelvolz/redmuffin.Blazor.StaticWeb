---
title: Stale git lock recovery for interrupted agent sessions
date: 2026-04-04
category: developer-experience
module: development_workflow
problem_type: developer_experience
component: tooling
severity: medium
applies_when:
  - Agent session is interrupted mid-commit (Esc, timeout, process kill)
  - Subsequent git operations fail with "Another git process seems to be running"
  - `.git/index.lock` file persists after session ends
tags:
  - git-lock
  - session-interruption
  - agent-workflow
  - self-healing
  - powershell
---

# Stale git lock recovery for interrupted agent sessions

## Context

When an agent session is interrupted (user presses Esc, timeout fires, process is killed) while git holds `.git/index.lock`, the lock file persists indefinitely. All subsequent git operations fail with:

```
Another git process seems to be running in this repository, e.g.
an editor opened by 'git commit'. Please make sure all processes
are terminated then try again.
```

This required manual intervention every time — the agent or user had to delete `.git/index.lock` before any git command would work.

## Guidance

Add a **pre-flight stale lock check** to any git write operation in agent workflows. The check runs before `git add` and `git commit`:

```powershell
$lockFile = ".git/index.lock"
if (Test-Path $lockFile) {
    $age = (Get-Date) - (Get-Item $lockFile).LastWriteTime
    $ageSeconds = [math]::Round($age.TotalSeconds)
    if ($age -gt [TimeSpan]::FromMinutes(1)) {
        Write-Warning "Stale git lock detected (${ageSeconds}s old). Removing..."
        Remove-Item $lockFile -Force
    } else {
        Write-Error "Active git operation in progress (${ageSeconds}s old). Wait or investigate."
        exit 1
    }
}
```

**Safety rules**:

- Only remove locks older than **1 minute** — no legitimate git operation holds a lock that long without progress
- If the lock is fresh (< 1 min), **abort and warn** — something is actively running
- This check is **idempotent** — safe to run even when no lock exists
- Only applies to `.git/index.lock` — do not use this logic for `.git/refs/*.lock` or `.git/HEAD.lock`

## Why This Matters

Without self-healing lock recovery:

- Every session interruption requires manual file deletion
- The agent cannot recover autonomously — it needs human intervention
- The error message is misleading ("another git process") when the real cause is a dead session
- Repeated interruptions compound the friction

The 1-minute threshold is safe because:

- A normal `git commit` completes in < 1 second
- Even a slow commit with hooks completes in < 10 seconds
- No legitimate git operation holds a lock for 60+ seconds without making progress

## When to Apply

- Agent workflows that run git write operations (`git add`, `git commit`, `git rebase`)
- Any automated process that could be interrupted mid-operation
- Skills or scripts that interact with git on Windows (where lock files are more prone to orphaning)

## Examples

**In a commit skill** (rm-commit):

```powershell
# Step 1: Stale lock recovery
$lockFile = ".git/index.lock"
if (Test-Path $lockFile) {
    $age = (Get-Date) - (Get-Item $lockFile).LastWriteTime
    if ($age -gt [TimeSpan]::FromMinutes(1)) {
        Remove-Item $lockFile -Force
    }
}

# Step 2: Normal git operations
git status
git add <files>
git commit -F <message-file>
```

**What NOT to do** — changing the commit message format does NOT fix this:

```powershell
# These ALL fail the same way if interrupted mid-flight:
@"msg"@ | git commit -F -     # pipe pattern
git commit -F $tmpFile        # temp file pattern
git commit -m "message"       # inline pattern
```

The lock is acquired by git itself, not by the message delivery mechanism. Any git write operation killed after lock acquisition but before cleanup leaves the same orphaned file.

## Related

- `docs/solutions/developer-experience/bash-timeout-kills-long-running-dotnet-processes-2026-04-04.md` — another session-interruption issue (bash timeout kills dev servers)
- `docs/solutions/developer-experience/fix-rm-commit-heredoc-syntax-and-line-length-violations-2026-04-03.md` — the rm-commit skill that received the lock recovery fix
