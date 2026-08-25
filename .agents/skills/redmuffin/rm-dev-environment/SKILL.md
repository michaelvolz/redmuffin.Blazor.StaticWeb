---
name: rm-dev-environment
description: >-
  Dev host startup and process rules for this repo. DEFAULT is frontend-only
  on http://localhost:5233 with synthetic data (no API) — ~99% of local QA.
  Use when: start/stop the site, site is already up, port 5233, launch profile
  https or Watch, background dotnet run for agent tests, kill host before
  rebuild, Start-Process vs harness background, hot reload, SRI site broken,
  ports/watchers, which browser tool to use. Full API stack is opt-in only
  when the user names it. Pair with rm-agent-browser-companion for browser work and
  rm-dev-shutdown for cleanup. Load immediately when starting or choosing a
  host — do not invent full-site startup.
---

# rm-dev-environment

Canonical reference for development workflow on Windows. Covers process management, port handling, browser tab hygiene, and tool selection.

**Default for agents:** frontend-only host on `:5233` with synthetic data (SITE STARTUP → Default mode). Full stack only when explicitly required.

## CRITICAL

- This skill is a **reference**, not an execution workflow. Load it when you need guidance on how to do something in the dev environment.
- Follow every rule in the BOUNDARIES section. No exceptions.
- **Default site mode is frontend-only** (see SITE STARTUP → Default mode). Use that path for ~99% of agent QA and local page checks. Do not start the API / full stack unless the user or scenario explicitly requires it.
- **Long-running hosts must not block the agent.** A foreground shell with a hard timeout kills `dotnet run` / `dotnet watch` and children. For **default agent frontend QA**, start the host as a harness **background** task, probe readiness (a few seconds), then drive the browser. For a **human-visible** console window, use `Start-Process powershell` (Frontend Commands). Never leave a multi-minute foreground `dotnet run` waiting on the agent turn. See `docs/solutions/developer-experience/bash-timeout-kills-long-running-dotnet-processes.md`.
- **Stop the host before assembly-touching rebuilds or edits** that lock `bin/` / `obj/`. Start again after a successful build.

## BOUNDARIES

Rules are stated inline in each section. Key cross-references:

- Default frontend-only mode → SITE STARTUP
- Process launch rules → CRITICAL
- Session management → BROWSER SESSION HYGIENE
- Safe process handling → PROCESS MANAGEMENT
- Tool choice → TOOL SELECTION

## OUT OF SCOPE

This skill does NOT cover:

- Cleanup execution → use `rm-dev-shutdown`
- Commit workflow → use `rm-commit`
- NuGet management → use `rm-nuget-manager`
- Coding standards → use `strict-coding-standards`
- Tool installation and SCSS compilation → use `rm-dev-tools`
- SCSS conventions → use `rm-scss`

## BROWSER SESSION HYGIENE

**agent-browser** (`rm-agent-browser-companion`, co-loads upstream `agent-browser`) is the
browser automation path on all OS.

Bundled Chromium from `agent-browser install` — **not** the user's Brave profile.
**User browser (all OS): Brave only** — never kill user Brave.

### agent-browser

- Load `rm-agent-browser-companion` before the first command — Blazor WASM boot wait is mandatory.
- Never omit `--session <name>` on any `agent-browser` command.
- Never issue two `agent-browser` command chains in parallel.
- Never run `agent-browser close --all` while a human inspects a headed browser.
- Never kill user **Brave** or non-agent-browser Chromium — only processes under `~/.agent-browser`.
- End sessions with `agent-browser --session <name> close` or `rm-dev-shutdown`.

## SITE STARTUP

### Default mode (99%) — frontend-only, synthetic

**This is the default** for agent browser QA, route checks, lazy-load checks, and most local verification. Use it whenever the task does not explicitly require the full API / Functions stack.

