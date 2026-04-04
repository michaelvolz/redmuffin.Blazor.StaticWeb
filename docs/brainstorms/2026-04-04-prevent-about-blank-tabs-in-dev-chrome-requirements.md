---
date: 2026-04-04
topic: prevent-about-blank-tabs-in-dev-chrome
source: docs/sidenotes/SN-0002.md
---

# Prevent Many Open about:blank Tabs in Dev Chrome Browser

## Problem Frame

During development sessions with AI coding assistants, the Chrome DevTools MCP browser session accumulates many `about:blank` tabs. This happens because:

- Cleanup runs can wake up the MCP browser session
- Page probing during teardown reopens blank tabs
- The MCP/browser lifecycle retains or recreates blank pages
- Agents use `close_tab` tool wastefully instead of reusing existing tabs

This is a recurring pain point — at least 4 prior plans have touched on it without a durable solution.

## Requirements

**Browser Lifecycle**

- R1. Dev Chrome browser should maintain exactly one tab with our site
- R2. No `about:blank` tabs should accumulate during development sessions
- R3. Cleanup operations must not recreate blank tabs as a side effect

**Agent Behavior**

- R4. Agents must reuse existing tabs and navigate via URL instead of creating new blank tabs
- R5. Agents must NOT use `close_tab` tool for cleanup (wastes time, can trigger session recreation)

## Success Criteria

- Dev Chrome browser has exactly one tab with our site after any operation
- Zero `about:blank` tabs accumulate during development
- Cleanup runs complete without side-effect tab creation
- Agent browser guidance is consistent across all skills

## Scope Boundaries

- NOT a general browser management tool
- NOT a solution for production browser behavior
- Focused on development workflow optimization for AI coding agents
- Does not change MCP browser package internals — works within its constraints

## Key Decisions

- **Tab reuse over close/reopen**: Navigate existing tabs to target URLs rather than closing and creating new ones
- **Document the limitation**: If MCP browser keeps one last tab open by design, document it clearly and avoid treating it as a new page

## Outstanding Questions

### Deferred to Planning

- [Affects R3][Technical] What's the root cause of about:blank tab recreation — is it the MCP package, the cleanup skill, or agent behavior?
- [Affects R5][Needs research] Is there a way to prevent agents from using `close_tab` — skill guidance, tool restriction, or both?

## Next Steps

→ /ce:plan for structured implementation planning
