---
title: Parallel Agent Team Pattern for Dev Environment Cleanup
date: 2026-05-09
category: best-practices
module: opencode
problem_type: best_practice
component: development_workflow
severity: low
applies_when:
  - Cleaning up development environment processes after agent sessions
  - Designing parallel agent dispatch patterns
  - Writing cleanup scripts that distinguish agent-owned from IDE-owned processes
tags:
  [cleanup, parallel-agents, process-management, brave, dotnet, team-pattern]
---

# Parallel Agent Team Pattern for Dev Environment Cleanup

## Context

Development sessions leave behind running processes (Brave with MCP devtools, agent-owned `dotnet.exe`) and stray artifacts (`nul` files). Manual cleanup requires tracking PIDs and distinguishing agent-owned from IDE-owned processes — getting this wrong could crash Visual Studio.

## Guidance

The `rm-cleanup` skill uses a swarm/team agent pattern: spawn 4 teammates in a single response message for concurrent execution:

1. **Browser Cleanup** — Kills `brave.exe` instances launched with `chrome-devtools-mcp*` command line
2. **Server Cleanup** — Kills `dotnet.exe` instances whose parent PID is NOT `devenv.exe` (protecting Visual Studio)
3. **Filesystem Cleanup** — Removes stray `nul` files
4. **Verification** — Background check that no agent-owned processes remain

**Key safety mechanism:** The `devenv.exe` parent-PID guard prevents accidentally killing Visual Studio's dotnet host, which would crash the IDE.

**Output philosophy:** Silent on success; only surfaces warnings or errors. No "all clear" messages — noise-free cleanup.

## Why This Matters

Parallel dispatch reduces cleanup time from sequential O(n) to concurrent O(1). The parent-PID guard is the critical safety layer — without it, cleaning up agent dotnet processes would also kill the IDE. The silent-on-success pattern means cleanup doesn't add noise to the session output.

## When to Apply

- When the user says "cleanup", "rm:cleanup", or when switching between work sessions
- Always use the 4-agent team pattern — never sequential Task calls for cleanup

## Related

- `.opencode/skills/rm-cleanup/SKILL.md` — Full cleanup workflow
- `.opencode/scripts/cleanup-sessions.ps1` — Session database cleanup (separate concern)
