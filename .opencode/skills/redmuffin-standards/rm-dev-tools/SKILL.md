---
name: rm-dev-tools
description: "Complete dev toolchain reference: required tools, versions, cross-platform install (Arch Linux / Windows 11), SCSS compilation, JS minification, and tool command summaries. Use when installing required tools, troubleshooting missing executables, or looking up the correct dev tool for a task."
---

# rm-dev-tools

Complete development toolchain for the redmuffin.Blazor.StaticWeb project. Every tool required to build, test, style, and deploy. Cross-platform: Arch Linux (Omarchy) and Windows 11.

## Tool Inventory

| Tool                       | Version              | Required | Purpose                      |
| -------------------------- | -------------------- | -------- | ---------------------------- |
| .NET SDK                   | 10.0.104+            | Required | Build, test, publish, format |
| wasm-tools workload        | Latest (SDK-matched) | Required | Blazor WASM compilation      |
| dart-sass                  | 1.99.0               | Required | SCSS → CSS compilation       |
| Node.js                    | Latest LTS           | Required | Host for npx/terser          |
| terser                     | Latest (via npx)     | Required | JS minification              |
| Git                        | Any 2.x              | Required | Version control              |
| Azure Functions Core Tools | 4.x                  | Optional | API integration testing      |
| PowerShell 7+              | 7.4+                 | Required | Scripting (cross-platform)   |
| jq                         | Any 1.x              | Optional | JSON processing in scripts   |

### Not Required Locally

| Tool                       | Where             | Purpose         |
| -------------------------- | ----------------- | --------------- |
| @azure/static-web-apps-cli | CI only           | SWA deployment  |
| dotnet-format              | Built into SDK 10 | Code formatting |

---

## Install by Platform

### Arch Linux (Omarchy)

```bash
# .NET SDK (priority: pacman)
sudo pacman -S dotnet-sdk aspnet-runtime
dotnet workload install wasm-tools

# dart-sass (priority: pacman — extra repo)
sudo pacman -S dart-sass

# Node.js (priority: mise — Omarchy default for dev runtimes)
mise use -g node@latest

# Git
sudo pacman -S git

# Azure Functions Core Tools (priority: AUR)
yay -S azure-functions-core-tools-bin

# PowerShell
sudo pacman -S powershell

# jq
sudo pacman -S jq
```

**Verification**:

```bash
dotnet --version          # 10.0.104+
dotnet workload list       # wasm-tools must appear
sass --version             # 1.99.0+
node --version             # LTS
git --version              # 2.x
func --version             # 4.x (optional)
pwsh --version             # 7.4+
jq --version               # 1.x (optional)
```

### Windows 11

```powershell
# .NET SDK
winget install Microsoft.DotNet.SDK.10
dotnet workload install wasm-tools

# dart-sass
winget install Sass.DartSass

# Node.js
winget install OpenJS.NodeJS.LTS

# Git
winget install Git.Git

# Azure Functions Core Tools
winget install Microsoft.AzureFunctionsCoreTools

# PowerShell (already included in Windows 11)
winget install Microsoft.PowerShell

# jq
winget install jqlang.jq
```

After install, restart terminal or refresh PATH. Verify with the same commands as Linux above.

---

## Workflows

### SCSS Development (watch mode)

Start ONCE per session. Background process — never interact with it during work.

**Linux / macOS**:

```bash
# Check if already running
pgrep -f "sass --watch" > /dev/null || sass --watch scss:wwwroot/css &

# Kill when done
pkill -f "sass --watch"
```

**Windows**:

```powershell
# Check if already running
$running = Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'sass' -and $_.CommandLine -like '*--watch*' }
if (-not $running) { Start-Process sass -ArgumentList '--watch scss:wwwroot/css' -WindowStyle Hidden }

# Kill when done
Get-CimInstance Win32_Process | Where-Object { $_.Name -eq 'sass' -and $_.CommandLine -like '*--watch*' } | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }
```

**How it integrates with `dotnet watch`**:

1. `sass --watch` runs in background, watches `scss/`, auto-compiles to `wwwroot/css/` on every SCSS save
2. `dotnet watch` runs the Blazor site, detects CSS file changes, triggers browser hot reload
3. Edit SCSS → auto-compiled to CSS → Blazor hot reload → browser updates
4. Zero manual compilation steps

