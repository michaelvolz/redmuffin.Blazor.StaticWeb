---
name: commits
description: Conventional commit format and standards for this project.
invocable: false
---

# Commits - READ THIS FIRST

## Template (Copy-Paste This)

```bash
git commit -m "type(scope): description

Body explaining what changed."
```

**CRITICAL: The blank line between title and body is required.**

## Before Every Commit

Check these two things:

1. **Body exists?** Must have blank line + description
2. **Lock files staged?** Run `git status`, include all `packages.lock.json` if modified

## Format Reference

**Types:** feat, fix, docs, style, refactor, perf, test, build, chore, ci, revert
**Scopes:** blazor, api, ui, deps, build, scripts, ci, docs, opencode
**Max length:** 100 chars per line

## Right vs Wrong

✅ **CORRECT:**

```bash
git commit -m "feat(api): add endpoint

Added POST endpoint."
```

❌ **WRONG (no body):**

```bash
git commit -m "feat(api): add endpoint"
```

❌ **WRONG (missing lock files):**
Only committing `Directory.Packages.props` without `packages.lock.json` files

## Common Patterns

**Dependencies:**

```bash
git commit -m "chore(deps): bump PackageName

PackageName: 1.0.0 → 2.0.0"
```

**Multiple packages:**

```bash
git commit -m "chore(deps): bump packages

- PackageA: 1.0.0 → 2.0.0
- PackageB: 2.0.0 → 2.1.0"
```

## Rules

- **ALWAYS** include body (commitlint rejects without it)
- **ALWAYS** include `packages.lock.json` when dependencies change (required for CI/CD reproducible builds)
- **NEVER** mix unrelated changes in one commit (one logical concern per commit)
- **NEVER** push (plugin blocks this)
- **NEVER** commit without explicit user command
- **NEVER** use custom types like `config` or `security` (use `chore(opencode):` or `fix(security):`)

**Breaking changes:** Add `!` after scope

```
feat(api)!: remove deprecated endpoint

BREAKING CHANGE: v1 endpoints removed.
```

## Lock Files Location

Always commit when modified:

- `src/**/packages.lock.json`
- `tests/**/packages.lock.json`
- `src/SwaLauncher/packages.lock.json`

## Type Meanings

- **feat:** New feature
- **fix:** Bug fix
- **docs:** Documentation only
- **style:** Formatting, whitespace
- **refactor:** Code change, no feature/fix
- **perf:** Performance improvement
- **test:** Adding/correcting tests
- **build:** Build system or deps
- **chore:** Maintenance, tooling
- **ci:** CI/CD changes
- **revert:** Revert previous commit

## Scripts

Setup git hooks: `pwsh scripts/Setup-GitHooks.ps1`

Skip hooks (emergency): `git commit -m "msg" -n`
