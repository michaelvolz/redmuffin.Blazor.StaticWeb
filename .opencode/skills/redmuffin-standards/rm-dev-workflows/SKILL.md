---
name: rm-dev-workflows
description: "Canonical reference for dev sessions, site startup, process management, port workflow, browser tab hygiene, and tool selection. Use when managing dev environment processes, checking ports, or deciding which tools to use."
---

# rm-dev-workflows

Canonical reference for development workflow on Windows. Covers process management, port handling, browser tab hygiene, and tool selection.

## CRITICAL

- This skill is a **reference**, not an execution workflow. Load it when you need guidance on how to do something in the dev environment.
- Follow every rule in the BOUNDARIES section. No exceptions.
- **NEVER run long-running dev servers (`dotnet watch`, `dotnet run`) via `bash`.** The bash tool enforces a hard timeout that kills the process and all children. Use `Start-Process powershell` to launch in a separate window. See `docs/solutions/developer-experience/bash-timeout-kills-long-running-dotnet-processes-2026-04-04.md`.

## OUT OF SCOPE

This skill does NOT cover:

- Cleanup execution → use `rm-cleanup`
- Commit workflow → use `rm-commit`
- NuGet management → use `rm-nuget-manager`
- Coding standards → use `strict-coding-standards`
- Tool installation and SCSS compilation → use `rm-dev-tools`
- SCSS conventions → use `rm-scss`

## BROWSER TAB HYGIENE

- Never omit the `url` parameter from browser tool calls. Reuse existing pages. Never leave blank tabs open.
- Never navigate to a URL by creating a new tab when an existing tab can be reused.
- **NEVER** use `chrome-devtools_close_page` for cleanup. Use process-level identification only (see `rm-cleanup` skill).
- **NEVER** create new blank tabs when an existing tab can be reused.
- If only one tab remains open in the MCP browser after cleanup, leave it. Do not attempt to close the last tab or treat it as a stale page to reopen.

## SITE STARTUP

### Decision Tree — Pick the Right Command

> **Note:** Commands below show the `dotnet` portion only. Always wrap in `Start-Process powershell.exe` as shown in Frontend Commands. Port comes from the profile's `applicationUrl` in `launchSettings.json`.

| Situation                                                                                             | Command                                                                                      | Profile | Why                                                                |
| ----------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------- | ------- | ------------------------------------------------------------------ |
| **Active development** — editing `.razor` markup, `.cs` method bodies, CSS                            | `dotnet run --project src/redmuffin.Blazor.StaticWeb --launch-profile Watch`                 | `Watch` | Profile includes `watch --non-interactive`, working directory, URL |
| **Quick verification** — checking a page renders, testing Chrome DevTools MCP, no code edits expected | `dotnet run --project src/redmuffin.Blazor.StaticWeb --launch-profile https`                 | `https` | Fastest startup. No file watcher overhead.                         |
| **After a rude edit** — hot reload rejected the change                                                | `dotnet run --project src/redmuffin.Blazor.StaticWeb --launch-profile Watch` (auto-restarts) | `Watch` | Profile's `--non-interactive` auto-restarts without prompting.     |

### Frontend Commands

> **Mock data:** The frontend uses mock data by default. No backend/API is needed for development. The API project (`src/redmuffin.Blazor.StaticWeb.Api`) is only for rare final integration testing.
>
> **Launch profiles are the single source of truth.** All ports, working directories, and command-line args are defined in `src/redmuffin.Blazor.StaticWeb/Properties/launchSettings.json`. Never hardcode them.

```powershell
# Read the port from the active profile
$port = 5233  # from launchSettings.json profiles.Watch.applicationUrl

# Active development with hot reload (DEFAULT for coding sessions)
# Watch profile includes: watch --non-interactive, workingDirectory, applicationUrl
Start-Process powershell.exe -ArgumentList '-NoExit', '-Command', '$Host.UI.RawUI.WindowTitle = ''Frontend (''$port'')''; dotnet run --project src/redmuffin.Blazor.StaticWeb --launch-profile Watch'

# Quick start, no hot reload (verification, Chrome DevTools MCP testing)
Start-Process powershell.exe -ArgumentList '-NoExit', '-Command', '$Host.UI.RawUI.WindowTitle = ''Frontend (''$port'')''; dotnet run --project src/redmuffin.Blazor.StaticWeb --launch-profile https'

# Wait for site to be ready (30s timeout)
$timeout = 30; $start = Get-Date
while (-not (Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue) -and ((Get-Date) - $start).TotalSeconds -lt $timeout) { Start-Sleep -Milliseconds 250 }
if (-not (Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue)) { Write-Error 'Site failed to start within 30s' }
```

