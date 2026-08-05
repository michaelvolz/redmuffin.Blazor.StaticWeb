---
title: "ConfigureAwaitFixer daemon must spawn outside the harness Job Object"
date: 2026-08-05
last_updated: 2026-08-05
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
  - "Detached daemon opens a black Windows Terminal window and steals focus with no return"
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
  - winexe
  - windows-terminal
  - focus
  - observability
related_components:
  - development_workflow
  - tooling
---

# ConfigureAwaitFixer daemon must spawn outside the harness Job Object

## Problem

The warm ConfigureAwaitFixer daemon is required so per-edit CA2007 fixes stay under the PostToolUse budget and approach sub-second warm latency. Two independent Windows constraints both had to be solved:

1. **Job Object kill** — agent harnesses run hooks inside a Job Object that kills the entire descendant tree when the hook ends. An in-job `Process.Start` daemon dies with the hook, so every edit re-pays cold MSBuild cost (~6 s) and often hits the ~30 s orchestrator timeout.
2. **Default terminal focus steal** — when Windows Terminal Preview is the default console host, spawning a **console-subsystem** daemon (even via Task Scheduler, VBS style 0/4, or "no activate") opens a black WT window, steals keyboard focus, and **never returns focus** to the agent session. Post-hoc `SetForegroundWindow` cannot reclaim it reliably.

## Symptoms

- PostToolUse / formatter path stalls near the orchestrator limit (~30 s), then fails or is killed.
- First `--fix` after a manual start can look fine; the next edit after the spawner exits is cold again (~6 s).
- Process list shows no surviving `--daemon` after the client returns (Job Object path).
- Daemon log may show `Daemon starting` and even `Responded`, while the hook still times out if the client is stuck on stdio or a dead pipe.
- Failure JSONL may record START only — an externally killed process never logs its own FAIL.
- A black Terminal Preview window appears on daemon spawn; focus stays there; agent UI is dead until the user clicks back (console-subsystem + DefTerm path).

## What Didn't Work

