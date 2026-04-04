---
title: "feat: Remove obsolete git branches, keep only master"
type: feat
status: completed
date: 2026-04-04
---

# Remove Obsolete Git Branches — Keep Only `master`

## Overview

The repository has accumulated 10 local branches and ~13 remote branches beyond `master`. Most are merged and safe to delete, but 4 local branches contain unmerged work. The goal is to safely remove all branches except `master` while preserving any unmerged work the user may want to keep.

## Problem Frame

Branch sprawl creates noise in tooling, slows `git branch` output, and increases cognitive overhead. The user wants a clean state with only `master` remaining. The risk is accidental loss of unmerged commits.

## Requirements Trace

- R1. After completion, only `master` exists locally and on remote
- R2. No unmerged work is lost without explicit user decision
- R3. Stale remote-tracking references are pruned
- R4. Operation is reversible for merged branches (recoverable from master history)

## Scope Boundaries

- **In scope:** All local branches, all remote branches, stale remote-tracking refs
- **Out of scope:** Any code changes, refactors, or feature work on those branches
- **Out of scope:** Branches in other repositories or forks

## Context & Research

### Current Branch State

**Merged into master (safe to delete):**

- `feat/sidenote-capture-skill`
- `feature/016-fix-brotli-compression`
- `feature/PRD-014-wasm-loading-optimization`
- `feature/PRD-017-workflow-output-optimization`
- `fix/prevent-about-blank-tabs`
- `fix/rm-cleanup-rewrite`

**NOT merged into master (require user decision):**

- `feature/019-refactor-shared-components` — 1 commit: `src/redmuffin.Blazor.StaticWeb/Features/Common/RaindropPageBase.cs` (493 lines, abstract base class for Videos/Articles pages)
- `shimmer` — 1 commit from 9 months ago: WIP shimmer animation improvements
- `temp` — 1 commit from 9 months ago: WIP mixed changes (logging refactors, task files)
- `test/docs-change` — 1 commit: `README.md` test comment for CodeQL v4 verification

**Remote-only branches to delete:**

- `dependabot/github_actions/dot-github/workflows/github_actions-558eba0880` (stale)
- `dependabot/github_actions/github/codeql-action-4` (stale)
- `dependabot/nuget/Meziantou.Analyzer-3.0.39` (stale)
- `dependabot/nuget/Meziantou.Analyzer-3.0.44`
- `dependabot/nuget/Microsoft.ApplicationInsights.WorkerService-3.0.0` (stale)
- `dependabot/nuget/Microsoft.ApplicationInsights.WorkerService-3.1.0`
- `dependabot/nuget/bunit-2.7.2`
- `dependabot/nuget/src/redmuffin.Blazor.StaticWeb.Api/Microsoft.ApplicationInsights.WorkerService-3.0.0` (stale)
- `dependabot/nuget/src/redmuffin.Blazor.StaticWeb/Markdig-1.1.2` (stale)
- `dependabot/nuget/src/redmuffin.Blazor.StaticWeb/Microsoft.AspNetCore.Components.Authorization-9.0.14` (stale)
- `dependabot/nuget/src/redmuffin.Blazor.StaticWeb/Microsoft.AspNetCore.Components.WebAssembly-9.0.14` (stale)
- `dependabot/nuget/src/redmuffin.Blazor.StaticWeb/Microsoft.AspNetCore.WebUtilities-9.0.14` (stale)
- `dependabot/nuget/tests/redmuffin.Blazor.StaticWeb.Tests/bunit-2.6.2` (stale)
- `feature/018-videos-articles-performance-optimization`
- `feature/PRD-014-wasm-loading-optimization`
- `feature/PRD-017-workflow-output-optimization`
- `shimmer`
- `shimmer-laptop` (stale)

### Institutional Learnings

- No relevant `docs/solutions/` entries for branch cleanup operations

### External References

- `git branch -d` safely refuses to delete unmerged branches; `git branch -D` force-deletes
- `git push origin --delete <branch>` removes remote branches
- `git remote prune origin` cleans up stale remote-tracking references
- `git fetch --prune` combines fetch with pruning in one operation

## Key Technical Decisions

- **Decision: Ask user about unmerged branches before deletion.** The 4 unmerged local branches contain work that would be permanently lost if force-deleted. The implementer must present the branch summaries to the user and get explicit yes/no for each before proceeding.
- **Decision: Delete merged branches without prompting.** These are already in `master` history and recoverable.
- **Decision: Prune remote-tracking refs after remote branch deletion.** Ensures `git branch -a` output is clean.
- **Decision: Delete remote branches via `git push origin --delete`.** Standard approach, works with the existing `git@github.com:michaelvolz/redmuffin.Blazor.StaticWeb.git` remote.

## Open Questions

### Resolved During Planning

- **Which branches to keep?** Only `master`. All others are candidates for deletion.
- **How to handle unmerged work?** Present summaries to user, get explicit decision per branch. If user says "delete all", force-delete. If user says "keep some", preserve those.

### Deferred to Implementation

- **Exact force-delete vs safe-delete commands for each unmerged branch.** Depends on user's per-branch decision.
- **Whether to create backup tags for unmerged branches before deletion.** Only if user wants a safety net.

## Implementation Units

- [x] **Unit 1: Present unmerged branch summary and get user decisions**

**Goal:** Show the user what unmerged work exists and get explicit keep/delete decisions for each of the 4 unmerged local branches.

**Requirements:** R2

**Dependencies:** None

**Files:** None (interactive step)

**Approach:**

