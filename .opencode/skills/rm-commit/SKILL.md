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
2. Inspect the working tree and recent history.
3. Decide whether changes belong in one commit or several.
4. Stage only the intended files or hunks.
5. Commit with Conventional Commits using here-string piped to `git commit -F -`.

## COMMANDS

| Command                     | Purpose                      | When                   |
| --------------------------- | ---------------------------- | ---------------------- |
| `git status`                | Show working tree status     | Always first           |
| `git diff HEAD`             | Show all changes             | After status           |
| `git branch --show-current` | Get current branch           | After diff             |
| `git log --oneline -10`     | Show recent history          | After branch           |
| `git add -p`                | Stage partial hunks          | File has mixed changes |
| See WORKFLOWS → Commit      | Commit with here-string pipe | After staging          |

## WORKFLOWS

### 1. Gather Context

```
git status && git diff HEAD && git branch --show-current && git log --oneline -10
```

Stop if tree is clean.

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
- Subject must describe the actual user-visible or repo-behavior change,
  not just the file type or a vague editorial action.
- Avoid generic subjects like `clarify`, `update`, or `tweak` unless the
  change is purely editorial and the intent would otherwise be unclear.
- Body: required for every commit; keep each line at or under 100 characters
- Body: explain why, impact, and any trade-offs — not just what changed or
  which file was edited
- Body should mention the concrete behavior being changed in plain language.
- Footer: `Refs: #123`, `Co-authored-by:`, or `BREAKING CHANGE:`
- **CRITICAL: Every line in the body must be ≤ 100 characters** — commitlint enforces `body-max-line-length: [2, 'always', 100]`

### 4. Stage Carefully

- Stage by file or partial hunk
- Avoid `git add -A` or `git add .` when safer staging exists
- Exclude secrets, generated files, accidental edits

### 5. Commit with Here-String Pipe

Use this single bash command for the commit:

```powershell
@"
type(scope): imperative subject

Body paragraph one. Each line ≤ 80 chars.
Wrap manually at ~80 to stay safe.

Refs: #123
"@ | git commit -F -
```

**Why this works:**

- **No temp files** — Message piped directly to git via stdin
- **No shell parsing** — Here-string is literal, no variable expansion
- **No parser errors** — Single isolated command, no squashing issues
- **Line length preserved** — Exact line breaks piped to git
- **No cleanup needed** — Nothing written to disk

**CRITICAL: Wrap every body line at ≤ 80 characters.** The here-string preserves your exact line breaks, so what you type is what commitlint sees. Count characters if unsure — do NOT guess.

**Template:**

```powershell
@"
type(scope): imperative subject

First body paragraph. Each line must be ≤ 80 characters.
Wrap manually at ~80 chars to stay safe.

Second body paragraph if needed. Same line length rule.

Refs: #123
"@ | git commit -F -
```

**Basic commit (subject + body):**

```powershell
@"
fix(frontend): prevent duplicate form submits

Disable submit button immediately so rapid clicks
cannot queue duplicate requests.
"@ | git commit -F -
```

**Multi-paragraph body with footer:**

```powershell
@"
refactor(core): extract validation logic

Move input validation into a dedicated service so
controllers stay thin.

This also makes it easier to reuse validation across
API and web endpoints.

Refs: #123
"@ | git commit -F -
```

**Message with `$` or backticks (always safe — no shell parsing):**

```powershell
@"
fix(api): handle null $userId gracefully

When $userId is null, return 401 instead of 500.
The backtick ` character is also safe here.

Refs: #456
"@ | git commit -F -
```

**Line length enforcement:**

- Wrap every body line at **≤ 80 characters** (safe margin under the 100-char limit)
- Count characters if unsure — do NOT guess
- The here-string preserves your exact line breaks, so what you type is what commitlint sees
- **Good:** Each line is a short, readable sentence fragment
- **Bad:** One long run-on line that exceeds 100 characters

## PATTERNS

### Here-String Pipe to `git commit -F -`

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

**With `$` or backticks (safe — no shell parsing):**

```powershell
@"
fix(api): handle null $userId gracefully

When $userId is null, return 401 instead of 500.
The backtick ` character is also safe here.
"@ | git commit -F -
```

### Partial Staging

```bash
git add -p  # Interactively stage hunks
git diff --cached  # Review staged changes
```

## BOUNDARIES

### ALWAYS

- Check AGENTS.md/CLAUDE.md for repo-specific rules

### ASK FIRST

- Push to remote: only if user explicitly asks

### NEVER

- `git revert` - use regular commits to undo
- Commit secrets - check for credentials/keys
- `git add -A` when targeted staging is safer
- Force unrelated changes into one commit

## CONTEXT

This skill creates conventional commits. It handles staging and message formatting. It respects repo-specific rules defined in AGENTS.md or similar files.