### SCSS Production Build

One-shot. Minified, no source maps.

**All platforms**:

```bash
sass --style=compressed --no-source-map scss/app.scss:wwwroot/css/app.min.css
```

Arguments:

- `--style=compressed`: minified output (no whitespace)
- `--no-source-map`: omit `.css.map` files from production output

### JS Minification

One-shot. Via npx (no global install).

**All platforms**:

```bash
npx --yes terser wwwroot/js/page-load-timing.js -o wwwroot/js/page-load-timing.min.js -c
```

Arguments:

- `--yes`: auto-confirm package download (one-off temporary install)
- `-o`: output file
- `-c`: compress (default optimizations)

### Full Production Build

```bash
# 1. SCSS → minified CSS
sass --style=compressed --no-source-map scss/app.scss:wwwroot/css/app.min.css

# 2. JS → minified JS
npx --yes terser wwwroot/js/page-load-timing.js -o wwwroot/js/page-load-timing.min.js -c

# 3. .NET publish (Blazor WASM + API)
dotnet publish src/redmuffin.Blazor.StaticWeb -c Release -p:PublishTrimmed=true
```

---

## What Was Removed

| Old Tool                     | Why Removed                                                                                                                                |
| ---------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| BuildWebCompiler2022 (NuGet) | Wraps LibSass (EOL Oct 2025). Windows-only. Required `dotnet build -c Debug-Sass` after every SCSS edit. Tied SCSS compilation to MSBuild. |
| compilerconfig.json          | BuildWebCompiler config format. Replaced by direct `sass` CLI invocation.                                                                  |

---

## Conflict Prevention

### sass --watch

- **Only ONE instance** must watch the same `scss/` directory. Multiple watchers writing to the same CSS files on save causes race conditions and corrupt output.
- Always check before starting: `pgrep -f "sass --watch"` (Linux) or `Get-CimInstance Win32_Process` (Windows).
- Cleanup kills it: `pkill -f "sass --watch"` (Linux) or `Stop-Process` by command line match (Windows).
- `rm-cleanup` handles this automatically at session end.

### dotnet watch

- `dotnet watch` owns a port (5233 by default). Only ONE instance can bind to that port.
- `rm-cleanup` kills stale dotnet processes before starting a new session.
- The `sass --watch` process has NO port — cannot conflict with `dotnet watch`.

---

## Dev Server (Linux — systemd)

The dev server runs as a transient systemd user service. This detaches it from the bash tool's timeout and provides proper process lifecycle management.

### Architecture

The service launches three processes in one unit:

- `sass --watch scss/app.scss:wwwroot/css/app.css` — expanded, readable in DevTools
- `sass --style=compressed --watch scss/app.scss:wwwroot/css/app.min.css` — minified, ready to commit
- `dotnet watch --launch-profile https` — hot reloads C# + detects CSS changes + auto-refreshes browser

Both sass watchers track imported partials automatically. On every SCSS save, both `app.css` and `app.min.css` update in ~0.45s. `app.min.css` is always fresh — commit it directly.

### Commands

**Start**:

```bash
systemd-run --user \
  --working-directory=/home/flynn/Projects/redmuffin.Blazor.StaticWeb/src/redmuffin.Blazor.StaticWeb \
  -p TimeoutStopSec=1 \
  --unit=redmuffin.Blazor.StaticWeb-sass-dotnet-watch \
  bash -c 'sass --watch scss/app.scss:wwwroot/css/app.css & sass --style=compressed --watch scss/app.scss:wwwroot/css/app.min.css & dotnet watch --launch-profile https'
```

**Stop** (~2s):

```bash
systemctl --user stop redmuffin.Blazor.StaticWeb-sass-dotnet-watch.service && \
  systemctl --user reset-failed redmuffin.Blazor.StaticWeb-sass-dotnet-watch.service
```

_(`reset-failed` is required after every stop. `TimeoutStopSec=1` always force-kills, putting the unit in `failed` state. The sidebar plugin shows a red dot until reset.)_

**Restart**:

