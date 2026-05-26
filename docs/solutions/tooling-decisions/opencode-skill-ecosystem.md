---
date: 2026-05-09
title: OpenCode Skill Ecosystem
tags: [opencode, skills, ecosystem]
problem_type: architecture
---

# OpenCode Skill Ecosystem

## Design Constraints

This document is the canonical reference for OpenCode's multi-vendor skill ecosystem: how skills are discovered, organized, ported, and integrated across agent harnesses. It covers Claude Code porting patterns, foundational system skills (rm-omarchy, rm-opencode), third-party vendor integration, and the invariants that keep the ecosystem coherent.

What belongs: skill discovery mechanics, directory layout, vendor namespace conventions, porting guidance, foundational references, integration decisions.

What does NOT belong: individual skill content, specific package install instructions, desktop customization, or any workflow that isn't about skill ecosystem management.

---

## Skill Discovery

OpenCode's skill discovery uses `{skill,skills}/**/SKILL.md` globstar — **unlimited recursive depth**. Confirmed from OpenCode source at `packages/opencode/src/skill/index.ts`.

This means any nesting scheme works:

- Single-level: `~/.config/opencode/skills/my-skill/SKILL.md`
- 2-level: `~/.config/opencode/skills/compound-engineering/ce-brainstorm/SKILL.md`
- 3-level: `~/.config/opencode/skills/matt-pocock/engineering/tdd/SKILL.md`
- Deeper: `~/.config/opencode/skills/vendor/cursor/thermo-nuclear-code-quality-review/SKILL.md`

The only requirement is that `SKILL.md` lives in a named directory somewhere under the skills root.

---

## Directory Layout

Skills live at `~/.config/opencode/skills/` and agents at `~/.config/opencode/agents/`. The ecosystem uses four vendor namespaces:

| Namespace            | Prefix           | Count | Source                                           |
| -------------------- | ---------------- | ----- | ------------------------------------------------ |
| Redmuffin workflows  | `rm-*`           | ~20   | Built in-house                                   |
| Compound Engineering | `ce-*`           | ~11   | Ported from Claude Code                          |
| Matt Pocock          | `matt-pocock/`   | 7     | Third-party integration                          |
| Vendor/cursor        | `vendor/cursor/` | 1     | Third-party (thermo-nuclear-code-quality-review) |

Namespaces are independent — no prefix collisions, no shared directories. Adding a new vendor means creating a top-level directory at the same level as the others.

---

## Claude Code Porting Patterns

The compound-engineering skill pack was originally built for Claude Code. Porting to OpenCode required adapting CC-specific conventions while preserving all workflows. The following adaptations apply to any CC→OpenCode port:

**Model references.** CC model names (`sonnet`, `opus`, `haiku`) map to OpenCode agent archetypes (`rm-brilliant`, `rm-karpathy`, `rm-rigurous`). Agent dispatch updated from `@compound-engineering/ce-*` (CC syntax) to `ce-*` subagent types (OpenCode syntax).

**Tool mapping.**

- CC `Task` → OpenCode `task` with `subagent_type` parameter
- CC `Skill` → OpenCode `skill`
- CC `AskUserQuestion` → OpenCode `question`

**File paths.** CC stores skills in `~/.claude/skills/`; OpenCode uses `~/.config/opencode/skills/`. All path references must be updated.

**Plugin coexistence.** The `ce-*` subagent types coexist with `rm-*` agents and vendor agents. Each namespace is independent.

**Port principle.** Well-structured skills can move between agent harnesses with primarily mechanical changes (paths, tool names, model references). The workflow logic — research → analyze → assemble → write — is harness-agnostic. Keep skill logic harness-agnostic and isolate harness-specific references to a single config layer.

---

## Foundational System Skills

Two domains require authoritative reference skills that every agent session should consult before acting. These are load-once-per-domain — an agent loads them at the start of relevant work and the knowledge persists for the session.

### rm-omarchy

Loads first for ANY system-level change. Contains:

- Pacman-first philosophy for system packages, yay for AUR
- mise for development runtimes (Node, Python, Ruby, etc.)
- Update workflow (`omarchy-update`)
- Distinction between system packages and user-level tools
- Never use for desktop customization (themes, Hyprland, Waybar)

Without `rm-omarchy`, agents on this machine would use `apt` (wrong), `pip install --user` (wrong for some cases), or `npm install -g` without the 7-day release age filter.

**Apply before:** ANY `pacman`, `yay`, `npm install -g`, `pip install`, `cargo install`, `gem install`, or system service modification.

### rm-opencode

Loads first when modifying OpenCode itself. Contains:

- Single source of truth for all OpenCode file locations
- Session database schema and query SQL
- Config patterns (`opencode.jsonc`, `tui.json`)
- Skill/agent naming conventions
- Plugin installation and troubleshooting

Without `rm-opencode`, agents would place skills in wrong directories, use wrong config formats, or corrupt the session database.

**Apply before:** modifying `opencode.jsonc`, `AGENTS.md` agent blocks, skill files, MCP server configs, or debugging session history.

---

## Matt Pocock Integration

The Matt Pocock skill pack provides 7 engineering and productivity skills. They are organized in a 3-level nested structure under `matt-pocock/`:

```
~/.config/opencode/skills/matt-pocock/
├── engineering/       # grill-with-docs, improve-codebase-architecture, prototype
├── productivity/      # grill-me, handoff, write-a-skill
└── misc/
```

**Key decisions:**

- No conversion needed for 6 of 7 skills. OpenCode's globstar discovery handles any depth natively.
- `git-guardrails-claude-code` is Claude-specific. Equivalent functionality exists in our `block-push.js` plugin.
- The 3-level nesting proves OpenCode's skill discovery is truly depth-agnostic.
- Multi-vendor layout scales to any number of vendors without namespace collisions.

---

## When to Apply

- **Porting a CC plugin:** Follow the model/tool/path mapping patterns above. Test globstar discovery before assuming a nesting change is needed.
- **Adding a new vendor:** Create a top-level directory under `~/.config/opencode/skills/`. No conversion needed unless the skill references CC-specific tool names or model names.
- **Modifying OpenCode config:** Load `rm-opencode` first.
- **Installing system packages:** Load `rm-omarchy` first.
- **Evaluating skill compatibility:** Most skills are harness-agnostic. Only check for CC-specific references (model names, tool names, `~/.claude/` paths).