- Present the 4 unmerged branches with their commit summaries (already gathered in research phase)
- Ask the user: "Delete all unmerged branches, or keep any?"
- If the user wants to keep any, note which ones and skip them in Unit 2
- If the user wants to delete all, proceed with force-deletion in Unit 2
- Optionally offer to create backup tags (e.g., `backup/feature/019-refactor-shared-components`) before force-deleting, as a safety net

**Test scenarios:**

- Test expectation: none -- this is an interactive decision-gathering step with no code changes

**Verification:**

- User has given explicit keep/delete decision for each of the 4 unmerged branches

- [x] **Unit 2: Delete merged local branches**

**Goal:** Remove all 6 local branches that are already merged into `master`.

**Requirements:** R1

**Dependencies:** None

**Files:** None (git operations)

**Approach:**

- Use safe delete (`git branch -d`) for each merged branch. This will refuse to delete if the branch is not actually merged, providing a safety check.
- Branches to delete: `feat/sidenote-capture-skill`, `feature/016-fix-brotli-compression`, `feature/PRD-014-wasm-loading-optimization`, `feature/PRD-017-workflow-output-optimization`, `fix/prevent-about-blank-tabs`, `fix/rm-cleanup-rewrite`

**Patterns to follow:**

- Use `git branch -d` (safe delete) not `git branch -D` (force delete) for merged branches

**Test scenarios:**

- Happy path: Each `git branch -d` succeeds without error for all 6 merged branches
- Edge case: If any branch reports "not fully merged", stop and investigate rather than force-deleting

**Verification:**

- `git branch` output shows only `master` plus any unmerged branches the user chose to keep

- [ ] **Unit 3: Delete unmerged local branches (per user decision)**

**Goal:** Remove the unmerged local branches the user approved for deletion.

**Requirements:** R1, R2

**Dependencies:** Unit 1 (user decisions)

**Files:** None (git operations)

**Approach:**

- For each unmerged branch the user approved for deletion:
  - If user requested backup tags, create tag first: `git tag backup/<branch-name> <branch-name>`
  - Force-delete: `git branch -D <branch-name>`
- Skip any branches the user chose to keep
- Branches under consideration: `feature/019-refactor-shared-components`, `shimmer`, `temp`, `test/docs-change`

**Test scenarios:**

- Happy path: Each approved branch is force-deleted successfully
- Edge case: Branch that user chose to keep is NOT deleted
- Error path: If a branch name doesn't exist (already deleted), skip gracefully

**Verification:**

- `git branch` shows only `master` (or `master` plus any kept branches)

- [ ] **Unit 4: Delete all remote branches except `master`**

**Goal:** Remove all remote branches from `origin` except `master`.

**Requirements:** R1

**Dependencies:** None (can run in parallel with Unit 2)

**Files:** None (git operations)

**Approach:**

- Delete all remote branches except `master` using `git push origin --delete <branch>`
- This includes all dependabot branches, feature branches, and stale branches
- Can batch multiple branches in a single push command for efficiency
- Remote branches to delete: all 18 listed in the research section above

**Test scenarios:**

- Happy path: All remote branches except `master` are deleted
- Error path: If a remote branch was already deleted by someone else, the push reports it but doesn't fail the overall operation
- Error path: If push is rejected (permissions), report clearly and stop

**Verification:**

- `git remote show origin` lists only `master` under Remote branches

- [ ] **Unit 5: Prune stale remote-tracking references and verify clean state**

**Goal:** Clean up any stale remote-tracking refs and verify the final state is exactly `master` only.

**Requirements:** R1, R3

**Dependencies:** Unit 2, Unit 3, Unit 4

**Files:** None (git operations)

**Approach:**

- Run `git fetch --prune` to remove all stale remote-tracking references
- Run `git branch -a` to verify only `master` and `remotes/origin/master` and `remotes/origin/HEAD -> origin/master` remain
- If backup tags were created in Unit 3, confirm they exist

**Test scenarios:**

- Happy path: `git branch -a` shows only `master`, `remotes/origin/master`, and `remotes/origin/HEAD -> origin/master`
- Happy path: No stale references remain
- Edge case: If user kept local branches, those appear alongside `master` as expected

**Verification:**

- `git branch -a` output contains only `master`-related refs (plus any user-kept branches)
- No stale or orphaned remote-tracking references remain

## System-Wide Impact

- **Interaction graph:** None — this is a pure git housekeeping operation with no code or runtime impact
- **Error propagation:** N/A
- **State lifecycle risks:** Only risk is permanent loss of unmerged commits. Mitigated by Unit 1 (user decision) and optional backup tags
- **API surface parity:** N/A
- **Unchanged invariants:** `master` branch is untouched. No code, config, or CI/CD changes.

## Risks & Dependencies

| Risk                                              | Likelihood | Impact | Mitigation                                                                       |
| ------------------------------------------------- | ---------- | ------ | -------------------------------------------------------------------------------- |
| Unmerged work lost permanently                    | Medium     | High   | Unit 1 requires explicit user decision per unmerged branch; optional backup tags |
| Remote branch deletion fails (permissions)        | Low        | Medium | Clear error reporting; operation can be retried                                  |
| Accidentally deleting a branch still in use by CI | Low        | Medium | All branches reviewed; dependabot branches are auto-created and safe to delete   |
| User changes mind after deletion                  | Low        | High   | Backup tags option in Unit 3; merged branches recoverable from `master` history  |

## Documentation / Operational Notes

- No documentation changes needed
- No CI/CD or deployment impact
- This is a one-time housekeeping operation

## Sources & References

- Local branch state: `git branch -a`, `git branch -v --merged master`, `git branch -v --no-merged master`
- Remote state: `git remote show origin`
- Commit details: `git show --stat` for each unmerged branch
