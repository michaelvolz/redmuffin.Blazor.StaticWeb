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

## Body

Blank line + 2-3 sentences explaining:
- What was changed
- Why it was changed
- Any breaking changes or migration notes

## Examples

```
feat(blazor): add new navigation component
fix(api): resolve null reference in user service
docs(readme): update installation instructions
refactor(components): extract shared button styles
```
