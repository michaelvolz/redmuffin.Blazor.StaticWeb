---
date: 2026-04-05
topic: rm-commit-herestring-parser-error-fix
---

# rm-commit Here-String Parser Error — Structural Fix

## Problem Frame

The `rm-commit` skill uses PowerShell here-strings (`@"..."@`) to create commit messages with guaranteed line-length control. This solved the chronic commitlint retry loop that `-m -m` flags caused. But the AI agent consistently produces a `ParserError` by squashing `@"` onto the same line as other code:

```powershell
$msg = [System.IO.Path]::GetTempFileName(); try { @" fix(rm-commit): ...
```

PowerShell requires `@"` to be followed immediately by a newline. The AI violates this when composing multi-operation bash calls. Previous fixes (instructional warnings, template rearrangement, COMMANDS table cross-references) have all failed because the AI ignores instructions when composing one-liners.

## Proposed Solution: Isolated Here-String Piped to `git commit -F -`

### The Pattern

```powershell
@"
fix(scope): imperative subject

Body paragraph one. Each line ≤ 80 chars.
Wrap manually to stay safe.

Refs: #123
"@ | git commit -F -
```

That's it. No temp file. No cleanup. No try/finally. No other code.

### Why This Is Structurally Safer

| Failure Mode                  | Why It Can't Happen                                                                                             |
| ----------------------------- | --------------------------------------------------------------------------------------------------------------- |
| `@"` squashed with other code | `@"` is the **first character** in the bash call. There is no other code to squash it with.                     |
| Line length violations        | Here-string template preserves exact line breaks. AI fills in the visual template (proven to work since Apr 3). |
| Temp file orphan              | No temp file created.                                                                                           |
| BOM corruption                | `git commit -F -` reads stdin directly — no file encoding involved.                                             |
| Stale lock during commit      | Lock check is a separate step BEFORE this call.                                                                 |

### What Changes in the Skill

- **Remove** all temp file patterns, `[System.IO.Path]::GetTempFileName()`, try/finally blocks, and alternative methods
- **Replace** with this single pattern as the ONLY way to commit
- **Staging** is a completed step BEFORE this (separate bash call)
- **Stale lock check** is a separate step BEFORE this (separate bash call)
- **Keep** the here-string quoting rules (single vs double), line length guidance, and CRITICAL rules about `#` in body

### What Must Be Tested

1. **Basic commit** — Does `@"..."@ | git commit -F -` work in OpenCode's bash tool (PowerShell)?
2. **Line length** — Does commitlint pass on first attempt?
3. **Special characters** — Does `$` or backtick in commit body cause issues with double-quoted here-string?
4. **Single-quoted here-string** — Does `@'...'@ | git commit -F -` work for messages with `$` literals?
5. **Stale lock retry** — If commit fails with lock error, can we retry with the same here-string? (This is the one drawback — no temp file to re-read. May need to re-generate the here-string or capture it in a variable first.)

### Known Risks / Open Questions

- **Retry on lock failure**: Without a temp file, a retry requires re-generating the here-string or storing it in a variable first. This adds complexity back. Need to test if the lock retry is actually needed in practice.
- **AI still adds code before `@"`**: The AI could still deliberately add `git add .; @"...` before the here-string. The skill must present staging as a COMPLETED step before the commit step, with no reason to combine them.
- **`git commit -F -` in PowerShell**: Verified in the Apr 3 solution doc as working. But worth re-testing in the current OpenCode environment.

## Other Directions to Explore

### Direction A: Pre-Commit Line-Length Enforcement Script

A PowerShell script that takes raw text and wraps lines to ≤80 chars automatically. The AI writes the message (any length), the script enforces line length, then pipes to `git commit -F -`. No retry loops, no instruction dependency. Risk: wrapping might break formatting (code refs, URLs).

### Direction B: Two-Call Approach with Variable Capture

Call 1: Store here-string in a variable. Call 2: Pipe variable to `git commit -F -`. The here-string is isolated in Call 1, and the variable is reused in Call 2. Enables retry without re-generation. Risk: AI might still squash Call 1.

### Direction C: Write Tool + `git commit -F`

