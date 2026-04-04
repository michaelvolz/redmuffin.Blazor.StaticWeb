---
title: "fix: Prevent about:blank tab accumulation in dev Chrome"
type: fix
status: active
date: 2026-04-04
origin: docs/brainstorms/2026-04-04-prevent-about-blank-tabs-in-dev-chrome-requirements.md
---

# Prevent about:blank Tab Accumulation in Dev Chrome

## Overview

Closes out the remaining gaps from 4 prior plans that touched this problem. The rm-cleanup skill was successfully rewritten (plans 003, 005 completed), but two active plans remain unfinished: plan 002 (MCP config alignment) and plan 004 (rm-dev-workflows skill — which is missing from disk entirely). This plan creates the missing skill, adds tab hygiene enforcement to agent behavior, and closes the loop.

## Problem Frame

Three root causes of about:blank tab accumulation:

1. **Page probing during cleanup** — `chrome-devtools_*` MCP tool calls during teardown rehydrate the MCP browser session, opening blank tabs. **Fixed** by plans 003/005 (cleanup now uses process-level kill only).
2. **MCP browser lifecycle** — The `chrome-devtools-mcp` package retains or recreates blank pages. Plan 002 (MCP config alignment) is still active and unfinished.
3. **Agent behavior** — Agents use `close_tab` tool wastefully and create new blank tabs instead of reusing existing ones. **No enforcement mechanism exists** — guidance is documented but not enforced.

Additionally, the `rm-dev-workflows` skill (referenced as the canonical tab hygiene source by 4+ plans) **does not exist on disk**.

## Requirements Trace

- R1. Dev Chrome browser should maintain exactly one tab with our site
- R2. No `about:blank` tabs should accumulate during development sessions
- R3. Cleanup operations must not recreate blank tabs as a side effect
- R4. Agents must reuse existing tabs and navigate via URL instead of creating new blank tabs
- R5. Agents must NOT use `close_tab` tool for cleanup

## Scope Boundaries

- NOT a general browser management tool
- NOT a solution for production browser behavior
- Focused on development workflow optimization for AI coding agents
- Does not change MCP browser package internals — works within its constraints
- Closes out plan 002 and plan 004 as part of this work

## Context & Research

### Relevant Code and Patterns

- **`rm-cleanup` skill** (`.opencode/skills/rm-cleanup/SKILL.md`) — Successfully rewritten to use process-level kill only. Rule: "Never probe browser pages during cleanup. Use process identification only."
- **`agent-browser` skill** (`.opencode/skills/vendor/agent-browser/SKILL.md`) — Separate browser automation CLI with its own session management. No tab hygiene rules.
- **`chrome-devtools-mcp`** — Configured in `opencode.json` (lines 66-70). Launches Brave browser. Tools: `list_pages`, `close_page`, `navigate_page`, `take_snapshot`, etc.
- **`rm-dev-workflows` skill** — **Missing from disk**. Referenced by plans 002, 003, 004, 005 as canonical tab hygiene source. Plan 004 was to narrow/modernize it but the skill was never created or was deleted.
- **Plan 002** (`docs/plans/2026-04-03-002-fix-chrome-devtools-tab-reopen-plan.md`) — Status: active. MCP config alignment and teardown tightening never completed.
- **Plan 004** (`docs/plans/2026-04-03-004-fix-rm-dev-workflows-skill-plan.md`) — Status: active. Skill file missing.

### Institutional Learnings

- **`docs/solutions/developer-experience/replace-multi-agent-cleanup-with-fast-script-2026-04-04.md`** — Documents that a single PowerShell script (`scripts/Cleanup-DevEnv.ps1`) is faster than multi-agent cleanup for browser process management. Relevant for cleanup verification approach.

### External References

- Chrome DevTools MCP issue results indicate blank/about:blank tab behavior is a known ecosystem pain point.

## Key Technical Decisions

- **Create rm-dev-workflows skill from scratch** — Since the skill is missing, create it with focused scope: process management, port/Windows workflow, browser tab hygiene, tool-selection guidance. Follow plan 004's narrowing intent.
- **Tab hygiene as AGENTS.md rule** — Add a CRITICAL BOUNDARY to AGENTS.md: "NEVER use `close_tab` for cleanup. Always navigate existing tabs to target URLs." This enforces R5 at the project level, not just in one skill.
- **Close plan 002 as superseded** — The MCP config alignment work in plan 002 is subsumed by this plan's tab hygiene enforcement. Mark plan 002 status as `superseded` with reference to this plan.
- **Close plan 004 as superseded** — This plan creates the rm-dev-workflows skill directly. Mark plan 004 status as `superseded`.

