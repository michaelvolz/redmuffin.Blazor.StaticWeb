# Reorganize WIP Commits According to Single Responsibility Principle

## Overview

We have 7 WIP commits (1c624b9 through 6a707c4) that contain multiple unrelated changes mixed together. This PRD defines how to undo these commits and reorganize them into coherent, single-responsibility commits that follow best practices.

## Current State Analysis

The 7 WIP commits contain these mixed changes:

### WIP 1 (1c624b9): DevContainer and SSH Setup

- **Files:** .devcontainer/\*, .opencode/themes/dracula.json, .opencode/tui.json, README.md, docs/DEVCOTAINER-PLAN.md, docs/SSH-AGENT-SETUP.md, opencode.json, scripts/devcontainer-down.ps1, scripts/opencode-secure.ps1, scripts/setup-ssh-agent.ps1
- **Issue:** Mixes devcontainer infrastructure, opencode UI theming, documentation, and helper scripts

### WIP 2 (a0468fa): PowerShell SecretStore Implementation

- **Files:** .devcontainer/devcontainer.json, docs/SECRETS-SETUP.md, scripts/opencode-secure.ps1, scripts/setup-secrets.ps1
- **Issue:** Modifies devcontainer.json which overlaps with WIP 1 and 5

### WIP 3 (40957ec): Add Raindrop Secrets to SecretStore

- **Files:** .devcontainer/devcontainer.json, scripts/opencode-secure.ps1, scripts/setup-secrets.ps1
- **Issue:** Minor additions to files from WIP 2, very small commit

### WIP 4 (dde19ff): Generic WIP - Various Refinements

- **Files:** .devcontainer/Dockerfile, AGENTS.md, docs/DEVCOTAINER-PLAN.md, scripts/opencode-secure.ps1
- **Issue:** Mixes devcontainer changes, documentation updates, and script modifications

### WIP 5 (06d2b44): DevContainer Isolated Setup

- **Files:** .devcontainer/\* (including deletion of docker-compose.yml), scripts/devcontainer-down.ps1
- **Issue:** More devcontainer changes overlapping with previous commits

### WIP 6 (6fb55f7): Convert GitHub Prompts to Opencode Skills

- **Files:** .opencode/skills/\_execute-tasklist/SKILL.md, .opencode/skills/\_generate-prd/SKILL.md, .opencode/skills/\_tasklist-from-prd/SKILL.md
- **Issue:** Creates skills with underscore prefixes (later archived)

### WIP 7 (6a707c4): Generic WIP - Skills Archive and Rename

- **Files:** .archive/skills/\* (moved from .opencode), .opencode/skills/create-prd/SKILL.md, .opencode/skills/generate-tasks/SKILL.md, .opencode/tui.json
- **Issue:** Archives old skills and creates new ones, plus tui.json change

## Proposed Reorganization

### Commit 1: Opencode UI Theming and Configuration

**Scope:** Opencode UI customization files that are independent

**Files to Include:**

- `.opencode/themes/dracula.json` (new)
- `.opencode/tui.json` (new/modified)

**Commit Message:**

```
feat(opencode): add dracula theme and TUI configuration

Add Dracula color theme for opencode CLI interface and configure
TUI settings for improved developer experience.
```

### Commit 2: GitHub Prompts to Opencode Skills Conversion

**Scope:** Converting existing GitHub prompt templates to opencode skills

**Files to Include:**

- `.opencode/skills/create-prd/SKILL.md` (new)
- `.opencode/skills/generate-tasks/SKILL.md` (new)
- `.opencode/skills/_execute-tasklist/SKILL.md` (new - archived version)
- `.opencode/skills/_generate-prd/SKILL.md` (new - archived version)
- `.opencode/skills/_tasklist-from-prd/SKILL.md` (new - archived version)

**Rationale:** Include both current and archived versions in one commit since the archive represents the migration history.

**Commit Message:**

