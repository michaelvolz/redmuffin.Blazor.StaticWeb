---
module: CI/CD Pipeline
date: 2026-05-24
problem_type: logic_error
component: tooling
severity: high
symptoms:
  - Deploy workflow skipped when multiple commits were pushed in a single batch
  - "Only documentation files changed" incorrectly reported when earlier commits touched code
  - Pipeline inspected only the tip commit — docs-only tip hid code changes in earlier commits
root_cause: config_error
resolution_type: config_change
related_components:
  - GitHub Actions
  - docs-branch workflow
tags:
  - ci-cd
  - github-actions
  - docs-branch
  - deployment
  - multi-commit
  - base-sha
---

## Problem

The CI pipeline used `tj-actions/changed-files@v47.0.6` with `since_last_remote_commit: true` to
decide whether a push contained only documentation changes (which should skip deploy). This flag
only inspects the latest commit — the tip of the push.

When multiple commits were pushed in a single batch (e.g., `git push` after stacking commits
locally), only the tip was checked. If the tip was a docs-only commit, the pipeline incorrectly
skipped deploy even when earlier commits in the same push touched code files.

## Symptoms

- Push 4 commits: commits 1-3 change `src/`, commit 4 updates `docs/`. CI sees only commit 4
  (docs-only) and skips the deploy step. The code changes never reach staging.
- Deploy is silently skipped. The only indication is the `docs_only_changed_job` running with
  "Only documentation and non-deployed files changed" — which is misleading.
- Discovered only when the expected deploy didn't appear on the live site.

## What Didn't Work

- `since_last_remote_commit: true` — fundamentally wrong for batched pushes. It diffs against
  `github.event.head_commit` only, which is a single commit.
- Using `fetch-depth: 0` alone without changing the diff base — fetched all history but still
  diffed against the wrong base (the tip commit vs the previous remote HEAD).
- Listing commits manually and checking each one — fragile, slow, and error-prone in complex
  merge scenarios.

## Solution

Replaced `since_last_remote_commit: true` with `base_sha` set to the appropriate base for the
event type:

**Before:**

```yaml
- uses: tj-actions/changed-files@v47.0.6
  with:
    since_last_remote_commit: true
    files: |
      ...
```

**After:**

```yaml
- uses: tj-actions/changed-files@v47.0.6
  with:
    base_sha: ${{ github.event_name == 'pull_request' && github.event.pull_request.base.sha || github.event.before }}
    files: |
      ...
```

For push events, `github.event.before` is the SHA of the remote HEAD before the push arrived.
Diffing against it captures ALL commits in the batch — if any commit touched code files, the
deploy step runs. For PR events, `github.event.pull_request.base.sha` provides the correct
merge base.

The `check_changes` job already uses `fetch-depth: 0` (full history), so the diff has all
commits available.

## Why This Works

`github.event.before` is the ref that was overwritten by the push. Git provides it as part of
the push event payload. By diffing the full push range (`before..after`) rather than just the tip,
every file changed by every commit in the batch is inspected. The `only_changed` output from
`tj-actions/changed-files` reflects the union of all changes, not just the tip.

## Prevention

- Any CI step that gates behavior on "did this push/PR change certain file patterns" must diff
  against the correct base, not the tip commit. `since_last_remote_commit: true` is correct only
  when the trigger guarantees a single-commit push.
- Audit other `tj-actions/changed-files` usages for the same pattern.
- Add a workflow comment explaining why `base_sha` uses the event-type expression — future readers
  must understand the multi-commit push scenario.