Use the Write tool to create the commit message file (no shell parsing at all), then `git commit -F <path>` in bash. 100% eliminates parser errors. Risk: AI might still write long lines in the Write tool content (same instruction compliance problem).

### Direction D: Commitlint Pre-Check

Run a pre-commit validation that checks line length before calling `git commit`. Catches violations early, avoids the commitlint hook retry loop. Risk: adds complexity, doesn't prevent the parser error.

## ~~New Direction: Switch to Git Bash (Real Heredoc)~~ — REJECTED

### Research Results: Heredocs Are Broken in AI Coding Tools

Multiple confirmed bugs across AI coding platforms prove heredocs don't work:

- **Claude Code #4315** (Closed "Not Planned"): Bash tool **appends `< /dev/null`** to heredoc content, causing `warning: here-document at line 1 delimited by end-of-file (wanted 'EOF')`. This is a **known, won't-fix issue**.
- **OpenCode #15810** (Closed): Bash tool on Windows has silent command corruption. While heredocs in Git Bash are "preserved" in theory, the broader environment corrupts multi-line constructs.
- **Claude Code #18526**: "Bad substitution error when using heredoc/EOF syntax"
- **Claude Code #25015**: "Bash tool hangs with heredoc in chained command"
- **Claude Code #32879**: "Bash tool: shell-quote mangles heredoc markers and $ in piped commands"
- **Zed #49068**: "Heredoc in AI shell results in syntax error: unexpected end of file"

### Verdict

**Heredocs (`<<'EOF'`) are fundamentally broken in AI coding tool bash execution environments.** The bash tool preprocesses input before passing it to the shell, and heredoc syntax gets mangled. This direction is **dead**.

## The Real Constraint Map

After reviewing all approaches, here's what's actually viable:

| Approach                                            | Parser Error Risk             | Line Length Guarantee           | Works in AI Tools?        |
| --------------------------------------------------- | ----------------------------- | ------------------------------- | ------------------------- |
| PowerShell here-string (`@"`)                       | ❌ `@"` gets squashed         | ✅ Preserved                    | ❌ Fails                  |
| Bash heredoc (`<<'EOF'`)                            | ❌ Bash tool mangles heredocs | ✅ Preserved                    | ❌ Fails                  |
| `-m -m` flags                                       | ✅ No parser errors           | ❌ AI writes long lines         | ❌ Retry loops            |
| Write tool + `git commit -F`                        | ✅ No shell parsing           | ⚠️ Depends on AI discipline     | ⚠️ Maybe                  |
| **Write tool + auto-wrap script + `git commit -F`** | ✅ No shell parsing           | ✅ Script enforces mechanically | ✅ **Only viable option** |

## Recommended Direction: Write Tool + Auto-Wrap Script + `git commit -F`

### The Pattern

**Step 1**: AI uses Write tool to create raw commit message (any length):

```
File: $env:TEMP\commit-msg-raw.txt
Content:
fix(scope): imperative subject

This is a very long body line that exceeds 100 characters and would normally fail commitlint validation.
```

**Step 2**: PowerShell script auto-wraps lines to ≤80 chars:

