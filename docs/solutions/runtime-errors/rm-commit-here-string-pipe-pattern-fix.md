---
title: Fix rm-commit ENOTDIR Error with Here-String Pipe Pattern
date: 2026-04-05
tags:
  - rm-commit
  - commit
  - temp-file
  - write-tool
  - here-string
  - pipe
---

> **Current (2026-06-08):** The here-string pipe pattern has been superseded for commit messages by the tool-call gate pattern in design-patterns/enforcement-surfaces-and-commit-body-verification.md (Write-Output → verify → Set-Content → git commit -F). Pipe is still valid for simple cases.

# Fix rm-commit ENOTDIR Error with Here-String Pipe Pattern

## Problem

The rm-commit skill failed with ENOTDIR error when attempting to create commit messages using the Write tool with `$env:TEMP` paths, preventing commits from working.

## Symptoms

- ENOTDIR: not a directory, mkdir 'B:\redmuffin.Blazor.StaticWeb\$env:TEMP'
- Commit attempts fail with path expansion issues
- Write tool cannot create temp files for commit messages

## What Didn't Work

- Write tool with `$env:TEMP\commit-msg.txt` - Node.js tool doesn't expand PowerShell environment variables
- Path becomes relative and contains invalid `$env` characters
- Attempting to create directory with `$` in name fails

## Solution

Replace Write tool + temp file approach with here-string piped directly to `git commit -F -`:

```powershell
@"
type(scope): imperative subject

Body paragraph one. Each line ≤ 80 chars.
Wrap manually at ~80 to stay safe.

Refs: #123
"@ | git commit -F -
```

Updated the rm-commit skill to use this pattern exclusively.

## Why This Works

- **No temp files**: Message piped directly to git via stdin
- **No $ signs**: Eliminates bash tool parsing issues and path expansion problems
- **No directory creation**: Avoids ENOTDIR errors entirely
- **Direct pipe**: Reliable across PowerShell/bash environments
- **Same formatting**: Preserves line breaks and commitlint compliance

## Prevention

- Use here-string pipe pattern for commit messages instead of temp files
- Test commit workflows across different models/environments
- Avoid $ signs in bash tool commands when possible
- Prefer direct pipes over intermediate file creation

## Related

- `docs/solutions/developer-experience/fix-rm-commit-parser-errors-with-write-tool-and-git-commit-f-2026-04-05.md` - Previous Write tool fix
- `docs/solutions/developer-experience/fix-rm-commit-heredoc-syntax-and-line-length-violations-2026-04-03.md` - Here-string migration
