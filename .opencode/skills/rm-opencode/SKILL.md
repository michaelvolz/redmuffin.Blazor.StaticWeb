---
name: rm-opencode
description: >
  LOAD FIRST whenever OpenCode itself is being modified, configured,
  debugged, or documented. Contains the single source of truth for
  OpenCode file locations, session database schema, config patterns,
  skill/agent conventions, session query SQL, and key doc references.
  USE FOR: modify opencode.jsonc, update AGENTS.md, add/remove agents
  or skills, configure MCP servers, debug session history,
  troubleshoot plugins, convert CC plugins to OpenCode, add plugins,
  configure TUI, understand OpenCode internals, find where anything
  lives. DO NOT USE FOR: general coding tasks unrelated to OpenCode
  itself.
---

# OpenCode Config Reference

> **GitHub:** [github.com/sst/open-code](https://github.com/sst/open-code) — open-source,
> native binary (not an npm package). Issues, source, and release notes live here.

Load before any task that touches OpenCode internals.

## Paths

All paths use `~/` as the home directory. Forward slashes work on all platforms.

| What                   | Where                                  |
| ---------------------- | -------------------------------------- |
| Main config            | `~/.config/opencode/opencode.jsonc`    |
| Agent instructions     | `~/.config/opencode/AGENTS.md`         |
| TUI config             | `~/.config/opencode/tui.json`          |
| Session database       | `~/.local/share/opencode/opencode.db`  |
| Skills                 | `~/.config/opencode/skills/`           |
| Agents                 | `~/.config/opencode/agents/`           |
| Plugins                | `~/.config/opencode/plugins/`          |
| Plugin package cache   | `~/.cache/opencode/packages/`          |
| npm debug logs         | `~/.npm/_logs/`                        |
| Docs & specs           | `~/.config/opencode/docs/`             |
| Context-mode config    | `~/.config/opencode/context-mode/`     |
| Server logs            | `~/.local/share/opencode/log/`         |
| Plugin logs            | `~/.config/opencode/logs/`             |
| Snippets plugin logs   | `~/.config/opencode/logs/snippets/`    |
| Scripts                | `~/.config/opencode/scripts/`          |
| Storage (session data) | `~/.local/share/opencode/storage/`     |
| Tool output cache      | `~/.local/share/opencode/tool-output/` |

Schema URLs:

- Config: `https://opencode.ai/config.json`
- TUI: `https://opencode.ai/tui.json`

---

## Config Patterns

### MCP Server

```jsonc
"<name>": {
  "type": "local",               // or "remote"
  "command": ["bin", "arg"],     // local only
  "url": "https://...",          // remote only
  "headers": { "KEY": "{env:VAR}" },
  "enabled": true
}
```

### Provider

```jsonc
"<name>": {
  "options": {
    "baseURL": "https://api.example.com/v1",
    "apiKey": "{env:API_KEY_VAR}"
  }
}
```

**Provider naming conventions:**

- Free tier provider ID is `opencode` (not `zen`, not `go`)
- Paid Go provider ID is `opencode-go`
- Model format for free tier: `opencode/<model-id>` (e.g., `opencode/minimax-m2.5-free`)
- Model format for paid direct: `<provider>/<model-id>` (e.g., `deepseek/deepseek-v4-pro`)

**Model-level configuration:**

Most models work with provider defaults auto-detected from models.dev —
no manual config required. Only add explicit `limit` or `options` when
you have a specific reason.

```jsonc
"<provider>": {
  "models": {
    "<model-id>": {
      // "limit" and "options" are optional — defaults from models.dev
      // are correct for standard providers (DeepSeek, OpenAI, Anthropic).
    }
  }
}
```

> **DeepSeek V4 Pro specifics:** Do not set `temperature` or `top_p` —
> thinking mode (enabled by default) silently drops these parameters
> (confirmed by official DeepSeek API docs). Use `limit.context: 1048576`
> (full 1M native window — CSA architecture makes this viable without
> memory pressure) and `limit.output: 262144` (256K, community-tested).
> Magic-context compaction uses `execute_threshold_percentage: 40`
> (400K trigger) — not per-model `execute_threshold_tokens` which caused
> excessive KV cache busting at 200K. `reasoning_effort: "max"` is
> explicit; the API auto-detects OpenCode and sets max anyway. See
> `docs/solutions/tooling-decisions/
deepseek-v4-pro-context-precision-tuning-2026-05-13.md` for the full
> rationale.

### Agent (in opencode.jsonc)

```jsonc
"<name>": {
  "model": "{env:MODEL_VAR}",
  "mode": "subagent"
}
```

### Permissions

Per-tool rules: `allow`, `ask`, `deny` with glob paths.

```jsonc
"permission": {
  "bash": { "*": "ask", "git *": "allow" },
  "read": { "*": "allow", "~/.ssh/**": "deny" }
}
```

---

## Plugins

Plugins are loaded dynamically from `~/.config/opencode/plugins/`. Each file
exports an async function returning hook handlers. The primary hook is
`tool.execute.before` — it intercepts commands before they run.

### block-push.js

Custom safety plugin. Blocks dangerous git operations:

- `git push` — restricted to the repository owner
- `git revert` — restricted to the repository owner
- `git update-ref` — restricted to the repository owner

Never add command protections or safety rules outside of block-push.js.

### npm plugin package cache

When `opencode.jsonc` loads a plugin from npm (`@scope/name@version` or
`@latest`), OpenCode installs it to `~/.cache/opencode/packages/<name>@<version>/`
— NOT into the local `node_modules/` tree. Each package gets an isolated
npm install with its own `.npmrc` derived from the user's `~/.npmrc`.

**`min-release-age` trap:** The global `~/.npmrc` `min-release-age` setting
applies to plugin auto-updates. If a plugin publishes a new version within
the age window, `npm install` silently fails with `notarget No matching version
found`. Debug logs go to `~/.npm/_logs/` (timestamped `*-debug-0.log` files).

Never bypass the age filter — it is intentional supply chain protection (see
global AGENTS.md §Secrets & Supply Chain for the full policy). If a new
OpenCode release falls inside the window, the update is delayed until the age
threshold passes. Wait for blocked updates.

---

## Skill & Agent Conventions

### Locations

- Skills: `~/.config/opencode/skills/<name>/SKILL.md`
- Compound Engineering skills: `~/.config/opencode/skills/compound-engineering/<name>/SKILL.md`
- Agents: `~/.config/opencode/agents/<name>.md` (flat)

### Skill Discovery Depth

OpenCode uses the globstar pattern `{skill,skills}/**/SKILL.md` (source:
`packages/opencode/src/skill/index.ts`). The `**` matches **zero or more**
directory levels. There is **no depth limit** — skills are discovered
recursively. Any nesting is valid:

- `skills/my-skill/SKILL.md` — 1 level, works
- `skills/namespace/my-skill/SKILL.md` — 2 levels, works (e.g., compound-engineering)
- `skills/ns/category/my-skill/SKILL.md` — 3 levels, works
- Arbitrary depth: all discovered

The docs state `skills/*/SKILL.md` as simplified notation, not a literal
one-level glob. Treat the docs as approximate; the code uses `**`.

### Naming

- The `name:` field in SKILL.md frontmatter must match the directory name

### Invocation Syntax

```
skill({ name: "skill-name" })     ← Invokes a skill
@compound-engineering/agent-name   ← Invokes a compound-engineering agent
```

---

## Common Operations

**Add a skill:** create `~/.config/opencode/skills/<name>/SKILL.md` with
`name:` + `description:` frontmatter (see §Naming above).

**Add an agent:** create `~/.config/opencode/agents/<name>.md`.

**Add MCP:** add entry to `opencode.jsonc` → `mcp` (see config pattern above).

**Add provider:** add entry to `opencode.jsonc` → `provider` (see config pattern above).

**Change default agent:** `opencode.jsonc` → `"default_agent"`.

**Query sessions:** `sqlite3 ~/.local/share/opencode/opencode.db` (see schema + queries below).

---

## Env Vars

| Variable                 | Used by                                                                |
| ------------------------ | ---------------------------------------------------------------------- |
| `OPENCODE_ANALYST_MODEL` | Subagent model selection                                               |
| `OPENCODE_PID`           | Process ID of the OpenCode TUI/server                                  |
| `OPENCODE_RUN_ID`        | UUID for the current run (not the session ID)                          |
| `OPENCODE_PROCESS_ROLE`  | Process role (`worker` for subprocess)                                 |
| `OPENCODE_DISABLE_PRUNE` | UI filter — hides sessions >30d from TUI picker. Does NOT delete data. |
| `CONTEXT7_API_KEY`       | Context7 MCP                                                           |
| `BRAVE_API_KEY`          | Brave Search MCP                                                       |

---

## Session Management CLI

```bash
opencode session list          # List all sessions (--max-count N, --format json|table)
opencode session delete <ID>   # Delete ONE session by ID (no bulk, no --older-than)
opencode db [query]            # Run raw SQL query (--format json|tsv)
opencode db path               # Print database file path
```

There is **no official bulk-delete or prune feature**. GitHub Issue #22110
requested `opencode session prune --older-than 30d` — closed as "not planned."

`OPENCODE_DISABLE_PRUNE` (env var) only filters the TUI `/sessions` picker to the
last 30 days. It does not delete anything. All sessions remain in DB and on disk.

### Session Cleanup Script

**`~/.config/opencode/scripts/cleanup-sessions.ps1`** — Cross-platform PowerShell
script to delete sessions older than N days. Uses `opencode session delete` in a
loop (safe — handles DB + filesystem cleanup).

```powershell
# Delete sessions untouched for 5+ days (the default)
.\cleanup-sessions.ps1

# Preview only — no deletions
.\cleanup-sessions.ps1 -WhatIf

# Delete sessions untouched for 30+ days
.\cleanup-sessions.ps1 -Days 30
```

Parameters: `-Days <int>` (default 5), `-WhatIf` (dry-run). Auto-excludes the
current session (detected via max `time_updated` + PID check + recency guard).
Handles forked session trees atomically (parent + all children must all be old,
or none are deleted). Reports each deleted session title + ID verbosely.

---

## Key Docs

- Conversion spec: `~/docs/specs/2026-05-01-compound-engineering-opencode-conversion-spec.md`
- Model tuning: `docs/solutions/tooling-decisions/deepseek-v4-pro-context-precision-tuning-2026-05-13.md`
- Proxy removal: `docs/solutions/tooling-decisions/deepseek-cursor-proxy-removal-opencode-1-14-41-2026-05-13.md`
- Session recovery: `docs/solutions/workflow-issues/magic-context-historian-stuck-compartment-flag-2026-05-11.md`

---

## Session Database Schema

**File:** `~/.local/share/opencode/opencode.db` (SQLite)

Foreign keys are defined with `ON DELETE CASCADE` (session → message → part,
session → todo, session → session_share, session → session_message). However,
SQLite's `PRAGMA foreign_keys` defaults to OFF — raw `sqlite3` deletes without
`PRAGMA foreign_keys = ON;` will **not** cascade. OpenCode's own code enables FKs.

### All Tables (complete schema)

```sql
session (
    id              TEXT PRIMARY KEY,       -- "ses_xxx"
    project_id      TEXT NOT NULL,
    parent_id       TEXT,                   -- forked-from session
    slug            TEXT NOT NULL,
    directory       TEXT NOT NULL,          -- working directory
    title           TEXT NOT NULL,
    version         TEXT NOT NULL,
    share_url       TEXT,
    summary_additions  INTEGER,
    summary_deletions  INTEGER,
    summary_files   INTEGER,
    summary_diffs   TEXT,
    revert          TEXT,
    permission      TEXT,
    time_created    INTEGER NOT NULL,       -- MILLISECONDS
    time_updated    INTEGER NOT NULL,       -- MILLISECONDS
    time_compacting INTEGER,
    time_archived   INTEGER,
    workspace_id    TEXT,
    path            TEXT
)
```

### Message

```sql
message (
    id              TEXT PRIMARY KEY,       -- "msg_xxx"
    session_id      TEXT NOT NULL,
    time_created    INTEGER NOT NULL,       -- MILLISECONDS
    time_updated    INTEGER NOT NULL,       -- MILLISECONDS
    data            TEXT NOT NULL           -- JSON
)
```

**Message `data` JSON:**

```json
{
  "role": "user" | "assistant",
  "time": {"created": 1776879636791},
  "agent": "build",
  "model": {"providerID": "opencode", "modelID": "big-pickle"},
  "summary": {"diffs": [...]}
}
```

### Part

```sql
part (
    id              TEXT PRIMARY KEY,
    message_id      TEXT NOT NULL,
    session_id      TEXT NOT NULL,
    time_created    INTEGER NOT NULL,       -- MILLISECONDS
    time_updated    INTEGER NOT NULL,       -- MILLISECONDS
    data            TEXT NOT NULL           -- JSON
)
```

**Part `data` types:**

| Type          | Extract? | Notes                              |
| ------------- | -------- | ---------------------------------- |
| `text`        | YES      | User messages, assistant responses |
| `tool`        | YES      | Tool calls and results             |
| `reasoning`   | SKIP     | Internal model reasoning           |
| `step-start`  | SKIP     | Session lifecycle                  |
| `step-finish` | SKIP     | Session lifecycle                  |
| `compaction`  | SKIP     | Context compaction events          |
| `patch`       | SKIP     | File patches                       |
| `file`        | SKIP     | File operations                    |

### Timestamps

ALL OpenCode timestamps are **milliseconds** since Unix epoch.

- SQLite: `datetime(time_created/1000, 'unixepoch')` → ISO 8601
- Code: divide by 1000 → Unix epoch seconds

### Additional Tables

```sql
todo (
    session_id  TEXT NOT NULL,
    content     TEXT NOT NULL,
    status      TEXT NOT NULL,
    priority    TEXT NOT NULL,
    position    INTEGER NOT NULL,
    time_created INTEGER NOT NULL,
    time_updated INTEGER NOT NULL,
    PRIMARY KEY (session_id, position),
    FOREIGN KEY (session_id) REFERENCES session(id) ON DELETE CASCADE
)

session_share (
    session_id   TEXT PRIMARY KEY,
    id           TEXT NOT NULL,
    secret       TEXT NOT NULL,
    url          TEXT NOT NULL,
    time_created INTEGER NOT NULL,
    time_updated INTEGER NOT NULL,
    FOREIGN KEY (session_id) REFERENCES session(id) ON DELETE CASCADE
)

session_message (
    id           TEXT PRIMARY KEY,
    session_id   TEXT NOT NULL,
    type         TEXT NOT NULL,
    time_created INTEGER NOT NULL,
    time_updated INTEGER NOT NULL,
    data         TEXT NOT NULL,
    FOREIGN KEY (session_id) REFERENCES session(id) ON DELETE CASCADE
)

project (
    id               TEXT PRIMARY KEY,
    worktree         TEXT NOT NULL,
    vcs              TEXT,
    name             TEXT,
    time_created     INTEGER NOT NULL,
    time_updated     INTEGER NOT NULL,
    sandboxes        TEXT NOT NULL
)

workspace (
    id         TEXT PRIMARY KEY,
    type       TEXT NOT NULL,
    name       TEXT NOT NULL DEFAULT '',
    branch     TEXT,
    directory  TEXT,
    project_id TEXT NOT NULL,
    FOREIGN KEY (project_id) REFERENCES project(id) ON DELETE CASCADE
)
```

---

## Session Queries

```sql
-- Find sessions by directory or title
SELECT id, title, directory,
       datetime(time_created/1000, 'unixepoch') as created
FROM session
WHERE directory LIKE '%<keyword>%'
   OR title LIKE '%<keyword>%'
ORDER BY time_created DESC
LIMIT 20;

-- Messages + parts for a session (skipping internal types)
SELECT p.data, p.time_created
FROM part p
WHERE p.session_id = '<session-id>'
  AND p.data NOT LIKE '%"type":"reasoning"%'
  AND p.data NOT LIKE '%"type":"step-start"%'
  AND p.data NOT LIKE '%"type":"step-finish"%'
  AND p.data NOT LIKE '%"type":"compaction"%'
ORDER BY p.time_created;

-- Recent sessions
SELECT id, title, directory,
       datetime(time_created/1000, 'unixepoch') as created
FROM session ORDER BY time_created DESC LIMIT 20;

-- Session count by project
SELECT directory, COUNT(*) as n
FROM session GROUP BY directory ORDER BY n DESC LIMIT 10;
```

---

## Troubleshooting

### TUI Plugin debugging — no badge, no slot rendering

**Symptom:** A registered TUI plugin's slot content (sidebar, badge) is
completely absent after restart, BUT the plugin was working before a
`tui.json` edit. All plugins may be silently absent — not just the one
you were working on.

**Critical diagnostic — unrecognized keybind cascade:** Adding a keybind
identifier that the installed OpenCode version does not recognize causes
the entire `tui.json` to fail to parse. This silently blocks **all**
plugins from loading. The commands may exist (work from Menu), but the
keybind identifier itself is not supported in your version.

Check this FIRST when a plugin that was working suddenly vanishes after
a `tui.json` edit: revert the edit and restart. If plugins return, the
unrecognized keybind was the cause.

**Known version-dependent keybinds (OpenCode 1.14.41):**

| Command       | Description                                | Works via Menu | Bindable in 1.14.41 |
| ------------- | ------------------------------------------ | -------------- | ------------------- |
| `app_console` | Toggle JS/DevTools console (plugin errors) | Yes            | No                  |
| `app_debug`   | Toggle performance overlay (FPS/memory)    | Yes            | No                  |

The `tui.json` schema at `https://opencode.ai/tui.json` reflects the
**latest** version — newer entries may not exist in the installed binary.
To access these commands in 1.14.41: `ctrl+p` (command palette) → type
"console" or "debug". `ctrl+shift+i` (Chromium universal DevTools shortcut)
may also work directly in the TUI window.

**Debugging gap:** OpenCode has no log file for plugin runtime errors.
Plugin crashes go to the terminal stderr where OpenCode was launched
(not accessible to the agent). The session database records no plugin
loading errors. Compiled output passes `bun build` successfully even
when runtime rendering logic will crash.

**Procedure:** Ask the user to open the OpenCode developer console
(Ctrl+Shift+I or `ctrl+p` → "console") and copy all red error messages
or stack traces. The console is the only window into `@opentui/solid`
rendering errors, slot registration failures, and JS exceptions.

**Common TUI rendering bugs:**

- `_$insert` receiving `""` (empty string) instead of `null`/`false` —
  the `@opentui/solid` compiler can produce `"" && <jsx>` patterns that
  evaluate to `""`, not `false`. Initialize stash/conditional text to
  `null`, never `""`.
- Missing `@opentui/solid/bun-plugin` in build — JSX compiles to raw
  `React.createElement` instead of `_$insert`/`_$memo`. Always use
  `scripts/build.ts`, never raw `bun build`.
- Silent build failure — `Bun.build` with `packages: "external"` can
  succeed even when imports are unresolvable. Check `result.logs` in
  the build script for warnings.

### Historian stuck — all runs fail with "JSON Parse error: Unexpected EOF"

**Symptom:** Every historian run fails identically in <1ms across all models with
`JSON Parse error: Unexpected EOF`. The first pass completes but validation rejects
the output; retry infrastructure is dead on arrival.

**Root cause:** `compartment_in_progress = 1` stuck in the `session_meta` table of
magic-context's database. A previous run using a broken model config (e.g.,
non-existent model ID like `deepseek/deepseek-v4-flash`) caused the first pass to
silently fail, permanently locking the flag. Non-zero `compartment_in_progress`
blocks all new historian runs.

**Recovery:**

```bash
sqlite3 ~/.local/share/cortexkit/magic-context/context.db "
UPDATE session_meta
SET compartment_in_progress = 0,
    historian_last_error = NULL,
    historian_last_failure_at = NULL,
    historian_failure_count = 0
WHERE session_id LIKE '%<session_id>%';
"
```

Also check `pending_ops` for stuck entries. Full procedure and root cause
analysis: `docs/solutions/workflow-issues/magic-context-historian-stuck-compartment-flag-2026-05-11.md`.
