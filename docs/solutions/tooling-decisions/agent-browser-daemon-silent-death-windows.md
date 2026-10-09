---
title: "agent-browser daemon dies silently on Windows when the launching CLI's stderr pipe closes"
date: 2026-10-09
category: tooling-decisions
module: agent-browser
problem_type: runtime_error
component: tooling
symptoms:
  - "get url answers about:blank with exit code 0 after a healthy working session"
  - "Page state, localStorage, and preset data vanish between agent-browser commands"
  - "No error output precedes the reset; the daemon dies without any log line"
  - "Deaths land minutes after a command chain succeeds, usually on the first command of the next chain (session history)"
root_cause: upstream_tool_defect
resolution_type: workflow_improvement
severity: medium
retire_when: "Upstream PR vercel-labs/agent-browser#2051 merges and the installed npm agent-browser carries the fix — check `gh pr view 2051 --repo vercel-labs/agent-browser --json state,mergedAt` plus `agent-browser --version`"
tags:
  - agent-browser
  - windows
  - daemon
  - cdp
  - browser-qa
  - silent-failure
---

# agent-browser daemon dies silently on Windows when the launching CLI's stderr pipe closes

> Satellite learning. The full recovery procedure lives in
> `.agents/skills/redmuffin/rm-agent-browser-companion/SKILL.md` §2.8 — the
> runtime source of truth. This doc records the root cause, the failed
> approaches, and the upstream links; do not re-copy the procedure here.

## Problem

During long agent-browser QA runs on Windows, the browser daemon dies without
any output. The next command relaunches a fresh daemon plus Chromium on
`about:blank`, reports exit code 0, and all page state is gone. The failure
looks like the CLI forgetting the session.

## Symptoms

- `get url` answers `about:blank` with exit code 0 after a healthy working
  session.
- Page state, localStorage, and preset data vanish between agent-browser
  commands.
- No error output precedes the reset; the daemon dies without any log line.
- Deaths land minutes after a command chain succeeds, usually on the first
  command of the next chain (session history).

## What Didn't Work

- **Parallel command bursts** — blamed first because collapses clustered
  around harness command bursts. Idle deaths during single-stream image QA
  falsified the parallel-only theory (session history).
- **Page weight** — a heavy Blazor page was suspected as the trigger. The
  daemon also died after eight idle minutes on a light page, so page weight
  was ruled out (session history).
- **`doctor --offline --quick`** — passes while the daemon is alive and
  prevents nothing; the daemon has no stderr surface to report its own death
  (session history).
- **Full restart between chains** — a fresh daemon plus Chromium opens
  `about:blank` and loses all state. Upstream also restarts the daemon
  silently on any launch-flag or env change (#1742, #1835, #1939).
- **`connect <port>`** — the attach does not stick across commands (#2001).
- **First manual attach attempt** — failed because `redmuffin.config` (the
  launch-fingerprint file) did not exist until one normal CLI launch wrote it
  (session history).

## Solution

Attach every session to a manually started daemon whose stderr is redirected
to a file. The complete four-step procedure — probe with `session info --json`,
let one normal launch write the fingerprint, start the detached daemon with
`2>` on a log file, attach and verify the pid — is in
`rm-agent-browser-companion` SKILL.md §2.8.

Verified 2026-10-09 on agent-browser 0.38.2: the manually attached daemon
survived eight minutes of idle with the page and preset localStorage intact.
Normal daemons died within minutes of their launching CLI exiting.

## Why This Works

Upstream vercel-labs/agent-browser#1993: in that repo's source, the upstream
path `cli/src/native/daemon.rs` redirects the daemon's stderr to `/dev/null`
only under `#[cfg(unix)]`. On Windows the
daemon inherits the launching CLI's piped stderr. The CLI exits immediately,
so the next daemon warning write hits a closed pipe and panics the daemon.
Blazor dev pages emit those trigger warnings routinely — for example
"failed to prepare new page session", "failed to prepare attached page
session", and "failed to prepare iframe session controls". The panic kills the daemon silently; the next command then
relaunches a fresh daemon and browser, and the session state is lost.
Redirecting stderr to a file keeps the write target open for the daemon's
lifetime. PR #2051 carries the upstream fix (unmerged as of 2026-10-09).

## Prevention

- Run long or multi-chain QA on the §2.8 manual-daemon attach.
- Keep launch flags and `AGENT_BROWSER_*` env identical across every command;
  a value change still restarts the daemon silently (#1742/#1835/#1939).
- After a close-then-open, redirect CLI output to files rather than letting
  PowerShell capture it — captured output hangs the CLI (#1407).
- Treat `get url` → `about:blank` with exit code 0 as a dead daemon, and check
  the live pid against `~\.agent-browser\<session>.pid`.

## Related Issues

- Procedure source of truth: `rm-agent-browser-companion` SKILL.md §2.8.
- Upstream root cause and fix: vercel-labs/agent-browser #1993 and PR #2051
  (unmerged as of 2026-10-09).
- Same symptom family, different triggers: #2079 (macOS silent browser swap),
  #1476 (stream reset).
- Windows failure-reading neighbors: #1407 (CLI hang with captured output),
  #1878 (unknown commands exit 0), #1713 (no per-command deadline).
- Human twin doc: `docs/agent-browser-guide-2026-06-20.md`.
- Same Windows agent-tooling family:
  `docs/solutions/tooling-decisions/grok-build-lsp-roslyn-windows.md`
  (direct-spawn child processes and silent failure modes).
