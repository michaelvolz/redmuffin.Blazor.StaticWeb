---
name: rm-commit
description: "Shortcut: rm:commit. Use when the user says commit or wants help making a commit. Generates repo-specific conventional commit payloads."
---

# rm-commit

Create clean, reviewable git commits from the working tree.

## CRITICAL

- NEVER use `git revert`
- NEVER commit secrets
- NEVER push to remote
- ALWAYS check repo-specific rules in AGENTS.md/CLAUDE.md first
- Treat "undo commit" as "undo the last commit and keep changes as unstaged edits"
- NEVER put `#` followed by an identifier (`[A-Za-z0-9_-]+`, e.g., `#1234`, `#abc`, `#SN-0001`) in the commit body — the conventional-commits-parser regex `([\w-]+)(?=\s|$|[,;)\]])` treats it as an issue reference and moves everything after into the footer, making the body appear empty to commitlint. Always use `Refs: #NNNNN` in the footer instead. If you must mention an issue in the body, omit the `#` prefix.
- Sidenotes are referenced as `SN-NNNN` (no `#` prefix) in commit bodies. They are local file IDs, not GitHub issues. Write `See SN-0003 for context`, never `See #SN-0003 for context`.

## FLOW

1. Check repo rules first.
2. **Recover from stale git locks** (caused by interrupted sessions).
3. Inspect the working tree and recent history.
4. Decide whether changes belong in one commit or several.
5. Stage only the intended files or hunks.
6. Commit with Conventional Commits using PowerShell here-string piped to `git commit -F -`.
7. Verify the result with `git status`.

## COMMANDS

| Command                      | Purpose                          | When                   |
| ---------------------------- | -------------------------------- | ---------------------- |
| `git status`                 | Show working tree status         | Always first           |
| `git diff HEAD`              | Show all changes                 | After status           |
| `git branch --show-current`  | Get current branch               | After diff             |
| `git log --oneline -10`      | Show recent history              | After branch           |
| `git add -p`                 | Stage partial hunks              | File has mixed changes |
| `@"..."@ \| git commit -F -` | Commit with here-string template | Always                 |

## STALE LOCK RECOVERY

Git creates `.git/index.lock` at the start of any write operation and removes it when done.
If the agent session is interrupted (Esc, timeout, process kill) while git holds the lock,
the file persists and blocks all subsequent git operations.

**Root cause**: Session interruption during an active git operation. The pipe pattern
(`@"..."@ | git commit -F -`) is NOT the cause — any git write operation killed mid-flight
leaves the same orphaned lock.

**Recovery**: Before any git write operation, check for a stale lock and remove it if safe.

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

- Only remove locks older than 1 minute — no legitimate git operation holds a lock that long without progress
- If the lock is fresh (< 1 min), abort and warn — something is actively running
- Never remove `.git/refs/*.lock` or `.git/HEAD.lock` with this logic — those are separate lock files for different operations
- This check runs BEFORE `git add` and BEFORE `git commit`

## WORKFLOWS

### 1. Gather Context

```
git status && git diff HEAD && git branch --show-current && git log --oneline -10
```

Stop if tree is clean.

**Stale lock check**: If `git status` fails with "Another git process seems to be running",
run the stale lock recovery script from the STALE LOCK RECOVERY section, then retry.

### 2. Decide Commit Shape

- Split unrelated work into separate commits
- Use `git add -p` for mixed changes in one file
- Order: scaffolding → behavior → tests → docs → formatting
- Keep each commit independently understandable
- If files conflict, prefer smaller coherent commit

### 3. Format Message

Prefer Conventional Commits:

```
type(scope): imperative subject
```

Types: `feat`, `fix`, `docs`, `refactor`, `test`, `chore`, `perf`, `ci`, `style`, `build`

Rules:

- Subject: concise, imperative, and specific
- Body: required for every commit; keep each line at or under 100 characters
- Body: explain why, trade-offs, and impact — not just what changed
- Footer: `Refs: #123`, `Co-authored-by:`, or `BREAKING CHANGE:`
- **CRITICAL: Every line in the body must be ≤ 100 characters** — commitlint enforces `body-max-line-length: [2, 'always', 100]`

### 4. Stage Carefully

- Stage by file or partial hunk
- Avoid `git add -A` or `git add .` when safer staging exists
- Exclude secrets, generated files, accidental edits

### 5. Commit

**Pre-flight**: Run the stale lock check before committing. If `git add` or any prior git
operation succeeded, the lock is clean — skip the check. If this is a resumed session or
a prior git command failed with a lock error, run the check first.

Use a PowerShell here-string piped to `git commit -F -`. This preserves exact line breaks, avoids shell escaping issues, and works reliably in OpenCode's PowerShell environment on Windows.

**Template:**

```powershell
@"
type(scope): imperative subject

First body paragraph. Each line must be ≤ 100 characters.
Wrap manually at ~80 chars to stay safe.

Second body paragraph if needed. Same line length rule.

Refs: #123
"@ | git commit -F -
```

**Basic commit (subject + body):**

```powershell
@"
fix(frontend): prevent duplicate form submits

Disable submit immediately so rapid clicks cannot queue
duplicate requests.
"@ | git commit -F -
```

**Multi-paragraph body with footer:**

```powershell
@"
refactor(core): extract validation logic

Move input validation into a dedicated service so controllers
stay thin.

This also makes it easier to reuse validation across API and
web endpoints.

Refs: #123
"@ | git commit -F -
```

**Line length enforcement:**

- Wrap every body line at **≤ 80 characters** (safe margin under the 100-char limit)
- Count characters if unsure — do NOT guess
- The here-string preserves your exact line breaks, so what you type is what commitlint sees
- **Good:** Each line is a short, readable sentence fragment
- **Bad:** One long run-on line that exceeds 100 characters

**Quoting rules for here-strings:**

- Use `@"..."@` (double-quoted here-string) — variables like `$var` and backticks will be expanded
- If the message contains `$`, backticks, or other PowerShell metacharacters that should be literal, use `@'...'@` (single-quoted here-string) instead
- Single-quoted here-string: `@'...'@` — no variable expansion, no escape sequences

### 6. Verify

Run `git status` post-commit. Report hash and subject.

## PATTERNS

### Here-String Commit (PowerShell)

**Basic (subject + body):**

```powershell
@"
type(scope): imperative subject

Body explaining why the change exists.
Keep each line under 80 characters.
"@ | git commit -F -
```

**Multi-paragraph with footer:**

```powershell
@"
type(scope): subject

First paragraph explaining the change.
Wrap lines at ~80 characters.

Second paragraph with additional context.
Same line length discipline.

Refs: #123
"@ | git commit -F -
```

**Single-quoted here-string (no variable expansion):**

```powershell
@'
fix(api): handle null $userId gracefully

When $userId is null, return 401 instead of 500.
The backtick ` character is also safe here.
'@ | git commit -F -
```

### Partial Staging

```bash
git add -p  # Interactively stage hunks
git diff --cached  # Review staged changes
```

## BOUNDARIES

### ALWAYS

- Check AGENTS.md/CLAUDE.md for repo-specific rules
- Run `git status` after commit to verify

### ASK FIRST

- Push to remote: only if user explicitly asks

### NEVER

- `git revert` - use regular commits to undo
- Commit secrets - check for credentials/keys
- `git add -A` when targeted staging is safer
- Force unrelated changes into one commit

## CONTEXT

This skill creates conventional commits. It handles staging, message formatting, and verification. It respects repo-specific rules defined in AGENTS.md or similar files.