## Open Questions

### Resolved During Planning

- **Should tab hygiene be enforced at skill level or project level?** → Both. AGENTS.md gets the hard rule (NEVER use close_tab), skills get the guidance context.
- **What about the MCP config alignment from plan 002?** → The rm-cleanup rewrite (plans 003/005) already solved the cleanup-side issue. The remaining gap is agent behavior, which this plan addresses. Plan 002 is superseded.

### Deferred to Implementation

- **None** — all questions are answerable from existing context.

## High-Level Technical Design

> _This illustrates the intended approach and is directional guidance for review, not implementation specification. The implementing agent should treat it as context, not code to reproduce._

```
┌─────────────────────────────────────────────────────────┐
│                    Agent Behavior Layer                  │
│  AGENTS.md: NEVER use close_tab for cleanup             │
│  AGENTS.md: Always navigate existing tabs to URLs        │
│  rm-dev-workflows: canonical tab hygiene reference       │
└──────────────────────┬──────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────┐
│                   Cleanup Layer (done)                   │
│  rm-cleanup: Process-level kill only                    │
│  No chrome-devtools_* calls during teardown             │
│  Plans 003, 005 completed                               │
└──────────────────────┬──────────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────────┐
│              MCP Browser Layer (platform)                │
│  chrome-devtools-mcp: may keep one last tab open        │
│  Document limitation, work within constraint            │
└─────────────────────────────────────────────────────────┘
```

## Implementation Units

- [x] **Unit 1: Add tab hygiene rules to AGENTS.md CRITICAL BOUNDARIES**

**Goal:** Enforce R4 and R5 at the project level so all agents inherit the rule.

**Requirements:** R4, R5

**Dependencies:** None

**Files:**

- Modify: `AGENTS.md`

**Approach:**

- Add two rules to the CRITICAL BOUNDARIES section:
  - "NEVER use `chrome-devtools_close_page` or `close_tab` for cleanup. Use process-level identification only."
  - "ALWAYS navigate existing browser tabs to target URLs. Never create new blank tabs when an existing tab can be reused."
- Keep rules concise — one line each, following existing CRITICAL BOUNDARIES style

**Test scenarios:**

- Happy path: AGENTS.md contains both tab hygiene rules in CRITICAL BOUNDARIES
- Happy path: Rules are concise, actionable, and follow existing style

**Verification:**

- AGENTS.md CRITICAL BOUNDARIES section includes tab hygiene rules
- Rules are clear and enforceable

- [x] **Unit 2: Create rm-dev-workflows skill with tab hygiene guidance**

**Goal:** Create the missing skill that serves as the canonical repo-local reference for process management, port/Windows workflow, browser tab hygiene, and tool-selection guidance.

**Requirements:** R1, R2, R3, R4

**Dependencies:** Unit 1

**Files:**

- Create: `.opencode/skills/rm-dev-workflows/SKILL.md`

**Approach:**

- Follow `rm-*` skill conventions: frontmatter with `name: rm-dev-workflows` and description including trigger phrases
- Skill body sections: `## CRITICAL`, `## FLOW`, `## COMMANDS`, `## BOUNDARIES`, `## CONTEXT`
- Include browser tab hygiene as one canonical note (concise, not overlong):
  - "Always pass `url` to browser tools. Reuse existing pages. Never leave blank tabs open."
  - "Use `chrome-devtools_navigate_page` to navigate existing tabs. Do NOT use `chrome-devtools_close_page` for cleanup."
- Include process management guidance (Windows, PowerShell 7.4+):
  - Prefer `Get-CimInstance` over `wmic` (deprecated)
  - Prefer `Get-NetTCPConnection` over `netstat | findstr`
  - Check ParentProcessId to identify IDE-owned processes
- Include tool-selection guidance:
  - Favor OpenCode builtin tools (`glob`, `grep`, `list`, `read`) over external tools
  - `es.exe` as secondary search tool when builtin tools are insufficient
- Keep skill focused — not a general "how to use the terminal" manual

**Patterns to follow:**

- `.opencode/skills/rm-cleanup/SKILL.md` — structure, conciseness, BOUNDARIES section
- `.opencode/skills/rm-commit/SKILL.md` — frontmatter style, COMMANDS table

**Test scenarios:**

