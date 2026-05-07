---
name: rm-dotfiles
description: >
  LOAD FIRST when evaluating untracked files in the dotfiles repo, after
  a git pull or git fetch from the dotfiles remote, or after installing
  software that may create new config files. Contains the portability
  classification engine that determines what to track or exclude, the
  file evaluation protocol, and the change analysis protocol that scans
  incoming changes for cross-machine issues.
  USE FOR: dotfiles, gitignore, untracked files, portability, config
  tracking, git pull review, post-install scan, evaluating new files,
  adding config directories, what to track, what to exclude.
  DO NOT USE FOR: general git operations outside the dotfiles repo,
  package installation (use rm-omarchy), desktop customization.
---

# rm-dotfiles — Principle-First Dotfiles Management

The reasoning framework for the selective-omarchy-dotfiles repo. This
skill replaces agent guesswork on what to track or exclude in the
deny-by-default `.gitignore`. Agents consult it to evaluate untracked
files, classify them against portability categories, auto-exclude what's
harmful, auto-include what's portable, and ask when uncertain.

---

## What Belongs in This File

- **Viewpoint**: Reference information — constraints, categories, and
  protocols agents apply. Not ordered workflow steps.
- **What belongs**: portability category definitions with concrete
  classification criteria, file evaluation protocol, change analysis
  rules, .gitignore discipline, handoff templates for uncertainty.
- **What does NOT belong**: implementation code, diagnostic command
  recipes, anything the model already knows how to do.

---

## Core Philosophy

### Default-YES

Everything in a whitelisted directory is a candidate for tracking. The
question is not "should this be tracked?" but **"does tracking this
cause harm on the other machine?"** If `signal + no-harm` holds, the
file stays. Exclusion requires a positive harm finding. The null
hypothesis is INCLUDE.

### The Harm Test

Judge by whether syncing causes damage, NOT by whether the file can be
regenerated. Examples:

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
agents from guessing. The skill encodes the thinking; the `.gitignore`
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
- **Laptop**: Full GUI stack. All configs are active.

No manual configuration needed.

---

## Portability Categories

Ordered decision checklist. Evaluate each file top-to-bottom. Stop at the
first match. If no category matches with 100% confidence → ask the human.
Never pick "best fit."

| #   | Category                       | Action       | Criteria                                                                                                                                                                                                                                                           |
| --- | ------------------------------ | ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| 1   | **Transient artifact**         | Auto-exclude | ELF binary or AppImage detected by `file` command or magic bytes (`\x7fELF`). These cause repo bloat and may be platform-specific.                                                                                                                                 |
| 2   | **Machine-specific dangerous** | Auto-exclude | File contains hardware-bound identifiers (`kb_device`, `input:`, `monitor=`), absolute paths that differ between machines, or secrets patterns — detected by reading the file and grepping for known patterns.                                                     |
| 3   | **Machine-specific inert**     | No action    | GUI config (Hyprland, Waybar, kitty, mako, etc.) when the compositor doesn't run here (WSL). Safe — the config is never consumed. Applies only when machine detection confirms WSL.                                                                                |
| 4   | **Universally portable**       | Auto-include | Text file with no dangerous signals. Pip entry points, npx wrappers, shell scripts, symlinks, configs without hardware references. If in a new directory: add whitelist patterns to `.gitignore`. If in an already-whitelisted dir: no `.gitignore` change needed. |

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

---

## File Evaluation Protocol

Before classifying, determine what the file is:

1. **`stat <path>`** — check type (regular file, symlink, directory)
2. **`file <path>`** — if ELF binary or AppImage → transient artifact,
   stop. File size is irrelevant — a 50MB text document is portable if
   its content is portable. Only binary vs. text matters.
3. **If `file` reports text** (ASCII, UTF-8, JSON, XML, YAML) or the
   file is a symlink → read and classify by content
4. **Symlinks**: always portable signal — track them. Resolve to
   identify what tool/version they reference for documentation.
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

Two activation modes. Both use the classification engine above.

### Mode A — Auto-activate on git pull

Triggered when the agent pulls or fetches from the dotfiles remote.

1. **State check**: `git status --porcelain`. If `UU` (unmerged) →
   "Merge conflict — resolve first" and stop.
2. **Diff scan**: `git diff @{1}..HEAD --name-status` — files changed
   by the pull. Filter to tracked paths in whitelisted directories.
3. **Readiness checklist** for each changed path:
   - **Tool check**: `command -v <tool>` or `pacman -Qi <pkg>`
   - **WSL safety**: GUI configs are inert on WSL (compositor never
     starts) → no action
4. **Report per path**. If nothing changed → "no changes requiring
   action."

### Mode B — Post-install scan and manual directory addition

Triggered after installing software (`pacman -S`, `pip install`, etc.)
or when the user asks to track a specific directory.

1. **Scan**: `git ls-files --others --exclude-standard` across
   whitelisted directories
2. **Classify** each untracked file using the portability categories:
   - **Transient/dangerous** → auto-exclude: add exclusion pattern to
     `## EXCLUSIONS` section with `# SKILL:` marker
   - **Inert** → no action
   - **Universally portable** → if directory not yet whitelisted:
     add `!.config/NewDir/` and `!.config/NewDir/**` to the whitelist
     section. If already whitelisted: no `.gitignore` change needed
   - **Uncertain** → ask the human. Normal conversation.
3. **Manual directory**: when user asks to track a directory, evaluate
   portability, add whitelist patterns, identify machine-specific
   carve-outs (like `hypr/input.conf`), add exclusions for them.
4. **Happy path**: everything certain → report summary and stop.
   **Uncertainty path**: normal conversation until resolved.

---

## .gitignore Discipline

Hard rules for writing to `.gitignore`:

1. **Append whitelists only to WHITELIST sections**, exclusions only to
   `## EXCLUSIONS`. Never insert between the `*` catch-all and `!`
   whitelists — this corrupts the deny-by-default architecture.
2. **Every skill-generated rule** carries a `# SKILL: <category>`
   prefix marker for auditability.
3. **Validate before committing**: `git check-ignore -v <path>` to
   confirm the new pattern works as intended.
4. **Recovery**: if a wrong exclusion is committed,
   `git checkout .gitignore` reverts to the last committed state.

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
- `.config/opencode/skills/rm-omarchy/SKILL.md` — package-check commands
