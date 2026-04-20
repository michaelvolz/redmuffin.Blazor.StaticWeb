---
module: git-workflow
date: 2026-04-20
problem_type: workflow_issue
component: development_workflow
severity: medium
category: docs/solutions/workflow-issues/
---

# Git Status Showing Modified File With No Changes

## Context

During development, git status may incorrectly show a file as modified even when `git diff` shows no insertions or deletions. This can happen due to index corruption, stale locks, or other git internal state issues that don't reflect actual file content changes.

## Guidance

When encountering a file that appears modified in `git status` but has no visible changes in `git diff`:

1. Verify with `git diff --cached` to check staged changes
2. Check for line ending issues with `od -c` or `file`
3. If no actual changes are found, use `git checkout --force` to reset the index and working directory

Avoid using `git checkout --force` unless you're certain no real changes exist, as it will discard uncommitted work.

## Why This Matters

False modified status can lead to confusion, unnecessary commits, or missed real changes. Understanding when to use force reset prevents workflow disruption and ensures clean git state.

## When to Apply

Apply this guidance when:

- `git status` shows files as modified
- `git diff` shows 0 insertions/deletions
- `git checkout` without force doesn't reset the status
- No actual content changes are intended

## Examples

### Case: chrome-devtools-launch.mjs

```bash
$ git status
On branch master
Changes to be committed:
  (use "git restore --staged <file>..." to unstage)
	modified:   scripts/mcp/chrome-devtools-launch.mjs

$ git diff scripts/mcp/chrome-devtools-launch.mjs
# No output - 0 insertions, 0 deletions

$ git checkout scripts/mcp/chrome-devtools-launch.mjs
M	scripts/mcp/chrome-devtools-launch.mjs
# Still shows modified

$ git checkout --force
# Resets to clean state
```

## Related Docs

- `docs/solutions/build-errors/debug-sass-lockfile-drift-2026-04-02.md` - Similar issue with lockfile drift appearing as changes
- `docs/solutions/best-practices/git-cli-optimizations-for-ai-agents-2026-04-18.md` - Git status command best practices
- `docs/solutions/developer-experience/stale-git-lock-recovery-for-interrupted-sessions-2026-04-04.md` - Index lock issues that can cause status problems</content>
  <parameter name="filePath">docs/solutions/workflow-issues/git-status-showing-modified-file-with-no-changes-2026-04-20.md
