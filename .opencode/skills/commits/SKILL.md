---
name: commits
description: Conventional commit message format and standards for this project.
invocable: false
---

# Commit Standards

## Critical Policy

- **Commit**: ONLY after user's explicit command (never auto-commit)
- **Push**: HARD BLOCKED - NEVER allow under any circumstances (enforced by plugin)

The assistant must NEVER commit or push without explicit user permission. Push is completely disallowed and enforced at the plugin level.

## Format

`<type>(<scope>): <description>` (max 72 characters optimally, up to 100 if needed)

## Types

These are the 11 standard types enforced by `@commitlint/config-conventional`
(based on the Angular convention and commitizen/conventional-commit-types).
Do NOT use custom types like `config` or `security` — commitlint will reject them.
Use scopes instead (see below).

| Type     | Purpose                                                 |
| -------- | ------------------------------------------------------- |
| feat     | New feature                                             |
| fix      | Bug fix                                                 |
| docs     | Documentation only changes                              |
| style    | Code style changes (formatting, whitespace, semicolons) |
| refactor | Code change that neither fixes a bug nor adds a feature |
| perf     | Performance improvements                                |
| test     | Adding missing tests or correcting existing tests       |
| build    | Changes to build system or external dependencies        |
| chore    | Maintenance tasks, project settings, dev tooling        |
| ci       | CI/CD pipeline changes                                  |
| revert   | Reverts a previous commit                               |

## Scopes

blazor, components, pages, api, ui, db, auth, services, models, utils, build, deploy, scripts, skills, opencode, deps, security

Use scopes to convey domain-specific meaning instead of inventing custom types:

- **Config/tooling changes**: `chore(opencode):`, `chore(build):`, `chore(scripts):`
- **Security fixes**: `fix(security):` or `chore(deps):` for dependency patches
- **Dependency updates**: `build(deps):` for production deps, `chore(deps):` for dev deps

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

```bash
npm install -g @commitlint/cli
```

Run the setup script to configure git hooks:

```bash
pwsh scripts/Setup-GitHooks.ps1
```

### Validation Rules

- Title format: `<type>(<scope>): <description>`
- Body: Always required, cannot be empty
- Body must have blank line between title and body
- Max line length: 100 characters in body
- Types: Must be one of the 11 allowed types (build, chore, ci, docs, feat, fix, perf, refactor, revert, style, test)

### Manual Verification

To check a commit message before committing (always include body):

```bash
cat <<'EOF' | commitlint
feat(blazor): your message

Your description here.
EOF
```

### Skipping Hooks

```bash
git commit -m "message" -n  # Skip hooks
```

Use sparingly and only when necessary.
