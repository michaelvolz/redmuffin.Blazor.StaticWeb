---
name: commits
description: Conventional commit message format and standards for this project.
invocable: false
---

# Commit Standards

## Format

`<type>(<scope>): <description>` (max 72 characters optimally, up to 100 if needed)

## Types

| Type | Purpose |
|------|---------|
| feat | New feature |
| fix | Bug fix |
| docs | Documentation changes |
| style | Code style changes (formatting, missing semicolons) |
| refactor | Code refactoring without changing functionality |
| perf | Performance improvements |
| test | Adding or updating tests |
| chore | Maintenance tasks, dependency updates |
| security | Security-related changes |
| ci | CI/CD pipeline changes |
| config | Configuration changes |
| revert | Reverting previous commits |

## Scopes

blazor, components, pages, api, ui, db, auth, services, models, utils, build, deploy, scripts, skills

## Breaking Changes

Add `!` after scope:
```
feat(api)!: remove deprecated endpoint
```

## Body (Always Required)

A body is **always required** for all commits. This is enforced by commitlint rules.

### Body Format
- Blank line between title and body (required)
- Use paragraphs/sentences, not bullet points
- Max 100 characters per line

### Body Content
- Provide brief context about what changed and why
- Even simple commits need a body (e.g., "Bump version" or "Fix typo")

### Examples

**All commits require a body:**
```
chore(deps): bump Meziantou.Analyzer to 2.0.163

Updated analyzer package to latest version.
fix(blazor): null reference in user service

Added null check before accessing user profile.
docs(readme): update installation steps

Clarified prerequisites in the installation section.
```

**With extended body (multiple or non-obvious changes):**
```
feat(blazor): add new navigation component

Added NavMenu component with responsive behavior.
This improves mobile navigation and provides better UX.
No breaking changes.
```

**Breaking changes:**
```
feat(api)!: remove deprecated endpoint

The v1 endpoints have been removed. Users should
migrate to the v2 endpoints documented in the migration guide.
BREAKING CHANGE: The /api/v1/* endpoints are no longer available.
```

## Git Hooks (Automated Validation)

This project uses **commitlint** for automated commit message validation.

### Setup
Install commitlint globally:
```powershell
npm install -g @commitlint/cli
```

Run the setup script to configure git hooks:
```powershell
.\scripts\Setup-GitHooks.ps1
```

### Validation Rules
- Title format: `<type>(<scope>): <description>`
- Body: Always required, cannot be empty
- Body must have blank line between title and body
- Max line length: 100 characters in body
- Types: Must be from the allowed list

### Manual Verification
To check a commit message before committing (always include body):
```
$body = @"
feat(blazor): your message

Your description here.
"@
$body | commitlint
```

### Skipping Hooks
```powershell
git commit -m "message" -n  # Skip hooks
```
Use sparingly and only when necessary.
