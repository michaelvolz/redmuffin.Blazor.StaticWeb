---
name: rm-commit
description: "Shortcut: rm:commit. Use whenever the user says commit or wants help making a commit. Generates repo-specific conventional commit payloads."
---

# rm-commit

Create clean, reviewable git commits from the working tree.

## CRITICAL

- NEVER use `git revert`
- NEVER commit secrets
- NEVER push to remote
- ALWAYS check repo-specific rules in AGENTS.md/CLAUDE.md first
- Treat "undo commit" as "undo the last commit and keep changes as unstaged edits"

## COMMANDS

| Command                             | Purpose                  | When                   |
| ----------------------------------- | ------------------------ | ---------------------- |
| `git status`                        | Show working tree status | Always first           |
| `git diff HEAD`                     | Show all changes         | After status           |
| `git branch --show-current`         | Get current branch       | After diff             |
| `git log --oneline -10`             | Show recent history      | After branch           |
| `git add -p`                        | Stage partial hunks      | File has mixed changes |
| `git commit -m "$(cat <<'EOF'...)"` | Commit with heredoc      | Always                 |

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

- Subject: ~50 chars, imperative
- Body: wrap at 72 chars, explain why not what
- Footer: `Refs: #123`, `Co-authored-by:`, or `BREAKING CHANGE:`

Example:

```
fix(frontend): prevent duplicate form submits

Disable submit immediately so rapid clicks cannot queue
duplicate requests.
```

### 4. Stage Carefully

- Stage by file or partial hunk
- Avoid `git add -A` or `git add .` when safer staging exists
- Exclude secrets, generated files, accidental edits

### 5. Commit

```bash
git commit -m "$(cat <<'EOF'
type(scope): subject

Body explaining why.
EOF
)"
```

### 6. Verify

Run `git status` post-commit. Report hash and subject.

## PATTERNS

### Heredoc Commit

```bash
git commit -m "$(cat <<'EOF'
type(scope): imperative subject

Body explaining why the change exists.
EOF
)"
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
