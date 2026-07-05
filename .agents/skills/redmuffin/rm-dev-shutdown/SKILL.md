---
name: rm-dev-shutdown
description: dev environment cleanup ΓÇö stop watchers, browsers, dotnet processes
---

# rm-dev-shutdown (Parallel)

Fast, parallel cleanup of the development environment. Run four
cleanup operations simultaneously using your harness's parallel
dispatch mechanism. Stay silent on successful steps ΓÇö emit only
warnings or errors.

## Execution

Run these four cleanup operations in parallel:

### Browser Cleanup

Run agent-browser cleanup first, then Chrome DevTools MCP cleanup if enabled.

**agent-browser (default):**

```powershell
agent-browser --session redmuffin close 2>$null
$ab = Join-Path $env:USERPROFILE '.agent-browser\browsers'
Get-CimInstance Win32_Process | Where-Object {
  ($_.Name -in 'chrome.exe','agent-browser-win32-x64.exe') -and
  ($_.CommandLine -like "*$ab*" -or $_.Name -eq 'agent-browser-win32-x64.exe' -or
   $_.CommandLine -like '*\.agent-browser\*')
} | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
```

Never kill user Chrome/Brave outside `~/.agent-browser`. Never run `close --all`
while a human inspects a headed browser.

**Chrome DevTools MCP (only when enabled):**

```powershell
Get-CimInstance Win32_Process | Where-Object {
  $_.Name -eq 'brave.exe' -and $_.CommandLine -like '*chrome-devtools-mcp*'
} | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
```

Stay silent on success; only report warnings or errors.

### Server Cleanup

1. Query devenv.exe PIDs first:
   $devenvPids = Get-CimInstance Win32*Process | Where-Object { $*.Name -eq 'devenv.exe' } | Select-Object -ExpandProperty ProcessId
2. Query dotnet.exe processes:
   $dotnet = Get-CimInstance Win32*Process | Where-Object { $*.Name -eq 'dotnet.exe' }
3. For each dotnet process, kill only those whose ParentProcessId is not in $devenvPids:
   Stop-Process -Id <PID> -Force
4. Stay silent on success; only report warnings or errors.

### Filesystem Cleanup

1. Check for a stray nul file in the workspace root.
2. If present, remove it.
3. Do not print a success summary unless there is a warning or error.

### Verification (background)

1. Wait until the 3 cleanup operations have completed.
2. Run one final `Get-CimInstance Win32_Process` snapshot with only the properties needed to evaluate `dotnet.exe`, `brave.exe`, `chrome.exe`, `agent-browser-win32-x64.exe`, and `devenv.exe` ownership.
3. Filter that single snapshot in memory to determine whether any agent-owned `dotnet.exe` remains, any `~/.agent-browser` Chrome/daemon remains, or any MCP-owned Brave process remains.
4. Use the `devenv.exe` PIDs from the same snapshot to protect Visual Studio-owned `dotnet.exe`.
5. Only surface warnings or errors; do not print a success table or success summary.

## Global Cleanup Rules

- Never probe browser pages, screenshots, or any identification method other than process inspection.
- Never kill Visual Studio-owned dotnet processes.
- Never halt or error when a target process is already gone.

## Linux Cleanup

When running on Linux, the dev server runs as a systemd user unit.
Two commands cover all cleanup:

1. **Stop dev server**: Kills dotnet watch and sass watchers via cgroup.
   ```bash
   systemctl --user stop redmuffin.Blazor.StaticWeb-sass-dotnet-watch.service 2>/dev/null
   ```
2. **Kill orphaned sass watchers**: Catches rare cgroup escapees from
   crashed sessions that systemd could not track.
   ```bash
   pkill -f "sass --watch" 2>/dev/null
   ```

Never run `pkill -u $USER dotnet` ΓÇö it will kill your own process.
Never kill MCP server processes ΓÇö they are permanent infrastructure.
No IDE-ownership protection needed on Linux (no Visual Studio).
