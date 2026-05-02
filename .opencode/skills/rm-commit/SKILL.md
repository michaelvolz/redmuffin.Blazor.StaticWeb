---
name: rm-commit
description: "Shortcut: rm:commit. Use when the user says commit or wants help making a commit. Generates repo-specific conventional commit payloads."
---

# rm-commit

Create clean, reviewable git commits from the working tree. Enforces
commitlint rules (body required, blank lines, 100-char line limit)
via Conventional Commits.

## Commitlint Rules (ENFORCED — non-negotiable)

All commits must pass these rules from the repo's commitlint config:

| Rule                   | Severity | Meaning                                                         |
| ---------------------- | -------- | --------------------------------------------------------------- |
| `body-leading-blank`   | error    | Blank line required between subject and body                    |
| `body-empty`           | error    | Body must NOT be empty (every commit has a body)                |
| `body-max-line-length` | error    | Every body line ≤ 100 characters                                |
| `footer-leading-blank` | error    | Blank line required before footer (`Refs:`, `BREAKING CHANGE:`) |

These extend `@commitlint/config-conventional` (which also
enforces `subject-empty`, `type-empty`, and `type-enum`).
Failing any rule blocks the commit.

## CRITICAL

### Destructive Git Commands — NEVER

These are blocked by cc-safety-net. Do NOT attempt any of them:

- `git push` (any push — only humans push to remote)
- `git push --force` / `-f` — destroys remote history
- `git push --force-with-lease` — even safe force push is blocked
- `git reset --hard` — destroys all uncommitted changes
- `git reset --merge` — can lose uncommitted changes
- `git checkout -- <files>` — discards uncommitted changes
- `git checkout <ref> -- <path>` — overwrites working tree
- `git restore <files>` — discards uncommitted changes
- `git restore --worktree` — explicitly discards working tree
- `git clean -f` — removes untracked files permanently
- `git branch -D` — force-deletes branch without merge check
- `git stash drop` — permanently deletes stashed changes
- `git stash clear` — deletes ALL stashed changes
- `git worktree remove --force` — force-deletes worktree
- `git reflog expire --expire=now --all` — permanently deletes reflog

Safe alternatives: `git stash` before destructive operations,
`git branch -d` (safe delete), `git clean -n` (dry-run),
`git restore --staged` (unstage only), `git reset HEAD~1`
(undo commit, keep work).

### Commit Safety Rules

- NEVER commit, push, or expose secrets (credentials, keys, tokens)
- NEVER use `git revert` — use a regular commit to undo
- NEVER use `git add -A` or `git add .` when targeted staging is safer
- NEVER force unrelated changes into one commit
- Treat "undo commit" as `git reset HEAD~1` — remove the last
  commit and keep all changes as unstaged edits. Never use
  `git reset --hard` to undo a commit (that destroys work).
- NEVER put `#` followed by an identifier (`[A-Za-z0-9_-]+`, e.g.,
  `#1234`, `#abc`, `#SN-0001`) in the commit body — the
  conventional-commits-parser treats it as an issue reference and
  moves everything after into the footer, making the body appear
  empty to commitlint. Write `Refs: #NNNNN` in the footer instead.
  If you must mention an issue in the body, omit the `#` prefix.
- Sidenotes are referenced as `SN-NNNN` (no `#` prefix). They are
  local IDs, not GitHub issues. Write `See SN-0003 for context`,
  never `See #SN-0003`.
- Use `git --no-optional-locks` for background status/diff to avoid
  index lock contention.

## FLOW

1. Check repo rules in AGENTS.md/CLAUDE.md first. Look for
   commit message conventions, scope lists, type restrictions,
   or branch-specific rules. If none exist, use the conventions
   in this skill as defaults.
2. Inspect the working tree and recent history.
3. Decide whether changes belong in one commit or several.
4. Stage only the intended files or hunks.
5. Format the message per WORKFLOWS §4 (commit types, scope,
   subject/body rules, breaking changes).
6. Commit with here-string pipe or heredoc (WORKFLOWS §5).
7. Verify: confirm clean `git status`, confirm HEAD matches
   intent, report the commit SHA to the user.

## WORKFLOWS

### 1. Gather Context

```
git status --porcelain=v2 --branch && git diff --numstat && git branch --show-current && git log --oneline -n 10
```

If tree is clean, there is nothing to commit — stop and report
to the user.

### 2. Decide Commit Shape

Group by concern — split when changes serve different purposes and
can be reviewed/reverted independently:

- Cleanup (deletions) separate from construction (additions)
- Features separate from infrastructure
- Order commits: scaffolding → behavior → tests → docs → formatting
- Keep each commit independently understandable

### 3. Stage Carefully

- Stage by file or partial hunk
- `git add -p` for mixed changes in one file — note: this is
  interactive and may not work in non-interactive environments;
  fall back to `git add <file>` and verify with `git diff --cached`
- Exclude secrets, generated files, accidental edits
- `git commit -a` stages all tracked changes automatically. Use
  only when the working tree has no untracked files that need
  separate handling. Prefer explicit `git add` for control.

### 4. Format Message

Use Conventional Commits (enforced by commitlint rules above):

```
type(scope): imperative subject

Body paragraph(s) explaining why. Target ≤ 80 chars per
line (commitlint enforces ≤ 100; 80 is a safe margin).

Refs: #123
```