| Rule                | Detail                                                                                                                                                                                                                                                                                                |
| ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Scope               | Blazor WASM host only: `src/redmuffin.Blazor.StaticWeb`. **Never** start `src/redmuffin.Blazor.StaticWeb.Api` on this path.                                                                                                                                                                           |
| Data                | Synthetic / mock Strategy by default. No Azure Functions needed.                                                                                                                                                                                                                                      |
| URL / port          | `http://localhost:5233` from `Properties/launchSettings.json` (`https` / `Watch`). Never invent another base URL.                                                                                                                                                                                     |
| Profile             | **Agent QA / quick verify:** `https` (fast, no watcher). **Human coding with hot reload:** `Watch`.                                                                                                                                                                                                   |
| Host process        | **Agent harness (default for tests):** `dotnet run` as a **background** task so the agent stays free. Startup is a few seconds — probe port or HTTP 200, then continue. Do not multi-minute-wait on host logs. **Human console:** `Start-Process powershell` when a visible window is wanted (below). |
| Reuse               | If port `5233` is already listening, **do not** start another host. Drive the existing origin.                                                                                                                                                                                                        |
| Stop before rebuild | Kill the host **before** any rebuild or edit that locks assemblies under `bin/` / `obj/`. Rebuild, then start again.                                                                                                                                                                                  |
| Cleanup             | Host stop + `agent-browser --session redmuffin close` when done (`rm-dev-shutdown`).                                                                                                                                                                                                                  |

**Never** start a full site (API + SWA + frontend) for ordinary QA. Full-stack / real Functions HTTP is **opt-in only** when the user or scenario names it. Do not expand this default section into full-stack procedure.

### Decision Tree — Pick the Right Frontend Command

> **Note:** Port comes from the profile's `applicationUrl` in `launchSettings.json`. Agent QA uses the background form; human windows use `Start-Process` as shown under Frontend Commands.

| Situation                                                                                         | Command                                                                      | Profile | Why                                                                       |
| ------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------- | ------- | ------------------------------------------------------------------------- |
| **Agent / quick verification** (default) — page renders, agent-browser QA, no code edits expected | `dotnet run --project src/redmuffin.Blazor.StaticWeb --launch-profile https` | `https` | Fastest startup. No file watcher overhead. Prefer harness **background**. |
| **Active development** — editing `.razor`, `.cs` method bodies, CSS                               | `dotnet run --project src/redmuffin.Blazor.StaticWeb --launch-profile Watch` | `Watch` | Hot reload; human-visible window is fine.                                 |
| **After a rude edit** — hot reload rejected the change                                            | same as Watch (auto-restarts)                                                | `Watch` | Profile's `--non-interactive` auto-restarts without prompting.            |

### Frontend Commands (default path)

> **Mock data:** The frontend uses mock / synthetic data by default. No backend/API is needed for this path. The API project is only for rare, explicitly requested integration testing (not described here).
>
> **Launch profiles are the single source of truth.** Ports, working directories, and args live in `src/redmuffin.Blazor.StaticWeb/Properties/launchSettings.json`. Never hardcode alternate hosts.

```powershell
# Port from launchSettings.json profiles.https / Watch.applicationUrl
$port = 5233

# --- Agent harness (DEFAULT for QA / tests): non-blocking background host ---
# Run via harness background so the agent is not blocked. Example shape:
#   $env:ASPNETCORE_ENVIRONMENT = 'Development'
#   dotnet run --project src/redmuffin.Blazor.StaticWeb --launch-profile https
# Then probe (few seconds), do not multi-minute-wait:
#   Invoke-WebRequest http://localhost:5233/ -UseBasicParsing -TimeoutSec 5

# --- Human-visible console (coding sessions) ---
# Active development with hot reload
Start-Process powershell.exe -ArgumentList '-NoExit', '-Command', '$Host.UI.RawUI.WindowTitle = ''Frontend (5233)''; dotnet run --project src/redmuffin.Blazor.StaticWeb --launch-profile Watch'

# Quick start, no hot reload (verification if a window is preferred)
Start-Process powershell.exe -ArgumentList '-NoExit', '-Command', '$Host.UI.RawUI.WindowTitle = ''Frontend (5233)''; dotnet run --project src/redmuffin.Blazor.StaticWeb --launch-profile https'

# Ready check (short — site is up in a few seconds)
$timeout = 15; $start = Get-Date
while (-not (Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue) -and ((Get-Date) - $start).TotalSeconds -lt $timeout) { Start-Sleep -Milliseconds 250 }
if (-not (Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue)) { Write-Error 'Site failed to start within 15s' }
```

**Check if already running:**

