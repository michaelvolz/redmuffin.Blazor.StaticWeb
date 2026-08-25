---
date: 2026-06-20
last_updated: 2026-08-03
tags:
  - agent-browser
  - browser-automation
  - qa
  - blazor-wasm
  - windows
  - ai-agents
canonical_for:
  - agent-browser
  - browser-qa
---

# agent-browser Guide for redmuffin.Blazor.StaticWeb

> **Human reference** for browser automation on this project. Agents load
> **`rm-agent-browser-companion`** at runtime (co-loads upstream `agent-browser`).
> Keep this guide aligned with that skill when rules change.
>
> Official upstream docs: [agent-browser.dev](https://agent-browser.dev/)

## What Belongs in This File

- **Viewpoint**: Human developer or AI agent automating local frontend-only
  QA (`http://localhost:5233`) or [redmuffin.net](https://redmuffin.net) for
  this Blazor WASM app.
- **What belongs**: Install health, mandatory workflow rules, default local
  host vs opt-in production, Blazor WASM wait patterns, PowerShell-safe
  commands, verified feature matrix, use cases, cleanup, tool relationships.
- **What does NOT belong**: One-off test runner scripts, transient session
  logs, screenshot artifacts, commit instructions, bUnit patterns (see
  `docs/blazor-component-architecture-guide-2026-05-13.md`), or full-stack /
  Azure Functions startup (that lives in `rm-dev-environment` when needed).

## 0 — Critical viewpoint

**agent-browser** is the standard browser automation CLI for this project
(all OS). Rust daemon + CDP against **bundled Chromium** from
`agent-browser install` — **not** the user’s Brave browser. User browser
(all OS): **Brave only** — never Google Chrome the product.

**Use for:** agent-driven QA, accessibility snapshots, annotated
screenshots, live-site navigation, vitals on deployed or local builds.

**Default target:** local **frontend-only** host (`http://localhost:5233`,
synthetic data) — ~99% of tasks. Production and full-stack API are opt-in
when named. Host rules: `rm-dev-environment` Default mode.

**Never use for:** bUnit component tests, parallel command bursts from the
agent harness (daemon corruption on Windows), or assuming upstream examples
work without the Blazor wait below.

### Evaluation verdict (2026-06-20)

| Dimension                       | Rating    | Summary                                                             |
| ------------------------------- | --------- | ------------------------------------------------------------------- |
| Core value for AI agents        | Excellent | `snapshot -i` is token-efficient and a11y-rich                      |
| redmuffin.net compatibility     | Good      | Works after Blazor WASM boot wait                                   |
| Windows / PowerShell ergonomics | Fair      | Prefer `find` locators; sequential commands only                    |
| Reliability under stress        | Fair      | Parallel calls kill the daemon                                      |
| Observability                   | Excellent | Annotated screenshots, vitals, console, network                     |
| Upstream docs                   | Excellent | [agent-browser.dev](https://agent-browser.dev/) + `skills get core` |

---

## 1 — Installation and health

```powershell
npm install -g agent-browser
agent-browser install
agent-browser --version
```

Before sessions after upgrades or odd failures:

```powershell
agent-browser doctor --offline --quick
```

Never run full `agent-browser doctor` on Windows (hangs >60s). Prefer
`--offline --quick` — checks CLI, bundled Chromium path, and stale daemons
(auto-cleaned).

Load version-matched instructions from the CLI (never copy stale
`SKILL.md` from `node_modules`):

```powershell
agent-browser skills list
agent-browser skills get core --full
```

---

## 2 — Mandatory workflow rules

Verified on Windows against https://redmuffin.net. Non-negotiable.

### 2.1 Sequential commands only

- Never issue two `agent-browser` command chains in parallel.
- Never run browser automation in background while another `agent-browser`
  command is in flight.

Parallel calls caused: `about:blank`, `os error 10060`, empty snapshots.
Run one command (or one sequential chain) at a time.

### 2.2 Named session on every command

```powershell
agent-browser --session redmuffin open https://redmuffin.net
agent-browser --session redmuffin snapshot -i
```

Or: `$env:AGENT_BROWSER_SESSION = 'redmuffin'`

Never omit `--session` — wrong daemon or orphan bundled-Chromium windows
result.

### 2.3 Blazor WASM boot wait

Never treat `wait --load networkidle` as sufficient. WASM boots after
network idle.

```powershell
agent-browser --session redmuffin wait --load networkidle
agent-browser --session redmuffin wait --fn "document.querySelector('main') !== null && document.body.innerText.length > 50"
```

Re-run after `reload`, `back`, or tab switches if snapshots are empty.
~4s when the session is healthy.

### 2.4 Re-snapshot after page changes

Refs (`@e1`, `@e2`) go stale after navigation, submit, or re-render:

1. Act
2. Wait
3. `snapshot -i`
4. Use new refs or `find` locators

### 2.5 PowerShell: prefer `find` over bare `@refs`

```powershell
agent-browser --session redmuffin find role link click --name "COUNTER"
agent-browser --session redmuffin find label "Demo Input:" fill "test value"
agent-browser --session redmuffin find role button click --name "Click me"
```

- Never use bare `@eN` without single quotes (`'@e7'`).
- Never use `--%` stop-parsing through `agent-browser`.
- Quoted `'@e7'` works for `get text` / `get value` when the session is
  healthy.

### 2.6 Headed vs headless

| Mode     | Flag       | When                   |
| -------- | ---------- | ---------------------- |
| Headless | (default)  | CI, unattended loops   |
| Headed   | `--headed` | Human watching locally |

- Headless may flash a small black window on Windows — normal.
- Never change `--headed` on a running daemon — `close` the session first.
- Headless works on redmuffin.net with the Blazor `--fn` wait in an
  isolated session.

### 2.7 Cleanup

End of session:

```powershell
agent-browser --session redmuffin close
```

Orphan processes after interrupted sessions:

```powershell
$ab = Join-Path $env:USERPROFILE '.agent-browser\browsers'
Get-CimInstance Win32_Process | Where-Object {
  ($_.Name -in 'chrome.exe','agent-browser-win32-x64.exe') -and
  ($_.CommandLine -like "*$ab*" -or $_.Name -eq 'agent-browser-win32-x64.exe' -or
   $_.CommandLine -like '*\.agent-browser\*')
} | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
agent-browser close --all
```

Never run `close --all` while a human inspects a headed browser.
Never kill user **Brave** — only `~/.agent-browser` bundled-Chromium
processes (`chrome.exe` in agent-browser context = Chromium, not user
Chrome).

---

## 3 — Standard agent workflow

Operational procedure for any browser task (QA, screenshot, nav check,
form).

### 3.0 Default local target (99%) — frontend-only

**This is the default** for agent browser work on this app unless the task
explicitly names production, real Functions HTTP, or another full stack.

| Rule                | Detail                                                                                                                                                          |
| ------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Host                | Frontend only: `http://localhost:5233` (`rm-dev-environment` **Default mode**). Synthetic / mock data. **No API project.**                                      |
| Start               | Reuse if port is up. Otherwise start frontend-only as a harness **background** host — do not multi-minute-wait on startup logs; site is ready in a few seconds. |
| Stop before rebuild | Kill the host before assembly-touching rebuilds; restart after build.                                                                                           |
| Browser             | Sequential `agent-browser` with `--session redmuffin` (§2).                                                                                                     |
| Full stack          | Opt-in only when explicitly required. Do not invent API + SWA startup for ordinary QA.                                                                          |

```powershell
# Confirm / start frontend-only host first (rm-dev-environment Default mode).
# Then:

agent-browser --session redmuffin open http://localhost:5233/

agent-browser --session redmuffin wait --load networkidle
agent-browser --session redmuffin wait --fn "document.querySelector('main') !== null && document.body.innerText.length > 50"

agent-browser --session redmuffin snapshot -i
agent-browser --session redmuffin console
agent-browser --session redmuffin errors

# Route check example
agent-browser --session redmuffin find role link click --name "API HEALTH"
agent-browser --session redmuffin wait --url "**/api-health**"
agent-browser --session redmuffin wait --fn "document.body.innerText.length > 30"
agent-browser --session redmuffin snapshot -i
agent-browser --session redmuffin console

agent-browser --session redmuffin close
```

Port and profiles: `src/redmuffin.Blazor.StaticWeb/Properties/launchSettings.json`
— never invent alternate hosts. Host conventions: `rm-dev-environment`.

### 3.1 Production / deployed target (opt-in)

Use only when the task targets the live site (or another named origin).
Same session rules and Blazor wait; different base URL.

```powershell
agent-browser --headed --session redmuffin open https://redmuffin.net

agent-browser --session redmuffin wait --load networkidle
agent-browser --session redmuffin wait --fn "document.querySelector('main') !== null && document.body.innerText.length > 50"

agent-browser --session redmuffin snapshot -i
agent-browser --session redmuffin screenshot --annotate .tmp/page.png

agent-browser --session redmuffin find role link click --name "COUNTER"
agent-browser --session redmuffin wait --url "**/counter**"
agent-browser --session redmuffin wait --fn "document.body.innerText.length > 30"
agent-browser --session redmuffin snapshot -i

agent-browser --session redmuffin console
agent-browser --session redmuffin errors
agent-browser --session redmuffin vitals --json

agent-browser --session redmuffin close
```

---

## 4 — Verified feature matrix (redmuffin.net, 2026-06-20)

### Reliable (with workflow rules)

| Feature                                 | Notes                                     |
| --------------------------------------- | ----------------------------------------- |
| `open <url>`                            | Headed and headless                       |
| `wait --load networkidle`               | Required, not sufficient alone            |
| `wait --fn "<js>"`                      | Best Blazor boot detector                 |
| `wait --url "**/route**"`               | SPA route changes                         |
| `get url` / `get title`                 | Sanity checks                             |
| `snapshot -i`                           | ~33 refs: nav, regions, form, perf widget |
| `snapshot -i --json`                    | Machine-readable                          |
| `eval "<js>"`                           | e.g. main h1 text                         |
| `get text '@eN'`                        | Quoted refs + live session                |
| `find role/text/label/placeholder`      | **Recommended on PowerShell**             |
| `screenshot` / `--full` / `--annotate`  | Annotate maps `[N]` → `@eN`               |
| `scroll`, `is visible`                  | Basic interaction                         |
| `tab new --label <name>`                | Labeled tabs                              |
| `back` / `reload`                       | When session healthy                      |
| `console`, `errors`, `network requests` | After page load                           |
| `state save` / `state load`             | Persist auth state                        |
| `skills list` / `skills get core`       | Version-synced CLI docs                   |
| `doctor --offline --quick`              | Fast health check                         |

### Flaky / context-dependent

| Feature             | Notes                                        |
| ------------------- | -------------------------------------------- |
| `click '@eN'`       | Needs fresh snapshot; fails on `about:blank` |
| `vitals --json`     | Zeros when page not loaded                   |
| `batch --json`      | Session must be alive                        |
| `wait --text "..."` | Timed out; prefer `--fn` for Blazor          |

### Failed / not recommended

| Feature           | Notes                 |
| ----------------- | --------------------- |
| Parallel commands | Kills daemon          |
| Full `doctor`     | Hangs >60s on Windows |
| `--% click @eN`   | Does not pass through |

### Deferred (upstream docs; not verified here)

`auth save/login`, `--profile`, `--auto-connect`, `network route`, HAR,
`record`, `trace`, `profiler`, `react tree`, `mcp`, `chat`, `dogfood`,
`electron`, `slack` skills.

---

## 5 — Good snapshot on home page

When WASM is loaded, `snapshot -i` includes:

- Nav: HOME, COUNTER, WEATHER, MARKDOWN, FOUNDATION, ICONS, VIDEOS, ARTICLES,
  API HEALTH
- Regions: `main "redmuffin.StaticWeb"`, `Emoji Display`, `Interactive Controls`
- Controls: `Click me`, `Demo Input:` textbox, `Submit Form`
- PAGE PERFORMANCE widget (WASM metrics) as interactive ref

~30 lines vs thousands for raw DOM.

---

## 6 — Use cases

### High value

1. Agent-driven QA — routes, snapshot regressions (**default: local
   frontend-only**, §3.0)
2. Accessibility — ARIA roles, labels, regions in snapshot
3. Visual evidence — `screenshot --annotate` for PRs
4. Live-site complement to bUnit (opt-in origin §3.1)
5. API Health route — navigate + `console` / `network requests` (still
   frontend-only unless real API is named)
6. Web Vitals — `vitals --json`

### Low value / skip

- `chat` mode (harness has an agent)
- Electron/Slack skills
- Auth vault (no login on site)
- Repeatable test-runner scripts (use §3 per task)

---

## 7 — Risks and mitigations

| Risk                     | Mitigation                                      |
| ------------------------ | ----------------------------------------------- |
| Daemon corruption        | Sequential commands; `doctor --offline --quick` |
| Orphan Chrome            | §2.7 cleanup                                    |
| PowerShell `@ref`        | Default to `find` (§2.5)                        |
| Blazor boot race         | `--fn` wait on `main` (§2.3)                    |
| Accidental `close --all` | Close named session only                        |

---

## 8 — Relationship to other tools

| Tool                                         | Role                                                           |
| -------------------------------------------- | -------------------------------------------------------------- |
| bUnit                                        | Component tests in isolation                                   |
| agent-browser + `rm-agent-browser-companion` | Live-app QA — snapshots, screenshots, console, network, vitals |
| `rm-dev-environment` / `rm-dev-shutdown`     | Start/stop frontend-only host; process cleanup                 |
| ce-test-browser                              | Disabled in Grok; use §3 workflow when enabled elsewhere       |

Chrome DevTools MCP is not the project default for browser QA. Prefer
agent-browser. Enable DevTools MCP only when the user opts in and
agent-browser cannot satisfy the task.

---

## Related

- Skill (agent runtime twin): `rm-agent-browser-companion`
- Host startup: `rm-dev-environment` (default frontend-only `:5233`)
- [agent-browser.dev](https://agent-browser.dev/)
- [GitHub: vercel-labs/agent-browser](https://github.com/vercel-labs/agent-browser)
- `docs/blazor-component-architecture-guide-2026-05-13.md`