#### Commit types

| Type       | Use when                                                |
| ---------- | ------------------------------------------------------- |
| `feat`     | New user-facing feature or capability                   |
| `fix`      | Bug fix (user-visible or internal)                      |
| `docs`     | Documentation changes only                              |
| `refactor` | Code change that neither fixes a bug nor adds a feature |
| `test`     | Adding or updating tests                                |
| `chore`    | Build, CI, dependency, tooling, config maintenance      |
| `perf`     | Performance improvement                                 |
| `ci`       | CI/CD pipeline changes                                  |
| `style`    | Formatting, whitespace, linting (no logic change)       |
| `build`    | Build system or external dependency changes             |

#### Scope

Scope is optional but encouraged. Use the directory, module, or
component name affected (e.g., `feat(skills):`, `fix(core):`,
`chore(config):`). Match the repo's convention if one exists.

#### Subject rules

- Concise, imperative, specific — not a file type or vague label
- Good: `fix(systemd): add UV_NO_CACHE to avoid lock contention`
- Bad: `update systemd service` (vague), `fix: file change` (no info)

#### Body rules (commitlint-enforced)

- **Blank line after subject** — `body-leading-blank` is enforced
- **Body is required** — `body-empty: never` means every commit
  must have a body explaining why, impact, and trade-offs
- **Every body line ≤ 100 characters** — enforced by
  `body-max-line-length`. Target ≤ 80 as a safety margin:
  models miscount characters, so 80 keeps you safely under 100.
- Describe the concrete behavior being changed in plain language
- Footer: `Refs: #123`, `Co-authored-by:`, or `BREAKING CHANGE:`
- **Blank line before footer** — `footer-leading-blank` is enforced

#### Breaking changes

When a commit introduces a breaking API or behavior change, add
a footer:

```
feat(api): change response format to JSON:API

All endpoints now return JSON:API-compliant responses instead
of the legacy flat format.

BREAKING CHANGE: response format changed from flat JSON to
  JSON:API. Clients must update response parsing.
```

### 5. Commit

Two methods — both produce identical results. The here-string
approach is canonical (matches AGENTS.md `pwsh -NoProfile` rule).

**PowerShell here-string (cross-platform, requires `pwsh`):**

```powershell
@"
type(scope): imperative subject

Body explaining why the change exists. Wrap each line
at ≤ 80 characters (commitlint enforces ≤ 100, but
80 gives a safe margin since models miscount).

Refs: #123
"@ | git commit -F -
```

Why: no temp files, no shell parsing, exact line breaks preserved.

**Bash heredoc (Linux/macOS native):**

```bash
git commit -F - <<'EOF'
type(scope): imperative subject

Body explaining why the change exists. Wrap each line
at ≤ 80 characters (commitlint enforces ≤ 100, but
80 gives a safe margin since models miscount).

Refs: #123
EOF
```

Why: native bash, no `pwsh` dependency, quotes around EOF prevent
variable expansion.

**Message with `$` or backticks (safe in both methods):**

PowerShell:

```powershell
@"
fix(api): handle null `$userId` gracefully

When `$userId` is null, return 401 instead of 500.
The backtick and dollar sign are safe in here-strings.
"@ | git commit -F -
```

Bash (quotes around EOF prevent expansion):

```bash
git commit -F - <<'EOF'
fix(api): handle null `$userId` gracefully

When `$userId` is null, return 401 instead of 500.
The backtick and dollar sign are safe in heredocs.
EOF
```

## COMMANDS

| Command                              | Purpose                                                                                          | When                     |
| ------------------------------------ | ------------------------------------------------------------------------------------------------ | ------------------------ |
| `git status --porcelain=v2 --branch` | Machine-readable status                                                                          | Gather context           |
| `git diff --numstat`                 | Line counts per file                                                                             | After status             |
| `git diff HEAD`                      | Show all changes                                                                                 | After status             |
| `git branch --show-current`          | Get current branch                                                                               | After diff               |
| `git log --oneline -n 10`            | Show recent history                                                                              | After branch             |
| `git add -p`                         | Stage partial hunks (interactive; fall back to `git add <file>` if the shell is non-interactive) | File has mixed changes   |
| `git diff --cached`                  | Review staged changes                                                                            | After staging            |
| `git reset --soft HEAD~1`            | Undo last commit                                                                                 | Commit rejected or wrong |
| `git --no-optional-locks status`     | Status without lock contention                                                                   | Background check         |

## Error Recovery

| Situation                 | Action                                                                                                                                                                                        |
| ------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| commitlint rejects commit | Read the error, fix the specific rule violation, stage corrections, recommit                                                                                                                  |
| wrong files or message    | `git reset --soft HEAD~1` (undo commit, keep changes staged), fix, recommit                                                                                                                   |
| pre-commit hook fails     | Fix the issue reported by the hook, stage fixes, recommit. Do NOT `--amend` unless all amend conditions are met                                                                               |
| amend (conditions met)    | `git add <files> && git commit --amend -F -` (or `--no-edit` to reuse message). Only if: (1) user explicitly requested amend, (2) HEAD was created by you this session, (3) commit NOT pushed |

## BOUNDARIES

### ASK FIRST

- Rebasing or rewriting commit history (e.g., `git rebase`, `git commit --amend` when conditions aren't met)

### NEVER

See CRITICAL above for the full NEVER list.
