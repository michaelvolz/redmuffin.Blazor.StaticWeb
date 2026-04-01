---
name: commits
description: Conventional commit format and standards for this project.
invocable: false
---

# COMMIT PROTOCOL

## ⏻ BEFORE EDITING THIS FILE

□ Read **complete file** (line 1 to end)  
□ Identify **all sections** for your change  
□ Plan **all edits** in one pass  
□ Execute - if you need a second edit, re-read first

---

## ⏻ PRE-FLIGHT (Before every commit)

□ Body exists? (blank line + description)  
□ Lock files staged? (`packages.lock.json` when deps change)  
□ Concerns separated? (ONE logical change: docs|code|config|tooling)

## ✎ FORMAT

```
type(scope): subject (max 100 chars)

Body explaining what and why.
```

**CRITICAL:** Blank line between title and body is required.

## 📋 REFERENCE

**Types:** feat|fix|docs|style|refactor|perf|test|build|chore|ci|revert  
**Type meanings:** feat=feature, fix=bug fix, docs=documentation, style=formatting, refactor=code change no feature/fix, perf=performance, test=adding tests, build=build system, chore=maintenance/tooling, ci=CI/CD, revert=revert commit  
**Scopes:** blazor|api|ui|deps|build|scripts|ci|docs|opencode  
**Max length:** 100 chars per line  
**Breaking:** Add `!` after scope + `BREAKING CHANGE:` in body

## ⚠ GUARDRAILS

### NEVER

- No body (commitlint rejects)
- Missing lock files when deps change
- Mix concerns (docs+code, config+feature, tooling+bugfix)
- Push (plugin blocks this)
- Commit without explicit user command
- Use custom types like `config` or `security` (use `chore(opencode):` or `fix(security):`)

### ALWAYS

- One logical concern per commit
- Include `packages.lock.json` for CI/CD reproducible builds
- Explicit user approval before committing

## 📁 LOCK FILES (commit when modified)

- `src/**/packages.lock.json`
- `tests/**/packages.lock.json`
- `src/SwaLauncher/packages.lock.json`

## 💡 EXAMPLES

**Standard:**

```bash
git commit -m "feat(api): add endpoint

Added POST endpoint."
```

**Dependency bump:**

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

**Breaking change:**

```bash
git commit -m "feat(api)!: remove deprecated endpoint

BREAKING CHANGE: v1 endpoints removed."
```

**Commit separation:**

```bash
# ✓ docs(agents): add winget priority rule  (AGENTS.md - infrastructure docs)
# ✓ fix(swa): update apiVersion to 9.0       (swa-cli.config.json - config fix)
# ✗ chore: update configs                     (mixing unrelated changes)
```

**Revert test:** "If I revert this commit, would I lose work I want to keep?" If yes, separate them.

## 🛠 SCRIPTS

Setup git hooks: `pwsh scripts/Setup-GitHooks.ps1`  
Skip hooks (emergency): `git commit -m "msg" -n`
