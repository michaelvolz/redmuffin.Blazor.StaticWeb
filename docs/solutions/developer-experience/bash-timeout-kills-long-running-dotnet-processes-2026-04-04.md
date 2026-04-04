---
date: 2026-04-04
title: Bash timeout kills long-running dotnet processes
module: developer-experience
tags: [opencode, bash, dotnet-watch, timeout, long-running-process]
problem_type: integration-issue
---

# Bash Timeout Kills Long-Running dotnet Processes in OpenCode

## Problem

Running `dotnet watch` (or any long-running dev server) via the `bash` tool causes the process to be **killed when the timeout expires**. The default timeout is 120000ms (2 minutes), but even explicit timeouts like 30000ms will terminate the process.

This means:

- The site starts, serves requests, then **dies silently** when timeout hits
- DevTools shows `ERR_CONNECTION_REFUSED` after the kill
- The agent has no indication the process was terminated

## Root Cause

The `bash` tool runs commands in a persistent shell session but **enforces a hard timeout**. When the timeout fires, the shell session and all child processes are terminated. `dotnet watch` is a long-running process that never exits on its own — it's designed to run until Ctrl+C.

## Solution

**Never run long-running dev servers via `bash`.** Use `Start-Process powershell.exe` to launch in a separate window:

```powershell
# CORRECT: Starts in a separate window, survives indefinitely
Start-Process powershell.exe -ArgumentList '-NoExit', '-Command', '$Host.UI.RawUI.WindowTitle = ''Frontend (5233)''; dotnet run --project src/redmuffin.Blazor.StaticWeb --launch-profile Watch'

# WRONG: Killed after timeout expires
dotnet run --project src/redmuffin.Blazor.StaticWeb --launch-profile Watch
```

Then poll for readiness:

```powershell
$port = 5233  # from launchSettings.json
while (-not (Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue)) { Start-Sleep -Milliseconds 500 }
Write-Host "Port $port is ready"
```

For the full executable reference (all profiles, hot reload behavior, troubleshooting), see `.opencode/skills/rm-dev-workflows/SKILL.md` under `SITE STARTUP`.

## When to Use Each Approach

| Scenario                                    | Tool                               | Why                                       |
| ------------------------------------------- | ---------------------------------- | ----------------------------------------- |
| Start dev server (dotnet watch, dotnet run) | `Start-Process powershell.exe`     | Long-running, must survive beyond timeout |
| Build, test, one-shot commands              | `bash`                             | Completes quickly, output needed          |
| Check port availability                     | `bash` with `Get-NetTCPConnection` | Quick, returns immediately                |
| Kill processes                              | `bash` with `Stop-Process`         | Quick, returns immediately                |

## Symptoms of This Bug

1. Site starts successfully (`Now listening on: http://localhost:5233`)
2. DevTools can navigate and interact briefly
3. Suddenly `ERR_CONNECTION_REFUSED` on all requests
4. No error in agent output — the bash tool just reports "terminated after exceeding timeout"