```powershell
$content = Get-Content -Path "$env:TEMP\commit-msg-raw.txt" -Raw
$wrapped = $content -split "`n" | ForEach-Object {
    if ($_.Length -gt 80) {
        # Word-wrap at 80 chars
        $words = $_ -split '\s+'
        $line = ''
        foreach ($word in $words) {
            if (($line + ' ' + $word).Trim().Length -gt 80 -and $line) {
                $line.Trim()
                $line = $word
            } else {
                $line = ($line + ' ' + $word).Trim()
            }
        }
        $line.Trim()
    } else {
        $_
    }
}
$wrapped -join "`n" | Set-Content -Path "$env:TEMP\commit-msg.txt" -Encoding utf8NoBOM
```

**Step 3**: Commit:

```powershell
git commit -F "$env:TEMP\commit-msg.txt"
```

**Step 4**: Cleanup:

```powershell
Remove-Item "$env:TEMP\commit-msg-raw.txt", "$env:TEMP\commit-msg.txt" -Force -ErrorAction SilentlyContinue
```

### Why This Is the Only Viable Option

| Constraint             | How It's Satisfied                                               |
| ---------------------- | ---------------------------------------------------------------- |
| No parser errors       | Write tool writes raw text — no shell parsing at all             |
| Line length guaranteed | Script enforces mechanically — no AI discipline needed           |
| Works in AI tools      | Write tool + simple bash commands — no heredocs, no here-strings |
| No retry loops         | Line length is enforced before commitlint sees it                |

### What Changes in the Skill

- Replace all here-string/heredoc patterns with Write tool + auto-wrap script
- Skill provides the auto-wrap script as a copy-pasteable template
- AI writes raw message content via Write tool (no line length discipline needed)
- Script handles wrapping mechanically
- `git commit -F` reads the wrapped file

### Risks

- **Word wrapping might break formatting**: URLs, code references, or intentional long lines could be wrapped incorrectly. Mitigation: the script can preserve lines that start with specific patterns (URLs, code blocks).
- **More complex workflow**: 4 steps instead of 1. But each step is simple and mechanical.
- **Temp file cleanup**: Two temp files to clean up. Mitigation: use a single script that handles everything.

## Decision: Write Tool + `git commit -F` (Implemented)

### The Pattern

**Step 1**: Use the Write tool to create `$env:TEMP\commit-msg.txt` with the commit message content.

**Step 2**: `git commit -F "$env:TEMP\commit-msg.txt"`

**Step 3**: `Remove-Item "$env:TEMP\commit-msg.txt" -Force -ErrorAction SilentlyContinue`

### Why This Was Chosen

| Constraint              | How It's Satisfied                                                         |
| ----------------------- | -------------------------------------------------------------------------- |
| No parser errors        | Write tool writes raw text — no shell parsing, no quoting, no delimiters   |
| Line length preserved   | Write tool preserves exact line breaks — identical to here-string behavior |
| Retry-safe              | File persists after failed commit — retry is just `git commit -F` again    |
| No temp file generation | Fixed path, no `[System.IO.Path]::GetTempFileName()`, no try/finally       |

### Remaining Risks

- **AI might ignore the Write tool instruction** and try to compose a bash one-liner instead. The skill makes the Write tool the ONLY option with no fallback. The consequence of ignoring it is a wrong tool choice (not a parser error), which is less severe.
- **Line length still depends on AI discipline** — same as the here-string approach. The Write tool doesn't mechanically enforce line length. This is accepted because the here-string approach had the same limitation and worked in practice.
- **Apostrophes are now safe** — no shell parsing means no escaping needed. This was the fatal flaw of the single-quoted string approach.

### Status

Implemented in `.opencode/skills/rm-commit/SKILL.md`. The skill was rewritten to use the Write tool pattern exclusively.

## Planned: Pester Tests for `List-Sidenotes.ps1`

Pester tests for `scripts/List-Sidenotes.ps1` are planned but deferred. The approach:

1. Create a test branch (e.g., `test/list-sidenotes-pester`)
2. Write the complete test file covering all critical paths
3. Iterate and fix locally on the test branch
4. When all tests pass, create a single clean commit
5. Merge or discard the test branch

This avoids polluting the main branch with iterative test-fix commits.

### Test Coverage Planned

- Happy path: well-formed sidenotes listed correctly
- Empty directory: outputs "No pending sidenotes."
- Malformed frontmatter: skipped with `[malformed sidenote]` marker
- Non-pending status: filtered out
- Title fallback: truncated body text when title missing
- Long title warning: fires at >110 chars
- Natural sort order: SN-0001 through SN-0010
- Custom `-SidenotesPath` parameter
- Edge cases: SN-0000, SN-1000, malformed IDs

## Other Directions (Fallback — All Rejected)

### Direction A: Pre-Commit Line-Length Enforcement Script (Standalone)

Rejected: Still needs a way to get the raw text into the script without heredocs or here-strings. The Write tool is the only viable input mechanism.

### Direction B: Two-Call Approach with Variable Capture

Rejected: AI still squashes the here-string in Call 1. Same failure mode.

### Direction C: Write Tool + `git commit -F` (No Auto-Wrap)

Rejected: AI might still write long lines in the Write tool content. No mechanical enforcement.

### Direction D: Commitlint Pre-Check

Rejected: Adds complexity, doesn't prevent the parser error, and still needs a way to get the message into git.
