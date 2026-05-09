---
name: rm-commit
description: "Shortcut: rm:commit. Use when the user says commit or wants help making a commit. Generates repo-specific conventional commit payloads."
---

# rm-commit

Create clean, reviewable git commits from the working tree. Enforces
commitlint rules (body required, blank lines, 100-char line limit)
via Conventional Commits.

## What Belongs in This File

- **Viewpoint**: Information as reference, not recipes. The model
  already knows how to use git.
- **What belongs**: constraints (commitlint rules), conventions (type
  checklist, scope patterns), gotchas (`#` parser behavior, body
  template), the heredoc/here-string syntax.
- **What does NOT belong**: ordered workflow steps, diagnostic command
  recipes, staging instructions, anti-redundancy rules, hard numeric
  thresholds, anything the model already knows how to do.

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

## Pre-Commit Linting (ENFORCED — non-negotiable)

Linting runs on staged files before every commit. The same gate as commitlint —
findings block the commit.

### What Gets Linted — NOT Everything

We only lint **our own code**. Third-party scripts (vendored skills, auto-generated
wrappers, pip entry points, npm bins, AUR helpers) must never be held to our
linting standards. We determine ownership by **directory**, not by file markers
or git blame.

**Our code directories** (cross-linter, cross-language):

| Directory                        | What lives there                                     |
| -------------------------------- | ---------------------------------------------------- |
| `.config/opencode/scripts/`      | Our PowerShell scripts, test suites                  |
| `.config/opencode/plugins/`      | Our OpenCode plugins (JS/TS)                         |
| `.config/opencode/skills/rm-*/`  | Our custom skills (`rm-commit`, `rm-opencode`, etc.) |
| `.local/bin/` that we hand-wrote | Add specific files here if we author them            |

**NOT our code** (never linted):

| Directory / Pattern                             | What lives there                            |
| ----------------------------------------------- | ------------------------------------------- |
| `.config/opencode/skills/compound-engineering/` | CE third-party skills                       |
| `.config/opencode/skills/matt-pocock/`          | Matt Pocock third-party skills              |
| `.config/opencode/skills/vendor/`               | Other vendored skills                       |
| `.local/bin/` (default)                         | Auto-generated pip/npm wrappers             |
| `.config/omarchy/`                              | Omarchy package management — system scripts |

**How filtering works:** Before any linter runs, filter staged files against
the "our code" directories. Only files whose path starts with one of our
directories are linted. This works identically for PowerShell, JavaScript,
shell, and all future linters — no per-tool exclusion config needed.

**When we create a new directory of our scripts, add it to the "our code" list
above.**

### PowerShell — PSScriptAnalyzer

**Config:** `.config/opencode/PSScriptAnalyzerSettings.psd1`
**Severity:** Error + Warning. Both block commits. Zero tolerance.

```bash
# Filter to our .ps1/.psm1 files, then lint
our_files=$(git diff --cached --name-only --diff-filter=ACM |
  grep -E '\.config/opencode/(scripts|plugins|skills/rm-)/.*\.ps[1m]$')
if [ -n "$our_files" ]; then
  pwsh -NoProfile -Command "Invoke-ScriptAnalyzer -Path $our_files -Settings .config/opencode/PSScriptAnalyzerSettings.psd1 -EnableExit"
fi
```

No exceptions. No `-ExcludeRule` bandaids. Fix the code.

### JavaScript/TypeScript — oxlint

```bash
our_files=$(git diff --cached --name-only --diff-filter=ACM |
  grep -E '\.config/opencode/(scripts|plugins|skills/rm-)/.*\.(js|ts|jsx|tsx)$')
if [ -n "$our_files" ]; then
  npx oxlint $our_files
fi
```

### Markdown (future) — markdownlint

```bash
our_files=$(git diff --cached --name-only --diff-filter=ACM |
  grep -E '\.config/opencode/(scripts|plugins|skills/rm-)/.*\.md$')
if [ -n "$our_files" ]; then
  npx markdownlint $our_files
fi
```

### Linter Inventory (per machine)

This table tells each machine which linters are expected and which to install.

