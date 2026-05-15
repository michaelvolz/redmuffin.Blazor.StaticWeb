---
name: rm-omarchy
description: >
  LOAD FIRST for ANY system-level change on this machine — installing,
  updating, or removing packages, adding language runtimes, modifying
  system services, changing PATH, or running system administration
  commands. Contains the Omarchy package management philosophy: pacman
  first for system packages, mise for development runtimes, language
  runtime rules, update workflow, and the distinction between system
  packages and user-level tools. Also covers supply chain security
  (Socket Firewall Free, sfw wrappers, shell functions), runtime guards
  (dotnet guard, DevSolution mappings, guard infrastructure), command
  discovery, safety rules for ~/.local/share/omarchy/, and the decision
  framework for package source selection.
  USE FOR: pacman, yay, AUR, omarchy-pkg-*, omarchy-update, npm
  install -g, pip install, cargo install, gem install, adding a
  package, removing a package, updating the system, troubleshooting
  package conflicts, installing language runtimes, system maintenance,
  supply chain security, sfw, socket firewall, runtime guard, dotnet
  guard, DevSolution, command discovery.
  DO NOT USE FOR: desktop customization (themes, Hyprland, Waybar,
  keybindings), or general coding tasks.
---

# Omarchy System Management

Omarchy is an opinionated Arch Linux distribution by DHH. This skill covers
system-level administration under its philosophy.

## User's Omarchy Stance

**This user strictly adheres to Omarchy philosophy, guidelines, and DHH's
design decisions.** When making system changes, you must follow Omarchy's
opinionated defaults — never override them unless the user explicitly
asks for an exception. System packages first. Mise for runtimes.
Omarchy defaults are the defaults for a reason.

---

## Core Philosophy

**System packages first.** Everything that can come from pacman (core, extra,
multilib) or the Omarchy repository should. Language-specific package
managers (npm, pip, cargo, gem) are for project dependencies only — never
for system-wide tools or runtimes.

