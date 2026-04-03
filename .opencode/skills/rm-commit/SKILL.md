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

## FLOW

1. Check repo rules first.
2. Inspect the working tree and recent history.
3. Decide whether changes belong in one commit or several.
4. Stage only the intended files or hunks.
5. Commit with Conventional Commits using multiple `-m` flags.
6. Verify the result with `git status`.

## COMMANDS

| Command                                             | Purpose                         | When                   |
| --------------------------------------------------- | ------------------------------- | ---------------------- |
| `git status`                                        | Show working tree status        | Always first           |
| `git diff HEAD`                                     | Show all changes                | After status           |
| `git branch --show-current`                         | Get current branch              | After diff             |
| `git log --oneline -10`                             | Show recent history             | After branch           |
| `git add -p`                                        | Stage partial hunks             | File has mixed changes |
| `git commit -m "subject" -m "body" [-m "para" ...]` | Commit with multiple `-m` flags | Always                 |

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
- Body: required for every commit; keep each line at or under 100 characters
- Body: explain why, trade-offs, and impact — not just what changed
- Footer: `Refs: #123`, `Co-authored-by:`, or `BREAKING CHANGE:`

Example:

```
fix(frontend): prevent duplicate form submits

Disable submit immediately so rapid clicks cannot queue
duplicate requests.
```

Good:

```bash
git commit -m "fix(frontend): prevent duplicate form submits" -m "Disable submit immediately so rapid clicks cannot queue duplicate requests."
```

Bad:

```bash
git commit -m "fix(frontend): prevent duplicate form submits" -m "Disable submit immediately so rapid clicks cannot queue duplicate requests because the submit button stays enabled for too long and users can trigger duplicate server calls."
```

### 4. Stage Carefully

- Stage by file or partial hunk
- Avoid `git add -A` or `git add .` when safer staging exists
- Exclude secrets, generated files, accidental edits

### 5. Commit

Use multiple `-m` flags — one per paragraph. Each `-m` value is a separate paragraph in the final message.

**Basic commit (subject + body):**

```bash
git commit -m "type(scope): subject" -m "Body explaining why the change exists."
```

**Multi-paragraph body with footer:**

```bash
git commit -m "refactor(core): extract validation logic" \
  -m "Move input validation into a dedicated service so controllers stay thin." \
  -m "This also makes it easier to reuse validation across API and web endpoints." \
  -m "Refs: #123"
```

**Quoting rules:**

- Use double quotes for `-m` values unless the message contains `$`, backticks, or `!`
- If the message contains shell metacharacters (`$`, `` ` ``, `!`, `\`), use single quotes instead
- If the message contains both single and double quotes, escape the inner ones with `\`

### 6. Verify

Run `git status` post-commit. Report hash and subject.

## PATTERNS

### Multiple -m Commit

**Basic (subject + body):**

```bash
git commit -m "type(scope): imperative subject" -m "Body explaining why the change exists."
```

**Multi-paragraph with footer:**

```bash
git commit -m "type(scope): subject" \
  -m "First paragraph." \
  -m "Second paragraph." \
  -m "Refs: #123"
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
