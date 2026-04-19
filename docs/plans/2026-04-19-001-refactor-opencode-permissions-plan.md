---
title: Streamline opencode.json permissions using cc-safety-net
type: refactor
status: active
date: 2026-04-19
---

# Streamline opencode.json Permissions

## Overview

Simplify opencode.json permissions by leveraging cc-safety-net for bash command protection while keeping critical permissions that the plugin cannot handle.

## Problem Frame

Current opencode.json has ~25 permission rules. Many duplicate what cc-safety-net already blocks. The cc-safety-net plugin handles dangerous bash commands, but it cannot block read, edit, glob, grep, or bash tools directly.

## Requirements Trace

- R1. Minimal configuration
- R2. Leverage cc-safety-net for bash dangers
- R3. Protect sensitive files from ALL access
- R4. Cross-platform with ~ paths
- R5. No contradictions

## Scope Boundaries

- Does NOT modify OpenCode agent or SKILL configs
- Does NOT modify MCP server configs
- Focuses ONLY on permission section

---

## Files That Must Be DENIED (List 1 — ALL Access Blocked)

These paths block ALL access: read, edit, glob, grep, bash (cat, cp, mv, rm, scp, rsync, curl/wget to upload). Last rule wins. AI is completely blocked from these files.

### SSH / GPG / Crypto Keys

```
~/.ssh/**
~/.gnupg/**
~/.config/gnupg/**
```

### Cloud CLI & Auth Configs

```
~/.aws/**
~/.azure/**
~/.config/gcloud/**
~/.config/azure/**
~/.kube/**
~/.docker/**
~/.config/docker/**
```

### Password Managers & OS Secret Stores

```
~/.config/1Password/**
~/.config/Bitwarden/**
~/.local/share/keyrings/**
```

### Browser Credential Stores

```
~/AppData/Local/Google/Chrome/User Data/**
~/AppData/Local/Microsoft/Edge/User Data/**
~/.mozilla/**
```

### Shell Histories

```
~/.bash_history
~/.zsh_history
~/.config/fish/fish_history
~/.local/share/fish/fish_history
```

### User Data Libraries

```
~/Documents/**
~/OneDrive/Documents/**
~/Desktop/**
~/OneDrive/Desktop/**
~/Pictures/**
~/OneDrive/Pictures/**
~/Videos/**
~/OneDrive/Videos/**
~/Music/**
~/OneDrive/Music/**
~/Downloads/**
~/Favorites/**
~/Public/**
```

### Persistence / Autostart

```
~/.config/autostart/**
~/.config/systemd/user/**
```

### Git & Credentials

```
~/.gitconfig
~/.config/git/**
```

### OpenCode / Agent Self-Protection

```
~/.config/opencode/commands/**
~/.config/opencode/skills/**
~/.config/opencode/agents/**
~/.config/opencode/plugins/**
~/.config/opencode/modes/**
.opencode/commands/**
.opencode/skills/**
.opencode/agents/**
.opencode/plugins/**
```

### VSCode & Visual Studio

```
~/.config/Code/User/**
~/AppData/Roaming/Code/User/**
**/.vscode/tasks.json
**/.vscode/settings.json
**/.vscode/launch.json
~/AppData/Local/Microsoft/VisualStudio/**
~/AppData/Roaming/Microsoft/VisualStudio/**
```

### Git Hooks

```
**/.git/hooks/**
**/.git/config
```

### Linux Specific

```
~/.local/share/applications/**
```

---

## Files That Require ASK (List 2 — Your Admin Workflow)

These files require your explicit permission every time. AI will always prompt you. Place these AFTER List 1 so they override where needed.

### PowerShell Profiles

```
~/Documents/WindowsPowerShell/**
~/Documents/PowerShell/**
```

### Global Package Directories

```
~/AppData/Roaming/npm/**
~/.local/share/pnpm/global/**
/usr/local/lib/node_modules/**
~/.config/yarn/global/**
```

### OpenCode Config

```
~/.config/opencode/opencode.json
~/.config/opencode/tui.json
```

### System Admin

```
/etc/**
~/AppData/Local/Microsoft/WinGet/**
```

---

## cc-safety-net Plugin (What It Handles)

The cc-safety-net plugin intercepts dangerous bash commands:

| Command Type                                           | Blocked? |
| ------------------------------------------------------ | -------- |
| Git: reset --hard, clean -f, checkout --, push --force | YES      |
| Filesystem: rm -rf (outside cwd, root/home)            | YES      |
| Shell wrappers: bash -c, interpreter one-liners        | YES      |

## cc-safety-net Gap (What It CANNOT Handle)

| Tool               | Risk                                                         | Action               |
| ------------------ | ------------------------------------------------------------ | -------------------- |
| read               | Access sensitive files                                       | DENY List 1          |
| edit               | Modify files                                                 | DENY List 1          |
| glob               | File discovery                                               | DENY List 1          |
| grep               | Content search                                               | DENY List 1          |
| bash               | Shell commands (cat, cp, mv, rm, scp, rsync, curl upload...) | DENY List 1          |
| task               | Subagent launches                                            | ASK                  |
| external_directory | Workspace escape                                             | EXPLICIT             |
| doom_loop          | Runaway loops                                                | INTERNAL             |

---

## Key Technical Decisions

- read: DENY for List 1 paths only
- edit: DENY for List 1 paths only
- glob: DENY for List 1 paths only
- grep: DENY for List 1 paths only
- bash: DENY for List 1 paths only
- task: ALLOW for subagent launches
- external_directory: EXPLICIT
- doom_loop: INTERNAL — OpenCode handles internally
- cc-safety-net plugin: Handles dangerous bash commands
- One config for Linmux and Windows

---

## Implementation Units

- [ ] Unit 1: Audit Current Permissions
  - Compare existing opencode.json permissions against Lists 1 and 2
  - Document what already exists and what needs to change

- [ ] Unit 2: Add Missing Permissions
  - Add read deny rules for List 1 paths
  - Add edit deny rules for List 1 paths
  - Add glob deny rules for List 1 paths
  - Add grep deny rules for List 1 paths
  - Add bash deny rules for List 1 paths

- [ ] Unit 3: Add ASK Rules for Admin Workflow
  - Add edit ask rules for List 2 paths

- [ ] Unit 4: Verify JSON Valid
  - Run JSON validation on opencode.json
  - Confirm permissions apply correctly

## Files

- Modify: opencode.json

---

## Verification

Plan is complete when:

1. JSON is valid
2. List 1 paths blocked from read, edit, glob, grep, bash (ALL access)
3. List 2 paths require ask for edit
4. cc-safety-net plugin handles dangerous bash
5. No redundant rules, but Ask after Deny is valid.

## References

- cc-safety-net: https://github.com/kenryu42/claude-code-safety-net
