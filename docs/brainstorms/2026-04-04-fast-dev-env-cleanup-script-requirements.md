---
date: 2026-04-04
topic: fast-dev-env-cleanup-script
---

# Fast Dev Environment Cleanup Script

## Problem Frame

After debugging or testing the frontend in Chrome DevTools, residual processes remain: MCP-owned Brave browser, orphan dotnet.exe instances, and stray artifacts. The current `rm:cleanup` skill spawns 4 parallel subagents for this, which adds 10-15s of agent overhead for what is fundamentally a ~1s operations task.

## Requirements

**Script Location & Naming**

- R1. Script lives at `scripts/Cleanup-DevEnv.ps1`
- R2. Follows existing naming convention (`[Verb]-[Noun].ps1`)
- R3. No skill modifications — script is standalone and invocable directly

**Core Cleanup**

- R4. Close MCP-owned Brave browser processes (identified by `chrome-devtools-mcp` in command line)
- R5. Kill orphan `dotnet.exe` processes (those NOT owned by `devenv.exe` / Visual Studio)
- R6. Delete stray `nul` file from workspace root if present

**Verification**

- R7. After cleanup, check for residual `dotnet.exe` processes still running
- R8. Check open ports for lingering listeners from dotnet processes
- R9. Verification is part of the same script — no separate agent or process

**Output**

- R10. Single summary line showing what was cleaned and verification results
- R11. Format: `Cleanup: stopped N dotnet, closed N Brave, nul deleted: bool. Residual dotnet: N, listening ports: N`

**Performance**

- R12. Target execution time under 2 seconds
- R13. Single `Get-CimInstance Win32_Process` call (not multiple calls across agents)

## Success Criteria

- Script completes in under 2 seconds
- All residual processes cleaned correctly
- Visual Studio-owned dotnet processes are never killed
- One-line summary output is accurate and informative

## Scope Boundaries

- No skill modifications — `rm:cleanup` skill remains untouched
- No browser page probing — process identification only
- No cleanup of `bin/`, `obj/`, or other build artifacts
- No changes to running scheduled tasks or services

## Key Decisions

- **Single script vs multi-agent**: Single PowerShell script eliminates agent spawn overhead (the primary bottleneck)
- **Single CimInstance snapshot**: One process query shared across all cleanup steps instead of 4 separate queries
- **Sequential execution**: Sequential within one script is faster than parallel across agents due to zero spawn overhead

## Dependencies / Assumptions

- PowerShell is available (Windows environment)
- `Get-CimInstance` is available (PowerShell 3.0+)
- `netstat` is available for port verification

## Next Steps

→ `/ce:plan` for implementation planning
