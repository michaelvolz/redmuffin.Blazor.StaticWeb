---
title: cc-safety-net blocking rules configuration
date: 2026-04-19
category: developer-experience
module: cc-safety-net
problem_type: developer_experience
component: tooling
severity: medium
applies_when:
  - Configuring safety plugins for development environments
  - Setting up automated safeguards against destructive commands
  - Balancing security with development productivity
tags: safety-net, cc-safety-net, blocking-rules, dangerous-commands, git-safety, plugin-configuration
---

# cc-safety-net blocking rules configuration

## Context

When working with the cc-safety-net plugin in a development environment, there's a need to configure safety rules that block potentially dangerous commands while allowing necessary development tools. This arises when setting up automated safeguards to prevent accidental execution of destructive operations, such as force pushes or system modifications, without hindering productivity.

## Guidance

Configure the `.safety-net.json` file by adding rules to block dangerous commands, with the following exceptions to preserve development workflow:

- Exclude `npm global` installs (due to 7-day release age filter for security)
- Exclude `winget`, `choco`, and `pwsh rm` (as they are standard package management and cleanup tools)

Reorder the rules in this priority sequence:

1. Git push variants (e.g., `git push --force`, `git push --force-with-lease`) - highest risk
2. Remaining Git commands (e.g., `git reset --hard`, `git clean -fd`)
3. Other dangerous commands (e.g., `rm -rf /`, `format c:`)

Example `.safety-net.json` structure (current `block_args` format):

```json
{
  "version": 1,
  "rules": [
    {
      "name": "block-rtk-git-push",
      "command": "rtk",
      "subcommand": "git",
      "block_args": ["push"],
      "reason": "Only humans may push to remote repositories."
    }
  ]
}
```

The format changed from regex-based `pattern`/`action`/`message` to named
`command`/`subcommand`/`block_args`/`reason` rules. The `block_args` array
matches substrings in command arguments, not regex patterns.

## Why This Matters

This configuration prevents catastrophic mistakes like force-pushing to main or accidentally deleting system files, which could cause data loss or repository corruption. By excluding essential development commands, it maintains workflow efficiency while reducing risk. The prioritized ordering ensures the most destructive operations are caught first, improving response time in case of misconfigurations.

## When to Apply

- In team development environments where multiple contributors handle Git operations
- When setting up CI/CD pipelines that involve automated pushes
- In projects with sensitive codebases requiring strict version control safeguards
- After incidents involving accidental destructive commands

## Examples

**Before (Unprotected)**: Running `git push --force origin main` in a terminal would execute without warning, potentially overwriting team changes.

**After (Protected)**: The same command triggers a block with message "Force push blocked for safety", requiring explicit override via plugin settings.

**Usage Example**: During a hotfix, a developer attempts `git reset --hard HEAD~5`. The plugin blocks it, displaying "Hard reset blocked for safety", allowing the team to review the action before proceeding with a safer approach like `git revert`.

## Related

- docs/solutions/workflow-issues/opencode-local-plugin-npm-lookup-2026-04-18.md
- docs/solutions/logic-errors/block-push-plugin-logic-errors-2026-04-03.md
