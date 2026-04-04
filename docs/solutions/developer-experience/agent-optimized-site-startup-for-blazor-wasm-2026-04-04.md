---
title: Agent-optimized dev server lifecycle for Blazor WASM
date: 2026-04-04
category: developer-experience
module: development_workflow
problem_type: developer_experience
component: tooling
severity: low
applies_when:
  - Setting up a new dev session
  - Agent needs to start the Blazor WASM frontend
  - Hot reload behavior needs to be understood
  - Port conflicts occur during development
tags:
  [
    blazor-wasm,
    dotnet-watch,
    hot-reload,
    start-process,
    agent-workflow,
    launch-profiles,
  ]
---

# Agent-optimized dev server lifecycle for Blazor WASM

## Context

> **Audience:** This doc is optimized for AI agent workflows. Human developers may use alternative patterns (e.g., Visual Studio launch, Windows Terminal profiles).

Starting the Blazor WASM dev server was ad-hoc — agents used `dotnet run` with manual port adjustments, or ran `dotnet watch` via the `bash` tool which killed the process after the timeout expired. No documented guidance existed for which command to use when, what hot reload supports, or how to handle edge cases.

This doc captures the key decisions. The full executable reference lives in `.opencode/skills/rm-dev-workflows/SKILL.md` under the `SITE STARTUP` section.

## Guidance

**Use launch profiles as the single source of truth.** All ports, working directories, and command-line args are defined in `src/redmuffin.Blazor.StaticWeb/Properties/launchSettings.json`. Never hardcode them in scripts.

**Never run dev servers via `bash`** — the bash tool enforces a hard timeout that kills the process and all children. Always use `Start-Process powershell.exe` to launch in a separate, labeled console window:

```powershell
# Read the port from the Watch profile
$port = 5233  # from launchSettings.json profiles.Watch.applicationUrl

# Start the site using the Watch profile (includes --non-interactive, working directory, etc.)
Start-Process powershell.exe -ArgumentList '-NoExit', '-Command', '$Host.UI.RawUI.WindowTitle = ''Frontend (''$port'')''; dotnet run --project src/redmuffin.Blazor.StaticWeb --launch-profile Watch'

# Wait for site to be ready (30s timeout)
$timeout = 30; $start = Get-Date
while (-not (Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue) -and ((Get-Date) - $start).TotalSeconds -lt $timeout) { Start-Sleep -Milliseconds 250 }
if (-not (Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue)) { Write-Error 'Site failed to start within 30s' }
```

**Why launch profiles:** The `Watch` profile already configures `watch --non-interactive`, `workingDirectory: $(ProjectDir)`, and the application URL. Using `--launch-profile Watch` means if the port changes, you only update `launchSettings.json` — no scripts to fix.

**`--non-interactive` is mandatory** — without it, `dotnet watch` prompts for user input on rude edits (adding `await`, changing parameters, etc.), hanging the agent indefinitely. With it, `dotnet watch` automatically restarts the app instead of prompting. The `Watch` profile includes this via `commandLineArgs: "watch --non-interactive"`. Env var alternative: `DOTNET_WATCH_RESTART_ON_RUDE_EDIT=1` achieves the same.

**Use `powershell.exe` (Windows PowerShell 5.1), not `pwsh`** — `powershell.exe` opens a standalone console window with its own color, separate from Windows Terminal tabs. Faster startup (no profile load), clear visual separation, easy kill-by-close.

**Hot reload troubleshooting** — use `chrome-devtools_press_key` with `Control+R` to force a real browser reload (not `chrome-devtools_navigate_page`, which may serve from cache). Blazor WASM reinitializes and picks up latest assemblies.

## Why This Matters

Without this documentation, agents:

- Hang on rude edit prompts (missing `--non-interactive`)
- Kill dev servers via bash timeout (wrong tool for long-running processes)
- Open new terminal tabs instead of standalone windows (using `pwsh` instead of `powershell.exe`)
- Hardcode ports in scripts that drift from launchSettings.json
- Use cached page reloads instead of real reloads (wrong DevTools method)

Each of these wastes agent cycles and requires human intervention to recover.

## When to Apply

- Starting a new dev session
- Agent needs to verify the site is running
- Hot reload isn't applying changes
- Port is already in use

## Examples

**Decision tree** — pick the right profile (launch via `Start-Process powershell.exe`, never via `bash`):

| Situation          | Profile | Command                                                                      |
| ------------------ | ------- | ---------------------------------------------------------------------------- |
| Active development | `Watch` | `dotnet run --project src/redmuffin.Blazor.StaticWeb --launch-profile Watch` |
| Quick verification | `https` | `dotnet run --project src/redmuffin.Blazor.StaticWeb --launch-profile https` |

**Check if already running:**

```powershell
$port = 5233  # from launchSettings.json
if (Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue) {
    Write-Host "Site is already running on port $port — skip startup, navigate existing browser tab"
}
```

**Stop the site:**

```powershell
$port = 5233  # from launchSettings.json
Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue | Where-Object { $_.OwningProcess -ne 0 } | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue }
```

## Related

- `docs/solutions/developer-experience/bash-timeout-kills-long-running-dotnet-processes-2026-04-04.md` — the root cause analysis for why `bash` kills dev servers
- `.opencode/skills/rm-cleanup/SKILL.md` — process cleanup patterns (IDE-owned process protection)
- `AGENTS.md` — mandatory build/test rules referenced by the skill
