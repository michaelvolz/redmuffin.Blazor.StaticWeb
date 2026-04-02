---
name: commits
description: Schema-first conventional commit guidance for this repo. Trigger on any mention of committing, commit messages, conventional commits, or preparing to commit.
invocable: false
---

# commits

## PURPOSE

Generate a valid commit payload before any `git commit` attempt.

## TRIGGERS

Use for:

- Committing changes
- Creating a commit message
- Writing a commit
- Making a commit
- Preparing to commit
- Conventional commits

Trigger even if user does not say `git`.

## MESSAGE_SCHEMA

Required fields:

- `type`
- `scope` (optional)
- `subject`
- `body`
- `footers` (optional)
- `breaking` (optional)

Rule: do not attempt commit until every required field is populated and validated.

## PRECOMMIT_GATES

1. `.gitignore` staged? → NO: stage first
2. Dependencies committed? → NO: commit those first
3. One concern? → NO: separate commits
4. Body present? → NO: add body
5. Lock files changed? → if the only lockfile delta is Debug-Sass `BuildWebCompiler2022` drift,
   normalize first; otherwise include them

## PRECOMMIT_MESSAGE_CHECK

Before any `git commit` attempt, verify the final message yourself:

- Wrap every body line to 100 characters or fewer.
- Never rely on commitlint to catch wrapping mistakes for you.
- If any line is too long, rewrite the message before committing.
- Do not attempt the commit until the message is already valid.

## OUTPUT_CONTRACT

Return a commit payload, not advice:

```text
type: <allowed type>
scope: <allowed scope or empty>
subject: <short summary>
body: <why this change exists, wrapped to <= 100 chars per line>
footers: <optional trailers>
breaking: <true|false>
```

If the payload cannot satisfy the rules, stop and fix the payload first.

## FORMAT

`type(scope): subject` (max 100)

Body explaining why.

Blank line required between header/body and body/footer.

## COMMITLINT_RULES

- body is required for non-trivial changes here
- body must start with a blank line after the header
- body lines must stay at or under 100 characters
- treat this as a preflight requirement, not a hook failure to discover
- footer must start with a blank line after the body
- BREAKING CHANGE must be uppercase and belong in the footer or be marked with `!` in the header

## ALLOWED_VALUES

TYPES: `feat` `fix` `docs` `style` `refactor` `perf` `test` `build` `chore` `ci` `revert`

SCOPES: `blazor` `api` `ui` `deps` `build` `scripts` `ci` `docs` `opencode`

## HARD_RULES

NEVER: push, commit without approval, no body, mixed concerns, circumvent git hooks (`--no-verify`, `--no-gpg-sign`, etc.)

ALWAYS: one concern, explicit approval, lock files when changed; group files only when they changed for the same reason, otherwise split commits.

BREAKING: `!` after scope + `BREAKING CHANGE:` in body

## VALID_MESSAGE_SHAPE

```text
type(scope): subject

Short body paragraph wrapped so no line exceeds 100 characters.

Footer-One: value
```

## LOCK_PATHS

- `src/**/packages.lock.json`
- `tests/**/packages.lock.json`
- `src/SwaLauncher/packages.lock.json`
- `src/redmuffin.Blazor.StaticWeb/packages.lock.json`
- `src/redmuffin.Blazor.StaticWeb.Api/packages.lock.json`
- `src/redmuffin.Blazor.StaticWeb.Common/packages.lock.json`
- `tests/redmuffin.Blazor.StaticWeb.Tests/packages.lock.json`
- `tests/redmuffin.Blazor.StaticWeb.Api.Tests/packages.lock.json`

## LOCKFILE_EXCEPTION

- If the only lockfile drift is `BuildWebCompiler2022` in `packages.lock.json`, treat it as a
  Debug-Sass artifact and do not commit it unless the post-normalization diff still shows a real
  dependency change.
- Preserve the default rule for all other lockfile changes.