```powershell
$port = 5233  # from launchSettings.json
if (Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue) {
    Write-Host "Site is already running on port $port — skip startup, reuse agent-browser session"
} else {
    Write-Host "Site is not running — start it first (default: frontend-only background host)"
}
```

**Stop the site (required before rebuild / assembly edits):**

```powershell
$port = 5233  # from launchSettings.json
Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue | Where-Object { $_.OwningProcess -ne 0 } | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue }
```

**Profile details** (`src/redmuffin.Blazor.StaticWeb/Properties/launchSettings.json`):

| Profile       | Command                                                                         | URL                     | Hot Reload | Use Case                              |
| ------------- | ------------------------------------------------------------------------------- | ----------------------- | ---------- | ------------------------------------- |
| `https`       | `dotnet run` (Project)                                                          | `http://localhost:5233` | No         | Default agent QA / quick verification |
| `Watch`       | `dotnet watch --non-interactive -- -p:TreatWarningsAsErrors=false` (Executable) | `http://localhost:5233` | Yes        | Active development                    |
| `IIS Express` | IISExpress                                                                      | dynamic                 | Yes        | Legacy — do not use                   |

### ConfigureAwait Fixer

A ConfigureAwait fixer plugin auto-adds `.ConfigureAwait(false)` to
`.cs` files on agent writes. It uses the official CA2007 analyzer via Roslyn.

- **Agent writes:** Plugin fixes CA2007 before any tool reads the file.
  Zero configuration. Logs to the plugin's log file.
- **dotnet watch:** CA2007 is a warning, not an error ΓÇö gated by
  `TreatWarningsAsErrors` in `Directory.Build.props` and the
  `-p:TreatWarningsAsErrors=false` flag in the Watch launch profile.
  The watch loop continues. The plugin handles the common case;
  pre-commit `dotnet build` catches anything missed.
- **dotnet build:** `TreatWarningsAsErrors=true`. Every CA2007
  violation is a hard error. Never commit with a CA2007 violation.

### Hot Reload ΓÇö What Works, What Doesn't

**Supported edits** (applied without restart):

- `.razor` markup changes (HTML, CSS classes, text content)
- `.cs` method body changes (add/remove/edit variables, expressions, statements)
- Adding new types, nested classes
- Adding static/instance methods, fields, events, properties to existing types
- Lambda expression and local function body changes
- CSS changes (compiled CSS only ΓÇö SCSS requires rebuild, see SCSS/Sass note below)

**Rude edits** (require restart ΓÇö `dotnet watch` will prompt or auto-restart):

- Adding a new `await` expression to a method that didn't have one
- Adding a new `yield` expression
- Changing method parameter names
- Removing a component parameter attribute (component is disposed and re-initialized)
- Changes to `Program.cs` startup logic (middleware, service configuration, route creation)
- Adding/removing `@inject` directives
- Changing `@inherits` or `@layout` directives

**Hot Reload Troubleshooting**:

- **"No hot reload changes to apply"**: Common Blazor WASM quirk ΓÇö save the file again, or `agent-browser --session redmuffin press Control+R`
- **Force restart**: `agent-browser --session redmuffin press Control+R` ΓÇö Blazor WASM reinitializes and picks up latest assemblies
- **Disable hot reload**: Set `"hotReloadEnabled": false` in `launchSettings.json` profile
- **Env var alternative**: `DOTNET_WATCH_RESTART_ON_RUDE_EDIT=1` achieves the same as `--non-interactive`

**SCSS/Sass note**: Hot reload does NOT process SCSS changes directly. The `sass --watch` background process handles SCSS compilation automatically. After editing `.scss` files, the compiled CSS lands in `wwwroot/css/` and `dotnet watch` detects it instantly. No manual build step required.
See `rm-dev-tools` for tool installation and `rm-scss` for SCSS conventions.

### Style Change Workflow (CSS / SCSS)

When the dev server is running (`sass --watch` + `dotnet watch`), the pipeline is fully automatic:

1. Edit the `.scss` file
2. Sass watcher auto-compiles `app.css` and `app.min.css` (~0.5s)
3. `dotnet watch` detects the CSS file change and hot-reloads the browser
4. Verify: `agent-browser --session redmuffin snapshot -i` or `eval` to confirm computed styles

**Never restart the dev server for a CSS change. Never run manual `sass` commands.** The watchers are running. The pipeline handles it.

