---
name: rm-opencode
description: >
  OpenCode file locations, session DB schema, config patterns, and
  skill/agent conventions. Use when modifying OpenCode config, agents,
  skills, MCP servers, plugins, or TUI settings. Never use for general
  coding tasks.
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

### snippets — text expansion plugin

Expands `#hashtag` shortcuts in user messages into reusable text blocks.
Installed as an npm plugin at `~/.cache/opencode/packages/opencode-snippets@latest/`.

**Snippet locations:** `.md` files in `~/.config/opencode/snippet/` (primary,
singular) or `~/.config/opencode/snippets/` (alternate, plural). Project-level
variants at `.opencode/snippet/` and `.opencode/snippets/` override global
versions when present.

**Config:** `~/.config/opencode/snippet/config.jsonc` — logging, experimental
features (skill rendering, skill loading, inject blocks).

**Logs:** `~/.config/opencode/logs/snippets/daily/YYYY-MM-DD.log`

**Format:** filename minus `.md` is the primary hashtag. Optional YAML frontmatter
with `aliases:` (list) and `description:`.

```md
---
aliases:
  - short
  - alt
description: What this snippet does
---

Content expanded when user types #hashtag.
```

**Features:**

- `#other` — include another snippet inline (recursive, max depth 15)
- `` !`cmd` `` — shell command substitution, output injected into expansion
- `<prepend>` / `<append>` blocks — move section content to message start or end

**Experimental (disabled by default):** inject blocks (`<inject>...</inject>`
for persistent hidden context), skill rendering (`<skill>name</skill>`),
skill loading (`#skill(name)`). Enable in config per-feature.

**Commands:** `/snippet add <name> [content]`, `/snippet list`, `/snippet delete <name>`.
Use `--project` flag for project-scoped snippets.

### JSONC Parsing

`opencode.jsonc` is not valid JSON — `JSON.parse` and standard parsers fail
on line comments (`//`), block comments (`/* */`), trailing commas, and
multi-line strings embedded in values. When a script needs to extract config
values from `opencode.jsonc` without full parsing, use regex extraction for
the specific fields needed:

```
Provider context: /"limit"\s*:\s*\{[^}]*"context"\s*:\s*(\d+)[^}]*\}/g
Permission denies: /"skill"\s*:\s*\{[^}]*\}/g (then manual key inspection)
```

Never rely on `JSON.parse` with comment stripping as the only fallback —
the JSONC format has edge cases (multi-line strings, trailing commas) that
regex stripping cannot repair.

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

### Skill Prompt Budget

Every skill loaded at session startup consumes context tokens. The budget
math: **2% of the active provider's `limit.context`** is allocated to all
skill names and descriptions. Token cost is `ceil(utf8_bytes / 4)` per
rendered line. When descriptions exceed the 2% budget, OpenCode truncates
them equally across all skills.

At 393,216 context (DeepSeek V4 Pro default), the skills budget is 7,864
tokens. With 70 skills averaging ~110 description characters each,
descriptions consume ~85% of that allocation. Adding skills without removing
old ones silently pushes the budget past 100% — every response is then
shorter than it could be by the amount of overspend.

Use `rm-skill-cleaner` to audit the current budget. Prefer short, trigger-focused
descriptions over exhaustive prose. Every description character is a permanent
tax on every session.

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

### LSP Tools

The `lsp` tool provides code intelligence via Language Server Protocol.
Requires `"lsp": true` in `opencode.jsonc` and
`OPENCODE_EXPERIMENTAL_LSP_TOOL=true` (or `OPENCODE_EXPERIMENTAL=true`).
Enabled per-directory via `.envrc`.

**Automatic diagnostic injection** — the `edit` and `write` tools return
LSP diagnostics inline in their output as `<diagnostics>` tags. This is
always active when LSP servers are running. Zero diagnostics on the edited
files means the edit is clean; `dotnet build` is only needed for final
integration verification.

**Explicit tool operations** — invoked via `lsp({operation, filePath, line, character})`:

| Operation              | What it asks                  | Replaces                                    |
| ---------------------- | ----------------------------- | ------------------------------------------- |
| `findReferences`       | Who uses this symbol?         | `grep` for method name + manual audit       |
| `goToDefinition`       | Where is this defined?        | `grep` for class/method + `read` to inspect |
| `hover`                | What type/doc does this have? | `read` signature + memory/guess             |
| `goToImplementation`   | What classes implement this?  | `grep` for `: IInterface` patterns          |
| `documentSymbol`       | What symbols in this file?    | `read` entire file + manual scan            |
| `workspaceSymbol`      | Find symbol by name anywhere  | `glob` + `grep` across solution             |
| `prepareCallHierarchy` | Get call hierarchy root       | Manual call-graph construction              |
| `incomingCalls`        | Who calls this function?      | `findReferences` + manual tree              |
| `outgoingCalls`        | What does this function call? | `read` logic + manual walk                  |

Never use `grep`, `glob`, or `read` for a code structure question when
the corresponding LSP operation handles it. LSP matches symbols, not
strings — zero false positives from comments, string literals, or
similarly-named identifiers.

---

## Env Vars

| Variable                         | Used by                                                                |
| -------------------------------- | ---------------------------------------------------------------------- |
| `OPENCODE_ANALYST_MODEL`         | Subagent model selection                                               |
| `OPENCODE_PID`                   | Process ID of the OpenCode TUI/server                                  |
| `OPENCODE_RUN_ID`                | UUID for the current run (not the session ID)                          |
| `OPENCODE_PROCESS_ROLE`          | Process role (`worker` for subprocess)                                 |
| `OPENCODE_DISABLE_PRUNE`         | UI filter — hides sessions >30d from TUI picker. Does NOT delete data. |
| `OPENCODE_EXPERIMENTAL_LSP_TOOL` | Enables the `lsp` tool for semantic code intelligence                  |
| `OPENCODE_EXPERIMENTAL`          | Enables all experimental features (includes LSP tool)                  |
| `OPENCODE_DISABLE_LSP_DOWNLOAD`  | Blocks auto-downloading of LSP server binaries                         |
| `CONTEXT7_API_KEY`               | Context7 MCP                                                           |
| `BRAVE_API_KEY`                  | Brave Search MCP                                                       |

