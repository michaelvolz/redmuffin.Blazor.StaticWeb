---
name: rm-dotfiles
description: >
  LOAD FIRST when evaluating untracked files in the dotfiles repo, after
  a git pull or git fetch from the dotfiles remote, after installing
  software that may create new config files, or when tracked files are
  auto-dirtying the working tree (chromium state, runtime databases,
  machine-specific prefs). Contains the portability classification engine
  that determines what to track or exclude, the file evaluation protocol,
  the change analysis protocol that scans incoming changes for cross-machine
  issues, and the tracked file audit that catches chronic auto-dirty files.
  USE FOR: dotfiles, gitignore, untracked files, portability, config
  tracking, git pull review, post-install scan, evaluating new files,
  adding config directories, what to track, what to exclude, auto-dirty
  tracked files, why are these files modified.
  DO NOT USE FOR: general git operations outside the dotfiles repo,
  package installation (use rm-omarchy), desktop customization.
---

# rm-dotfiles — Principle-First Dotfiles Management

The reasoning framework for the selective-omarchy-dotfiles repo. This
skill replaces guesswork on what to track or exclude in the
deny-by-default `.gitignore`. You consult it to evaluate untracked
files, classify them against portability categories, auto-exclude what's
harmful, auto-include what's portable, and ask when uncertain.

---

## What Belongs in This File

- **Viewpoint**: Reference information — constraints, categories, and
  protocols for portability classification. Not ordered workflow steps.
- **What belongs**: portability category definitions with concrete
  classification criteria, file evaluation protocol, change analysis
  rules, .gitignore discipline, handoff templates for uncertainty.
- **What does NOT belong**: implementation code, diagnostic command
  recipes, anything you already know how to do.

---

## Core Philosophy

### Default-YES

Everything in a whitelisted directory is a candidate for tracking. The
question is not "should this be tracked?" but **"does tracking this
cause harm on the other machine?"** If `signal + no-harm` holds, the
file stays. Exclusion requires a positive harm finding. The null
hypothesis is INCLUDE.

### The Harm Test

Never exclude a file solely because it can be regenerated; exclude only
when syncing causes harm on the other machine. Examples:

- **Pip entry point** (~150 bytes): trivially regenerable with
  `pip install`, but syncing causes ZERO harm and provides value as a
  record of installed tools → keep
- **Audio cookie** (`.config/pulse/cookie`): causes immediate runtime
  corruption on incompatible machine → exclude
- **Large binary** (13MB AppImage): bloat is real harm, may be
  platform-specific → exclude
- **Symlink**: 50 bytes, shows what tool/version was installed, zero
  harm even if dead on the other machine → keep

### .gitignore as Source of Truth

The `.gitignore` file IS the policy. This skill does not override or
duplicate it — it is the reasoning engine that explains _why_ each
pattern exists, evaluates new candidates against the portability
categories, proposes additions/exclusions with justification, and keeps
you from guessing. The skill encodes the thinking; the `.gitignore`
encodes the outcome.

---

## Machine Detection

The skill runs on two machines: WSL Arch (terminal-only, no compositor)
and an Omarchy Laptop (full Hyprland/Wayland GUI stack).

Detect which machine we're on:

1. Check `/proc/version` for "Microsoft" or "WSL" → WSL detected
2. Check `command -v hyprctl && hyprctl version 2>/dev/null` →
   Hyprland present → Laptop GUI detected

**What detection means for checks:**

- **WSL**: No compositor runs. GUI configs (Hyprland, Waybar, kitty,
  mako) are never consumed → machine-specific inert → no action.
  GUI-only tools (`hyprpm`, `hyprctl`, `waybar`) are installed but
  will always fail when invoked. Topgrade auto-detection is a
  consumption vector — it will try to run `hyprpm update` and fail.
- **Laptop**: Full GUI stack. All configs are active.

No manual configuration needed.

**Unknown machine fallback:** If neither WSL nor Laptop is detected
(`/proc/version` has no "Microsoft"/"WSL" and `hyprctl` is absent),
treat the machine as terminal-only (like WSL). Report a warning:
"Running on unknown machine — treating as terminal-only." GUI configs
are inert; no dangerous files are consumed.