```
feat(opencode): convert GitHub prompts to skills with archival

Convert existing PRD and task list generation prompts to opencode
skills. Archive original underscore-prefixed versions for reference.
New skills:
- create-prd: Generate product requirements documents
- generate-tasks: Create task lists from PRDs
- _execute-tasklist: Execute implementation tasks (archived)
- _generate-prd: Original PRD generator (archived)
- _tasklist-from-prd: Original task generator (archived)

All skills target Blazor WebAssembly .NET 9 with TUnit testing
and LightMock.Generator mocking patterns.
```

### Commit 3: DevContainer Core Infrastructure

**Scope:** Essential devcontainer configuration without secrets or SSH

**Files to Include:**

- `.devcontainer/Dockerfile` (all changes consolidated)
- `.devcontainer/devcontainer.json` (all changes consolidated)
- `.devcontainer/post-create.sh` (all changes consolidated)
- `.devcontainer/post-start.sh` (all changes consolidated)
- `.devcontainer/docker-compose.yml` (deleted - from WIP 5)
- `scripts/devcontainer-down.ps1` (new)

**Rationale:** Consolidate all devcontainer configuration changes into one logical commit. This includes removing docker-compose.yml in favor of standalone configuration.

**Commit Message:**

```
feat(devcontainer): implement isolated container setup with folder-based volumes

Restructure devcontainer configuration for isolated, reproducible
environments:
- Remove docker-compose.yml in favor of standalone devcontainer.json
- Use automatic volume naming for persistent storage
- Simplify post-create and post-start lifecycle scripts
- Add devcontainer-down.ps1 helper script for cleanup
- Configure container with proper volume mounts and extensions

This change enables true isolation between projects while maintaining
persistent volumes for installed tools and configurations.
```

### Commit 4: SSH Agent Setup for Git Authentication

**Scope:** SSH configuration and documentation

**Files to Include:**

- `.devcontainer/setup-ssh.sh` (new)
- `scripts/setup-ssh-agent.ps1` (new)
- `docs/SSH-AGENT-SETUP.md` (new)

**Rationale:** SSH setup is a separate concern from the core devcontainer infrastructure.

**Commit Message:**

```
feat(devcontainer): add SSH agent forwarding for Git authentication

Enable SSH key-based Git authentication within devcontainer:
- Add setup-ssh.sh script for container-side SSH configuration
- Add setup-ssh-agent.ps1 for Windows host SSH agent management
- Document SSH setup process in SSH-AGENT-SETUP.md

Supports both Windows (OpenSSH) and WSL workflows for seamless
Git operations inside the container.
```

### Commit 5: PowerShell SecretStore for API Key Management

**Scope:** Secret management infrastructure

**Files to Include:**

- `scripts/setup-secrets.ps1` (new - all versions consolidated)
- `scripts/opencode-secure.ps1` (new - all versions consolidated)
- `docs/SECRETS-SETUP.md` (new)

**Rationale:** Secret management is a separate feature from devcontainer setup, though they work together.

**Commit Message:**

```
feat(secrets): implement PowerShell SecretStore for secure API key management

Add secure secrets management using PowerShell SecretStore:
- setup-secrets.ps1: Store API keys in encrypted vault (SecretStore)
- opencode-secure.ps1: Load secrets into environment variables
- SECRETS-SETUP.md: Complete documentation for setup workflow

Secrets flow: SecretStore -> temp env vars -> devcontainer -> MCP servers
All secrets encrypted at rest, memory-only during use.

Supports:
- RainDropClientID
- RainDropClientSecret
- RainDropTestToken
- BRAVE_API_KEY
- Other MCP server secrets
```

### Commit 6: Documentation and Configuration Updates

**Scope:** README, AGENTS.md, and devcontainer plan documentation

**Files to Include:**

- `README.md` (all changes consolidated)
- `AGENTS.md` (all changes consolidated)
- `docs/DEVCOTAINER-PLAN.md` (new - all versions consolidated)
- `opencode.json` (all changes consolidated)

**Rationale:** Documentation updates that reference all the above changes should come last.

**Commit Message:**

