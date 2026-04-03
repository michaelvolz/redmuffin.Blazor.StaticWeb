---
title: "fix: Rewrite rm-cleanup for fast, parallel, no-probe teardown"
type: fix
status: completed
date: 2026-04-03
origin: null
---

# Fix: Rewrite rm-cleanup for Fast, Parallel, No-Probe Teardown

## Overview

The `rm-cleanup` skill has been iteratively patched but still contains unnecessary commands, redundant verification steps, and a history of DevTools page probing that rehydrates the blank-tab MCP browser session. The user wants a clean, fast, parallel cleanup that: closes only the agent-owned Brave browser, kills only non-VS dotnet processes, and verifies with minimal concrete commands — no guessing, no page checks.

## Problem Frame

Current cleanup has three problems:

1. **Page probing reopens the MCP browser** — any `chrome-devtools_*` call during teardown can wake up the blank-tab session
2. **Redundant verification** — checking port 5233 twice, waiting 2 seconds, checking dotnet orphans with wmic parent lookups — all slow and unnecessary when the kill commands are targeted
3. **No concrete PID identification before killing** — the skill searches for ports to find PIDs instead of identifying processes by their known signatures first

## Requirements Trace

- R1. Close only the agent-owned Brave browser (MCP profile), never the user's main Brave
- R2. Kill all non-VS dotnet processes, never VS-owned ones
- R3. No `chrome-devtools_*` page commands during cleanup
- R4. Fast termination — if the browser is already closed, skip silently
- R5. Parallel execution where possible
- R6. Minimal verification — one concrete check per domain, not repeated polling

## Scope Boundaries

- **In scope:** `.opencode/skills/rm-cleanup/SKILL.md`
- **Out of scope:** `opencode.json` changes, `rm-dev-workflows` changes, app code changes

## Context & Research

### Relevant Code and Patterns

- `.opencode/skills/rm-cleanup/SKILL.md` — current file, 78 lines, needs rewrite
- `.opencode/skills/rm-dev-workflows/SKILL.md` — already has tab hygiene rules, no changes needed
- `opencode.json` — MCP config, no changes needed for this fix

### Key Technical Decisions

- **Decision: Use PowerShell `Get-CimInstance Win32_Process` for all process identification**
  - Rationale: gives PID, ParentProcessId, and CommandLine in one query. Can filter by name and command line pattern. No need for `wmic` + `findstr` pipelines.
- **Decision: Identify VS-owned dotnet by checking ParentProcessId against running `devenv.exe` PIDs**
  - Rationale: VS launches dotnet as child processes. If a dotnet process's parent is a devenv PID, it is VS-owned. Otherwise it is agent-owned and safe to kill.
- **Decision: Identify MCP Brave by matching `chrome-devtools-mcp` in CommandLine**
  - Rationale: the MCP server launches Brave with this string in its arguments. User's main Brave does not have it.
- **Decision: No port-based PID discovery**
  - Rationale: port scanning is slow and indirect. Process-level identification is faster and more precise.
- **Decision: No repeated verification loops**
  - Rationale: if the kill command succeeds, the process is gone. One final check is enough. If it was already gone, the kill is a no-op.

## Open Questions

### Resolved During Planning

- **Should we use `taskkill` or `Stop-Process`?**
  - Resolution: `Stop-Process -Force` in PowerShell. It's cleaner, doesn't need `//PID` escaping, and works reliably on Windows.
- **Should we keep the 3-agent parallel model?**
  - Resolution: Yes, but simplify the agents. Browser agent kills MCP Brave. Server agent kills non-VS dotnet. Filesystem agent removes stray `nul`. All three are independent and can run in parallel.
- **Should we verify port 5233 at all?**
  - Resolution: No. If we kill all non-VS dotnet processes, the port is freed by definition. Port checking is redundant.

### Deferred to Implementation

- None.

## Implementation Units

- [ ] **Unit 1: Rewrite rm-cleanup skill with concrete, fast, parallel commands**

**Goal:** Replace the entire cleanup skill with a minimal, fast, parallel workflow that uses PowerShell process identification instead of port scanning or DevTools probing.

**Requirements:** R1, R2, R3, R4, R5, R6

**Dependencies:** None

**Files:**

- Modify: `.opencode/skills/rm-cleanup/SKILL.md`

**Approach:**

The skill should define 3 parallel teammates with these exact behaviors:

**Agent A: Browser Cleanup**

- Use `Get-CimInstance Win32_Process` to find Brave processes with `chrome-devtools-mcp` in CommandLine
- If found, `Stop-Process -Force` on that PID
- If not found, report "already closed" — no error, no retry
- No `chrome-devtools_*` commands at all

**Agent B: Server Cleanup**

- Use `Get-CimInstance Win32_Process` to find all `devenv.exe` PIDs first
- Use `Get-CimInstance Win32_Process` to find all `dotnet.exe` processes
- For each dotnet process, check if its ParentProcessId is in the devenv PID set
- Kill only dotnet processes whose parent is NOT devenv
- Report which PIDs were killed and which were skipped (VS-owned)

**Agent C: Filesystem Cleanup**

- Check for `nul` file in workspace root
- Remove if present
- Report result

**Verification (Phase 2, sequential after all agents):**

- One `Get-CimInstance Win32_Process` query for any remaining `dotnet.exe` — list them with ParentProcessId so the user can see what's left
- One `Get-CimInstance Win32_Process` query for any remaining Brave with `chrome-devtools-mcp` — should be empty
- Print summary

**Patterns to follow:**

- Existing skill structure with Phase 1 parallel + Phase 2 verification
- PowerShell `Get-CimInstance` for process queries (not `wmic`, not `tasklist`)
- `Stop-Process -Force` for termination (not `taskkill`)

**Test scenarios:**

- Happy path: MCP Brave is running, agent-owned dotnet is running, `nul` exists — all three are cleaned up
- Edge case: MCP Brave is already closed — browser agent reports "already closed" without error
- Edge case: Only VS-owned dotnet is running — server agent skips all, reports "all dotnet processes are VS-owned"
- Edge case: No dotnet processes at all — server agent reports "no dotnet processes found"
- Error path: `Stop-Process` fails on a PID — agent reports the failure clearly

**Verification:**

- Running the cleanup with MCP Brave and agent dotnet running results in both being terminated
- Running the cleanup when everything is already closed produces clean "already closed" / "none found" messages
- No `chrome-devtools_*` commands appear in the skill text
- No `netstat` or port-based PID discovery appears in the skill text

## System-Wide Impact

- **Interaction graph:** `rm-cleanup` → process termination only. No DevTools, no port scanning.
- **Unchanged invariants:** VS-owned dotnet processes are never touched. User's main Brave browser is never touched.

## Risks & Dependencies

| Risk                                                            | Mitigation                                                                                                                                          |
| --------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| `Get-CimInstance` is slower than `wmic` on some systems         | It's a single query per agent, not a pipeline. The total time is still sub-second.                                                                  |
| A dotnet process has a non-devenv parent but is not agent-owned | This is unlikely in practice. The user can review the Phase 2 summary and decide.                                                                   |
| `Stop-Process -Force` leaves orphaned children                  | Dotnet dev server processes are typically a parent-child pair. Killing the parent usually takes the child. Phase 2 verification catches stragglers. |

## Sources & References

- Related code: `.opencode/skills/rm-cleanup/SKILL.md`
- Related code: `.opencode/skills/rm-dev-workflows/SKILL.md`
- Prior plan: `docs/plans/2026-04-03-002-fix-chrome-devtools-tab-reopen-plan.md`
