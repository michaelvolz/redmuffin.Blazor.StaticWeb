---
name: commits
description: Conventional commit guidance for this repo. Trigger on any mention of committing, commit messages, conventional commits, or preparing to commit.
invocable: false
---

# commits

## TRIGGER

Use for:

- Committing changes
- Creating a commit message
- Writing a commit
- Making a commit
- Preparing to commit
- Conventional commits

Trigger even if user does not say `git`.

## CHECK_ORDER

1. `.gitignore` staged? → NO: stage first
2. Dependencies committed? → NO: commit those first
3. One concern? → NO: separate commits
4. Body present? → NO: add body
5. Lock files changed? → YES: include them

## FORMAT

`type(scope): subject` (max 100)

Body explaining why.

## COMMITLINT RULES

- body is required for non-trivial changes here
- body must start with a blank line after the header
- body lines must stay at or under 100 characters
- footer must start with a blank line after the body
- BREAKING CHANGE must be uppercase and belong in the footer or be marked with `!` in the header

## ALLOWED VALUES

TYPES: `feat` `fix` `docs` `style` `refactor` `perf` `test` `build` `chore` `ci` `revert`

SCOPES: `blazor` `api` `ui` `deps` `build` `scripts` `ci` `docs` `opencode`

## HARD RULES

NEVER: push, commit without approval, no body, mixed concerns, circumvent git hooks (`--no-verify`, `--no-gpg-sign`, etc.)

ALWAYS: one concern, explicit approval, lock files when changed

BREAKING: `!` after scope + `BREAKING CHANGE:` in body

## VALID MESSAGE SHAPE

```text
type(scope): subject

Short body paragraph wrapped so no line exceeds 100 characters.

Footer-One: value
```

## LOCK_PATHS

- `src/**/packages.lock.json`
- `tests/**/packages.lock.json`
- `src/SwaLauncher/packages.lock.json`