---

## Syncing Local ↔ Global

OpenCode stores configuration in two locations: global (`~/.config/opencode/`)
and per-project local (`.opencode/`). Both accumulate changes independently.
Sync them to prevent drift.

### Pre-Sync Checklist

Before any sync:

1. Generate a full diff report. Never sync without seeing what differs.
2. Present the report. Never execute before the user reviews and gives
   exclusions.
3. Never delete or overwrite files without explicit confirmation.

### Copy vs Merge — Check Before Every File

For every file that differs between local and global, answer these two
questions before choosing an action:

1. **Does the local side have content the global side lacks?**
2. **Does the global side have content the local side lacks?**

| Local has new | Global has new | Action                                                                                                                                     |
| :------------ | :------------- | :----------------------------------------------------------------------------------------------------------------------------------------- |
| No            | Yes            | Copy global → local                                                                                                                        |
| Yes           | No             | Copy local → global                                                                                                                        |
| Yes           | Yes            | **Merge both sides.** Never discard one side's content. Read both files, combine new instructions from each, write back to both locations. |
| No            | No             | Files are identical — no action (should not appear in diff)                                                                                |

The numbered rules below pre-answer this for specific directories where
the direction is fixed by design. For everything else (rm-_ skills,
rm-_ agents, scripts, snippets, themes), apply this table individually
per file.

### Sync Rules

Apply these rules in order. Never deviate:

1. **Compound Engineering — global wins, all or nothing.**
   `skills/compound-engineering/` and `agents/compound-engineering/` sync
   as a unit. The newer side wins completely. Delete the stale side,
   copy fresh from the newer side. Never partially sync — partial sync
   leaves orphaned agents that OpenCode tries to load.

2. **matt-pocock folder — same as compound engineering.** Global wins.
   Delete local, copy fresh from global. Never partially sync.

3. **Magic-context — global only, never copy to local.**
   `magic-context.jsonc` and `context-mode/` exist only in global.
   Never copy them into `.opencode/`.

4. **Excluded files — never touch.**
   Never sync: `logs/`, `node_modules/`, `.gitignore`, `package.json`,
   `package-lock.json`. These are location-specific.

5. **Global AGENTS.md → local as `global-AGENTS.md`.**
   Global is the authoritative instruction file. The local copy is a
   dead reference for documentation only. Never sync in reverse.

6. **`tui.json` — global → local, never include `plugins` key.**
   Plugins are global infrastructure. Duplicating them in the local
   `tui.json` creates conflicts. Strip the `plugins` key before copying.

7. **Plugins — global wins, never edit locally.**
   `plugins/` directory syncs global → local. If any local plugin
   differs from global (has local edits), report and ask before
   overwriting.

8. **`opencode.jsonc` — global is authoritative.**
   Copy global → local. Global contains all provider, agent, and MCP
   configuration.

9. **Everything else — report and ask.**
   rm-_ skills, rm-_ agents, snippets, scripts, themes — these are
   not covered by the rules above. Diff individually, report
   differences, and ask for direction per file.

### Sync Workflow

1. Load `rm-opencode` skill.
2. Walk both `.opencode/` and `~/.config/opencode/` (excluding
   `node_modules`, `logs`, `.gitignore`, `package*`).
3. Categorize every difference by the rules above.
4. Present the report with automatic actions and questions.
5. Wait for user exclusions.
6. Execute sync operations.
7. Verify: every synced file has identical checksum on both sides.
8. Report results.

### Recovery

If a sync corrupts `opencode.jsonc`: OpenCode falls back to project
config only. Restore the global config from the dotfiles repo git
history. Never attempt to fix corruption by hand — the file structure
is too brittle.

### TUI plugin `execSync` buffer exhaustion (ENOBUFS)

**Symptom:** A TUI plugin (e.g. git sidebar) works on first poll, then
permanently enters error state (`git ?` in red) on subsequent polls.
The console shows:

```
spawnSync /bin/sh ENOBUFS (stdout or stderr buffer reached maxBuffer size limit)
```

**Root cause:** `execSync` calls inside the plugin (commonly SQLite queries
in `seedSessionFiles` or similar) return more data than Node's default
`maxBuffer` (200KB). Long sessions with many tool calls (especially bash
commands containing `/`) easily exceed this. The uncaught exception
propagates to the outer `pollGitStatus` catch, setting permanent error state.

**Fix:** Explicitly set `maxBuffer: 10 * 1024 * 1024` (10MB) on every
`execSync` call that may return large result sets:

```ts
execSync(sqliteQuery, {
  encoding: "utf8",
  timeout: 5000,
  maxBuffer: 10 * 1024 * 1024,
});
```

Also replace any `storedApi?.app.log(...)` calls with `console.error(...)`
— `app.log` is not always a function in CLI / certain OpenCode versions
and will itself throw, masking the real error.

**Prevention:** When writing TUI plugins that query the session database
or run commands that may produce large output, always set an explicit
`maxBuffer`.

**Endless session policy (magic-context):** In never-ending sessions,
`lastCleanTimestamp` must be capped at 7 days. Real sessions never last
that long, so older activity is irrelevant for session-scoped counts.
This hard floor prevents unbounded query growth while preserving all
meaningful data.

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