| Linter           | Language      | Command                 | Installed here |
| ---------------- | ------------- | ----------------------- | -------------- |
| PSScriptAnalyzer | PowerShell    | `Invoke-ScriptAnalyzer` | Yes (1.25.0)   |
| oxlint           | JavaScript/TS | `npx oxlint`            | Yes (1.62.0)   |
| commitlint       | Git commits   | `commitlint`            | Yes            |
| markdownlint     | Markdown      | `markdownlint`          | No             |
| shellcheck       | Shell         | `shellcheck`            | No             |

If a linter is not installed, **warn the user hard** — the other machine enforces
it; this machine is out of compliance — but proceed. A missing optional dev tool
should not block unrelated work.

**Performance:** No overhead for commits without our script files. ~2-5 seconds
when our `.ps1`/`.js` files are staged.
| markdownlint | Markdown | `markdownlint` | No |
| shellcheck | Shell | `shellcheck` | No |

When the other machine loads this skill, the table tells it which
linters to install.

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

If the working tree is clean (no modified or untracked files),
there is nothing to commit — stop and report to the user.

Check AGENTS.md/CLAUDE.md for repo-specific conventions (scope lists,
type restrictions, branch-specific rules). If none exist, use the
conventions in this skill as defaults.

## Commit Shape

Group by concern — split when changes serve different purposes and
can be reviewed/reverted independently:

- Cleanup (deletions) separate from construction (additions)
- Features separate from infrastructure
- Keep each commit independently understandable

**Commit ordering** — scan the diff for import/export chains. Skip
elaboration when there is nothing to find:

1. Does file A define something (export, type, interface, base class,
   config schema) that file B imports or depends on?
   → File A commits first. Resolve all import chains before committing
   the files that depend on them.
2. No dependencies between groups?
   → Static fallback: cleanup → scaffold → behavior → tests → docs
   → format.

Example — `src/types.ts` exports `User` and `Session`; `src/auth/login.ts`
and `src/auth/refresh.ts` import from it:

```
# Commit 1: types (depended on by auth)
# Commit 2: auth login + refresh (depend on types)
# Commit 3: tests (depend on auth behavior)
```

## Commit Message Format

Use Conventional Commits (enforced by commitlint rules above):

```
type(scope): imperative subject

Body paragraph(s) explaining why. Target ≤ 80 chars per
line (commitlint enforces ≤ 100; 80 is a safe margin).

Refs: #123
```

#### Commit type — ordered decision checklist

Scan top-to-bottom. Stop at the first match. Do not evaluate
further items.

1. User-facing new capability or behavior? → `feat`
2. Fixing a bug, crash, regression, or incorrect behavior? → `fix`
3. Only `.md`, docstrings, comments, no code logic changed? → `docs`
4. Only test files? → `test`
5. Moving/renaming/restructuring, no behavior change? → `refactor`
6. CI/CD pipeline changes? → `ci`
7. `.yml`/`.json` config, deps, build scripts, tooling? → `chore`
8. Measurable perf improvement (benchmark/profile evidence)? → `perf`
9. Pure formatting, whitespace, lint fixes (no logic change)? → `style`
10. Build system or external dependency changes? → `build`

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

**Body template** — every message: one summary line, 1-2 why lines:

```
<One-line concrete summary of what changed>

<1-2 lines: why this change exists or what it fixes.
Keep each line ≤ 80 chars.>
```

No "this commit" meta-talk. No filler. Target 2-3 lines of body for
most commits; use more only when a trade-off or edge case must be
explained.

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

## Commit Syntax

Two methods — both produce identical results. The here-string
approach is canonical (matches AGENTS.md `pwsh -NoProfile` rule).

**PowerShell here-string (cross-platform, requires `pwsh`):**

```powershell
@"
fix(auth): handle null userId in token refresh

Null userId was causing 500 errors when the refresh
token grant returned an unrecognized user_id.
"@ | git commit -F -
```

Why: no temp files, no shell parsing, exact line breaks preserved.

**Bash heredoc (Linux/macOS native):**

```bash
git commit -F - <<'EOF'
fix(auth): handle null userId in token refresh

Null userId was causing 500 errors when the refresh
token grant returned an unrecognized user_id.
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
| `git diff --stat`                    | Change summary per file                                                                          | After status             |
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