```bash
systemctl --user stop redmuffin.Blazor.StaticWeb-sass-dotnet-watch.service && \
  systemctl --user reset-failed redmuffin.Blazor.StaticWeb-sass-dotnet-watch.service && \
  systemd-run --user --working-directory=/home/flynn/Projects/redmuffin.Blazor.StaticWeb/src/redmuffin.Blazor.StaticWeb -p TimeoutStopSec=1 --unit=redmuffin.Blazor.StaticWeb-sass-dotnet-watch bash -c 'sass --watch scss/app.scss:wwwroot/css/app.css & sass --style=compressed --watch scss/app.scss:wwwroot/css/app.min.css & dotnet watch --launch-profile https'
```

**Status**:

```bash
systemctl --user is-active redmuffin.Blazor.StaticWeb-sass-dotnet-watch.service    # active/inactive/failed
ss -tlnp | grep 5233                                 # verify port listening
curl -s -o /dev/null -w '%{http_code}' http://localhost:5233/  # verify responds
```

**View logs** (if server crashes or you need build output):

```bash
journalctl --user-unit=redmuffin.Blazor.StaticWeb-sass-dotnet-watch.service --no-pager -n 40
```

### Design Decisions

**`--working-directory` instead of `cd &&`**: The `cd` command in `bash -c` does not reliably propagate to all background children. systemd's `--working-directory` sets the CWD at the unit level — before bash forks — guaranteeing all child processes inherit the same directory. This is the canonical systemd way.

**`-p TimeoutStopSec=1`**: `dotnet watch` is stateless — no database connections, no pending writes. It ignores SIGTERM entirely. A 1-second timeout means SIGKILL fires after 1s (vs default 90s). Stop completes in ~2s.

**`dotnet watch` as foreground (no `&`)**: If all three processes are backgrounded with `&`, bash exits immediately, systemd sees the service as failed, and unit cleanup is problematic. Keeping `dotnet watch` as the foreground process (no trailing `&`) keeps bash alive and the service properly active.

**`app.min.css` always fresh**: Both sass watchers use file-syntax targets (`scss/app.scss:wwwroot/css/app.css` and `scss/app.scss:wwwroot/css/app.min.css`). File-syntax watchers track imported partials (proven). Directory-syntax watchers (`scss:wwwroot/css`) would output `app.css` only — never `app.min.css`.

### Prerequisites

```bash
# One-time installs
sudo pacman -S dart-sass aspnet-runtime    # SCSS compiler + .NET 10 runtime for dotnet-watch
```

### Dev Server (Windows)

Use the existing `Start-Process powershell` pattern from `rm-dev-workflows`. The Windows PowerShell shell handles process detachment natively.

---

## Commands

| Command                                                                             | Purpose                           | Platform |
| ----------------------------------------------------------------------------------- | --------------------------------- | -------- |
| `sass --version`                                                                    | Verify dart-sass install          | Both     |
| `sass --watch scss:wwwroot/css`                                                     | Dev: auto-compile on SCSS save    | Both     |
| `sass --style=compressed --no-source-map scss/app.scss:wwwroot/css/app.min.css`     | Prod: one-shot minified build     | Both     |
| `npx --yes terser in.js -o out.min.js -c`                                           | JS minification                   | Both     |
| `pgrep -f "sass --watch"`                                                           | Check if watcher is running       | Linux    |
| `pkill -f "sass --watch"`                                                           | Kill the watcher                  | Linux    |
| `systemd-run --user --unit=redmuffin.Blazor.StaticWeb-sass-dotnet-watch ...`        | Start dev server (detached)       | Linux    |
| `systemctl --user stop redmuffin.Blazor.StaticWeb-sass-dotnet-watch.service`        | Stop dev server                   | Linux    |
| `journalctl --user-unit=redmuffin.Blazor.StaticWeb-sass-dotnet-watch.service -n 40` | View dev server logs              | Linux    |
| `fuser -k 5233/tcp`                                                                 | Kill stale port process           | Linux    |
| `dotnet workload list`                                                              | Verify wasm-tools installed       | Both     |
| `func --version`                                                                    | Verify Azure Functions Core Tools | Both     |
