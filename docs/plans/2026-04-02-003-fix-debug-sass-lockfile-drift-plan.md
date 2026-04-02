---
title: fix: prevent Debug-Sass lockfile drift
type: fix
status: active
date: 2026-04-02
deepened: 2026-04-02
---

# fix: prevent Debug-Sass lockfile drift

## Overview

The commit workflow should stop treating Debug-Sass-only `packages.lock.json` drift as a normal
commit candidate. When the only change is WebCompiler-generated lockfile noise, the workflow should
guide the operator back to the standard build state before commit selection continues.

## Problem Frame

This repo intentionally tracks NuGet lock files, but `Debug-Sass` introduces a recurring exception:
`BuildWebCompiler2022` can appear as the only lockfile delta even when no package change was
intended. The current commit guidance still leans toward always including lockfiles, which causes
repeated noisy commits and hides the real signal from genuine dependency changes.

The request is to harden both the `commit` command and the `commits` skill so they recognize this
pattern, avoid committing the drift-only file, and restore the tracked lockfile state through the
repo’s standard build/restore path when nothing else changed.

## Requirements Trace

- R1. Detect when a `packages.lock.json` diff is only `BuildWebCompiler2022` drift.
- R2. Prevent drift-only lockfiles from being staged or committed.
- R3. Restore the lockfile to the standard-build baseline when drift-only noise is recognized.
- R4. Preserve genuine lockfile changes when a real package update occurred.
- R5. Keep commit guidance aligned with repo policy, hook enforcement, and body-length rules.

## Scope Boundaries

- No changes to `commitlint.config.js`.
- No changes to `.githooks/commit-msg`.
- No global ignore rule for `packages.lock.json`.
- No new standalone cleanup script unless the existing command/skill flow cannot express the
  normalization step.
- No change to NuGet lock-file generation settings unless implementation proves it is required.
- No attempt to make `.opencode/commands/commit.md` perform staging itself; it can only route and
  describe workflow intent.
- No claim that the command or skill enforces staging at runtime; they only instruct the commit
  workflow.

## Context & Research

### Relevant Code and Patterns

- `.opencode/commands/commit.md` currently routes all commit attempts through the `commits` skill.
- `.opencode/skills/commits/SKILL.md` currently says lockfiles should be included whenever they
  changed.
- Actual tracked lockfiles in this repo:
  `src/redmuffin.Blazor.StaticWeb/packages.lock.json`,
  `src/redmuffin.Blazor.StaticWeb.Api/packages.lock.json`,
  `src/redmuffin.Blazor.StaticWeb.Common/packages.lock.json`,
  `src/SwaLauncher/packages.lock.json`,
  `tests/redmuffin.Blazor.StaticWeb.Tests/packages.lock.json`,
  `tests/redmuffin.Blazor.StaticWeb.Api.Tests/packages.lock.json`.
- `AGENTS.md` already contains the repo exception: ignore `BuildWebCompiler2022`-only drift in
  `packages.lock.json`.
- `src/redmuffin.Blazor.StaticWeb/compilerconfig.json` identifies the SCSS/WebCompiler build path.
- `commitlint.config.js` and `.githooks/commit-msg` define the message-validation boundary that must
  remain intact.

### Institutional Learnings

- `AGENTS.md` is the authoritative quick-reference for the exception: `BuildWebCompiler2022`-only
  drift should not be committed.
- The commit skill should stay terse, but it must not force a commit on generated noise.
- Lockfiles are normally committed when dependencies truly change, so normalization must stay narrow.

## Key Technical Decisions

- Treat the `commit` command as the orchestration layer and the `commits` skill as the policy layer;
  both must agree on the exception.
- Classify lockfile drift by diff content, not by filename alone, so real dependency updates are not
  suppressed.
- Keep the lockfile path inventory in the command and skill aligned with the repo’s full tracked set;
  do not rely on the narrower legacy subset.
- When drift-only noise is detected, use the repo’s standard build/restore flow for the active
  Debug-Sass path, then compare the current working tree against the staged lockfile diff before
  final commit selection; if the drift survives, treat it as a real dependency change.