**If `dotnet watch` decides to rebuild** (rare, caused by non-CSS file changes detected during the same save cycle): the rebuild produces new WASM assembly fingerprints. This causes an SRI integrity mismatch on the next page load. See "Site Broken ΓÇö Recognition Pattern" below.

### Site Broken ΓÇö Recognition Pattern

Blazor WASM enters a broken state after a `dotnet watch` rebuild when SRI hashes in `blazor.boot.json` no longer match the rebuilt assets. This is unrelated to your code changes ΓÇö it is infrastructure breakage from the hot reload cycle.

**Instant recognition** (`agent-browser --session redmuffin snapshot -i` and `console`):

| Signal            | What you see                                             | Meaning                            |
| ----------------- | -------------------------------------------------------- | ---------------------------------- |
| Page snapshot     | `"An unhandled error has occurred."` and `"Reload"` link | Blazor error boundary triggered    |
| Loading indicator | `"98%"`                                                  | WASM module failed to instantiate  |
| Console errors    | `"SRI's integrity checks failed"`                        | Hash mismatch on `.wasm` or `.pdb` |
| Console errors    | `"still waiting on run dependencies: wasm-instantiate"`  | Module never loaded                |

**Any one of these signals ΓåÆ site is broken. Stop and fix. Do not keep navigating or testing.**

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

# Kill all processes on the port in one pipeline (verify not IDE-owned first ΓÇö see PROCESS MANAGEMENT > Identify IDE-owned processes)
Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue | Where-Object { $_.OwningProcess -ne 0 } | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue }
```

## PROCESS MANAGEMENT (Windows, PowerShell 7.4+)

### Kill processes

```powershell
# agent-browser orphans only (never user Brave)
$ab = Join-Path $env:USERPROFILE '.agent-browser\browsers'
Get-CimInstance Win32_Process | Where-Object {
  ($_.Name -in 'chrome.exe','agent-browser-win32-x64.exe') -and
  ($_.CommandLine -like "*$ab*" -or $_.Name -eq 'agent-browser-win32-x64.exe' -or
   $_.CommandLine -like '*\.agent-browser\*')
}

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
- Always check ParentProcessId before killing processes ΓÇö protect IDE-owned processes
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

Load `rm-agent-browser-companion` before the first command.

| Task                | Tool / skill                                           |
| ------------------- | ------------------------------------------------------ |
| Navigate to URL     | `agent-browser --session redmuffin open <url>`         |
| Take snapshot       | `agent-browser --session redmuffin snapshot -i`        |
| Screenshot          | `agent-browser --session redmuffin screenshot`         |
| Click/fill/interact | `agent-browser find role/label/...` (see skill)        |
| Console / errors    | `agent-browser --session redmuffin console` / `errors` |
| Network / HAR       | `agent-browser --session redmuffin network …`          |
| Web Vitals          | `agent-browser --session redmuffin vitals --json`      |

### Code intelligence

| Task                     | Tool / skill                      |
| ------------------------ | --------------------------------- |
| Symbol refs, definitions | Harness `lsp` tool (when enabled) |
| Syntax-shape search      | `rm-structural-search` + ast-grep |
| Text / name search       | `grep` (builtin)                  |
| Compilation truth        | `dotnet build`                    |

### Dev Tools

| Task                      | Skill          |
| ------------------------- | -------------- |
| Tool install & versions   | `rm-dev-tools` |
| SCSS compilation workflow | `rm-dev-tools` |
| SCSS conventions          | `rm-scss`      |

### Rules

- Never use external tools when a builtin equivalent can express the same query.
- Use `es.exe` only when builtin tools cannot express the query

## COMMANDS

| Command      | Purpose         | When                                   |
| ------------ | --------------- | -------------------------------------- |
| `rm:dev`     | Load this skill | Need dev workflow guidance             |
| `rm:cleanup` | Run cleanup     | End of session, before switching tasks |
| `rm:commit`  | Create commit   | Ready to commit changes                |
| `rm:nuget`   | Manage packages | Add/remove/update NuGet packages       |

## CONTEXT

This skill is the canonical reference for development workflow on Windows. It replaces the old GitHub Copilot instructions file with focused, current guidance. For cleanup execution, use `rm-dev-shutdown`. For commit workflow, use `rm-commit`.
