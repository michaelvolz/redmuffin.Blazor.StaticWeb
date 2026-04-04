---
date: 2026-04-04
topic: sidenote-capture-skill
---

# Sidenote Capture Skill

## Problem Frame

During active work sessions with AI coding assistants, users frequently have tangential ideas, improvements, or observations that must be captured immediately to avoid losing them, but should not derail the current task. The current manual approach (appending to `docs/sidenotes.md`) is suboptimal — it requires manual formatting, lacks metadata, and has no structured conversion path to actual tasks.

This is a recognized pain point across AI coding agents — Claude Code has an open issue (#26376) for a non-interrupting todo queue/scratchpad, marked as duplicate due to high demand.

## Requirements

**Capture**

- R1. Trigger on inline keyword "sidenote:" or "/sidenote" command followed by text
- R2. Capture includes timestamp, source context (what was being discussed), and the sidenote text
- R3. Confirmation is brief (one line) and does not derail the active conversation
- R4. Both capture methods (inline keyword and slash command) are supported

**Storage**

- R5. One markdown file per sidenote in `docs/sidenotes/`, named by ID (e.g., `SN-001.md`)
- R6. Each sidenote gets a unique ID (e.g., SN-001, SN-002) for easy reference and filename
- R7. Frontmatter includes: id, date, status (pending|converted|dismissed), source-context, tags

**Retrieval & Conversion**

- R8. "show sidenotes" or "list sidenotes" displays numbered list of pending sidenotes with IDs
- R9. "convert sidenote SN-XXX" or "tackle sidenote SN-XXX" converts to appropriate artifact (todo, brainstorm, or direct task)
- R10. Converted sidenotes are marked with status and linked to the resulting artifact
- R11. "dismiss sidenote SN-XXX" marks as dismissed with optional reason

## Success Criteria

- User can capture a sidenote mid-conversation with one short phrase and zero context-switching
- Sidenotes are findable by ID, date, or keyword search
- Converting a sidenote to a task takes one command and produces the right artifact type
- The skill does not consume conversational context or derail active work

## Scope Boundaries

- NOT a full project management tool — just capture, store, retrieve, convert
- NOT cross-session memory — sidenotes persist in files but are not auto-loaded into context
- NOT auto-suggestions — user explicitly requests retrieval (no proactive nagging)

## Key Decisions

- **Skill, not plugin**: Skills are simpler, work across AI agents (OpenCode, Claude Code), and don't require external tooling
- **Structured markdown over JSON**: Human-readable, searchable, works with existing markdown tooling
- **Manual retrieval over auto-suggest**: Respects user flow — they decide when to review, not the agent

## Dependencies / Assumptions

- Assumes `docs/sidenotes/` directory exists (skill should create if missing)
- Assumes agent has file write access (standard for coding agents)

## Outstanding Questions

### Resolve Before Planning

- None

### Deferred to Planning

- [Affects R6][Technical] Should ID format be sequential (SN-001) or date-based (SN-20260404-001)?
- [Affects R9][Needs research] What's the best heuristic to decide if a sidenote becomes a todo vs brainstorm vs direct work?

## Next Steps

→ /ce:plan for structured implementation planning