- Keep tracked lockfiles as the default rule; the exception is narrow and specific to the
  WebCompiler-generated Debug-Sass path.

## Open Questions

### Resolved During Planning

- Should the fix change commitlint or hook behavior? No — enforcement stays where it is today.
- Should the repo stop tracking lock files altogether? No — only the Debug-Sass-only noise path is
  special-cased.
- Should this be a standalone helper script? Not initially; keep the change inside the existing
  command/skill workflow unless the implementation proves that is impossible.

### Deferred to Implementation

- The exact normalization sequence between classification and commit selection.
- Whether any wording in `docs/github-workflow-architecture.md` should be softened or
  cross-referenced after the workflow change lands.

## High-Level Technical Design

> _This illustrates the intended approach and is directional guidance for review, not implementation
> specification. The implementing agent should treat it as context, not code to reproduce._

| Input state                                                                  | Intended outcome                                                                                                                                                      |
| ---------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| No `packages.lock.json` changes                                              | Keep the current commit flow.                                                                                                                                         |
| Lockfile changed, but diff includes real dependency changes                  | Preserve the file and commit normally.                                                                                                                                |
| Lockfile changed, and the only package delta is `BuildWebCompiler2022` drift | Capture the pre-normalization diff, run the standard build/restore path, then re-check whether the same lockfile delta still exists before commit guidance continues. |
| Lockfile still changes after normalization                                   | Treat as a real package change and surface it explicitly.                                                                                                             |
| Normalization output is ambiguous                                            | Fail closed and preserve the lockfile change rather than hiding a possible dependency update.                                                                         |

## Implementation Units

- [ ] **Unit 1: Teach the commit command to normalize drift-only lockfiles**

**Goal:** Update `.opencode/commands/commit.md` so the command knows when to route lockfile drift
through a normalization step instead of treating it as ordinary stageable work.

**Requirements:** R1, R2, R3, R4

**Dependencies:** None

**Files:**

- Modify: `B:\redmuffin.Blazor.StaticWeb\.opencode\commands\commit.md`

**Approach:**

- Reframe the command as a classifier for open changes: ordinary changes, real lockfile updates, and
  drift-only lockfile noise.
- Make the lockfile scope explicit and correct the singular/plural mismatch so the command reflects
  the repo’s actual `packages.lock.json` files.
- Use the full tracked lockfile set, not the legacy partial list, when describing what can appear in
  scope.
- Describe the normalization branch as a preflight comparison: preserve the pre-normalization diff,
  run the standard build/restore path, then re-check whether the same lockfile delta still exists
  before commit guidance continues.
- Make fail-closed behavior explicit so ambiguous or mixed diffs remain staged as real dependency
  changes rather than being normalized away.

**Patterns to follow:**

- Existing concise command style in `.opencode/commands/commit.md`
- Repo lockfile policy in `AGENTS.md`

**Test scenarios:**

- Happy path: only non-lockfile changes are present, and the command keeps the current commit flow
  unchanged.
- Edge case: a `packages.lock.json` diff exists, but it includes real package changes as well as
  WebCompiler noise, and the command preserves the lockfile for normal commit handling.
- Error path: the drift-only lockfile remains dirty after normalization, and the command stops short
  of pretending the change was resolved.
- Integration: a Debug-Sass-generated lockfile diff is recognized before commit selection, so the
  file is not staged accidentally.

**Verification:**

- The command clearly distinguishes drift-only lockfile noise from real lockfile changes.
- The command no longer instructs an unconditional include of `packages.lock.json` when the diff is
  only generated noise.
- The command makes it clear that normalization is a comparison step, not a staging or rewrite step.

- [ ] **Unit 2: Update the commits skill to encode the drift exception**

**Goal:** Rewrite `.opencode/skills/commits/SKILL.md` so the skill explains the narrow lockfile
exception and keeps the commit payload aligned with repo policy.

**Requirements:** R1, R2, R4, R5