```
docs: update README, AGENTS.md, and add devcontainer planning documentation

Update project documentation to reflect new development workflow:
- Rewrite README.md with devcontainer-first development approach
- Update AGENTS.md with security-first policy and secret management
- Add comprehensive DEVCOTAINER-PLAN.md with architecture decisions
- Update opencode.json configuration for new skill structure

Documentation covers:
- DevContainer setup and usage
- SSH agent configuration
- Secret management with PowerShell SecretStore
- Security policies and best practices
- Updated development guidelines
```

## Implementation Steps

### Prerequisites

1. Ensure working directory is clean: `git status` should show "nothing to commit, working tree clean"
2. All changes are currently in the 7 WIP commits ahead of origin/master

### Step-by-Step Process

**Step 1: Backup Current Branch**

```bash
git branch backup-wip-reorganization
git checkout -b reorganize-wip-commits
```

**Step 2: Soft Reset to Pre-WIP State**

```bash
git reset --soft 3757d37
```

This keeps all changes staged but removes the 7 WIP commits from history.

**Step 3: Create Commit 1 - Opencode UI Theming**

```bash
git reset HEAD  # Unstage everything
# Stage only the theming files
git add .opencode/themes/dracula.json
git add .opencode/tui.json
git commit -m "feat(opencode): add dracula theme and TUI configuration

Add Dracula color theme for opencode CLI interface and configure
TUI settings for improved developer experience."
```

**Step 4: Create Commit 2 - Skills Conversion**

```bash
# Stage all skill files (both current and archived)
git add .opencode/skills/create-prd/SKILL.md
git add .opencode/skills/generate-tasks/SKILL.md
git add .opencode/skills/_execute-tasklist/SKILL.md
git add .opencode/skills/_generate-prd/SKILL.md
git add .opencode/skills/_tasklist-from-prd/SKILL.md
git commit -m "feat(opencode): convert GitHub prompts to skills with archival

Convert existing PRD and task list generation prompts to opencode
skills. Archive original underscore-prefixed versions for reference.
New skills:
- create-prd: Generate product requirements documents
- generate-tasks: Create task lists from PRDs
- _execute-tasklist: Execute implementation tasks (archived)
- _generate-prd: Original PRD generator (archived)
- _tasklist-from-prd: Original task generator (archived)

All skills target Blazor WebAssembly .NET 9 with TUnit testing
and LightMock.Generator mocking patterns."
```

**Step 5: Create Commit 3 - DevContainer Infrastructure**

```bash
# Stage devcontainer files (consolidating all changes)
git add .devcontainer/Dockerfile
git add .devcontainer/devcontainer.json
git add .devcontainer/post-create.sh
git add .devcontainer/post-start.sh
git add .devcontainer/docker-compose.yml  # This will be a deletion
git add scripts/devcontainer-down.ps1
git commit -m "feat(devcontainer): implement isolated container setup with folder-based volumes

Restructure devcontainer configuration for isolated, reproducible
environments:
- Remove docker-compose.yml in favor of standalone devcontainer.json
- Use automatic volume naming for persistent storage
- Simplify post-create and post-start lifecycle scripts
- Add devcontainer-down.ps1 helper script for cleanup
- Configure container with proper volume mounts and extensions

This change enables true isolation between projects while maintaining
persistent volumes for installed tools and configurations."
```

**Step 6: Create Commit 4 - SSH Setup**

```bash
git add .devcontainer/setup-ssh.sh
git add scripts/setup-ssh-agent.ps1
git add docs/SSH-AGENT-SETUP.md
git commit -m "feat(devcontainer): add SSH agent forwarding for Git authentication

Enable SSH key-based Git authentication within devcontainer:
- Add setup-ssh.sh script for container-side SSH configuration
- Add setup-ssh-agent.ps1 for Windows host SSH agent management
- Document SSH setup process in SSH-AGENT-SETUP.md

Supports both Windows (OpenSSH) and WSL workflows for seamless
Git operations inside the container."
```

**Step 7: Create Commit 5 - SecretStore Implementation**