---

## Portability Categories

Ordered decision checklist. Evaluate each file top-to-bottom. Stop at the
first match. If no category matches with 100% confidence → ask the user.
Never pick "best fit."

| #   | Category                       | Action       | Criteria                                                                                                                                                                                                                                                                                                                                                                               |
| --- | ------------------------------ | ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | **Transient artifact**         | Auto-exclude | ELF binary or AppImage detected by `file` command or magic bytes (`\x7fELF`). These cause repo bloat and may be platform-specific.                                                                                                                                                                                                                                                     |
| 2   | **Machine-specific dangerous** | Auto-exclude | File contains hardware-bound identifiers (`kb_device`, `input:`, `monitor=`), absolute paths that differ between machines, secrets patterns, or GUI configs consumed by any mechanism (shell init, systemd service, startup script, topgrade auto-detection, systemd timer, cron job) on a machine without a compositor. Detected by reading the file and grepping for known patterns. |
| 3   | **Machine-specific inert**     | No action    | GUI config (Hyprland, Waybar, kitty, mako, etc.) on WSL, AND not consumed by any mechanism: not sourced by shell init, not referenced by any systemd service, not executed at startup, not auto-detected by topgrade or similar updaters. Safe — the config is never consumed. Applies only when machine detection confirms WSL and no consumption mechanism is found.                 |
| 4   | **Universally portable**       | Auto-include | Text file with no dangerous signals. Pip entry points, npx wrappers, shell scripts, symlinks, configs without hardware references. If in a new directory: add whitelist patterns to `.gitignore`. If in an already-whitelisted dir: no `.gitignore` change needed.                                                                                                                     |

**Examples by category:**

- **Transient**: `rtk` (9.5MB ELF), `linuxdeploy-x86_64.AppImage` (13MB),
  `uv` (57MB ELF), `uvx` (346KB ELF)
- **Dangerous**: `.config/hypr/input.conf` (keyboard layout with
  `kb_layout = us,ru`), `.config/pulse/cookie` (runtime audio state),
  `.config/environment.d/` (API keys, tokens)
- **Inert**: `.config/hypr/` configs on WSL, `.config/waybar/` on WSL,
  `.config/mako/` on WSL — compositor never starts
- **Portable**: `.bashrc`, `.gitconfig`, pip entry points (~150B Python
  wrappers in `.local/bin/`), npx wrappers, symlinks, `.config/tmux/`,
  `.config/lazygit/`

**Uncertainty triggers — ask the user when:**

- `file` reports unknown, empty, or `data` format (cannot determine type)
- Text file matches some but not ALL dangerous patterns (partial match —
  e.g. has paths but no hardware IDs)
- Single file plausibly fits two conflicting categories (ambiguous signal)

---

## Consumption Mechanisms

To determine whether a GUI tool or config is **inert** (Category 3)
or **dangerous** (Category 2) on a terminal-only machine, check every
known consumption vector. Any single mechanism that invokes the tool
makes it dangerous. A tool with zero consumption mechanisms on this
machine is inert.

**Consumption vectors (checklist):**

1. **Shell init**: `grep -r <tool> ~/.bashrc ~/.zshrc ~/.profile` etc.
2. **Systemd user service**: `systemctl --user list-units | grep <tool>`
3. **Startup scripts**: `~/.config/autostart/`, `~/.xinitrc`, etc.
4. **Systemd timers**: `systemctl --user list-timers | grep <tool>`
5. **Cron jobs**: `crontab -l | grep <tool>`
6. **Topgrade auto-detection**: `topgrade --dry-run 2>&1 | grep <tool>`
   — topgrade probes for installed tools and invokes them as update
   steps. A GUI-only tool (e.g., `hyprpm`, `hyprctl`) detected by
   topgrade on WSL will fail because the compositor isn't running.
7. **Pacman hooks**: `grep -r <tool> /etc/pacman.d/hooks/` — some
   packages install hooks that run post-transaction.

**Topgrade override pattern:**

