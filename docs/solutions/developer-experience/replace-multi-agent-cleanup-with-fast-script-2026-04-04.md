---
title: Replace multi-agent dev cleanup with single fast PowerShell script
date: 2026-04-04
category: developer-experience
module: development_workflow
problem_type: developer_experience
component: tooling
severity: medium
applies_when:
  - Dev environment cleanup after frontend debugging or testing
  - Residual dotnet.exe or Brave (MCP) processes need to be stopped
  - Agent spawn overhead makes cleanup feel slow
symptoms:
  - Cleanup taking 10-15 seconds due to 4 parallel subagent spawns
  - Multiple Get-CimInstance calls across agents
root_cause: inadequate_documentation
resolution_type: tooling_addition
tags: [dev-environment, cleanup, powershell, agent-overhead, process-management]
---

# Replace multi-agent dev cleanup with single fast PowerShell script

## Context

The `rm:cleanup` skill spawns 4 parallel subagents (browser cleanup, server cleanup, filesystem cleanup, verification) to clean up the dev environment after debugging. Each subagent has startup overhead, and each makes its own `Get-CimInstance Win32_Process` call. The total cleanup takes 10-15 seconds for what is fundamentally a ~1 second operations task.

## Guidance

Use a single PowerShell script for dev environment cleanup instead of spawning multiple agents. The script lives at `scripts/Cleanup-DevEnv.ps1` and does everything in one process:

1. **Single process snapshot** — one `Get-CimInstance` call shared across all cleanup steps
2. **Kill MCP-owned Brave** — processes with `chrome-devtools-mcp` in their command line
3. **Kill orphan dotnet** — processes NOT owned by `devenv.exe` (Visual Studio)
4. **Delete stray artifacts** — workspace root `nul` file
5. **Verify** — check residual processes and open ports
6. **Summary** — one-line output showing what was done

```powershell
# Run cleanup
.\scripts\Cleanup-DevEnv.ps1

# Output example:
# Cleanup: stopped 2 dotnet, closed 7 Brave, nul deleted: False. Residual dotnet: 0, listening ports: 0
```

Execution time: ~450ms (vs 10-15s with the multi-agent approach).

## Why This Matters

Agent spawn overhead dominates simple operational tasks. A 4-agent parallel cleanup sounds efficient but each agent needs:

- Team creation and initialization
- Context loading
- Independent PowerShell startup
- Separate CimInstance queries

A single script eliminates all of that. Sequential within one script is faster than parallel across agents when the individual steps are fast and the spawn overhead is the bottleneck.

## When to Apply

- Dev environment cleanup after frontend debugging or testing
- Any simple operational task where agent spawn overhead exceeds the actual work
- Process management that needs to be fast and reliable

## Examples

**Before (slow — multi-agent approach):**

```
4 subagents spawned in parallel
  Agent A: Get-CimInstance → kill Brave (~3s startup + 1s work)
  Agent B: Get-CimInstance → kill orphan dotnet (~3s startup + 1s work)
  Agent C: check nul file (~3s startup + 0.1s work)
  Agent D: wait + Get-CimInstance → verify (~3s startup + 1s work)
Total: 10-15 seconds
```

**After (fast — single script):**

```
.\scripts\Cleanup-DevEnv.ps1
  1x Get-CimInstance → shared snapshot
  Kill Brave, orphan dotnet, delete nul, verify
Total: ~450 milliseconds
```

## Prevention

- Before spawning agents for a task, ask: is the work itself slower than agent startup?
- For simple operational tasks (process management, file cleanup, config checks), prefer direct scripts
- Reserve multi-agent patterns for tasks that benefit from parallel reasoning (code review, research, analysis)

## Related

- `.opencode/skills/rm-cleanup/SKILL.md` — original multi-agent cleanup skill (unchanged)
- `docs/brainstorms/2026-04-04-fast-dev-env-cleanup-script-requirements.md` — requirements document