**Check if already running:**

```powershell
$port = 5233  # from launchSettings.json
if (Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue) {
    Write-Host "Site is already running on port $port — skip startup, navigate existing browser tab"
} else {
    Write-Host "Site is not running — start it first"
}
```

**Stop the site:**

```powershell
$port = 5233  # from launchSettings.json
Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue | Where-Object { $_.OwningProcess -ne 0 } | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue }
```

**Profile details** (`src/redmuffin.Blazor.StaticWeb/Properties/launchSettings.json`):

| Profile       | Command                                       | URL                     | Hot Reload | Use Case                                |
| ------------- | --------------------------------------------- | ----------------------- | ---------- | --------------------------------------- |
| `https`       | `dotnet run` (Project)                        | `http://localhost:5233` | No         | Quick verification, Chrome DevTools MCP |
| `Watch`       | `dotnet watch --non-interactive` (Executable) | `http://localhost:5233` | Yes        | Active development                      |
| `IIS Express` | IISExpress                                    | dynamic                 | Yes        | Legacy — do not use                     |

### Hot Reload — What Works, What Doesn't

**Supported edits** (applied without restart):

- `.razor` markup changes (HTML, CSS classes, text content)
- `.cs` method body changes (add/remove/edit variables, expressions, statements)
- Adding new types, nested classes
- Adding static/instance methods, fields, events, properties to existing types
- Lambda expression and local function body changes
- CSS changes (compiled CSS only — SCSS requires rebuild, see SCSS/Sass note below)

**Rude edits** (require restart — `dotnet watch` will prompt or auto-restart):

- Adding a new `await` expression to a method that didn't have one
- Adding a new `yield` expression
- Changing method parameter names
- Removing a component parameter attribute (component is disposed and re-initialized)
- Changes to `Program.cs` startup logic (middleware, service configuration, route creation)
- Adding/removing `@inject` directives
- Changing `@inherits` or `@layout` directives

**Hot Reload Troubleshooting**:

- **"No hot reload changes to apply"**: Common Blazor WASM quirk — save the file again, or use `chrome-devtools_press_key` with `Control+R`
- **Force restart**: Use `chrome-devtools_press_key` with `Control+R` to reload the browser — Blazor WASM reinitializes and picks up latest assemblies
- **Disable hot reload**: Set `"hotReloadEnabled": false` in `launchSettings.json` profile
- **Env var alternative**: `DOTNET_WATCH_RESTART_ON_RUDE_EDIT=1` achieves the same as `--non-interactive`

**SCSS/Sass note**: Hot reload does NOT process SCSS changes directly. The `sass --watch` background process handles SCSS compilation automatically. After editing `.scss` files, the compiled CSS lands in `wwwroot/css/` and `dotnet watch` detects it instantly. No manual build step required.
See `rm-dev-tools` for tool installation and `rm-scss` for SCSS conventions.

### Style Change Workflow (CSS / SCSS)

When the dev server is running (`sass --watch` + `dotnet watch`), the pipeline is fully automatic:

1. Edit the `.scss` file
2. Sass watcher auto-compiles `app.css` and `app.min.css` (~0.5s)
3. `dotnet watch` detects the CSS file change and hot-reloads the browser
4. Verify: take a snapshot or evaluate script to confirm computed styles

**Never restart the dev server for a CSS change. Never run manual `sass` commands.** The watchers are running. The pipeline handles it.

**If `dotnet watch` decides to rebuild** (rare, caused by non-CSS file changes detected during the same save cycle): the rebuild produces new WASM assembly fingerprints. This causes an SRI integrity mismatch on the next page load. See "Site Broken — Recognition Pattern" below.

### Site Broken — Recognition Pattern

