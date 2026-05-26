---
module: Git Workflow
date: 2026-03-30
problem_type: workflow_issue
component: version_control
severity: medium
symptoms:
  - Multiple WIP commits contain unrelated changes mixed together
  - Commit history makes it hard to understand what each change does
  - Files like devcontainer.json modified across 3+ commits with incremental states
root_cause: no_review_before_commit
resolution_type: process_change
tags:
  - git
  - commit-hygiene
  - conventional-commits
  - git-reset
  - history-rewrite
---

# Reorganizing Mixed WIP Commits into Single-Responsibility Commits

## Problem

Seven WIP commits (1c624b9 through 6a707c4) mixed unrelated changes together. A single commit contained devcontainer infrastructure, opencode UI theming, documentation, and helper scripts. The devcontainer.json file was modified across three separate commits, making it impossible to review or revert changes in isolation.

### What Was Mixed

| Domain                              | Files                                                                                                             | Spread Across     |
| ----------------------------------- | ----------------------------------------------------------------------------------------------------------------- | ----------------- |
| DevContainer infrastructure         | Dockerfile, devcontainer.json, post-create.sh, post-start.sh, docker-compose.yml (deleted), devcontainer-down.ps1 | WIP 1, 2, 3, 4, 5 |
| Opencode UI theming                 | dracula.json, tui.json                                                                                            | WIP 1, 7          |
| Skills (create-prd, generate-tasks) | SKILL.md files + archived versions                                                                                | WIP 6, 7          |
| SSH agent setup                     | setup-ssh.sh, setup-ssh-agent.ps1, SSH-AGENT-SETUP.md                                                             | WIP 1             |
| SecretStore (secrets)               | setup-secrets.ps1, opencode-secure.ps1, SECRETS-SETUP.md                                                          | WIP 2, 3          |
| Documentation                       | README.md, AGENTS.md, DEVCOTAINER-PLAN.md, opencode.json                                                          | WIP 1, 4          |

## Root Cause

Commits were created as generic "WIP" snapshots rather than atomic, single-responsibility units. Files accumulated changes across multiple commits instead of being completed in one logical change.

## Solution

Used `git reset --soft` to undo the 7 commits while preserving all changes in the staging area, then re-staged and committed files by logical domain:

1. **Opencode UI theming** (2 files)
2. **Skills conversion** (5 files, incl. archived versions)
3. **DevContainer core infrastructure** (6 files, consolidated all changes incl. docker-compose.yml deletion)
4. **SSH agent setup** (3 files)
5. **SecretStore implementation** (3 files)
6. **Documentation updates** (4+ files, placed last since they reference all other changes)

### Key Technique

```bash
# Backup first
git branch backup-wip-reorganization

# Soft reset to before the 7 WIP commits
git reset --soft 3757d37

# Unstage everything, then stage by domain
git reset HEAD
git add .opencode/themes/dracula.json .opencode/tui.json
git commit -m "feat(opencode): add dracula theme and TUI configuration"
# ... repeat for each domain
```

### Verification

```bash
git diff backup-wip-reorganization --stat  # Must show zero differences
```

## Prevention

- **Commit by domain, not by time**: A commit should contain exactly one logical change. If two unrelated files changed, they belong in separate commits.
- **Finish a file in one commit**: Don't modify the same file across multiple commits in a batch unless each commit represents a distinct logical refinement of that file.
- **`git add -p` for granular staging**: When a file has changes belonging to multiple domains, stage hunks selectively.
- **Review before committing**: The `git status` diff should tell a coherent story. If the diff description reads like "various things," the commit is too broad.
