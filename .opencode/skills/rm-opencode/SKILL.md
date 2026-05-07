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

Load before any task that touches OpenCode internals.

## Paths

All paths use `~/` as the home directory. Forward slashes work on all platforms.

| What                 | Where                                 |
| -------------------- | ------------------------------------- |
| Main config          | `~/.config/opencode/opencode.jsonc`   |
| Agent instructions   | `~/.config/opencode/AGENTS.md`        |
| TUI config           | `~/.config/opencode/tui.json`         |
| Session database     | `~/.local/share/opencode/opencode.db` |
| Skills               | `~/.config/opencode/skills/`          |
| Agents               | `~/.config/opencode/agents/`          |
| Plugins              | `~/.config/opencode/plugins/`         |
| Docs & specs         | `~/.config/opencode/docs/`            |
| Context-mode config  | `~/.config/opencode/context-mode/`    |
| Server logs          | `~/.local/share/opencode/log/`        |
| Plugin logs          | `~/.config/opencode/logs/`            |
| Snippets plugin logs | `~/.config/opencode/logs/snippets/`   |

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

Any new command protections or safety rules go in this file.

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
`name:` + `description:` frontmatter. The `name:` field must match the
directory name.

**Add an agent:** create `~/.config/opencode/agents/<name>.md`.

**Add MCP:** add entry to `opencode.jsonc` → `mcp` (see config pattern above).

**Add provider:** add entry to `opencode.jsonc` → `provider` (see config pattern above).

**Change default agent:** `opencode.jsonc` → `"default_agent"`.

**Query sessions:** `sqlite3 ~/.local/share/opencode/opencode.db` (see schema + queries below).

---

## Env Vars

| Variable                 | Used by                  |
| ------------------------ | ------------------------ |
| `OPENCODE_ANALYST_MODEL` | Subagent model selection |
| `CONTEXT7_API_KEY`       | Context7 MCP             |
| `BRAVE_API_KEY`          | Brave Search MCP         |

---

## Key Docs

- Conversion spec: `~/docs/specs/2026-05-01-compound-engineering-opencode-conversion-spec.md`
- Session scripts plan: `~/.config/opencode/docs/plans/2026-05-01-001-feat-opencode-session-scripts-plan.md`

---

## Session Database Schema

**File:** `~/.local/share/opencode/opencode.db` (SQLite)

### Session

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
