---
title: Agent Context and Safety Guardrails via AGENTS.md
date: 2026-05-09
category: best-practices
module: opencode
problem_type: best_practice
component: development_workflow
severity: high
applies_when:
  - Configuring AGENTS.md for multi-model agent harnesses
  - Protecting context window from flooding in long sessions
  - Establishing safety blocks for destructive operations
tags:
  [context-mode, aguents-md, context-window, safety-net, routing-rules, sandbox]
---

> **Current (2026-06-08):** AGENTS.md line numbers referenced in this doc have shifted. lock-push.js lives in ~/.config/opencode/plugins/ (global config), not the project's .opencode/ directory (which is gitignored).

# Agent Context and Safety Guardrails via AGENTS.md

## Context

Without explicit routing rules, agents dump raw file contents and large command outputs directly into context, wasting the context window within the first few messages of a session. A single unrouted command can dump 56KB into context. Additionally, agents need unambiguous safety blocks for destructive operations (pushing code, writing secrets, skipping hooks).

## Guidance

AGENTS.md includes a verbatim **"context-mode — MANDATORY routing rules"** section with these policies:

**Think in Code — MANDATORY.** When analyzing, counting, filtering, or processing data, write JavaScript/Python that does the work in a sandbox. Only `console.log()` output enters context. Never read raw data into context for mental processing.

**BLOCKED commands.** `curl`, `wget`, and inline HTTP (`fetch()`, `requests.get()`, etc.) are blocked and redirected to sandbox equivalents (`ctx_fetch_and_index`, `ctx_execute` with `fetch`).

**REDIRECTED tools.** Shell output >20 lines must use `ctx_batch_execute`. File reading for analysis must use `ctx_execute_file`. Grep/search with large results must run in sandbox.

**Tool selection hierarchy:** Gather (`ctx_batch_execute`) → Follow-up (`ctx_search`) → Processing (`ctx_execute`) → Web (`ctx_fetch_and_index`) → Index (`ctx_index`).

**Safety blocks.** `cc-safety-net` plugin blocks `.env*` file writes and push operations. The `block-push` regex in OpenCode plugins prevents accidental pushes.

The routing rules section is marked non-optional — they protect the context window from flooding.

## Why This Matters

Context windows are the scarcest resource in agent sessions. A single `cat large-file.log` can consume half a session's context. The routing rules ensure data stays in sandboxes and only processed results enter context. Safety blocks prevent catastrophic mistakes (pushing secrets, force-pushing to main) that a single misinstructed agent could cause.

The rules are expressed as descriptions, not commands — they inform agent judgment rather than creating redundant "always check" loops that would themselves consume context.

## When to Apply

- Every agent session — the routing rules in AGENTS.md are loaded automatically
- When configuring a new repo's AGENTS.md for multi-model agent use
- When adding new tooling that could produce large outputs

## Related

- `AGENTS.md` — "context-mode — MANDATORY routing rules" section (lines 119-163)
- `.opencode/plugins/block-push.js` — Push safety plugin