**Dependencies:** Unit 1

**Files:**

- Modify: `B:\redmuffin.Blazor.StaticWeb\.opencode\skills\commits\SKILL.md`

**Approach:**

- Keep the schema-first commit contract, but add an explicit decision branch for Debug-Sass-only
  `BuildWebCompiler2022` drift.
- Replace the partial lockfile inventory with the repo’s actual tracked paths before applying the
  exception logic.
- Make the “include lockfiles” guidance conditional on actual package changes.
- Preserve the message-format checks so the skill still protects body wrapping and footer spacing.
- Define the exception as structural and narrow: only suppress when the lockfile diff is exclusively
  the WebCompiler drift pattern; any mixed or ambiguous change must be treated as a real dependency
  update.

**Patterns to follow:**

- Existing schema-first format in `.opencode/skills/commits/SKILL.md`
- Commitlint constraints from `commitlint.config.js`
- Repo exception already documented in `AGENTS.md`

**Test scenarios:**

- Happy path: the skill returns a payload that keeps genuine lockfile updates in scope when packages
  actually changed.
- Happy path: the skill suppresses drift-only `BuildWebCompiler2022` noise instead of telling the
  agent to commit it.
- Edge case: a commit with both regular source changes and drift-only lockfile noise still produces a
  valid, single-purpose payload.
- Error path: the skill must not suggest bypassing hooks or weakening body/footer formatting just to
  get around the lockfile exception.
- Integration: the skill’s commit guidance matches the command’s classification rules so the two
  surfaces do not contradict each other.

**Verification:**

- The skill distinguishes real dependency changes from generated lockfile drift in its own policy
  language.
- The skill still preserves body-length and commit-format safeguards.
- The skill fails closed when the lockfile diff cannot be classified unambiguously.

## System-Wide Impact

- **Interaction graph:** commit command → commits skill → lockfile classification → pre-normalization
  diff capture → standard build/restore comparison → staging/commit validation.
- **Error propagation:** a false positive would hide a real dependency update; a false negative would
  keep generating noisy commits.
- **State lifecycle risks:** staging can become stale if normalization runs after the wrong subset of
  files is selected or if the post-build state is trusted without comparing it to the original diff.
- **Unchanged invariants:** commitlint remains the validator, hooks remain enforced, and real
  `packages.lock.json` updates still belong in commits.
- **Integration coverage:** the workflow must behave the same whether the user commits a full change
  set or a lockfile-only change set.

## Risks & Dependencies

| Risk                                                              | Mitigation                                                                                                           |
| ----------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------- |
| Drift detection is too broad and suppresses real package changes. | Classify by lockfile diff content and require the normalization pass to fail closed.                                 |
| Drift detection is too narrow and keeps allowing noisy commits.   | Make the exception explicit in both command and skill, and document the same rule in repo guidance.                  |
| Normalization happens at the wrong point in the flow.             | Keep the build/reset step before final staging decisions, not after commit payload generation.                       |
| Docs and workflow drift apart again later.                        | Track the exception in the same surfaces that govern commit behavior and revisit broader docs only if they conflict. |

## Documentation / Operational Notes

- This change is workflow-facing, so the important outcome is consistent agent behavior rather than
  runtime monitoring.
- The repo should still treat lockfiles as source-controlled artifacts when dependency changes are
  real.
- If implementation exposes any recurring false positives beyond `BuildWebCompiler2022`, capture them
  as follow-up learnings rather than widening the exception now.
- Broader docs reconciliation is intentionally deferred unless implementation reveals a direct
  contradiction that would confuse contributors.

## Sources & References

- **Related code:** `.opencode/commands/commit.md`
- **Related code:** `.opencode/skills/commits/SKILL.md`
- **Related code:** `AGENTS.md`
- **Related code:** `src/redmuffin.Blazor.StaticWeb/compilerconfig.json`
- **Related code:** `commitlint.config.js`
- **Related code:** `.githooks/commit-msg`
- **Related docs:** `docs/github-workflow-architecture.md`
