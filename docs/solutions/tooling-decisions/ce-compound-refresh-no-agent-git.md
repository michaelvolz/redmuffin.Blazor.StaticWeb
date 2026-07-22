---
title: "ce-compound-refresh never branches or commits (trunk-based git policy)"
date: 2026-07-22
category: tooling-decisions
module: ce-compound-refresh
problem_type: tooling_decision
component: tooling
severity: medium
applies_when:
  - "Running ce-compound-refresh in any mode"
  - "Stock Phase 5 would offer or perform branch/commit/PR"
  - "Patching vendor skills that assume autonomous git writes"
tags:
  - git-policy
  - trunk-based-development
  - vendor-skill-patches
  - ce-compound-refresh
  - agent-restrictions
  - rm-vendor-skill-patches
---

# ce-compound-refresh never branches or commits (trunk-based git policy)

## Context

Stock **ce-compound-refresh** Phase 5 (*Commit Changes*) treats durable git writes as part of the skill:

- **Interactive:** after the report, detect branch and offer branch+PR (recommended), commit on main/master, or don't commit.
- **Headless on main/master:** create a named refresh branch, commit, attempt `gh pr create`.
- **Discoverability step 6:** amend or follow-up-commit instruction-file edits.

This team is **trunk-based**. Branch creation, staging, commit, push, amend, and PR open are **only the human's job**. The agent must not decide *when* or *whether* those happen. Stock Phase 5 conflicts with that discipline (and with global `rm-commit` rules: never commit unless the user says `commit`).

## Guidance

Suppress agent git operations with the **rm-vendor-skill-patches** overlay for `compound-engineering` / `ce-compound-refresh` — do not hand-edit the live vendor `SKILL.md` after every upstream sync.

### Overlay layout (source of truth)

All of the following live under the **home-directory skill store** (outside this git repo), managed by **rm-vendor-skill-patches**:

- **git-policy** addendum — NEVER branch/commit/push/PR; ALWAYS stop after the report
- **redmuffin README** for ce-compound-refresh — declares Phase 5 and Discoverability step 6 **superseded**
- **skill-md.json** patch ops — idempotent “read redmuffin README first” anchor on stock SKILL.md
- **patch log** entry for ce-compound-refresh — why/verify greps for reapply

Live install after apply: the compound-engineering **ce-compound-refresh** skill tree under the agents skills directory (redmuffin subdirectory for additive files).

### NEVER (agent during ce-compound-refresh)

- Create a branch (`git checkout -b`, `git switch -c`, …)
- Stage, commit, amend, or push
- Open a PR (`gh pr create`, …)
- Ask whether to branch, commit on trunk, or open a PR
- Offer Phase 5-style commit menus
- Create follow-up commits for Discoverability instruction-file edits
- Headless: auto branch named for the refresh + commit + PR

### ALWAYS

- End after the refresh report (Discoverability edits only as working-tree changes the user already consented to)
- List modified paths so the human can commit when they choose
- Leave git state alone

### Explicit `commit` is a different request

If the user separately says `commit`, that is **not** Phase 5 of this skill — follow the harness commit skill (`rm-commit`). It is not implied by finishing a refresh.

### Reapply after topgrade

Reapply via the **rm-vendor-skill-patches** apply script with vendor `compound-engineering` and skill `ce-compound-refresh` (same command as in that skill’s patch log header).

Topgrade’s skill sync calls the apply script after mirror so overlays reapply automatically. Broken anchors → mark the log **NEEDS REVIEW**; never invent new anchors.

## Why This Matters

| Stock behavior | Trunk-based cost |
| -------------- | ---------------- |
| Agent-created branches | Unwanted remote/local branch clutter |
| Auto-commit on trunk | Commits without human intent or message ownership |
| Auto-PR | PR workflow the team may not want for doc refresh |
| Discoverability follow-up commits | Interleaved commits the human did not schedule |

Additive overlays survive upstream compound-engineering updates without forking the whole skill. Policy stays in one file agents load via the existing "read redmuffin README first" anchor.

## When to Apply

- Any `ce-compound-refresh` run (interactive or headless)
- Designing patches for other vendor skills that auto-commit or open PRs
- After topgrade: confirm `git-policy.md` still present under the live install

## Examples

### Before (stock)

Interactive ends with a commit menu recommending branch+PR. Headless creates `docs/refresh-…`, commits, tries to open a PR.

### After (overlay applied)

Agent prints the Compound Refresh Summary, lists dirty paths, **stops**. No git write commands. User commits on trunk when ready.

### Invocation chain

1. Read stock `SKILL.md` → cross-harness line points at the skill-local redmuffin README
2. That README marks Phase 5 superseded → read skill-local `git-policy.md`
3. Run Phases 0–4.5 (and Discoverability) without Phase 5 git
4. Report only

## Related

- `rm-vendor-skill-patches` skill — overlay + apply + log model
- Global commit discipline (`rm-commit` / CLAUDE.md) — never commit unless user says `commit`
- `docs/solutions/best-practices/csharp-standards-final.md` — human-facing trunk-based notes (no agent enforcement)
- Patch log entry **2026-07-22** under rm-vendor-skill-patches for ce-compound-refresh
