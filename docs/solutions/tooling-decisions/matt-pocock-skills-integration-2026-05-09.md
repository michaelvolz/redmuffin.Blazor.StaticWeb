---
title: Matt Pocock Engineering Skills Integration
date: 2026-05-09
category: tooling-decisions
module: opencode
problem_type: tooling_decision
component: development_workflow
severity: medium
applies_when:
  - Integrating third-party skill packs into OpenCode
  - Organizing multi-vendor skill directories
  - Evaluating skill compatibility across agent harnesses
tags:
  [matt-pocock, skills, engineering, opencode, multi-vendor, skill-organization]
---

# Matt Pocock Engineering Skills Integration

## Context

The Matt Pocock skill pack provides 14 engineering, productivity, and misc skills (tdd, diagnose, triage, to-issues, to-prd, grill-me, grill-with-docs, prototype, zoom-out, improve-codebase-architecture, setup-matt-pocock-skills, write-a-skill, caveman, git-guardrails-claude-code). These needed to be integrated into OpenCode while maintaining clear vendor separation from our own skills and compound-engineering.

## Guidance

Installed all 14 skills into a 3-level nested structure:

```
.opencode/skills/matt-pocock/
├── engineering/       # 10 skills (tdd, diagnose, triage, etc.)
├── productivity/      # 3 skills (caveman, write-a-skill, handoff)
└── misc/             # 1 skill (git-guardrails-claude-code)
```

**Key decisions:**

1. **No conversion needed for 13 of 14 skills.** OpenCode's skill discovery uses `{skill,skills}/**/SKILL.md` globstar — unlimited recursive depth. Confirmed from OpenCode source at `packages/opencode/src/skill/index.ts`.

2. **3-level nesting confirmed compatible.** Mirrors compound-engineering's 2-level pattern. Both coexist: `compound-engineering/ce-tdd/` and `matt-pocock/engineering/tdd/`.

3. **`git-guardrails-claude-code` is Claude-specific.** Captured as SN-0027 for analysis. Not usable directly in OpenCode, but the equivalent functionality exists in our `block-push.js` plugin.

4. **`setup-matt-pocock-skills`** required an `## Agent skills` block in AGENTS.md linking to `docs/agents/` files. Created during grill-with-docs session rather than formal invocation.

## Why This Matters

The 3-level nesting proves OpenCode's skill discovery is truly depth-agnostic. The multi-vendor layout (`compound-engineering/`, `matt-pocock/`, `redmuffin-standards/`, `rm-*/`) scales to any number of vendors without namespace collisions.

## When to Apply

- When adding a new third-party skill pack — create a vendor directory at the same level
- When evaluating whether a skill pack needs conversion for OpenCode — most don't

## Related

- `.opencode/skills/matt-pocock/` — All 14 skills
- `docs/agents/` — Agent configuration files referenced by AGENTS.md
- SN-0027 — git-guardrails-claude-code analysis (sidenote)
