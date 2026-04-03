---
title: Prevent Debug-Sass Lockfile Drift From Entering Commits
problem_type: bug
category: build-errors
component: commit-workflow
module: opencode-commands
tags:
  - debug-sass
  - buildwebcompiler2022
  - packages-lock-json
  - commit-guidance
  - opencode
  - lockfiles
date: 2026-04-02
track: bug
---

# Prevent Debug-Sass Lockfile Drift From Entering Commits

## Problem

`Debug-Sass` on Windows can introduce `BuildWebCompiler2022`-only drift in `packages.lock.json`
even when no dependency change was intended. The commit workflow was treating that generated noise
like a normal lockfile update, which made the workflow noisy and obscured real package changes.

## Symptoms

- `packages.lock.json` showed up as the only changed file after a Debug-Sass build.
- The commit workflow kept pushing the lockfile toward “always include it” behavior.
- The lockfile exception lived in `AGENTS.md`, but the command and commit guidance still conflicted
  with it.
- The workflow kept surfacing commit candidates that were only environmental drift.

## What Didn't Work

- Keeping the exception only in `AGENTS.md`.
- Leaving `.opencode/commands/commit.md` too generic about lockfiles.
- Letting `.opencode/skills/commits/SKILL.md` treat every lockfile diff as a real dependency change.
- Keeping a broad `packages.lock.json` rule in the workflow docs without the Debug-Sass exception.

## Solution

Make the commit workflow treat isolated `BuildWebCompiler2022` drift as generated noise, not a real
dependency change.

```text
Run the commits skill first.

If the only lockfile change is Debug-Sass-generated BuildWebCompiler2022 drift,
follow the skill’s normalize-first rule before commit selection continues.
```

The shared policy now lives in the commit skill:

```text
Lock files changed? → if the only lockfile delta is Debug-Sass `BuildWebCompiler2022` drift,
normalize first; otherwise include them
```

The skill also carries the full tracked lockfile set so the exception stays narrow and the command
doesn’t have to repeat path inventory.

## Why This Works

The drift is environmental: it comes from the Debug-Sass/WebCompiler path, not from a genuine package
change. Classifying it as generated noise keeps the workflow from treating it as a commit-worthy
dependency update. Keeping the exception in the skill preserves the default rule for every other
lockfile change.

## Prevention

- Keep `commit.md` terse and delegate lockfile policy to the commits skill.
- Keep the tracked `packages.lock.json` paths in one place.
- Preserve the narrow `BuildWebCompiler2022` exception instead of widening it to “ignore lockfiles.”
- Update workflow docs when the exception changes so command guidance, skill guidance, and reference
  docs stay aligned.

## Related Docs

- `.opencode/commands/commit.md` — short pointer into the commit workflow.
- `.opencode/skills/commits/SKILL.md` — shared lockfile policy and normalize-first rule.
- `AGENTS.md` — authoritative note that isolated `BuildWebCompiler2022` drift should not be
  committed.
- `docs/github-workflow-architecture.md` — broader lockfile guidance now aligned with the exception.
- `docs/plans/2026-04-02-003-fix-debug-sass-lockfile-drift-plan.md` — implementation plan that drove
  the workflow update.
