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

## Body (Required for Non-Trivial Changes)

A body/description is required when there's more than one change or the change isn't completely obvious from the title alone.

### Body Format
- Blank line between title and body (required)
- Use paragraphs/sentences, not bullet points
- Max 100 characters per line

### When Body is Required
Include a body when:
- There are multiple changes in one commit
- The change isn't completely obvious from the title
- Additional context helps reviewers understand the change
- It's not a simple, obvious one-liner fix

### Examples

**Title only (simple, obvious single changes):**
```
chore(deps): bump Meziantou.Analyzer from 2.0.161 to 2.0.163
fix(blazor): null reference in user service
docs(readme): update installation steps
```

**With body (multiple or non-obvious changes):**
```
feat(blazor): add new navigation component

Added NavMenu component with responsive behavior.
This improves mobile navigation and provides better UX.
No breaking changes.
```

**Breaking changes (body required):**
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
- Body (if present): Must have blank line between title and body
- Max line length: 100 characters in body
- Types: Must be from the allowed list

### Manual Verification
To check a commit message before committing:
```powershell
echo "feat(blazor): your message" | commitlint
```

Or with body:
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
