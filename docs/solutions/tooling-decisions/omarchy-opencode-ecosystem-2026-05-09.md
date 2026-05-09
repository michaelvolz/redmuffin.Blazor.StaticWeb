---
title: rm-omarchy and rm-opencode Foundational Skills
date: 2026-05-09
category: tooling-decisions
module: opencode
problem_type: tooling_decision
component: development_workflow
severity: high
applies_when:
  - Installing or removing system packages on an Omarchy machine
  - Modifying OpenCode configuration, skills, agents, or MCP servers
  - Debugging OpenCode session history or plugin issues
tags: [omarchy, opencode, package-management, pacman, mise, skills, agents]
---

# rm-omarchy and rm-opencode Foundational Skills

## Context

Two domains required authoritative reference skills that every agent session should consult before acting:

1. **System-level changes** (installing packages, adding runtimes, modifying services) on an Arch/Omarchy machine have specific rules: pacman first for system packages, mise for development runtimes, strict separation of system vs user-level tools.
2. **OpenCode internals** (file locations, session database schema, config patterns, skill/agent conventions) have no public documentation — configuration mistakes silently break the harness.

Without these skills loaded, agents would use incorrect package managers or modify OpenCode configs in breaking ways.

## Guidance

**rm-omarchy** — loads first for ANY system-level change. Contains:

- Pacman-first philosophy for system packages, yay for AUR
- mise for development runtimes (Node, Python, Ruby, etc.)
- Update workflow (`omarchy-update`)
- Distinction between system packages and user-level tools
- Never use for desktop customization (themes, Hyprland, Waybar)

**rm-opencode** — loads first when modifying OpenCode itself. Contains:

- Single source of truth for all OpenCode file locations
- Session database schema and query SQL
- Config patterns (`opencode.jsonc`, `tui.json`)
- Skill/agent naming conventions
- Plugin installation and troubleshooting

Both skills live at `.opencode/skills/rm-omarchy/` and `.opencode/skills/rm-opencode/` respectively, following the `rm-*` top-level naming convention for triggered workflows.

## Why This Matters

Without `rm-omarchy`, agents on this machine would use `apt` (wrong), `pip install --user` (wrong for some cases), or `npm install -g` without the 7-day release age filter. Without `rm-opencode`, agents would place skills in wrong directories, use wrong config formats, or corrupt the session database.

These are load-once-per-domain skills — an agent loads them at the start of relevant work and the knowledge persists for the session.

## When to Apply

- **rm-omarchy**: Before ANY `pacman`, `yay`, `npm install -g`, `pip install`, `cargo install`, `gem install`, or system service modification
- **rm-opencode**: Before modifying `opencode.jsonc`, `AGENTS.md` agent blocks, skill files, MCP server configs, or debugging session history

## Related

- `.opencode/skills/rm-omarchy/SKILL.md` — Full Omarchy philosophy and package rules
- `.opencode/skills/rm-opencode/SKILL.md` — Complete OpenCode internals reference
- `.opencode/skills/rm-dotfiles/SKILL.md` — Dotfiles repo management (loaded separately)
