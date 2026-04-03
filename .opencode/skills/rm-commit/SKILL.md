---
name: rm-commit
description: "Shortcut: rm:commit. Generate conventional commit payloads. Use when the user says 'commit', 'commit this', 'commit these changes', 'save my changes', 'save changes', 'create a commit', 'make a commit', 'git commit', 'check in', 'checkin', or wants to commit staged or unstaged work. Also trigger on any commit-related request, preparing to commit, writing commit messages, or commit-related questions. Produces well-structured conventional commit messages that follow this repo's conventions."
---

# rm-commits

## WORKFLOW_CONTEXT

Trunk-based repo. Commits go directly to the default branch. Pushing is always manual — **never run `git push`**. The `/rm-commit` command is the primary invocation method.

## EXECUTION FLOW

Follow these steps in order. Do not skip.

1. **Gather** — Run `git status` and `git diff HEAD`. If clean tree, stop.
2. **Group** — Scan changed files for distinct concerns. If clearly separate (e.g., unrelated fix + new feature), create separate commits. Group at file level only — no `git add -p`.
3. **Generate** — Build the commit payload following MESSAGE FORMAT below.
4. **Validate** — Check every body line ≤100 chars. If any line exceeds, rewrite before committing.
5. **Stage** — Stage specific files by name. Never `git add -A` or `git add .`.
6. **Commit** — Use heredoc to preserve formatting. Never use `--no-verify` or `--no-gpg-sign`.
7. **Verify** — Run `git status` after commit. Report hash and subject.

## MESSAGE FORMAT

```
type(scope): subject

Body paragraph explaining why this change exists.
Each line is a complete thought under 100 characters.
Break at word boundaries, never mid-word.

Co-authored-by: name <email>
```

- **Header**: `type(scope): subject` — imperative mood, max 100 chars total
- **Body**: Always required. Blank line after header. Explain _why_, not _what_.
- **Footer**: Optional. Blank line after body. `BREAKING CHANGE:` must be uppercase.
- **Breaking**: Use `!` after scope in header AND `BREAKING CHANGE:` in footer.

## LINE_LENGTH_RULES (PRIORITY 1)

Every body line must be ≤100 characters. This includes spaces. Count before committing.

**GOOD** (each line is a short, complete thought):

```
fix(blazor): prevent double-submit on form post

The submit button was not disabled after the first click.
Users could trigger duplicate requests by rapid clicking.
This caused duplicate database entries and validation errors.
The button is now disabled immediately on first click.
```

**BAD** (run-on lines that exceed 100 characters):

```
fix(blazor): prevent double-submit on form post

The submit button was not disabled after the first click which allowed users to trigger duplicate requests by rapid clicking and this caused duplicate database entries and validation errors throughout the system.
```

**Rules:**

- Compose each line as a complete thought from the start — do not write long then break
- Break at word boundaries, never mid-word
- If a sentence would exceed 100 chars, split it into two sentences
- Never rely on commitlint to catch violations — validate yourself first
- If commit fails twice due to line length, stop and ask

## PRECOMMIT_GATES

1. `.gitignore` staged? → NO: unstage first
2. Dependencies changed? → NO: commit those separately first
3. Multiple concerns? → YES: split into separate commits
4. Body missing? → YES: add body (always required per commitlint)
5. Lock files changed? → See LOCKFILES section below

## ALLOWED_VALUES

**TYPES**: `feat` `fix` `docs` `style` `refactor` `perf` `test` `build` `chore` `ci` `revert`

**SCOPES**: `blazor` `api` `ui` `deps` `build` `scripts` `ci` `docs` `opencode`

## LOCKFILES

**Paths**: `**/packages.lock.json` in `src/`, `tests/`, and `src/SwaLauncher/`.

**Exception**: If the only lockfile drift is `BuildWebCompiler2022` in `packages.lock.json`, treat it as a Debug-Sass artifact. Normalize first (`dotnet build -c Debug-Sass`), then commit only if a real dependency change remains.

**Rule**: All other lockfile changes must be committed with the dependency change that caused them.
