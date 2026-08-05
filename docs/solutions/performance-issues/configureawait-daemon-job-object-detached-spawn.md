---
title: "ConfigureAwaitFixer daemon must spawn outside the harness Job Object"
date: 2026-08-05
category: performance-issues
module: tools
problem_type: performance_issue
component: tooling
severity: high
symptoms:
  - "Grok PostToolUse formatter hangs until the ~30 s orchestrator kill"
  - "Cold --fix works once, then every later edit pays cold MSBuild cost again"
  - "Daemon process disappears when the spawning hook or terminal command ends"
  - "morpheus-hook-failures.jsonl shows START without a matching FAIL (external kill)"
root_cause: wrong_api
resolution_type: code_fix
tags:
  - configureawait
  - daemon
  - job-object
  - task-scheduler
  - posttooluse
  - windows
  - detached-spawn
  - fix-client
related_components:
  - development_workflow
  - tooling
---

# ConfigureAwaitFixer daemon must spawn outside the harness Job Object

## Problem

The warm ConfigureAwaitFixer daemon is required so per-edit CA2007 fixes stay under the PostToolUse budget. When the client started that daemon with in-process `Process.Start`, Windows Job Objects used by the agent harness killed the daemon with the hook, so every edit re-paid cold MSBuild cost and often hit the 30 s timeout.

## Symptoms

- PostToolUse / formatter path stalls near the orchestrator limit (~30 s), then fails or is killed.
- First `--fix` after a manual start can look fine; the next edit after the spawner exits is cold again.
- Process list shows no surviving `--daemon` after the client returns.
- Daemon log may show `Daemon starting` and even `Responded`, while the hook still times out if the client is stuck on stdio or a dead pipe.
- Failure JSONL may record START only — an externally killed process never logs its own FAIL.

## What Didn't Work

- **In-job `Process.Start` with redirected stdio** — clears inherit flags and closes client handles, but the child is still inside the harness Job Object and dies when the hook/job completes. Empirically, a direct-start daemon died ~25–30 s after the spawning command; a Task Scheduler–parented daemon survived.
- **Always-on fixed Windows service / logon task** — conflicts with the product requirement of **no fixed service** and dynamic start on first need.
- **One-shot `--file` fallback when the daemon fails** — hides daemon bugs and keeps paying cold cost; contract is fail-loud on daemon failure.
- **Loose client timeouts (minutes)** — still loses to the non-negotiable ~30 s harness kill; the client must own a wall budget under that limit.
- **Passing isolation only via environment variables across Task Scheduler** — scheduler-launched processes do not reliably inherit the client's env; instance, log, and idle must be CLI args.

## Solution

Keep **dynamic, on-demand** daemon start (no fixed service). Change only how the process is born and how long the client will wait.

### 1. Detached spawn via Task Scheduler (Windows)

`FixClient` demand-starts the daemon through the Task Scheduler 2.0 COM API (`Schedule.Service`): register a hidden one-shot task, `Run`, delete the task definition. The process is parented by the Task Scheduler service, **not** by the hook Job Object.

```csharp
// tools/src/redmuffin.Tools.ConfigureAwaitFixer/FixClient.cs
// SpawnDaemonDetached → TaskSchedulerSpawn.RunDetached(...)
// No Process.Start child of the client. Fail loud if Schedule.Service is unavailable.
```

### 2. Forward isolation as CLI args

The client reads `CONFIGUREAWAITFIXER_INSTANCE`, `CONFIGUREAWAITFIXER_LOG`, and `CONFIGUREAWAITFIXER_IDLE_SECONDS` and appends `--instance`, `--log`, `--idle` on the daemon command line. `Daemon.RunAsync` applies those to the process environment so pipe name, log path, and idle timeout stay consistent for tests and live use.

### 3. Hard client wall budget

One `CancellationTokenSource` for connect + request (22 s in current code) so `--fix` finishes under Host (~25 s) and orchestrator (30 s) budgets. No multi-minute request timeout on the client path.

### 4. Deploy and hook wiring

- Source: `tools/src/redmuffin.Tools.ConfigureAwaitFixer/`
- Deploy: `~/.local/bin/ConfigureAwaitFixer/` (publish after build so the hook binary is not stale)
- Grok formatter entry: `ConfigureAwaitFixer.exe --fix {{file}}` in `~/.grok/hooks/bin/code-formatters.json`

### Verified behavior (2026-08-05)

| Check                       | Result                                                                 |
| --------------------------- | ---------------------------------------------------------------------- |
| Test suite                  | 36/36 passed (`tools/tests/redmuffin.Tools.ConfigureAwaitFixer.Tests`) |
| Cold `--fix` (deployed exe) | exit 0, ~6.2 s                                                         |
| Warm `--fix`                | exit 0, ~107 ms                                                        |
| After client exit           | one `--daemon` process still alive (parent not the client)             |
| Daemon log                  | `Detached daemon spawn requested via Task Scheduler`                   |

## Why This Works

Windows Job Objects can kill an entire process tree when the harness finishes a hook or command. A warm daemon **must outlive** that boundary. Task Scheduler starts the executable under a different parent, so the warm `MSBuildWorkspace` cache survives. Dynamic start still means: connect to the named pipe; if missing, claim the spawn mutex once, detached-spawn, poll until the pipe accepts or the client budget expires. Failures surface as stderr `FATAL` and exit 1 — no silent one-shot fallback.

## Prevention

- Never spawn long-lived formatter daemons with plain `Process.Start` from a Grok/Morpheus/OpenCode hook path on Windows without proving Job Object breakaway.
- Prefer Task Scheduler (or another proven break-out) for detached spawn; do not add a permanent service to paper over lifecycle.
- Keep client wall time strictly under the harness timeout; treat orchestrator kill as non-extendable.
- After CAF changes: rebuild, refresh `tools/src/redmuffin.Tools.ConfigureAwaitFixer/publish/`, copy to `~/.local/bin/ConfigureAwaitFixer/`, then cold/warm `--fix` and confirm a daemon PID remains after the client exits.
- Correlate hangs with: `%TEMP%\morpheus-hook-failures.jsonl`, `~/.grok/logs/configureawait-daemon.log`, and whether a `--daemon` process still exists.
- Preserve `// clj-mutate-manifest-*` blocks when rewriting fixer sources; only the mutation gate refreshes hashes.

## Related Issues

- `docs/solutions/developer-experience/automated-configureawait-fixer.md` — fixer purpose and history (MSBuild / plugin delivery); does not cover Job Object lifecycle.
- `docs/solutions/tooling-decisions/configureawait-auto-fix-research.md` — official CA2007 + MSBuildWorkspace engine choice.
- `docs/solutions/tooling-decisions/configureawait-msbuild-hook-incompatibility.md` — why save-time / external hooks replaced build-time `.targets` deadlocks.
- `docs/configureawait-fixer-status-guide-2026-06-08.md` — dated status snapshot; prefer this learning for daemon spawn lifecycle.
- Morpheus Host (separate repo) bounds formatter wait; it cannot extend the harness 30 s kill — the daemon lifecycle fix lives in the fixer client.
