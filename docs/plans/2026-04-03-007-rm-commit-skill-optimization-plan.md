---
date: 2026-04-03
topic: rm-commit-skill-optimization
status: completed
requirements: docs/brainstorms/2026-04-03-rm-commits-skill-optimization-requirements.md
---

# rm-commit Skill Optimization Plan

## Overview

Optimize the `rm-commit` skill to fix heredoc syntax failures (PowerShell incompatibility), eliminate body line length violations, and reduce instruction drift. The solution uses `git commit -m "subject" -m "body"` syntax with improved AI instructions.

**Origin:** [docs/brainstorms/2026-04-03-rm-commits-skill-optimization-requirements.md](../brainstorms/2026-04-03-rm-commits-skill-optimization-requirements.md)

## Files to Modify

| File                                  | Purpose                                |
| ------------------------------------- | -------------------------------------- |
| `.opencode/skills/rm-commit/SKILL.md` | Main skill file requiring full rewrite |

## Implementation Units

### Unit 1: Commit Command Syntax (R1-R3)

**Objective:** Replace heredoc with multiple `-m` flags

| Task | Description                                                                       |
| ---- | --------------------------------------------------------------------------------- |
| U1.1 | Replace heredoc commit example with `git commit -m "subject" -m "body"` syntax    |
| U1.2 | Add explicit copy-pasteable command examples for each commit type                 |
| U1.3 | Document WHY multiple `-m` flags (bash/PowerShell compatibility, line validation) |

**Verification:**

- Commands work in PowerShell without syntax errors
- AI generates correct syntax on first attempt

### Unit 2: Message Generation Quality (R4-R6)

**Objective:** Eliminate body line length violations

| Task | Description                                                                               |
| ---- | ----------------------------------------------------------------------------------------- |
| U2.1 | Add concrete line-length rule: "Each body line ≤100 characters, count manually if needed" |
| U2.2 | Include good/bad examples showing 100-char violation visually                             |
| U2.3 | Remove "wrap at 72 chars" language (causes write-long-then-break behavior)                |
| U2.4 | Add negative constraint: "Never exceed 100 chars even if it means incomplete sentences"   |

**Verification:**

- Generated commit body lines ≤100 chars on first attempt ≥90% of the time

### Unit 3: Workflow Alignment (R7-R9)

**Objective:** Optimize for trunk-based repo, remove token waste

| Task | Description                                                                                   |
| ---- | --------------------------------------------------------------------------------------------- |
| U3.1 | Add explicit statement: "This is a trunk-based repo — no branch checks, no push attempts"     |
| U3.2 | Remove all branch protection instructions                                                     |
| U3.3 | Remove all detached HEAD warnings                                                             |
| U3.4 | Remove all push safety checks                                                                 |
| U3.5 | Keep commitlint validation rules (body presence, line length, format)                         |
| U3.6 | Change "body required for non-trivial changes" → "body always required (commitlint enforces)" |

**Verification:**

- Skill file reduced to ~80-100 lines (from 133)
- No token waste on irrelevant checks

### Unit 4: Structural Clarity (R10-R12)

**Objective:** Linear execution flow, improved trigger reliability

| Task | Description                                                    |
| ---- | -------------------------------------------------------------- |
| U4.1 | Restructure as linear step-by-step flow (harder to skip steps) |
| U4.2 | Optimize skill description for auto-trigger reliability        |
| U4.3 | Keep `rm-commit` name (preserve `rm-` namespace convention)    |

**Verification:**

- Skill follows linear flow: Gather Context → Stage → Format → Commit → Verify

### Unit 5: Content Preservation (R13-R14)

**Objective:** Maintain project-specific knowledge

| Task | Description                                                                                   |
| ---- | --------------------------------------------------------------------------------------------- |
| U5.1 | Preserve allowed types/scopes: feat, fix, docs, refactor, test, chore, perf, ci, style, build |
| U5.2 | Preserve lockfile paths: packages.lock.json, project.assets.json, etc.                        |
| U5.3 | Preserve BuildWebCompiler2022 exception                                                       |
| U5.4 | Align with commitlint.config.js: body always required                                         |

**Verification:**

- All project-specific knowledge retained in skill

## Test Scenarios

### Scenario T1: Basic Commit (Must Pass)

```
Given: 1 modified file with small change
When:  User runs rm-commit skill
Then:  git commit -m "type: subject" -m "body" executes without syntax error
And:   Body lines ≤100 characters
And:   commitlint passes without retry
```

### Scenario T2: Multi-line Body (Must Pass)

```
Given: 3 files changed requiring explanation
When:  User runs rm-commit skill with detailed explanation
Then:  Each body line individually ≤100 characters
And:   commitlint passes without retry
```

### Scenario T3: Line Length Enforcement (Must Pass)

```
Given: A change that would naturally produce >100 char lines
When:  AI generates commit message
Then:  AI splits into multiple lines ≤100 chars each
And:   commitlint passes without retry
```

### Scenario T4: Lockfile Exception (Must Pass)

```
Given: Modified BuildWebCompiler2022.cache file
When:  User runs rm-commit skill
Then:  Skill excludes this file from staging (lockfile exception preserved)
```

### Scenario T5: Trunk-based Workflow (Must Pass)

```
Given: Any commit scenario
When:  Skill executes
Then:  No branch protection checks run
And:   No detached HEAD warnings appear
And:   No push attempts made
```

### Scenario T6: Auto-trigger Reliability (Should Pass After Optimization)

```
Given: User types "commit my changes"
When:  rm-commit skill auto-triggers (not global commits skill)
Then:  Correct skill executes
```

## Success Metrics

| Metric                                      | Current              | Target |
| ------------------------------------------- | -------------------- | ------ |
| Syntax error rate (first attempt)           | ~20% (heredoc fails) | <5%    |
| Body line length compliance (first attempt) | ~20%                 | ≥90%   |
| commitlint retry count                      | ~5                   | ≤1     |
| Skill file lines                            | 133                  | 80-100 |
| Token waste on irrelevant checks            | Yes                  | No     |

## Deferred Items

- **R11 follow-up:** After skill is working, shorten description to ~100 chars with explicit trigger phrases
- **R105 follow-up:** Test sample commits with final instruction phrasing before deployment

## Out of Scope

- `git-commit-push-pr` global skill changes
- commitlint.config.js modifications
- Git hooks configuration
- Renaming to `commits` (deferred until content validated)
