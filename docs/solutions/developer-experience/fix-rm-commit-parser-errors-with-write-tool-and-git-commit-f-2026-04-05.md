---
date: 2026-04-05
title: Fix rm-commit Parser Errors with Write Tool and git commit -F
module: Developer Experience
tags: [rm-commit, powershell, git, commit, write-tool, parser-error, bash-tool]
problem_type: Bug Fix
---

# Fix rm-commit Parser Errors with Write Tool and git commit -F

## Problem

The `rm-commit` skill needed a deterministic way to create commit messages
without relying on brittle PowerShell quoting. The prior here-string and
single-quoted-string attempts both reintroduced parser errors in different
ways:

- here-strings (`@"..."@`) failed when the agent squashed `@"` onto one line
- single-quoted strings failed on apostrophes (`don't`, `user's`)

The result was the same failure class the workflow was meant to eliminate:
parser errors before `git commit` ever ran.

## Symptoms

- `ParserError: No characters are allowed after a here-string header but before the end of the line.`
- `ParserError` on apostrophes inside single-quoted strings
- line-length retry loops when `-m -m` was used instead of a visual template

## What Didn't Work

| Attempt                         | Why It Failed                                                                     |
| ------------------------------- | --------------------------------------------------------------------------------- |
| Here-string in bash             | PowerShell syntax required `@"` to be alone on its line                           |
| Single-quoted multi-line string | Apostrophes in commit messages broke parsing                                      |
| Git Bash heredoc                | AI coding tools mangled heredoc syntax in the shell tool                          |
| `-m -m` flags                   | Line-length discipline relied on AI behavior and drifted into commitlint failures |

## Solution

Use the **Write tool** to create a temp file containing the full commit message,
then run `git commit -F <file>` and delete the temp file afterward.

### Pattern

1. Write commit text to `$env:TEMP\commit-msg.txt`
2. Run `git commit -F "$env:TEMP\commit-msg.txt"`
3. Remove the temp file

### Example

```text
File: $env:TEMP\commit-msg.txt
Content:
fix(scope): imperative subject

Body paragraph one. Each line ≤ 80 chars.
Wrap manually at ~80 to stay safe.

Refs: #123
```

Then:

```powershell
git commit -F "$env:TEMP\commit-msg.txt"
Remove-Item "$env:TEMP\commit-msg.txt" -Force -ErrorAction SilentlyContinue
```

## Why This Works

| Constraint             | How It's Satisfied                                                               |
| ---------------------- | -------------------------------------------------------------------------------- |
| No parser errors       | The Write tool writes raw text. No shell quoting, delimiters, or escaping        |
| Line length preserved  | The file preserves exact line breaks, just like the earlier here-string template |
| Apostrophes safe       | The file is plain text, so `don't` and `user's` require no special handling      |
| `$` and backticks safe | No PowerShell parsing occurs while creating the message                          |
| Retry-safe             | If commit fails, the file still exists and can be re-used                        |

## Test Results

The skill change itself was committed successfully using this approach:

- Commit: `a8566ea`
- No parser errors
- The commit message preserved line breaks and body length correctly

## Prevention

- Keep commit-message construction out of shell syntax whenever possible
- Use a temp file for retryable commit messages
- Keep `Refs: #123` in the footer, not the body
- Defer test-heavy validation to a throwaway branch so iteration does not pollute trunk

## Related Docs

- `docs/solutions/developer-experience/fix-rm-commit-commands-table-parser-error-2026-04-05.md`
- `docs/solutions/developer-experience/fix-rm-commit-heredoc-syntax-and-line-length-violations-2026-04-03.md`
- `docs/brainstorms/2026-04-05-rm-commit-herestring-parser-error-fix.md`

## Notes

The `rm-sidenotes` list fix is documented separately in the associated plan and
should be expanded into its own solution doc once the test branch work lands.
