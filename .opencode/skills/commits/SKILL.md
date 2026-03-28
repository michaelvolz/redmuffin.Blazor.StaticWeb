---
name: commits
description: Conventional commit message format and standards for this project.
invocable: false
---

# Commit Standards

## Format

`<type>(<scope>): <description>` (max 112 chars)

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

## Body (REQUIRED)

**Body is MANDATORY** for all commits except:
- Dependency bumps (`chore(deps): bump X from 1.0 to 2.0`)
- Merge commits
- Simple typo fixes

### Body Format
Blank line after title, then bullet points explaining:
1. What was changed
2. Why it was changed
3. Any breaking changes or migration notes

### When Body is Required
Include a body if the commit:
- Adds new functionality or features
- Fixes a bug (describe what was broken and how it's fixed)
- Changes behavior in any way
- Refactors code (explain the refactoring purpose)
- Is not completely self-evident from the title

### Examples

**Good (with body):**
```
feat(blazor): add new navigation component

- Added NavMenu component with responsive behavior
- Improves mobile navigation and provides better UX
- No breaking changes
```

**Good (minimal - dependency bump):**
```
chore(deps): bump Meziantou.Analyzer from 2.0.161 to 2.0.163
```

**Bad (missing body for significant change):**
```
refactor: clean up service code
```

## Git Hooks (Automated Validation)

This project's commits are validated automatically via git hooks.

### Setup
```powershell
.\scripts\Setup-GitHooks.ps1
```

This configures git to use hooks in `.githooks/` directory, which validate:
- Title format (`<type>(<scope>): <description>`)
- Max 112 characters
- Body required (except deps/merge/revert)
- Bullet points required in body

### Manual Verification
To check a commit message before pushing:
```powershell
# Validate without committing
git commit --dry-run --message "feat(blazor): your message here"
```