- **In-job `Process.Start` with redirected stdio** — clears inherit flags and closes client handles, but the child is still inside the harness Job Object and dies when the hook/job completes. Empirically, a direct-start daemon died ~25–30 s after the spawning command; a Task Scheduler–parented daemon survived.
- **Always-on fixed Windows service / logon task** — conflicts with the product requirement of **no fixed service** and dynamic start on first need.
- **One-shot `--file` fallback when the daemon fails** — hides daemon bugs and keeps paying cold cost; contract is fail-loud on daemon failure.
- **Loose client timeouts (minutes)** — still loses to the non-negotiable ~30 s harness kill; the client must own a wall budget under that limit.
- **Passing isolation only via environment variables across Task Scheduler** — scheduler-launched processes do not reliably inherit the client's env; instance, log, and idle must be CLI args.
- **Task Scheduler → console `ConfigureAwaitFixer.exe`** — survives the Job Object but opens a console. With Windows Terminal Preview as default terminal, that becomes a focus-stealing black window.
- **Task Scheduler → `wscript` → `WshShell.Run` style 4 (SW_SHOWNOACTIVATE)** — still shows a console; WT activates it; focus never returns.
- **Same path with style 0 (SW_HIDE)** — **fails when Windows Terminal is the default terminal**. Confirmed community report: classic VBS hide and PowerShell `-WindowStyle Hidden` do not hide under WT DefTerm ([microsoft/terminal#12464](https://github.com/microsoft/terminal/issues/12464)). Style 0 was also never sufficient alone even if deployed: the PE console subsystem still triggers DefTerm.
- **Assuming "no visible window" means "broken"** — after WinExe, success looks like _nothing_ on screen. Verify via process list, log, and warm latency — not via a terminal window.

## Solution

Keep **dynamic, on-demand** daemon start (no fixed service). Fix both birth constraints: **who parents the process** and **whether a console is allocated**.

### 1. Detached spawn via Task Scheduler (Windows) — Job Object breakout

`FixClient` demand-starts the daemon through the Task Scheduler 2.0 COM API (`Schedule.Service`): register a hidden one-shot task, `Run`, delete the task definition. The process is parented by the Task Scheduler service (`svchost.exe`), **not** by the hook Job Object.

```csharp
// tools/src/redmuffin.Tools.ConfigureAwaitFixer/FixClient.cs
// SpawnDaemonDetached → TaskSchedulerSpawn.RunDetached(executable, arguments, ...)
// No Process.Start child of the client. Fail loud if Schedule.Service is unavailable.
```

### 2. WinExe (Windows subsystem) — no console, no DefTerm window

`ConfigureAwaitFixer.csproj` sets `<OutputType>WinExe</OutputType>`. The PE **subsystem is Windows GUI (2)**, not Console (3). Windows does **not** allocate a console, so Windows Terminal Preview never attaches and never steals focus.

- Client path (`--fix`) still writes `FATAL` / fix messages to **stderr**; hooks capture redirected handles.
- Daemon has no interactive console; all daemon signal goes to **DaemonLog** (file).

Do **not** reintroduce console-subsystem hide tricks (VBS style 0/4, `CreateNoWindow` alone without breakout, `conhost --headless` wrappers) as the primary design — WinExe removes the need for them on the production binary.

### 3. Forward isolation as CLI args

The client reads `CONFIGUREAWAITFIXER_INSTANCE`, `CONFIGUREAWAITFIXER_LOG`, and `CONFIGUREAWAITFIXER_IDLE_SECONDS` and appends `--instance`, `--log`, `--idle` on the daemon command line. `Daemon.RunAsync` applies those to the process environment so pipe name, log path, and idle timeout stay consistent for tests and live use.

### 4. Hard client wall budget

One `CancellationTokenSource` for connect + request (**22 s** in current code) so `--fix` finishes under Host (~25 s) and orchestrator (30 s) budgets. No multi-minute request timeout on the client path.

### 5. Deploy and hook wiring

- Source: `tools/src/redmuffin.Tools.ConfigureAwaitFixer/`
- Build copies to `publish/` (csproj `CopyAnalyzerDll` target)
- Deploy: copy `publish/` → `~/.local/bin/ConfigureAwaitFixer/` (stale deploy is the #1 false "still broken" cause)
- Grok formatter entry: `ConfigureAwaitFixer.exe --fix {{file}}` in `~/.grok/hooks/bin/code-formatters.json`
- Verify PE after deploy: subsystem **2** (WinGUI), not **3** (Console)

### Verified behavior (2026-08-05)

| Check                        | Result                                                                                        |
| ---------------------------- | --------------------------------------------------------------------------------------------- |
| Test suite                   | 36/36 passed (`tools/tests/redmuffin.Tools.ConfigureAwaitFixer.Tests`)                        |
| PE subsystem of deployed exe | **2** (Windows GUI / WinExe)                                                                  |
| Cold `--fix` (deployed exe)  | exit 0, ~6.2–6.5 s                                                                            |
| Warm `--fix`                 | exit 0, **~150–180 ms** (e.g. 154 ms, 177 ms)                                                 |
| After client exit            | one `--daemon` process still alive, parent **`svchost.exe`**                                  |
| Visible window               | **none** (correct for WinExe)                                                                 |
| Daemon log                   | `Detached daemon spawn requested via Task Scheduler (exe=...)` then `Daemon starting (pid …)` |
| Warm reuse                   | same PID; log has `Request` / `Responded` **without** a new `Daemon starting`                 |

## Why This Works

Windows Job Objects can kill an entire process tree when the harness finishes a hook or command. A warm daemon **must outlive** that boundary. Task Scheduler starts the executable under a different parent, so the warm `MSBuildWorkspace` cache survives across edits.

Separately, the **default terminal application** (here: Windows Terminal Preview) hosts **console-subsystem** processes. Hide/no-activate APIs that worked with classic conhost do **not** prevent a visible, focus-stealing WT window. Building as **WinExe** means no console is created at all, so DefTerm never runs.

Dynamic start still means: connect to the named pipe; if missing, claim the spawn mutex once, detached-spawn, poll until the pipe accepts or the client budget expires. Failures surface as stderr `FATAL` and exit 1 — no silent one-shot fallback.

## Observability (headless daemon)

There is **no daemon console** after WinExe. That is intentional. Bugs are diagnosed from **files + process state**, not from a window.

### What exists today (enough for most failures)

| Signal                        | Where                                                                          | What it tells you                                                                                                                |
| ----------------------------- | ------------------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------- |
| Daemon lifecycle              | `~/.grok/logs/configureawait-daemon.log` (override: `CONFIGUREAWAITFIXER_LOG`) | `Daemon starting (pid)`, `Listening on pipe …`, `Idle timeout reached`, `Daemon exited`                                          |
| Spawn path                    | same log (client also writes here)                                             | `Detached daemon spawn requested via Task Scheduler (exe=…)`                                                                     |
| Per request                   | same log                                                                       | `Request: <path>`, `Opening project <csproj>` (cold/workspace miss), `Responded: …` / `Failure response: …`, `Fixed N await(s)…` |
| Workspace / analyzer failures | same log                                                                       | `Workspace: …`, `FATAL: …`, pipe accept failures                                                                                 |
| Client hard fail              | stderr of `--fix` **and** daemon log                                           | `FATAL: …` with client process id on some paths                                                                                  |
| Host / hook timeout           | `%TEMP%\morpheus-hook-failures.jsonl`                                          | Host-owned START / FAIL / timeout (Morpheus); empty at incident time means Host never ran or external kill beat logging          |
| Alive + parent                | process list (`ConfigureAwaitFixer.exe --daemon`)                              | Surviving PID; parent should be **svchost** (Task Scheduler), not the hook                                                       |
| Latency class                 | wall clock of `--fix` + log shape                                              | Cold ≈ multi-second + `Opening project`; warm ≈ ~100–200 ms, no new `Daemon starting`                                            |

`DaemonLog` is append-only, timestamped (`yyyy-MM-dd HH:mm:ss.fff`), levels INFO/ERROR. A failed log write never crashes the process; the error is attached to the next successful line.

### Gaps (know them; do not invent console noise)

These are **real limits**, not reasons to bring the window back:

1. **External kill is silent** — Job Object or user kill leaves no `Daemon exited` line. Correlate with missing PID + incomplete Morpheus JSONL.
2. **No per-request duration field** — infer from wall clock and whether `Opening project` appears; there is no `durationMs=` line.
3. **Connect/retry loop is quiet** — client polls the pipe under the 22 s budget without logging each attempt; only terminal `FATAL` is loud.
4. **Spawn is "requested", not "confirmed"** — Task Scheduler `Run` success is not separately logged; confirmation is the next `Daemon starting` line (or client timeout).
5. **Unstructured text log** — no JSONL rotation policy in-product; path is a single append file under `~/.grok/logs/`.
6. **Client stderr is the hook-visible channel** — success with empty message is silent on stderr (by design); warm "did nothing" looks like exit 0 + quiet.

### Minimum debug checklist when something "disappears"

1. Is `ConfigureAwaitFixer.exe --daemon` running? Parent = `svchost.exe`?
2. Tail `~/.grok/logs/configureawait-daemon.log` for the last `Daemon starting` / `Request` / `FATAL`.
3. Check `%TEMP%\morpheus-hook-failures.jsonl` for Host timeout vs missing START.
4. Confirm deploy is current: `~/.local/bin/ConfigureAwaitFixer/ConfigureAwaitFixer.exe` timestamp and PE subsystem **2**.
5. Time one `--fix` on a known `.cs` file: multi-second cold vs ~150 ms warm tells you cache state.

If those five cannot explain a failure, the missing pieces are usually **(a)** per-request duration in the log or **(b)** client connect-attempt traces — add those deliberately; do not reintroduce a console for "visibility."

## Prevention

- Never spawn long-lived formatter daemons with plain `Process.Start` from a Grok/Morpheus/OpenCode hook path on Windows without proving Job Object breakaway.
- Prefer Task Scheduler (or another proven break-out) for detached spawn; do not add a permanent service to paper over lifecycle.
- Keep the production binary as **WinExe** while Windows Terminal (or any DefTerm) may host console apps. Do not "fix focus" by re-adding VBS hide scripts against a console PE.
- Keep client wall time strictly under the harness timeout; treat orchestrator kill as non-extendable.
- After CAF changes: rebuild, refresh `publish/`, copy to `~/.local/bin/ConfigureAwaitFixer/`, verify subsystem 2, then cold/warm `--fix` and confirm a daemon PID remains after the client exits **with no new terminal window**.
- Correlate hangs with: `%TEMP%\morpheus-hook-failures.jsonl`, `~/.grok/logs/configureawait-daemon.log`, and whether a `--daemon` process still exists under `svchost`.
- Preserve `// clj-mutate-manifest-*` blocks when rewriting fixer sources; only the mutation gate refreshes hashes.
- Treat "no window" as success for spawn; use log + process list, not a console, as the health surface.

## Related Issues

- `docs/solutions/developer-experience/automated-configureawait-fixer.md` — fixer purpose and history (MSBuild / plugin delivery); does not cover Job Object lifecycle.
- `docs/solutions/tooling-decisions/configureawait-auto-fix-research.md` — official CA2007 + MSBuildWorkspace engine choice.
- `docs/solutions/tooling-decisions/configureawait-msbuild-hook-incompatibility.md` — why save-time / external hooks replaced build-time `.targets` deadlocks.
- `docs/configureawait-fixer-status-guide-2026-06-08.md` — dated status snapshot; prefer this learning for daemon spawn lifecycle and WinExe.
- Morpheus Host (separate repo) bounds formatter wait; it cannot extend the harness 30 s kill — the daemon lifecycle fix lives in the fixer client.
- External: [microsoft/terminal#12464](https://github.com/microsoft/terminal/issues/12464) — hide/no-activate ignored when Windows Terminal is the default terminal.