- Happy path: Skill file exists with valid frontmatter and all required sections
- Happy path: Browser tab hygiene guidance is present and concise (one canonical note)
- Happy path: Process management guidance uses current Windows/PowerShell patterns
- Happy path: Tool-selection guidance favors OpenCode builtin tools

**Verification:**

- Skill file exists at `.opencode/skills/rm-dev-workflows/SKILL.md`
- Skill is under ~200 lines (focused, not monolithic)
- Browser tab hygiene guidance is present and consistent with AGENTS.md rules
- No deprecated Windows commands referenced

- [x] **Unit 3: Update OpencodeCatalog.md to reference rm-dev-workflows**

**Goal:** Ensure the OpencodeCatalog lists the newly created rm-dev-workflows skill.

**Requirements:** R4

**Dependencies:** Unit 2

**Files:**

- Modify: `docs/OpencodeCatalog.md`

**Approach:**

- Add rm-dev-workflows entry to the skills catalog with description: "Windows dev sessions, builtin tool selection, and tab hygiene"
- Verify the entry matches the skill's actual frontmatter description

**Test scenarios:**

- Happy path: OpencodeCatalog includes rm-dev-workflows with correct description

**Verification:**

- OpencodeCatalog lists rm-dev-workflows skill

- [x] **Unit 4: Close out superseded plans**

**Goal:** Mark plan 002 and plan 004 as superseded with references to this plan.

**Requirements:** R3

**Dependencies:** Units 1-3

**Files:**

- Modify: `docs/plans/2026-04-03-002-fix-chrome-devtools-tab-reopen-plan.md`
- Modify: `docs/plans/2026-04-03-004-fix-rm-dev-workflows-skill-plan.md`

**Approach:**

- Update plan 002 frontmatter: `status: superseded`, add `superseded-by: docs/plans/2026-04-04-NNN-fix-prevent-about-blank-tabs-in-dev-chrome-plan.md`
- Update plan 004 frontmatter: `status: superseded`, add `superseded-by: docs/plans/2026-04-04-NNN-fix-prevent-about-blank-tabs-in-dev-chrome-plan.md`
- Add one-line note to each plan's overview: "Superseded by [this plan] which closes out the remaining tab hygiene gaps."

**Test scenarios:**

- Happy path: Both plans have `status: superseded` and `superseded-by` frontmatter
- Happy path: Overview sections note the superseding plan

**Verification:**

- Plan 002 and 004 are marked superseded
- Cross-references are correct

## System-Wide Impact

- **Interaction graph:** AGENTS.md → all agents inherit tab hygiene rules. rm-dev-workflows skill → loaded when user needs dev workflow guidance. No existing code paths are modified.
- **Error propagation:** None — this is guidance/documentation only. No runtime behavior changes.
- **State lifecycle risks:** None — no data mutations or state changes.
- **Unchanged invariants:** The rm-cleanup skill (plans 003/005) remains unchanged. The chrome-devtools-mcp configuration in `opencode.json` remains unchanged. This plan addresses agent behavior, not platform configuration.

## Risks & Dependencies

| Risk                                                | Mitigation                                                                                                                              |
| --------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------- |
| Agents still use `close_tab` despite AGENTS.md rule | Rule is in CRITICAL BOUNDARIES (hard block). Skill guidance reinforces it. No technical enforcement possible without tool restrictions. |
| rm-dev-workflows skill grows beyond scope           | Keep it under ~200 lines. Focus on 4 concerns only: process management, port/Windows workflow, tab hygiene, tool selection.             |
| MCP browser still keeps one last tab open by design | Document the limitation in rm-dev-workflows. Agents should not treat it as a new page to reopen.                                        |

## Documentation / Operational Notes

- Plan 002 and plan 004 are marked superseded — future work should reference this plan instead.
- The rm-dev-workflows skill fills a gap that was referenced by 4+ prior plans but never existed on disk.

## Sources & References

- **Origin document:** [docs/brainstorms/2026-04-04-prevent-about-blank-tabs-in-dev-chrome-requirements.md](docs/brainstorms/2026-04-04-prevent-about-blank-tabs-in-dev-chrome-requirements.md)
- Related code: `.opencode/skills/rm-cleanup/SKILL.md`, `.opencode/skills/rm-commit/SKILL.md`
- Related plans: `docs/plans/2026-04-03-002-fix-chrome-devtools-tab-reopen-plan.md`, `docs/plans/2026-04-03-004-fix-rm-dev-workflows-skill-plan.md`
- Institutional learning: `docs/solutions/developer-experience/replace-multi-agent-cleanup-with-fast-script-2026-04-04.md`
