---
name: rm-dev-workflows
description: "Shortcut: rm:dev. Canonical reference for Windows dev sessions, process management, port workflow, browser tab hygiene, and tool selection. Use when managing dev environment processes, checking ports, or deciding which tools to use."
---

# rm-dev-workflows

Canonical reference for development workflow on Windows. Covers process management, port handling, browser tab hygiene, and tool selection.

## CRITICAL

- This skill is a **reference**, not an execution workflow. Load it when you need guidance on how to do something in the dev environment.
- Follow every rule in the BOUNDARIES section. No exceptions.

## BROWSER TAB HYGIENE

- **ALWAYS** pass `url` to browser tools. Reuse existing pages. Never leave blank tabs open.
- **ALWAYS** use `chrome-devtools_navigate_page` to navigate existing tabs to target URLs.
- **NEVER** use `chrome-devtools_close_page` for cleanup. Use process-level identification only (see `rm-cleanup` skill).
- **NEVER** create new blank tabs when an existing tab can be reused.
- The MCP browser may keep one last tab open by design. Do not treat it as a new page to reopen.

## PROCESS MANAGEMENT (Windows, PowerShell 7.4+)

### Kill processes

```powershell
# Find processes by name and command line
Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'brave.exe' -and $_.CommandLine -like '*chrome-devtools-mcp*' }

# Stop by PID
Stop-Process -Id <PID> -Force
```

### Identify IDE-owned processes

```powershell
# Get Visual Studio PIDs
$devenvPids = Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'devenv.exe' } | Select-Object -ExpandProperty ProcessId

# Kill dotnet processes NOT owned by VS
Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'dotnet.exe' -and $_.ParentProcessId -notin $devenvPids } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }
```

### Check ports

```powershell
# Check if a port is in use
Get-NetTCPConnection -LocalPort 5000 -ErrorAction SilentlyContinue
```

### Rules

- Prefer `Get-CimInstance` over `wmic` (deprecated)
- Prefer `Get-NetTCPConnection` over `netstat | findstr`
- Always check ParentProcessId before killing processes — protect IDE-owned processes
- Use `Stop-Process -Force` only when graceful shutdown is not applicable

## TOOL SELECTION

### File operations

| Task                   | Tool                                                      |
| ---------------------- | --------------------------------------------------------- |
| Find files by pattern  | `glob` (builtin)                                          |
| Search file contents   | `grep` (builtin)                                          |
| Read files             | `read` (builtin)                                          |
| List directory         | `ls` via `bash`                                           |
| Search codebase (deep) | `es.exe` (secondary, when builtin tools are insufficient) |

### Browser operations

| Task                | Tool                                                  |
| ------------------- | ----------------------------------------------------- |
| Navigate to URL     | `chrome-devtools_navigate_page`                       |
| Take snapshot       | `chrome-devtools_take_snapshot`                       |
| Screenshot          | `chrome-devtools_take_screenshot`                     |
| Click/fill/interact | `chrome-devtools_click`, `chrome-devtools_fill`, etc. |
| Performance tracing | `chrome-devtools_performance_start_trace`             |
| List pages          | `chrome-devtools_list_pages`                          |
| Session isolation   | `agent-browser` (separate daemon, not MCP)            |

### Rules

- Favor OpenCode builtin tools (`glob`, `grep`, `read`, `list`) over external tools
- Use `es.exe` only when builtin tools cannot express the query
- Use `chrome-devtools_*` MCP tools for browser interaction — not `agent-browser` unless session isolation is needed
- Never mix `agent-browser` and `chrome-devtools_*` for the same browser session

## COMMANDS

| Command      | Purpose         | When                                   |
| ------------ | --------------- | -------------------------------------- |
| `rm:dev`     | Load this skill | Need dev workflow guidance             |
| `rm:cleanup` | Run cleanup     | End of session, before switching tasks |
| `rm:commit`  | Create commit   | Ready to commit changes                |

## BOUNDARIES

### ALWAYS

- Reuse existing browser tabs — navigate, don't create
- Protect IDE-owned processes (check ParentProcessId)
- Use current PowerShell patterns (`Get-CimInstance`, `Get-NetTCPConnection`)
- Favor builtin tools over external tools

### NEVER

- Use `chrome-devtools_close_page` for cleanup
- Use deprecated commands (`wmic`, `netstat | findstr`)
- Kill processes without verifying ownership
- Create blank tabs when an existing tab can be reused
- Mix `agent-browser` and `chrome-devtools_*` for the same session

## CONTEXT

This skill is the canonical reference for development workflow on Windows. It replaces the old GitHub Copilot instructions file with focused, current guidance. For cleanup execution, use `rm-cleanup`. For commit workflow, use `rm-commit`.
