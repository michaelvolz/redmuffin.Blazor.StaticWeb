---
name: rm-cleanup
description: "Fast, low-noise dev environment cleanup — closes the agent-owned Brave browser, stops non-VS dotnet processes, and removes stray artifacts. Use when you want speed, minimal chatter, and warnings/errors only."
---

# Dev Environment Cleanup (Parallel)

Fast, parallel cleanup of the development environment. Spawn 3 cleanup teammates and 1 background verification teammate so the final checks happen without extra visible chatter.

**Output rule:** stay silent on successful cleanup steps. Emit only warnings or errors; do not print status summaries, progress narration, or todo chatter.

## Critical Execution Rule

Use a temporary team and spawn all 4 teammates in a single response message.

- Do not issue the cleanup work one agent at a time.
- Do not use plain one-off Task calls for this workflow.
- Do not put `run_in_background` on non-teammate tasks.
- Use the swarm pattern: create one team, then spawn 4 teammates together so the runtime can schedule them concurrently.

## Phase 1: Parallel Dispatch

1. Create a temporary team for the cleanup run.
2. Spawn these 3 teammates simultaneously (same response, same team):

### Agent A: Browser Cleanup

```
team_name: "rm-cleanup"
name: "browser-cleanup"
description: "Browser cleanup"
subagent_type: general
prompt: |
  1. Query Brave processes with PowerShell:
     Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'brave.exe' -and $_.CommandLine -like '*chrome-devtools-mcp*' }
  2. If one or more matches are found, stop each PID with:
     Stop-Process -Id <PID> -Force
  3. If no match is found, report "already closed" and exit cleanly.
  4. Do not print a success summary unless a warning or error occurred.
```

### Agent B: Server Cleanup

```
team_name: "rm-cleanup"
name: "server-cleanup"
description: "Server cleanup"
subagent_type: general
prompt: |
  1. Query devenv.exe PIDs first:
     $devenvPids = Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'devenv.exe' } | Select-Object -ExpandProperty ProcessId
  2. Query dotnet.exe processes:
     $dotnet = Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'dotnet.exe' }
  3. For each dotnet process, kill only those whose ParentProcessId is not in $devenvPids:
     Stop-Process -Id <PID> -Force
  4. Stay silent on success; only report warnings or errors.
```

### Agent C: Filesystem Cleanup

```
team_name: "rm-cleanup"
name: "filesystem-cleanup"
description: "Filesystem cleanup"
subagent_type: general
prompt: |
  1. Check for a stray nul file in the workspace root.
  2. If present, remove it.
  3. Do not print a success summary unless there is a warning or error.
```

### Agent D: Verification

```
team_name: "rm-cleanup"
name: "verification"
description: "Verification"
subagent_type: general
run_in_background: true
prompt: |
  1. Wait until the 3 cleanup teammates have completed.
  2. Run one final `Get-CimInstance Win32_Process` snapshot with only the properties needed to evaluate `dotnet.exe`, `brave.exe`, and `devenv.exe` ownership.
  3. Filter that single snapshot in memory to determine whether any agent-owned `dotnet.exe` remains and whether any MCP-owned Brave process remains.
  4. Use the `devenv.exe` PIDs from the same snapshot to protect Visual Studio-owned `dotnet.exe`.
  5. Only surface warnings or errors; do not print a success table or success summary.
```

## Global Cleanup Rules

- Never probe browser pages during cleanup.
- Use process identification only.
- Never kill Visual Studio-owned dotnet processes.
- If a target process is already gone, report it as already closed and continue.

## Linux Cleanup

When running on Linux, adapt the cleanup workflow:

1. **Stop dev server**: Use proper systemd stop.
   ```bash
   systemctl --user stop redmuffin.Blazor.StaticWeb-sass-dotnet-watch.service 2>/dev/null    # ~2s (TimeoutStopSec=1)
   ```
2. **Reset unit state**: Clear failed state so the unit can be reused.
   ```bash
   systemctl --user reset-failed redmuffin.Blazor.StaticWeb-sass-dotnet-watch.service 2>/dev/null
   ```
3. **Stop sass watchers**: Kill any orphaned background SCSS watchers.
   ```bash
   pkill -f "sass --watch" 2>/dev/null
   ```
4. **Stop dotnet processes**: Kill any remaining dotnet processes owned by the current user (stale processes from crashed sessions).
   ```bash
   pkill -u $USER dotnet 2>/dev/null
   ```
5. **Stop browser processes**: Kill Brave/Chromium processes launched by MCP tools.
   ```bash
   pkill -f "chrome-devtools-mcp" 2>/dev/null
   ```
6. **Stray file**: Remove any `nul` file in the workspace root.
   ```bash
   rm -f nul
   ```

No IDE-ownership protection needed on Linux (no Visual Studio).