```bash
git add scripts/setup-secrets.ps1
git add scripts/opencode-secure.ps1
git add docs/SECRETS-SETUP.md
git commit -m "feat(secrets): implement PowerShell SecretStore for secure API key management

Add secure secrets management using PowerShell SecretStore:
- setup-secrets.ps1: Store API keys in encrypted vault (SecretStore)
- opencode-secure.ps1: Load secrets into environment variables
- SECRETS-SETUP.md: Complete documentation for setup workflow

Secrets flow: SecretStore -> temp env vars -> devcontainer -> MCP servers
All secrets encrypted at rest, memory-only during use.

Supports:
- RainDropClientID
- RainDropClientSecret
- RainDropTestToken
- BRAVE_API_KEY
- Other MCP server secrets"
```

**Step 8: Create Commit 6 - Documentation Updates**

```bash
git add README.md
git add AGENTS.md
git add docs/DEVCOTAINER-PLAN.md
git add opencode.json
git commit -m "docs: update README, AGENTS.md, and add devcontainer planning documentation

Update project documentation to reflect new development workflow:
- Rewrite README.md with devcontainer-first development approach
- Update AGENTS.md with security-first policy and secret management
- Add comprehensive DEVCOTAINER-PLAN.md with architecture decisions
- Update opencode.json configuration for new skill structure

Documentation covers:
- DevContainer setup and usage
- SSH agent configuration
- Secret management with PowerShell SecretStore
- Security policies and best practices
- Updated development guidelines"
```

**Step 9: Verify the Reorganization**

```bash
# Check that all files are committed
git status

# Review the new commit history
git log --oneline -10

# Verify no changes are lost by comparing with backup
git diff backup-wip-reorganization --stat
```

The diff should show no differences, confirming all changes are preserved.

**Step 10: Cleanup (Optional)**

```bash
# If everything looks good, delete the backup branch
git branch -D backup-wip-reorganization

# Rename branch to master (if you want to replace current master)
# Or merge/rebase as appropriate for your workflow
```

## Alternative: Using git add -p for Granular Control

If staging entire files at once is too coarse, use interactive staging:

```bash
# After git reset --soft 3757d37
git reset HEAD  # Unstage everything

# For each file, selectively stage hunks
git add -p .devcontainer/Dockerfile
# Choose 'y' for hunks that belong to the current commit, 'n' for others

# Repeat for each commit group
```

## Success Criteria

- [ ] All 7 original WIP commits are removed from history
- [ ] Exactly 6 new commits replace them with clear, single-responsibility messages
- [ ] No changes are lost (verify with `git diff` against original)
- [ ] Each commit passes a "squash test" - could be squash-merged without losing meaning
- [ ] Commit messages follow conventional commit format
- [ ] File history is clean and logical

## Risks and Mitigation

### Risk 1: Merge Conflicts if Pushed

**Mitigation:** This reorganizes local-only commits (8 commits ahead of origin). If already pushed, force push will be required: `git push --force-with-lease`

### Risk 2: Lost Changes

**Mitigation:**

- Create backup branch before starting
- Use `git diff backup-branch --stat` to verify no lost changes
- Review each commit with `git show` before finalizing

### Risk 3: Build/Test Failures in Intermediate Commits

**Mitigation:** These are infrastructure/documentation changes, not code. No build/test impact expected.

### Risk 4: Partial File States

**Mitigation:** Some files (like devcontainer.json) have incremental changes across WIP commits. Ensure final state matches the cumulative changes by reviewing the original last.

## Rollback Plan

If issues arise during reorganization:

```bash
# Reset to the backup branch
git checkout backup-wip-reorganization
git branch -D reorganize-wip-commits  # Delete failed attempt
git checkout -b reorganize-wip-commits-v2  # Start fresh
```

## Notes

- **Commit Order Matters:** Infrastructure (devcontainer) should come before features that depend on it (SSH, secrets)
- **Documentation Last:** README and AGENTS.md reference other changes, so they come last
- **Archive Skills:** Including archived skills with current ones preserves history in a single logical commit
- **Test Thoroughly:** After reorganization, verify all files have correct final state by comparing byte-for-byte with original if needed
