---
date: 2026-04-18
module: opencode
component: tooling
problem_type: workflow_issue
severity: high
title: Opencode Permission Settings - Fail-Safe First Philosophy
applies_when:
  - Configuring opencode settings for any repo
  - Protecting against agent/model errors causing file deletion/corruption
  - New opencode configuration for .NET/Blazor repos
  - Repo uses both Windows and omarchy (Linux)
tags:
  - opencode
  - permissions
  - configuration
  - security
  - fail-safe
---

> **Current (2026-06-08):** Permission settings are configured in
> `~/.config/opencode/opencode.jsonc` (global config, not project-level
> `opencode.json`). The project's `.opencode/` directory is gitignored
> and contains only `magic-context/`.

## Problem

Agent or model errors can cause accidental file deletion or corruption. Without proper permission boundaries, a well-intentioned but mistaken command can delete critical files, corrupt the repo, or break the development environment. Need a fail-safe configuration that errs on the side of restriction.

## Context

When configuring opencode permissions, initial recommendations often start too permissive or lack critical protections. The goal is fail-safe first: start restrictive, add more permissions as needed rather than trying to predict and deny every dangerous command upfront.

### Compound Plugin ADR-003

The compound-engineering plugin originally defaulted to `--permissions broad`, which wrote 14 tool permissions to opencode.json on every install. ADR-003 changed this default to `--permissions none` because broad permissions pollute user config and apply globally across all sessions. For a fail-safe approach, start with no global permissions and add your own restrictive config.

### Global vs Local Merge Behavior

OpenCode merges global (`~/.config/opencode/opencode.json`) and local (`{repo}/opencode.json`) configs with **local taking precedence**. This means:

- Global config provides baseline protection
- Local repo configs can override with stricter rules
- Your local config will always win on conflicts

This enables dual-layer protection: global for all sessions, local for repo-specific needs.

## Guidance

### Core Philosophy: Fail-Safe First

1. Default to `ask` for unknown commands
2. Explicitly allow known-safe operations
3. Explicitly deny known-dangerous operations
4. Add permissions incrementally as new safe patterns emerge

### Permission Configuration Structure

```json
{
  "$schema": "https://opencode.ai/config.json",
  "permission": {
    "skill": {
      "git-commit": "deny",
      "git-commit-push-pr": "deny"
    },
    "read": {
      "*": "allow",
      "**/.env*": "deny",
      "**/secrets*": "deny",
      "**/*.env.example": "allow",
      "docs/**": "allow",
      ".github/**": "allow"
    },
    "lsp": "allow",
    "edit": "ask",
    "bash": {
      "*": "ask",
      "dotnet *": "allow",
      "pwsh *": "allow",
      "powershell *": "allow",
      "git *": "allow",
      "npm *": "allow",
      "npx *": "allow",
      "node *": "allow",
      "dir *": "allow",
      "ls *": "allow",
      "grep *": "allow"
    },
    "external_directory": "ask"
  }
}
```

### Key Elements

| Permission                  | Value | Rationale                                       |
| --------------------------- | ----- | ----------------------------------------------- |
| `read: *`                   | allow | Agents need to read files to understand context |
| `read: **/.env*`            | deny  | Protect secrets from accidental exposure        |
| `read: **/secrets*`         | deny  | Protect secrets from accidental exposure        |
| `lsp`                       | allow | Language server needed for code analysis        |
| `edit`                      | ask   | Require human approval for edits                |
| `bash: dotnet *`            | allow | Broad dotnet patterns for maintainability       |
| `bash: pwsh *`              | allow | PowerShell for cross-platform scripts           |
| `bash: git *`               | allow | Git operations needed for workflow              |
| `bash: npm *`               | allow | npm/package.json for opencode tooling           |
| `skill: git-commit`         | deny  | Use rm-commit for controlled commits            |
| `skill: git-commit-push-pr` | deny  | Use rm-commit for controlled commits            |
| `external_directory`        | ask   | Ask before accessing outside repo               |

### Wildcard Patterns Over Explicit Commands

Use wildcard patterns like `dotnet *`, `pwsh *`, `git *` instead of explicit commands for maintainability. Explicit commands like `dotnet test`, `dotnet build`, `dotnet run`, etc. become brittle over time as new dotnet subcommands emerge.

**Before (brittle)**:

```json
"bash": {
  "dotnet test": "allow",
  "dotnet build": "allow",
  "dotnet restore": "allow",
  "dotnet clean": "allow",
  "dotnet run": "allow",
  "dotnet watch": "allow",
  "dotnet format": "allow"
}
```

**After (maintainable)**:

```json
"bash": {
  "dotnet *": "allow"
}
```

### Dual Configuration: Global + Local

Apply the same configuration to both:

1. **Global**: `~/.config/opencode/opencode.json` (user-wide)
2. **Local**: `{repo}/opencode.json` (repo-specific)

Local config takes precedence and allows repo-specific overrides while maintaining the same baseline protections.

### Shell Execution Differences

On **Windows** (shell = pwsh): Run PowerShell commands directly

```
Get-ChildItem | ForEach-Object { $_.Name }
```

On **omarchy** (shell = bash): Wrap PowerShell with single quotes

```
pwsh -NoProfile -Command 'Get-ChildItem | ForEach-Object { $_.Name }'
```

This matters because double-evaluation on Windows destroys `$` and `@` syntax.

## Why This Matters

1. **Prevents data loss**: Restrictive defaults prevent accidental deletion of critical files
2. **Maintains control**: `edit: ask` ensures human oversight before changes
3. **Protects secrets**: Environment files and secrets are explicitly denied
4. **Skill denials**: Prevents unauthorized commits; use rm-commit for controlled commits
5. **Cross-platform**: Wildcard patterns work across Windows (pwsh) and Linux/omarchy (bash)
6. **Maintainable**: Wildcards like `dotnet *` adapt to new commands without updates

## When to Apply

- Any new opencode configuration
- Repos with sensitive data (secrets, credentials, API keys)
- Teams wanting controlled agent behavior
- Cross-platform repos (Windows + Linux/omarchy)
- Repos using npm/package.json for tooling
- When creating global opencode config (`~/.config/opencode/opencode.json`)
- When creating local opencode config (`{repo}/opencode.json`)
