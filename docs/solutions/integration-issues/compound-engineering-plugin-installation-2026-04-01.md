---
title: Compound Engineering Plugin Installation and npm Supply Chain Protection
problem_type: knowledge
component: devops
module: tooling
tags:
  - compound-engineering
  - npm
  - bun
  - supply-chain
  - min-release-age
  - opencode
  - skills
date: 2026-04-01
track: knowledge
applies_when:
  - Installing AI agent plugins via npm/bun
  - Configuring supply chain protection for package managers
  - Choosing between npm and bun as project standard
---

# Compound Engineering Plugin Installation and npm Supply Chain Protection

## Context

We wanted to install the Compound Engineering plugin from EveryInc (https://github.com/EveryInc/compound-engineering-plugin) to add planning, review, and compounding workflow skills to OpenCode. The installation process revealed several important lessons about package manager choice, supply chain protection, and plugin installation.

## Key Decisions and Learnings

### 1. npm vs Bun: Choose npm for .NET Projects

**Decision:** Standardize on npm, keep bun as an undocumented local convenience tool.

**Rationale:**

- .NET project with minimal JavaScript tooling needs
- npm already documented in 50+ places across the codebase
- All global tools (SWA CLI, prettier, commitlint, devcontainers CLI) installed via npm
- GitHub Actions has native npm support, bun requires extra setup
- Supply chain protection works identically via `.npmrc` for both tools
- Bun's speed advantage irrelevant for occasional CLI runs (3x/year usage pattern)

**When to reconsider:** If the project becomes JS/TS-heavy with daily installs.

### 2. Supply Chain Protection via min-release-age

**Problem:** npm/bun packages can be compromised shortly after publication (supply chain attacks).

**Solution:** Set `min-release-age=10080` (7 days in minutes) in `.npmrc`.

**Implementation (three layers):**

| Layer   | File                         | Scope                      |
| ------- | ---------------------------- | -------------------------- |
| Project | `.npmrc` (repo root)         | Anyone who clones the repo |
| Global  | `$HOME/.npmrc`               | Your machine (npm + bun)   |
| CI      | GitHub Actions workflow step | Pipeline runs              |

**Project `.npmrc`:**

```
# Supply chain protection: reject packages published less than 7 days ago
# 10080 minutes = 7 days (npm format)
min-release-age=10080
```

**GitHub Actions step:**

```yaml
- name: Configure npm supply chain protection
  run: |
    echo "min-release-age=10080" >> ~/.npmrc
    echo "Supply chain protection: minimum release age set to 7 days"
```

**Important:** Both npm and bun read `.npmrc`, so this protects both tools with one config.

### 3. Compound Engineering Plugin Installation

**Command:**

```bash
npm install -g @every-env/compound-plugin
compound-plugin install compound-engineering --to opencode
```

**What gets installed:**

- 41 skills → `~/.config/opencode/skills/`
- 48 agents → `~/.config/opencode/agents/`
- No executable code, just markdown files
- MCP config (Context7) - redundant if already configured

**Important notes:**

- The `@every-env/compound-plugin` CLI is downloaded temporarily during install
- Skills load with colon syntax: `ce:plan`, `ce:review`, `ce:work`
- Commands are bundled as skills, not separate command files
- Plugin was published recently, so supply chain protection may block initial install
- Can bypass for one-time install with `--min-release-age=0` if needed

### 4. What Didn't Work

1. **Using bunx initially:** The compound-engineering plugin is a Bun-native tool, but we decided to standardize on npm for consistency
2. **npx with supply chain protection:** Our own `min-release-age=7` blocked the newly-published package, which is the protection working as intended
3. **Global `.bunfig.toml`:** Has known bugs for detection, `.npmrc` is more reliable

## Why This Matters

1. **Supply chain attacks are real:** Popular packages get compromised within minutes of publication. A 7-day delay lets the community detect and remove malicious packages before they reach your codebase.

2. **Consistency reduces cognitive load:** Having one package manager standard (npm) means less documentation, fewer edge cases, and easier onboarding.

3. **Compound Engineering adds value:** The planning (`ce:plan`), review (`ce:review`), and compounding (`ce:compound`) workflows fill gaps in our existing tooling around structured planning and knowledge capture.

## When to Apply

- Setting up new projects with AI agent tooling
- Configuring supply chain protection for any npm/bun project
- Installing compound-engineering or similar AI development plugins
- Choosing between npm and bun for a project

## Examples

### Installing compound-engineering plugin:

```bash
# Install the CLI tool globally
npm install -g @every-env/compound-plugin

# Install compound-engineering skills to OpenCode
compound-plugin install compound-engineering --to opencode
```

### Bypassing supply chain protection for trusted package:

```bash
# Only if you trust the package and need it immediately
npm install -g @every-env/compound-plugin --min-release-age=0
```

### Verifying installed skills:

```bash
ls ~/.config/opencode/skills/
ls ~/.config/opencode/agents/
```

## Prevention

1. **Always set min-release-age** in project `.npmrc` for any project using npm/bun
2. **Document package manager choice** in AGENTS.md or README
3. **Test plugin installations** before documenting them
4. **Check for duplicate MCP configs** before installing plugins that add MCP servers
5. **Use explicit version pins** when installing global tools in CI/CD
