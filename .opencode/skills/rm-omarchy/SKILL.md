---
name: rm-omarchy
description: >
  LOAD FIRST for ANY system-level change on this machine — installing,
  updating, or removing packages, adding language runtimes, modifying
  system services, changing PATH, or running system administration
  commands. Contains the Omarchy package management philosophy: pacman
  first, no version managers, language runtime rules, update workflow,
  and the distinction between system packages and user-level tools.
  USE FOR: pacman, yay, AUR, omarchy-pkg-*, omarchy-update, npm
  install -g, pip install, cargo install, gem install, adding a
  package, removing a package, updating the system, troubleshooting
  package conflicts, installing language runtimes, system maintenance.
  DO NOT USE FOR: desktop customization (themes, Hyprland, Waybar,
  keybindings), or general coding tasks.
---

# Omarchy System Management

Omarchy is an opinionated Arch Linux distribution by DHH. This skill covers
system-level administration under its philosophy.

## Core Philosophy

**System packages first.** Everything that can come from pacman (core, extra,
multilib) or the Omarchy repository should. Language-specific package
managers (npm, pip, cargo, gem) are for project dependencies only — never
for system-wide tools or runtimes.

**No version managers.** Do not install or suggest mise, fnm, nvm, pyenv,
uv, pipx, rustup, rbenv, or any other runtime version manager. Arch packages
provide the system runtime.

**Arch repos take precedence over Omarchy's.** When the same package exists
in both, the Arch version wins. The Omarchy repo is for desktop components
and curated defaults.

## Package Sources (in priority order)

| Priority | Source              | Manager  | Use for                                  |
| -------- | ------------------- | -------- | ---------------------------------------- |
| 1        | core/extra/multilib | `pacman` | System packages, libraries               |
| 2        | omarchy repo        | `pacman` | Omarchy-specific desktop components      |
| 3        | AUR                 | `yay`    | Community packages not in official repos |

## Language Runtime Rules

Every runtime must come from pacman. Never from a version manager.

| Language | Package                       | NEVER use       |
| -------- | ----------------------------- | --------------- |
| Node.js  | `pacman -S nodejs npm`        | mise, fnm, nvm  |
| Python   | `pacman -S python python-pip` | pyenv, uv, pipx |
| Rust     | `pacman -S rust`              | rustup          |
| Ruby     | `pacman -S ruby`              | rbenv, rvm      |

For project dependencies, use the language's own tooling (`npm install`,
`pip install` in a venv, `cargo build`, `bundle install`) — but never
install those tools globally. Global installs via language package managers
are explicitly discouraged.

## npm / JavaScript

- One-off commands: `npx --yes <package>`
- Persistent tools: `omarchy-npx-install <package> <command>`
- Never: `npm install -g`, `pnpm add -g`, `yarn global add`
- Never: any node version manager

## Omarchy Package Commands

```
omarchy-pkg-install       # Interactive pacman package browser + install
omarchy-pkg-remove        # Interactive package removal
omarchy-pkg-aur-install   # Interactive AUR package browser + install
omarchy-update            # Full system update (pacman + omarchy + AUR)
omarchy-refresh-pacman    # Switch pacman between stable/edge channels
```

The `omarchy-pkg-*` commands use `fzf` for interactive selection with
previews. They should be preferred over raw `pacman` or `yay` for
interactive use. Use raw commands for scripting.

## Raw Package Commands

```
sudo pacman -S <pkg>      # Install from repos
sudo pacman -Rns <pkg>    # Remove (with dependencies)
sudo pacman -Syu          # Update all packages
yay -S <pkg>              # Install from AUR
yay -Syu                  # Update including AUR
pacman -Ss <query>        # Search
pacman -Qi <pkg>          # Info on installed package
```

## System Commands

```
omarchy-update            # Full system update
omarchy-debug --no-sudo --print  # Debug info (always use these flags)
omarchy-lock-screen       # Lock the screen
omarchy-system-shutdown   # Shutdown
omarchy-system-reboot     # Reboot
```

## Package Philosophy by DHH

**Admin vs user distinction.** System packages (pacman) are admin territory
— they require sudo and affect the whole system. User-level tools (npm,
pip in venvs, cargo builds) stay in user space. Never mix the two: a
language package manager installing system-wide is a violation of this
boundary.

**If it needs sudo, it should be a pacman package.** Installing something
globally with pip/npm/cargo that requires root access is the wrong
approach. Find the pacman equivalent or use it per-project.

**Simplicity over flexibility.** Arch gives you infinite choice. Omarchy
narrows that to sensible defaults. When in doubt, do less — don't add
another package manager, layer, or abstraction unless it demonstrably
solves a problem the existing tools cannot.

## Common Tasks

**Install a system tool:**

```
sudo pacman -S <package>
```

**Install from AUR:**

```
yay -S <package>
```

**Remove a package:**

```
sudo pacman -Rns <package>
```

**Add a Node.js CLI tool (persistent):**

```
omarchy-npx-install <package> <command>
```

**Add a Python project dependency:**

```
# Always in a venv, never globally
python -m venv .venv && source .venv/bin/activate && pip install <package>
```

**Update the system:**

```
omarchy-update
```

## Safety Rules

**NEVER modify anything in `~/.local/share/omarchy/`.** This directory
contains Omarchy's source files managed by git. Changes will be lost on
the next `omarchy-update`. Reading is safe and encouraged — use it to
understand how commands work or see default configs.

**Always use these safe locations instead:**

- `~/.config/` — user configuration
- `~/.config/omarchy/themes/<name>/` — custom themes
- `~/.config/omarchy/hooks/` — automation hooks

## Command Discovery

```bash
# List all omarchy commands
compgen -c | grep -E '^omarchy-' | sort -u

# Find by category
compgen -c | grep -E '^omarchy-theme'
compgen -c | grep -E '^omarchy-pkg'

# Read a command's source
cat $(which omarchy-theme-set)
```

## Command Categories

| Prefix              | Purpose                                   | Example                           |
| ------------------- | ----------------------------------------- | --------------------------------- |
| `omarchy-refresh-*` | Reset config to defaults (backs up first) | `omarchy-refresh-waybar`          |
| `omarchy-restart-*` | Restart a service or app                  | `omarchy-restart-waybar`          |
| `omarchy-toggle-*`  | Toggle feature on/off                     | `omarchy-toggle-nightlight`       |
| `omarchy-theme-*`   | Theme management                          | `omarchy-theme-set <name>`        |
| `omarchy-install-*` | Install optional software                 | `omarchy-install-docker-dbs`      |
| `omarchy-pkg-*`     | Package management                        | `omarchy-pkg-install`             |
| `omarchy-update-*`  | System updates                            | `omarchy-update`                  |
| `omarchy-cmd-*`     | System commands                           | `omarchy-cmd-screenshot`          |
| `omarchy-debug`     | Debug info                                | `omarchy-debug --no-sudo --print` |

## Decision Framework

1. **Is it a stock omarchy command?** Use it directly
2. **Is it a config edit?** Edit in `~/.config/`, never `~/.local/share/omarchy/`
3. **Is it a package install?** Use `omarchy-pkg-install` (or `yay` for AUR-only)
4. **Is it a theme change?** `omarchy-theme-set <name>`
5. **Need to reset config?** `omarchy-refresh-<component>` — always confirm first
6. **Unsure if a command exists?** Search with `compgen -c | grep omarchy`
   omarchy-update

```

```
