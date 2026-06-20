---
date: 2026-06-20
last_updated: 2026-06-20
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
> **`rm-agent-browser`** at runtime — same procedural content, different
> delivery. Intentional duplication; keep both in sync when rules change.
>
> Official upstream docs: [agent-browser.dev](https://agent-browser.dev/)

## What Belongs in This File

- **Viewpoint**: AI agent or developer automating [redmuffin.net](https://redmuffin.net)
  or local dev instances of this Blazor WASM app on Windows.
- **What belongs**: Install health, mandatory workflow rules, Blazor WASM wait
  patterns, PowerShell-safe command patterns, verified feature matrix,
  recommended use cases, cleanup, and links to upstream docs.
- **What does NOT belong**: One-off test runner scripts, transient session
  logs, screenshot artifacts, commit instructions, or bUnit test patterns
  (see `docs/blazor-component-architecture-guide-2026-05-13.md`). The
  `rm-agent-browser` skill mirrors this file for agents — update both together.

## 0 — Critical Viewpoint (READ FIRST)

**agent-browser** (Vercel Labs, v0.28.0 tested) is the standard browser
automation CLI for AI agents on this project. It uses a persistent Rust
daemon + Chrome DevTools Protocol (CDP). No Playwright/Puppeteer runtime.

**Adopt it for:** agent-driven QA, accessibility snapshots, annotated
screenshots, live-site navigation checks, and performance vitals on deployed
or local builds.

**Do not use it for:** component unit tests (use bUnit), CI unless the
workflow rules below are encoded in the pipeline, or parallel command bursts
from the agent harness (causes daemon corruption on Windows).

### Evaluation verdict (2026-06-20)

| Dimension | Rating | Summary |
| --------- | ------ | ------- |
| Core value for AI agents | Excellent | `snapshot -i` is token-efficient and a11y-rich |
| redmuffin.net compatibility | Good | Works after Blazor WASM boot wait |
| Windows / PowerShell ergonomics | Fair | Prefer `find` locators; sequential commands only |
| Reliability under stress | Fair | Parallel calls kill the daemon |
| Observability | Excellent | Annotated screenshots, vitals, console, network |
| Upstream docs | Excellent | [agent-browser.dev](https://agent-browser.dev/) + `skills get core` |

---

## 1 — Installation and health

```powershell
npm install -g agent-browser
agent-browser install    # Chrome for Testing (first time)
agent-browser --version  # expect 0.28.x or newer
```

Before any browser session after upgrades or odd failures:

```powershell
agent-browser doctor --offline --quick
```

`doctor` checks CLI version, Chrome path, stale daemons (auto-cleaned), and
socket files. Full `doctor` (with launch test) can hang on Windows — prefer
`--offline --quick`.

Load version-matched agent instructions from the CLI (never copy stale
`SKILL.md` from `node_modules`):

```powershell
agent-browser skills list
agent-browser skills get core --full
```

---

## 2 — Mandatory workflow rules

These rules were verified on Windows against https://redmuffin.net and are
**non-negotiable** for agents.

### 2.1 Sequential commands only

Never issue two `agent-browser` command chains in parallel against the same
session. Parallel harness tool calls caused:

- `about:blank` pages
- `os error 10060` (daemon connection lost)
- Stale or empty snapshots

Run one command (or one `&&` chain) at a time.

### 2.2 Named session on every command

```powershell
agent-browser --session redmuffin open https://redmuffin.net
agent-browser --session redmuffin snapshot -i
```

Or set once per shell: `$env:AGENT_BROWSER_SESSION = 'redmuffin'`

Without `--session`, commands hit the wrong daemon or spawn orphan Chrome
windows.

### 2.3 Blazor WASM boot wait

`wait --load networkidle` alone is **insufficient**. WASM boot completes
after network idle.

**Reliable pattern (~4s when healthy):**

```powershell
agent-browser --session redmuffin wait --load networkidle
agent-browser --session redmuffin wait --fn "document.querySelector('main') !== null && document.body.innerText.length > 50"
```

Re-run the `--fn` wait after `reload`, `back`, or tab switches if content
looks empty.

### 2.4 Re-snapshot after page changes

Refs (`@e1`, `@e2`, …) are assigned on each `snapshot` and become stale
after navigation, form submit, or dynamic re-render. Always:

1. Act (click, fill, navigate)
2. Wait for expected state
3. `snapshot -i` again
4. Use new refs

### 2.5 PowerShell: prefer `find` over bare `@refs`

Semantic locators work consistently on PowerShell:

```powershell
agent-browser --session redmuffin find role link click --name "COUNTER"
agent-browser --session redmuffin find label "Demo Input:" fill "test value"
agent-browser --session redmuffin find role button click --name "Click me"
```

Quoted refs (`'@e7'`) work for `get text` / `get value` when the session is
healthy. Bare `@eN` can be mangled by PowerShell or fail on stale sessions.

### 2.6 Headed vs headless

| Mode | Flag | When to use |
| ---- | ---- | ----------- |
| Headless (default) | (none) | CI, unattended agent loops |
| Headed | `--headed` | Local debugging while a human watches |

- Headless may flash a small black window on Windows — that is normal.
- `--headed` is ignored if a daemon is already running. Run
  `agent-browser --session <name> close` first to change mode.
- Headless **does work** on redmuffin.net when the Blazor `--fn` wait is used
  in an isolated session.

### 2.7 Cleanup

End of session:

```powershell
agent-browser --session redmuffin close
```

If orphan processes accumulate (common after interrupted tests):

```powershell
# Kill only Chrome/daemons launched from ~/.agent-browser — not user Chrome
$ab = Join-Path $env:USERPROFILE '.agent-browser\browsers'
Get-CimInstance Win32_Process | Where-Object {
  ($_.Name -in 'chrome.exe','agent-browser-win32-x64.exe') -and
  ($_.CommandLine -like "*$ab*" -or $_.Name -eq 'agent-browser-win32-x64.exe' -or
   $_.CommandLine -like '*\.agent-browser\*')
} | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
agent-browser close --all
```

Do **not** run `close --all` while a human is inspecting a headed browser.

---

## 3 — Standard agent workflow

Use this sequence when an agent needs to interact with the live site — not as
a "test script", but as the **operational procedure** for any browser task
(QA, screenshot, navigation check, form verification).

```powershell
# 1. Open (add --headed when a human is watching)
agent-browser --headed --session redmuffin open https://redmuffin.net

# 2. Blazor boot (ALWAYS)
agent-browser --session redmuffin wait --load networkidle
agent-browser --session redmuffin wait --fn "document.querySelector('main') !== null && document.body.innerText.length > 50"

# 3. Understand page (low token cost)
agent-browser --session redmuffin snapshot -i

# 4. Optional: visual evidence for PRs or human review
agent-browser --session redmuffin screenshot --annotate .tmp/page.png

# 5. Interact (PowerShell-safe)
agent-browser --session redmuffin find role link click --name "COUNTER"
agent-browser --session redmuffin wait --url "**/counter**"
agent-browser --session redmuffin wait --fn "document.body.innerText.length > 30"
agent-browser --session redmuffin snapshot -i

# 6. Debug if needed
agent-browser --session redmuffin console
agent-browser --session redmuffin errors
agent-browser --session redmuffin vitals --json

# 7. Close when done
agent-browser --session redmuffin close
```

For local dev, replace the URL with the SwaLauncher or `dotnet watch` origin.

---

## 4 — Verified feature matrix (redmuffin.net, 2026-06-20)

### Reliable when workflow rules are followed

| Feature | Notes |
| ------- | ----- |
| `open <url>` | Works headed and headless |
| `wait --load networkidle` | Required but not sufficient alone |
| `wait --fn "<js>"` | Best Blazor boot detector |
| `wait --url "**/route**"` | Good for SPA route changes |
| `get url` / `get title` | Quick sanity checks |
| `snapshot -i` | Full nav, regions, form, perf widget (~33 refs) |
| `snapshot -i --json` | Machine-readable for agents |
| `eval "<js>"` | e.g. `document.querySelector('main h1')?.innerText` |
| `get text '@eN'` | With quoted refs + live session |
| `find role/text/label/placeholder` | **Recommended on PowerShell** |
| `screenshot` / `--full` / `--annotate` | Annotate maps `[N]` labels to `@eN` |
| `scroll`, `is visible` | Basic page interaction |
| `tab new --label <name>` | Labeled tabs (`t1`, `weather`, …) |
| `back` / `reload` | Works when session is healthy |
| `console`, `errors`, `network requests` | Debug after page load |
| `state save` / `state load` | Persist cookies/localStorage |
| `skills list` / `skills get core` | Version-synced docs from CLI |
| `doctor --offline --quick` | Fast health check |

### Flaky or context-dependent

| Feature | Notes |
| ------- | ----- |
| `click '@eN'` | Works with fresh snapshot; fails on `about:blank` |
| `vitals --json` | Valid JSON but zeros when page not loaded |
| `batch --json` | Useful for single-invocation chains; session must be alive |
| `wait --text "..."` | Timed out in testing; prefer `--fn` for Blazor |

### Not recommended / failed in testing

| Feature | Notes |
| ------- | ----- |
| Parallel agent-browser commands | Kills daemon |
| `doctor` (full, no `--quick`) | Hung >60s on Windows |
| `--% click @eN` through agent-browser | PowerShell stop-parsing does not pass through |

### Deferred (documented upstream, not verified here)

- `auth save/login`, `--profile`, `--auto-connect`
- `network route` (mock/abort), HAR export
- `record start/stop` (video), `trace`, `profiler`
- `react tree` (needs `--enable react-devtools`)
- `mcp` server, `chat` (needs `AI_GATEWAY_API_KEY`)
- `dogfood`, `electron`, `slack` specialized skills

---

## 5 — What a good snapshot looks like

When WASM is loaded, `snapshot -i` on the home page includes:

- Navigation: HOME, COUNTER, WEATHER, MARKDOWN, FOUNDATION, ICONS, VIDEOS,
  ARTICLES, API HEALTH
- Regions: `main "redmuffin.StaticWeb"`, `Emoji Display`, `Interactive Controls`
- Controls: `button "Click me"`, `textbox "Demo Input:"`, `button "Submit Form"`
- Built-in PAGE PERFORMANCE widget (WASM metrics) as an interactive ref

This is ~30 lines of text versus thousands for raw DOM — ideal for LLM context.

---

## 6 — Use cases for this project

### High value

1. **Agent-driven QA** — navigate routes, verify snapshots, catch regressions
2. **Accessibility checks** — snapshot exposes ARIA roles, labels, regions
3. **Visual evidence** — `screenshot --annotate` for PR review
4. **Live-site complement to bUnit** — bUnit tests components in isolation;
   agent-browser tests the published/running app
5. **API Health route** — navigate and inspect `network requests`
6. **Web Vitals** — `vitals --json` on loaded pages

### Low value / skip

- `chat` mode (harness already has an agent)
- Electron/Slack skills (not relevant)
- Auth vault (site has no login flow)
- Repeatable "test runner" scripts (use the workflow in §3 ad hoc per task)

---

## 7 — Risks and mitigations

| Risk | Mitigation |
| ---- | ---------- |
| Daemon corruption | Sequential commands; `doctor --offline --quick` before runs |
| Orphan Chrome processes | §2.7 cleanup after interrupted sessions |
| PowerShell `@ref` issues | Default to `find` locators (§2.5) |
| Blazor boot race | Always use `--fn` wait on `main` (§2.3) |
| Accidental `close --all` | Only close the named session unless explicitly cleaning up |

---

## 8 — Relationship to other tools

| Tool | Role |
| ---- | ---- |
| **bUnit** | Component/unit tests in isolation |
| **agent-browser** | Live app QA, a11y snapshots, screenshots, agent navigation |
| **Chrome DevTools MCP** | Deep debugging when user enables it; heavier context |
| **ce-test-browser** | PR-based browser test skill; can delegate to this workflow |

---

## Related

- **`rm-agent-browser` skill** — agent-loaded twin of this guide
- [agent-browser.dev](https://agent-browser.dev/) — official command reference
- [agent-browser commands](https://agent-browser.dev/commands)
- [agent-browser sessions](https://agent-browser.dev/sessions)
- [GitHub: vercel-labs/agent-browser](https://github.com/vercel-labs/agent-browser)
- [Blazor Component Architecture Guide](blazor-component-architecture-guide-2026-05-13.md)
- Skill install: `npx skills add vercel-labs/agent-browser`