Blazor WASM enters a broken state after a `dotnet watch` rebuild when SRI hashes in `blazor.boot.json` no longer match the rebuilt assets. This is unrelated to your code changes — it is infrastructure breakage from the hot reload cycle.

**Instant recognition** (use `take_snapshot`, `list_console_messages`):

| Signal            | What you see                                             | Meaning                            |
| ----------------- | -------------------------------------------------------- | ---------------------------------- |
| Page snapshot     | `"An unhandled error has occurred."` and `"Reload"` link | Blazor error boundary triggered    |
| Loading indicator | `"98%"`                                                  | WASM module failed to instantiate  |
| Console errors    | `"SRI's integrity checks failed"`                        | Hash mismatch on `.wasm` or `.pdb` |
| Console errors    | `"still waiting on run dependencies: wasm-instantiate"`  | Module never loaded                |

**Any one of these signals → site is broken. Stop and fix. Do not keep navigating or testing.**

**Fix** (clean rebuild, no hacks):

```bash
systemctl --user stop redmuffin.Blazor.StaticWeb-sass-dotnet-watch.service
rm -rf src/redmuffin.Blazor.StaticWeb/{bin,obj}
dotnet build src/redmuffin.Blazor.StaticWeb -c Debug --verbosity quiet
systemctl --user reset-failed redmuffin.Blazor.StaticWeb-sass-dotnet-watch.service
systemd-run --user \
  --working-directory=/home/flynn/Projects/redmuffin.Blazor.StaticWeb/src/redmuffin.Blazor.StaticWeb \
  -p TimeoutStopSec=1 \
  --unit=redmuffin.Blazor.StaticWeb-sass-dotnet-watch \
  bash -c 'sass --watch scss/app.scss:wwwroot/css/app.css & sass --style=compressed --watch scss/app.scss:wwwroot/css/app.min.css & dotnet watch --launch-profile https'
```

### Build Verification

See AGENTS.md CRITICAL BOUNDARIES for mandatory build and test rules.

### Port Conflict Resolution

If a port is already in use:

```powershell
$port = 5233  # from launchSettings.json

# Check what's on the port
Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue

# Kill all processes on the port in one pipeline (verify not IDE-owned first — see PROCESS MANAGEMENT > Identify IDE-owned processes)
Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue | Where-Object { $_.OwningProcess -ne 0 } | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue }
```

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

- Never use `wmic`.
- Never use `netstat | findstr` for port checking.
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

### Dev Tools

| Task                      | Skill          |
| ------------------------- | -------------- |
| Tool install & versions   | `rm-dev-tools` |
| SCSS compilation workflow | `rm-dev-tools` |
| SCSS conventions          | `rm-scss`      |

### Rules

- Never use external tools when a builtin equivalent can express the same query.
- Use `es.exe` only when builtin tools cannot express the query
- Use `chrome-devtools_*` MCP tools for browser interaction — not `agent-browser` unless session isolation is needed
- Never mix `agent-browser` and `chrome-devtools_*` for the same browser session

## COMMANDS

| Command      | Purpose         | When                                   |
| ------------ | --------------- | -------------------------------------- |
| `rm:dev`     | Load this skill | Need dev workflow guidance             |
| `rm:cleanup` | Run cleanup     | End of session, before switching tasks |
| `rm:commit`  | Create commit   | Ready to commit changes                |
| `rm:nuget`   | Manage packages | Add/remove/update NuGet packages       |

## COMMANDS

| Command      | Purpose         | When                                   |
| ------------ | --------------- | -------------------------------------- |
| `rm:dev`     | Load this skill | Need dev workflow guidance             |
| `rm:cleanup` | Run cleanup     | End of session, before switching tasks |
| `rm:commit`  | Create commit   | Ready to commit changes                |
| `rm:nuget`   | Manage packages | Add/remove/update NuGet packages       |

## BOUNDARIES

Rules are stated inline in each section. Key cross-references:

- Process launch rules → CRITICAL
- Tab management → BROWSER TAB HYGIENE
- Safe process handling → PROCESS MANAGEMENT
- Tool choice → TOOL SELECTION

## CONTEXT

This skill is the canonical reference for development workflow on Windows. It replaces the old GitHub Copilot instructions file with focused, current guidance. For cleanup execution, use `rm-cleanup`. For commit workflow, use `rm-commit`.
