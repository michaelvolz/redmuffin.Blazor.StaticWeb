---
date: 2026-04-04
topic: bash-timeout-kills-long-running-dotnet-processes
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

**Never run long-running dev servers via `bash`.** Use `Start-Process` to launch them in a separate window:

```powershell
# CORRECT: Starts in a separate window, survives indefinitely
Start-Process powershell -ArgumentList '-NoExit', '-Command', '$Host.UI.RawUI.WindowTitle = ''Frontend (5233)''; dotnet watch --non-interactive --project src/redmuffin.Blazor.StaticWeb'

# WRONG: Killed after timeout expires
dotnet watch --non-interactive --project src/redmuffin.Blazor.StaticWeb
```

Then poll for readiness:

```powershell
while (-not (Get-NetTCPConnection -LocalPort 5233 -ErrorAction SilentlyContinue)) { Start-Sleep -Milliseconds 500 }
Write-Host "Port 5233 is ready"
```

## When to Use Each Approach

| Scenario                                    | Tool                               | Why                                       |
| ------------------------------------------- | ---------------------------------- | ----------------------------------------- |
| Start dev server (dotnet watch, dotnet run) | `Start-Process powershell`         | Long-running, must survive beyond timeout |
| Build, test, one-shot commands              | `bash`                             | Completes quickly, output needed          |
| Check port availability                     | `bash` with `Get-NetTCPConnection` | Quick, returns immediately                |
| Kill processes                              | `bash` with `Stop-Process`         | Quick, returns immediately                |

## Symptoms of This Bug

1. Site starts successfully (`Now listening on: http://localhost:5233`)
2. DevTools can navigate and interact briefly
3. Suddenly `ERR_CONNECTION_REFUSED` on all requests
4. No error in agent output — the bash tool just reports "terminated after exceeding timeout"
