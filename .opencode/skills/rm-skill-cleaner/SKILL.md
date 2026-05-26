---
name: rm-skill-cleaner
description: "Audit OpenCode skills: loaded roots, duplicate skills, unused skills from session history, prompt-budget token costs, compact descriptions. Use when trimming skill budget, finding dupes, or pruning unused skills."
---

# rm-skill-cleaner

Audit OpenCode skills for budget, duplicates, and usage. Scans OpenCode's session
database (SQLite) to determine which skills are actually invoked, then computes an
exact token budget and identifies unused candidates. Adapted from Peter Steinberger's
skill-cleaner for Codex/OpenClaw.

## Workflow

1. Run from this skill's directory or any repo root:

```bash
bun run ~/.config/opencode/skills/rm-skill-cleaner/scripts/skill-cleaner.ts --months 3
```

2. Read the report sections in order:

- **Skill Budget** — context window capacity, 2% skills allocation, tokens consumed
  vs. available. Derived from the active provider's `limit.context` in `opencode.jsonc`.
- **Description Candidates** — skills with long descriptions where relaxed phrasing
  could save budget.
- **Duplicates By Name** — same name across global and project skill roots.
- **Duplicate Delete Suggestions** — near-identical bodies, ranked by keep-priority.
- **Duplicates By Body Hash** — identical content across multiple names.
- **Unused Candidates** — skills with zero session-history mentions in the window.
- **Root Summary** — counts per root directory, including disabled skills.

3. Before deleting or disabling:

- Verify the kept copy exists and is loaded.
- Prefer deleting project-local duplicates over global when content is identical.
- Preserve trigger nouns in descriptions: product, tool, action, object.

## CLI flags

| Flag                  | Default | Description                                          |
| --------------------- | ------- | ---------------------------------------------------- |
| `--months 3`          | 3       | Session history window in months                     |
| `--no-logs`           | off     | Skip session-history scan (budget + duplicates only) |
| `--deep-logs`         | off     | Include archived sessions                            |
| `--json`              | off     | Output JSON instead of markdown                      |
| `--all`               | off     | Include disabled skills in analysis                  |
| `--budget-percent 2`  | 2       | Percentage of context window for skills budget       |
| `--context-tokens N`  | auto    | Override context window from config                  |
| `--chars-per-token 4` | 4       | Token cost ratio (bytes per token)                   |
| `--max-log-mb 300`    | 300     | Maximum log data to read from session DB             |
| `--root /path`        | none    | Include additional skill directories                 |

## How it works

1. **Skill discovery** — walks `~/.config/opencode/skills/` (global) and
   `.opencode/skills/` (project). Parses YAML frontmatter for `name` +
   `description`. Checks `opencode.jsonc` for `permission.skill` deny patterns
   that disable skills.

2. **Usage scanning** — queries `~/.local/share/opencode/opencode.db` (SQLite)
   for text and tool parts in recent sessions. Matches OpenCode invocation syntax:
   `skill({ name: "name" })`, `@skill-name`, `$skill-name`, and
   `skills/<name>/SKILL.md` file references.

3. **Budget model** — reads the active provider's `limit.context` from
   `opencode.jsonc`. Allocates 2% for skills. Computes token cost as
   `ceil(utf8_bytes / 4)`. Applies equal-description-truncation when over budget.

4. **Duplicate detection** — groups by name, then by body hash (FNV1a).
   Computes Jaccard similarity on normalized word sets. Suggests deletion
   candidates ranked by keep-priority: plugin cache < project < user global.

5. **Unused detection** — skills with zero mentions in the session window
   are flagged as candidates for removal or disabling.
