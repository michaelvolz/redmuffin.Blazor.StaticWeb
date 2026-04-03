---
name: rm-commit
description: "Shortcut: rm:commit. Use whenever the user says commit or wants help making a commit. Generates repo-specific conventional commit payloads."
---

# rm-commit

Create (a) clean, reviewable git commit(s) from the current working tree.

## Default intent

Use this skill to turn a messy change set into one or more logical commits that are easy to
review, easy to revert, and easy to describe.

## Workflow

### 1. Gather context

Run:

```bash
git status
git diff HEAD
git branch --show-current
git log --oneline -10
```

If the tree is clean, stop.

If the repo instructions in `AGENTS.md`, `CLAUDE.md`, or similar already define commit
conventions, follow those first.

- never use `git revert`
- never commit secrets
- never push to remote
- treat “undo commit” as “undo the last commit and keep the changes as unstaged edits”

### 2. Decide the commit shape

Look for distinct logical changes, not just distinct files.

- Split unrelated work into separate commits whenever possible.
- Use `git add -p` when one file contains more than one logical change.
- Group by dependency order when multiple commits are needed:
  1. scaffolding, renames, or refactors
  2. behavior changes
  3. tests
  4. docs, changelog, and cleanup
  5. formatting-only work, if it is separate
- Keep each commit independently understandable.
- If two changes fight for the same files and cannot be split cleanly, prefer the smaller,
  more coherent commit and leave the rest for a follow-up.

### 3. Choose the message convention

Prefer Conventional Commits unless the repo already uses another pattern.

Format:

```text
type(scope): imperative subject
```

Use one of these types when appropriate: `feat`, `fix`, `docs`, `refactor`, `test`, `chore`,
`perf`, `ci`, `style`, `build`.

Rules:

- Subject should be short, specific, and imperative.
- Aim for about 50 characters for the subject.
- Wrap body text around 72 characters when you include one.
- Explain why the change exists, not just what changed.
- Use a body for non-trivial work; omit it for obvious one-liners if the repo allows.
- Use `BREAKING CHANGE:` in the footer, or `!` in the header, for breaking changes.
- Keep footers for metadata like `Refs: #123` or `Co-authored-by:`.

Good example:

```text
fix(frontend): prevent duplicate form submits

Disable the submit action immediately so rapid clicks cannot queue
duplicate requests and create double writes.
```

### 4. Stage carefully

Stage by file name or partial hunk as needed.

- Prefer `git add -p` or targeted staging when one file contains mixed changes.
- Do not stage unrelated files together just because they were edited in the same session.
- Avoid pulling in secrets, generated files, or accidental edits.

### 5. Commit

Use a heredoc so the message stays formatted correctly:

```bash
git commit -m "$(cat <<'EOF'
type(scope): subject

Body explaining why the change exists.
EOF
)"
```

### 6. Verify

Run `git status` after the commit.

Report the commit hash and subject line.

## Repo-specific guardrails

- Do not push unless the user explicitly asks for it.
- Do not force unrelated changes into one commit.
- Do not use `git add -A` or `git add .` when safer staging is possible.
- If the repo has stricter local commit rules, follow those over this baseline.