When topgrade auto-detects a GUI-only tool on WSL, the fix is a
`topgrade.d/` drop-in — a machine-specific config file that disables
the step. The `~/.config/topgrade.d/` directory is blocked by the
deny-by-default `*` catch-all, so files placed there never sync to
other machines.

```toml
# ~/.config/topgrade.d/hyprpm-disable.toml
# WSL-only — topgrade.d/ is excluded from dotfiles by deny-by-default.
# On the Laptop (where Hyprland runs), this file doesn't exist and
# hyprpm updates work normally.
[misc]
disable = ["hyprpm"]
```

This pattern generalizes: any topgrade step that requires a compositor
or GUI session can be disabled via a `topgrade.d/` drop-in without
affecting the synced `topgrade.toml` or the other machine.

---

## File Evaluation Protocol

Before classifying, determine what the file is:

1. **`stat <path>`** — check type (regular file, symlink, directory)
2. **`file <path>`** — if ELF binary or AppImage → transient artifact,
   stop. File size is irrelevant — a 50MB text document is portable if
   its content is portable. Only binary vs. text matters.
3. **If `file` reports text** (ASCII, UTF-8, JSON, XML, YAML) or the
   file is a symlink → read and classify by content
4. **Symlinks**: always portable signal — track them, unless they point
   to an excluded file (zero signal → exclude). Resolve to identify what
   tool/version they reference for documentation.
5. **Fallback when `file` absent**: check first 4 bytes for `\x7fELF`
   (binary), `#!` (script shebang), `\x89PNG` (image)

---

## .local/bin/ Rule

The `!.local/bin/**` whitelist is correct. It tracks:

- **Hand-authored scripts**: shell scripts, helper utilities
- **npx wrappers**: `exec mise exec node@latest -- npx --yes <pkg> "$@"`
- **Pip entry points**: ~150B Python wrappers that ARE the install
  manifest — they tell you what tools were `pip install`'d
- **Symlinks**: 50 bytes, shows what tool/version was installed

ELF binaries and AppImages in `.local/bin/` are excluded. This
precedent is established by the binary exclusions in `.gitignore`
(`rtk`, `uv`, `uvx`, `linuxdeploy-x86_64.AppImage`).

---

## Scan Modes

Three activation modes. All use the classification engine above.
Never execute `git pull` or `git fetch` in the dotfiles repo without
loading this skill. You must recognize the trigger from the skill
description — no user invocation needed. Post-install scans (Mode B) are triggered after software
installation.

### Mode A — Change Analysis (git pull / git fetch)

Triggered automatically when you execute `git pull` or
`git fetch` in the dotfiles repo. You MUST load this skill
immediately — see AGENTS.md Communication Protocol for authority.

1. **State check**: `git status --porcelain`. If `UU` (unmerged) →
   "Merge conflict — resolve first" and stop.
2. **Diff scan**: `git diff @{1}..HEAD --name-status` — files changed
   by the pull. Filter to tracked paths in whitelisted directories.
