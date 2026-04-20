# Package Management Alignment with DHH/Omarchy Principles

**Date:** 2026-04-20
**Author:** User (with OpenCode assistance)
**System:** WSL Arch Linux (Omarchy remix)

## Problem

Windows randomly installed npm packages into WSL, creating shadow versions that conflicted with our setup. Additionally, the system had non-Omarchy-compliant version managers installed.

## Principles Established

Based on DHH's (David Heinemeier Hansson) guidance from Omarchy:

1. **Use Pacman/AUR** for system packages - aligns with Omarchy philosophy
2. **Use Arch packages** for node/npm (NOT mise, fnm, nvm)
3. **Use npx** for one-off npm commands
4. **Use omarchy-npx-install** pattern (npx lazy-loading) for persistent npm tools

## Audit Results

### Before Cleanup

| Component           | Status                              |
| ------------------- | ----------------------------------- |
| node/npm via mise   | YES - non-compliant                 |
| node/npm via pacman | YES - correct (shadowed)            |
| fnm                 | YES - non-compliant version manager |
| Global npm packages | 7 installed via mise                |

### Removed

- Mise node installation (`~/.local/share/mise/installs/node/`)
- fnm (`~/.local/share/fnm/`)
- Stale fnm temp dirs (`/run/user/1000/fnm_multishells/`)
- Stale mise shims

### Installed (DHH-Compliant Pattern)

Replaced global npm installs with npx wrappers in `~/.local/bin/`:

| Command               | Package                         |
| --------------------- | ------------------------------- |
| `swa`                 | @azure/static-web-apps-cli      |
| `commitlint`          | @commitlint/cli                 |
| `commitlint-config`   | @commitlint/config-conventional |
| `devcontainer`        | @devcontainers/cli              |
| `cc-safety-net`       | cc-safety-net                   |
| `chrome-devtools-mcp` | chrome-devtools-mcp             |
| `prettier`            | prettier                        |

### Retained (Correct)

| Component                          | Source                    |
| ---------------------------------- | ------------------------- |
| node v25.9.0                       | Pacman (nodejs 25.9.0-1)  |
| npm 11.12.1                        | Pacman (npm 11.12.1-1)    |
| rust 1.94.1                        | Pacman (rust 1:1.94.1-1)  |
| mise (tool version switching only) | Pacman (mise 2026.3.17-1) |

## Why This Matters

1. **Consistency** - Omarchy uses pacman; deviating creates maintenance burden
2. **Simplicity** - Single package manager for system = easier upgrades/rollbacks
3. **Update alignment** - Omarchy auto-updates; external version managers conflict
4. **DHH's intent** - He designed omarchy-npx-install specifically for npm tools that update frequently

## Usage

One-off:

```bash
npx --yes <package>
```

Persistent tools (DHH's method):

```bash
# Creates wrapper in ~/.local/bin/
omarchy-npx-install <package> <command-name>
# Or manually:
echo '#!/bin/bash' > ~/.local/bin/<cmd>
echo 'exec npx --yes <package> "$@"' >> ~/.local/bin/<cmd>
chmod +x ~/.local/bin/<cmd>
```

## Non-NPM Tools

Some tools aren't on npm. For these, use their native install method + create an update wrapper.

### rtk (Token Killer)

AI context optimizer - installed via quick install script.

| File                           | Purpose                                       |
| ------------------------------ | --------------------------------------------- |
| `~/.local/bin/rtk`             | Main binary                                   |
| `~/.local/bin/rtk-update`      | Manual update                                 |
| `~/.local/lib/auto-updates.sh` | Auto-update script (runs on login, 24h check) |

Auto-update behavior:

- Runs on login shell only (not subshells)
- Checks every 24 hours
- Updates silently in background

Manual update:

```bash
rtk-update
```

Current version: 0.37.1

## References

- Omarchy: https://omarchy.org
- DHH's npx wrapper: https://github.com/basecamp/omarchy/commit/e294394
- DHH's guidance on NPM vs Pacman: Omarchy issue #4509
