---
title: Ownership-Based Pre-Commit Linting with Directory Filtering
date: 2026-05-09
category: best-practices
module: opencode
problem_type: best_practice
component: development_workflow
severity: medium
applies_when:
  - Configuring pre-commit linting in repos with third-party code
  - Adding lint enforcement without flagging vendored dependencies
  - Choosing between "lint nothing" and "lint everything" in mixed-ownership repos
tags:
  [
    pre-commit,
    linting,
    ownership-filtering,
    oxlint,
    psscriptanalyzer,
    markdownlint,
  ]
---

# Ownership-Based Pre-Commit Linting with Directory Filtering

## Context

The repo contains both our custom code and vendored third-party skills (compound-engineering, matt-pocock, vendor/). Running linters across everything flags thousands of issues in code we don't own, creating noise and blocking unrelated commits. The choice between "lint nothing" (which allows bad code) and "lint everything" (which blocks commits for third-party code) is a false choice.

## Guidance

Implemented ownership-based lint filtering in `rm-commit`. Only lints files in directories we control:

- `.config/opencode/scripts/` — our PowerShell scripts
- `.config/opencode/plugins/` — our OpenCode plugins
- `.opencode/skills/rm-*/` — our redmuffin skills

Never lints: vendored skills (compound-engineering/, matt-pocock/, vendor/), auto-generated wrappers, or system scripts.

**How it works:** Filter by directory path BEFORE any linter runs. No per-tool exclusion config needed. Three linters run in sequence:

1. **PSScriptAnalyzer** for PowerShell (`.ps1`, `.psm1`, `.psd1`)
2. **oxlint** for JS/TS (`.js`, `.ts`, `.tsx`)
3. **markdownlint** for Markdown (`.md`)

**Zero tolerance:** Both errors and warnings block commits. The directory-based filter works identically across all linters and all future linters — a single policy, not a per-tool config maze.

## Why This Matters

Selective linting eliminates the false choice. The directory filter is the universal mechanism — add a new directory to the "our code" list and all linters automatically apply. Remove it and they stop. No `.eslintignore`, no `.oxlintrc.json` exclusions, no per-tool configuration drift.

## When to Apply

- On every pre-commit hook (enforced by `rm-commit`)
- When creating a new directory of our scripts — add it to the ownership list
- When adding a new vendored dependency — verify its path is excluded

## Examples

**Ownership filter (conceptual):**

```
Our code (linted):
  .config/opencode/scripts/**     → PSScriptAnalyzer
  .config/opencode/plugins/**     → oxlint
  .opencode/skills/rm-*/**        → markdownlint

Vendored (skipped):
  .opencode/skills/compound-engineering/**
  .opencode/skills/matt-pocock/**
  .opencode/skills/vendor/**
```

## Related

- `.opencode/skills/rm-commit/SKILL.md` — Pre-Commit Linting section
- `.opencode/PSScriptAnalyzerSettings.psd1` — PowerShell linting rules