3. **Readiness checklist** for each changed path:
   - **Tool dependency scan (v2)**: For each tool in the
     [Cross-Machine Tool Check table](#cross-machine-tool-check-table)
     below: `grep -l <tool> <changed-files>`. If any changed file
     references the tool and the tool is absent
     (`command -v <tool> || pacman -Qi <pkg>`), this is a
     **compatibility gap** — the pull created a state where a tracked
     feature cannot run on this machine. Report the gap with the
     install path from the table. Tools managed by mise (`node`,
     `python`, `ruby`) and Omarchy base packages (`pacman`, `git`,
     `bash`, `mise`) are NOT checked — they are guaranteed on every
     Omarchy machine. npm packages referenced in npx wrappers are NOT
     checked — they are fetched at runtime by npx.
   - **WSL safety**: GUI configs are inert on WSL (compositor never
     starts) → no action
   - **Plugin dependency check**: For changed tracked `package.json`
     files under a `plugins/` directory: if the file has a
     `dependencies` field, cite `bun install --ignore-scripts`. A
     `package.json` with deps that arrived via pull IS a
     **compatibility gap** — dependencies are declared but not
     installed on this machine. The same command creates the lockfile
     if absent. Do NOT run without explicit instruction.
4. **Report findings and stop**. Produce a report with: portability
   classification per file, tool check results (gaps flagged), and
   resolution paths for any gaps. If the diff includes files in a new
   `plugins/` subdirectory or a changed `tui.json`, append: "Restart
   OpenCode to load new or updated plugins." Do NOT edit SKILL.md,
   `.gitignore`, or install packages — wait for explicit instruction.

### Mode B — Post-install scan and manual directory addition

Triggered after installing software (`pacman -S`, `pip install`, etc.)
or when the user asks to track a specific directory.

1. **Scan**: `git ls-files --others --exclude-standard` across
   whitelisted directories
2. **Classify** each untracked file using the portability categories:
   - **Transient/dangerous** → auto-exclude: add exclusion pattern to
     `=== EXCLUSIONS: ... ===` section with `# SKILL:` marker
   - **Inert** → no action
   - **Universally portable** → if directory not yet whitelisted:
     add `!.config/NewDir/` and `!.config/NewDir/**` to the whitelist
     section. Then scan the new directory for files needing carve-out
     exclusions (logs, caches, machine-specific sub-files) and add
     exclusion patterns with `# SKILL:` markers. Same pattern as Typora
     and Chromium in the existing `.gitignore`. If already whitelisted:
     no `.gitignore` change needed
   - **Uncertain** → ask the user. Normal conversation.
3. **Manual directory**: when user asks to track a directory, evaluate
   portability, add whitelist patterns, identify machine-specific
   carve-outs (like `hypr/input.conf`), add exclusions for them.
4. **Happy path**: everything certain → report summary and stop.
   **Uncertainty path**: normal conversation until resolved.

### Mode C — Tracked File Audit (auto-dirty detection)

Triggered when `git status` shows modified tracked files that the user
did not intentionally change. These are files that auto-dirty on every
session — browser state, runtime databases, machine-specific prefs.

1. **Detect**: `git status --porcelain` → find tracked files with `.M`
   (working-tree modified) status. Filter out files the user knows they
   changed (commits, intentional edits).
2. **Classify** each dirty tracked file against portability categories:
   uses the same `stat → file → classify` pipeline as Mode B.
   - **Transient/dangerous** → the file should not be tracked at all.
     Report it, propose: remove whitelist pattern from `.gitignore`,
     add exclusion pattern with `# SKILL:` marker, `git rm --cached`.
   - **Inert/portable** → the file is legitimately tracked but auto-
     changed. Report it with classification — no action unless user
     wants to de-track it.
3. **Resolution**: for files that should be de-tracked, follow the same
   `.gitignore` discipline as Mode B. Remove from whitelist, add to
   exclusions, `git rm --cached`, and for permanent removal from history
   use `git filter-repo` (requires force push — inform user).
4. **Chronic offenders**: directories like `.config/chromium/` where
   only 2 of 8 tracked files are portable. Flag as a directory-level
   problem — consider replacing file-specific whitelists with narrower
   patterns and broader exclusions.

Trigger phrases: "why are these files dirty", "clean up tracked files",
"what's modifying these", or any `git status` showing unexplained
modified tracked files.

---

## Installed Tool Inventory

Complete inventory of non-system tools installed on this machine.
Omarchy base packages (pacman, git, bash, mise, node, python) are
excluded — they are guaranteed on every Omarchy machine.

**How to read**: Tools marked `(hard dep)` are required by tracked
files. Missing tools produce compatibility gaps during Change Analysis.
Tools marked `(runtime)` are npm/Python packages fetched at runtime by
npx/pip — no check needed.

### System-level tools (pacman / AUR / binary)

| Tool       | Type                 | Install                        | Notes                                                                                         |
| ---------- | -------------------- | ------------------------------ | --------------------------------------------------------------------------------------------- |
| `sfw`      | Security (hard dep)  | curl binary to `~/.local/bin/` | Socket Firewall Free — supply chain protection for all npm/pip installs                       |
| `bun`      | Runtime (hard dep)   | `sudo pacman -S bun`           | JavaScript runtime, used to build git-sidebar plugin                                          |
| `chromium` | Browser (hard dep)   | `sudo pacman -S chromium`      | Required by chrome-devtools-mcp and `PUPPETEER_EXECUTABLE_PATH`                               |
| `docker`   | Container (hard dep) | `sudo pacman -S docker`        | Required by devcontainer wrapper and docker MCP commands                                      |
| `dotnet`   | Runtime (hard dep)   | `sudo pacman -S dotnet-sdk`    | Required by dotnet MCP server in opencode.jsonc                                               |
| `code`     | Editor (soft dep)    | Platform-dependent             | WSL: Windows VS Code; Laptop: `sudo pacman -S code`. Used by `EDITOR` and `~/.local/bin/code` |

### npm tools (via npx wrappers in ~/.local/bin/)

All installed by `omarchy-npx-install <pkg> <cmd>`. Fetched at runtime
by npx — no check needed during Change Analysis. Listed for inventory.

| Wrapper                | npm Package                       | Category        |
| ---------------------- | --------------------------------- | --------------- |
| `acp`                  | `opencode-ai`                     | OpenCode CLI    |
| `bash-language-server` | `bash-language-server`            | LSP             |
| `brave-search`         | `brave-search`                    | MCP server      |
| `cc-safety-net`        | `cc-safety-net`                   | Security plugin |
| `chrome-devtools`      | `chrome-devtools`                 | MCP server      |
| `chrome-devtools-mcp`  | `chrome-devtools-mcp`             | MCP server      |
| `codex`                | `codex`                           | CLI             |
| `commitlint`           | `@commitlint/cli`                 | Git hooks       |
| `commitlint-config`    | `@commitlint/config-conventional` | Git hooks       |
| `context-mode`         | `context-mode`                    | MCP server      |
| `copilot`              | `copilot`                         | CLI             |
| `devcontainer`         | `@devcontainers/cli`              | Containers      |
| `gemini`               | `gemini`                          | CLI             |
| `opencode`             | `opencode-ai`                     | OpenCode CLI    |
| `opencode-snippets`    | `opencode-snippets`               | OpenCode plugin |
| `pi`                   | `pi`                              | CLI             |
| `playwright-cli`       | `playwright`                      | Testing         |
| `prettier`             | `prettier`                        | Formatter       |
| `swa`                  | `swa`                             | CLI             |

### Python tools (via pip entry points in ~/.local/bin/)

Installed via `pip install` in venvs or user site. Import wrappers —
Python is the only hard dep. Listed for inventory.

| Wrapper           | Package           | Category            |
| ----------------- | ----------------- | ------------------- |
| `cyclopts`        | `cyclopts`        | CLI framework       |
| `dotenv`          | `python-dotenv`   | Environment         |
| `docutils`        | `docutils`        | Documentation       |
| `email_validator` | `email-validator` | Validation          |
| `fastmcp`         | `fastmcp`         | MCP framework       |
| `httpx`           | `httpx`           | HTTP client         |
| `jsonschema`      | `jsonschema`      | Validation          |
| `keyring`         | `keyring`         | Credentials         |
| `markdown-it`     | `markdown-it-py`  | Markdown parser     |
| `mcp`             | `mcp`             | MCP framework       |
| `pygmentize`      | `pygments`        | Syntax highlighting |
| `typer`           | `typer`           | CLI framework       |
| `uvicorn`         | `uvicorn`         | ASGI server         |
| `watchmedo`       | `watchdog`        | File watcher        |
| `websockets`      | `websockets`      | WebSocket library   |

### MCP servers (opencode.jsonc)

Configured in `~/.config/opencode/opencode.jsonc`. OpenCode reports
missing servers at startup — no explicit check needed during Change
Analysis. Listed for inventory.

| Server              | Command                        | Type   |
| ------------------- | ------------------------------ | ------ |
| brave-search        | `node .../brave-search`        | npm    |
| context7            | `node .../context7`            | npm    |
| sequential-thinking | `node .../sequential-thinking` | npm    |
| chrome-devtools     | `node .../chrome-devtools-mcp` | npm    |
| dotnet              | `dotnet <dll>`                 | system |
| prettier            | `prettier`                     | npm    |
| docker              | `docker`                       | system |
| context-mode        | `context-mode`                 | npm    |
| socket              | `node .../socket`              | npm    |

---

## Cross-Machine Tool Check Table

During Change Analysis, for each tool below: grep changed files for
the tool name, check availability, report gap if missing.

Tools that ARE listed here: user-installed, referenced by tracked
files, would cause failures if absent on the other machine.
NOT listed: Omarchy base packages, mise-managed runtimes,
npm/Python packages (fetched at runtime).

| Tool       | Install command                                                                                                                                 | Tracked files that reference it                                                                           |
| ---------- | ----------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------- |
| `sfw`      | `curl -L -o ~/.local/bin/sfw https://github.com/SocketDev/sfw-free/releases/latest/download/sfw-free-linux-x86_64 && chmod +x ~/.local/bin/sfw` | `~/.config/omarchy/sfw/bin/*`, `~/bash/fns/socket-firewall.sh`, 19 `~/.local/bin/*` wrappers, `~/.bashrc` |
| `bun`      | `sudo pacman -S bun`                                                                                                                            | `~/.config/opencode/plugins/rm-git-sidebar/scripts/build.ts`, `~/.bashrc` (PATH prepend)                  |
| `chromium` | `sudo pacman -S chromium`                                                                                                                       | `~/.bashrc` (`PUPPETEER_EXECUTABLE_PATH`), `~/.local/bin/chrome-devtools-mcp`                             |
| `docker`   | `sudo pacman -S docker`                                                                                                                         | `~/.config/opencode/opencode.jsonc` (MCP), `~/.local/bin/devcontainer`                                    |
| `dotnet`   | `sudo pacman -S dotnet-sdk`                                                                                                                     | `~/.config/opencode/opencode.jsonc` (MCP)                                                                 |

---

## .gitignore Discipline

Hard rules for writing to `.gitignore`:

1. **Never append .gitignore patterns outside their designated section
   boundaries.** Whitelists go only into `=== WHITELIST: ... ===`
   sections, exclusions only into `=== EXCLUSIONS: ... ===` sections.
   Never insert between the `*` catch-all and `!` whitelists — this
   corrupts the deny-by-default architecture.
2. **Every skill-generated rule** carries a `# SKILL: <category>`
   prefix marker for auditability.
3. **Never commit a .gitignore change without first validating the
   pattern**: `git check-ignore -v <path>` to confirm the new pattern
   works as intended.
4. **Recovery**: if a wrong exclusion is committed,
   `git checkout .gitignore` reverts to the last committed state.
5. **Never use a file-specific exclusion glob when a directory-based
   pattern covers the same target without collateral exclusion.** In the
   `=== EXCLUSIONS ===` section, prefer directory-based patterns
   (`**/<dirname>/`) over file-specific globs (`**/<filename>`). Directory globs target categories (caches, logs, deps) that are
   never wanted. File-specific globs block the named file in every
   whitelisted directory — including plugin and component subdirectories.
   When a specific file must be excluded, use a path-qualified pattern
   (e.g., `.config/opencode/package.json`) instead of `**/package.json`.

---

## BOUNDARIES

### ASK FIRST

- Any file that does not cleanly match a portability category with
  100% certainty. Never guess.

### NEVER

- Insert patterns between the `*` catch-all and `!` whitelists in
  `.gitignore`. This corrupts the deny-by-default architecture.
- Commit secrets, API keys, or tokens.
- Restructure existing `.gitignore` whitelists without explicit
  permission.

---

## References

- `docs/brainstorms/rm-dotfiles-skill-requirements.md` — requirements
- `docs/solutions/tooling-decisions/rm-dotfiles-skill-principle-first-design-2026-05-06.md` — design doc
- `docs/solutions/tooling-decisions/dotfiles-gitignore-file-specific-glob-trap-2026-05-08.md` — pattern rule: file-specific globs vs directory globs
- `.config/opencode/skills/rm-omarchy/SKILL.md` — package-check commands