**Mise for development runtimes.** Omarchy uses [Mise](https://mise.jdx.dev/)
to manage development language runtimes (Node.js, Ruby, Python, etc.).
System packages (pacman) handle OS-level tools; mise handles development
environments. As DHH puts it in the Omarchy Manual: "The majority of these
environments are managed by Mise."

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

Development runtimes are managed by [Mise](https://mise.jdx.dev/), which
is installed as a base pacman package. Use `mise use -g` to install and
set global defaults. For toolchain-level tools (like Rust), pacman is
used directly.

| Language | Install via               | NEVER use   |
| -------- | ------------------------- | ----------- |
| Node.js  | `mise use -g node@latest` | fnm, nvm    |
| Ruby     | `mise use -g ruby`        | rbenv, rvm  |
| Python   | `mise use -g python`      | pyenv, pipx |
| Rust     | `pacman -S rust`          | rustup      |

For project dependencies, use the language's own tooling (`npm install`,
`pip install` in a venv, `cargo build`, `bundle install`) — but never
install those tools globally. Never install globally via language
package managers.

## npm / JavaScript

- Runtime: `mise use -g node@latest`
- One-off commands: `npx --yes <package>`
- Persistent tools: `omarchy-npx-install <package> <command>`
- Never: `npm install -g`, `pnpm add -g`, `yarn global add`
- Never: fnm, nvm, or any node version manager other than mise

## Omarchy Package Commands

```
omarchy-pkg-install       # Interactive pacman package browser + install
omarchy-pkg-remove        # Interactive package removal
omarchy-pkg-aur-install   # Interactive AUR package browser + install
omarchy-update            # Full system update (pacman + omarchy + AUR)
omarchy-refresh-pacman    # Switch pacman between stable/edge channels
```

The `omarchy-pkg-*` commands use `fzf` for interactive selection with
previews. Never use raw `pacman` or `yay` for interactive package
management. Use raw commands for scripting only.

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

**If it needs sudo, it must be a pacman package.** Installing something
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

**Install a development runtime:**

```
mise use -g node@latest    # Node.js
mise use -g ruby           # Ruby
mise use -g python         # Python
```

**Add a Node.js CLI tool (persistent):**

```
omarchy-npx-install <package> <command>
```

(Wraps via mise-managed Node.js — no `npm install -g`.)

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
the next `omarchy-update`. Read it to understand how commands work or
see default configs.

**Never modify configs outside these safe locations:**

- `~/.config/` — user configuration
- `~/.config/omarchy/themes/<name>/` — custom themes
- `~/.config/omarchy/hooks/` — automation hooks

## Supply Chain Security (Socket Firewall)

You must route all `npm`, `npx`, and `pip` package installs through
Socket Firewall Free (`sfw`) — a zero-config binary that checks
packages against Socket's threat intelligence before allowing install.
Supply chain attacks through npm/pip have risen ~700% year-over-year.

**How it works:** `sfw` is a transparent proxy. Prefix any package
manager command with `sfw`:

```
sfw npm install <pkg>
sfw npx <tool>
sfw pip install <pkg>
```

**Installation:** Binary to `~/.local/bin/`, never `npm install -g`:

```
curl -L -o ~/.local/bin/sfw \
  https://github.com/SocketDev/sfw-free/releases/latest/download/sfw-free-linux-x86_64
chmod +x ~/.local/bin/sfw
```

**Shell functions** in `~/bash/fns/socket-firewall.sh` make this
transparent for interactive use — you type `npm install` as normal
and it routes through sfw automatically.

**Wrappers:** All `~/.local/bin/` wrappers generated by
`omarchy-npx-install` include `sfw` in the command chain. The
shadowed version of `omarchy-npx-install` at `~/.local/bin/` injects
`sfw` into the template automatically.

**OpenCode bash tool:** Non-interactive shells bypass shell functions.
Never issue bare `npm install`, `npx`, or `pip install` commands in
non-interactive shells; always prefix with `sfw` (e.g., `sfw npm install`).

**Bun:** Not supported by sfw. Mitigate with `npx @socketsecurity/cli scan .`
after `bun install`.

**What is NOT wrapped:** `pacman`/`yay` (signed packages), `mise`
(runtimes not packages), `cargo` (system tool via pacman), Python
CLI wrappers (pre-installed modules, no network install).

**Bypass:** `\npm install ...` or `command npm install ...` skips the
shell function when needed (e.g., sfw is down).

Full reference: `docs/solutions/security-issues/socket-firewall-omarchy-integration-2026-05-12.md`

**sfw binary updates:** Re-run the curl command periodically.
Add it to `topgrade` (aliased as `update-all`).

## Runtime Guards

Command-level pre-flight checks that block execution when runtime
conditions would cause crashes or hangs. Same architectural pattern
as Supply Chain Security: a shell function intercepts a command
before execution, checks a condition, and either blocks or passes
through. Both are **Guards** — the difference is the domain
(install-time malware vs. runtime process conflicts).

**Guard File:** `~/bash/fns/runtime-guards.sh` — single source of truth
for runtime guards. Supply chain guards live in `~/bash/fns/socket-firewall.sh`
— same architectural pattern, separate file. Both sourced in `.bashrc`
before the interactivity guard, covering all shells.

**DevSolution Guard Mapping:** The `_DOTNET_SOLUTION_GUARDS` associative
array in the guard file connects directory paths (DevSolutions) to
systemd service names. The dotnet guard only blocks when $PWD is inside
a registered DevSolution AND its corresponding service is running.
No overlapping DevSolution directories allowed.

**Active Guards:**

| Command  | DevSolution Directory                             | Service                                                | Rationale                                                |
| -------- | ------------------------------------------------- | ------------------------------------------------------ | -------------------------------------------------------- |
| `dotnet` | `/home/flynn/Projects/redmuffin.Blazor.StaticWeb` | `redmuffin.Blazor.StaticWeb-sass-dotnet-watch.service` | Running dev server + `dotnet watch/run` = crash and hang |

**Adding a DevSolution:** Add one line to `_DOTNET_SOLUTION_GUARDS` in
`~/bash/fns/runtime-guards.sh`. Format: `[directory]="service-name.service"`.
No other changes needed — the dotnet guard iterates the mapping.

**Adding a new guard:** Add a function to `~/bash/fns/runtime-guards.sh`.
A guard intercepts the command name, runs a cheap pre-flight check
(<10ms), and either blocks (`return 1` with message to stderr) or
passes through (`command <name> "$@"`).

**Escape hatch:** Prefix with `\` or `command` to bypass all guards.
`\dotnet build` runs the real dotnet regardless of dev server state.

Full reference: `CONTEXT.md` §Runtime Guards.

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

## References

Source material backing this skill:

- [Development Tools — The Omarchy Manual by DHH](https://learn.omacom.io/2/the-omarchy-manual/62/development-tools) — _"The majority of these environments are managed by Mise."_
- [omarchy-base.packages](https://github.com/basecamp/omarchy/blob/dev/install/omarchy-base.packages) — `mise` is a base package; `nodejs` is not
- [install/config/mise-work.sh](https://github.com/basecamp/omarchy/blob/dev/install/config/mise-work.sh) — `mise use -g node@latest` runs during Omarchy setup
- [bin/omarchy-npx-install](https://github.com/basecamp/omarchy/blob/dev/bin/omarchy-npx-install) — wrappers resolve Node.js via `mise where node@latest`

```

```
