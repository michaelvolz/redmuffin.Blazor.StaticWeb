---
name: rm-sidenotes
description: "Shortcut: rm:sn. Use when the user says 'sidenote:' or '/sidenote' to capture a tangential idea, or says 'show sidenotes', 'list sidenotes', 'convert sidenote', 'tackle sidenote', or 'dismiss sidenote' to manage the sidenote backlog. Handles capture, storage, retrieval, conversion, and dismissal of sidenotes during active work sessions."
---

# rm-sidenotes

Capture tangential ideas mid-conversation without derailing the current task. Store as structured files. Retrieve and convert to actionable artifacts when ready.

## CRITICAL

- When you see `sidenote:` or `/sidenote` in user input, ALWAYS use this skill — do not handle inline
- NEVER act on a sidenote immediately after capture — the current task continues uninterrupted
- NEVER ask follow-up questions about a captured sidenote
- NEVER auto-suggest sidenotes to the user — they explicitly request retrieval
- Trigger detection: `sidenote:` must be the first non-whitespace token on a line, or `/sidenote` must be at message start. Do NOT trigger on casual prose containing the word "sidenote"

## FLOW

### 1. Capture (sidenote: / /sidenote)

1. Create `docs/sidenotes/` if it does not exist
2. Compute next ID: glob `docs/sidenotes/SN-*.md`, extract numeric suffix from filename, take max + 1, zero-pad to 4 digits (e.g., SN-0003). If no files exist, start at SN-0001. Re-scan immediately before write — if the computed ID already exists (rapid successive captures), increment and retry once
3. Derive a brief title from the first few words of the sidenote text (kebab-case)
4. Write `docs/sidenotes/SN-NNNN.md` with frontmatter and body
5. Confirm in one line: "Sidenote SN-NNNN captured."
6. Do nothing else — the current task continues

### 2. Retrieval (show sidenotes / list sidenotes)

1. Glob `docs/sidenotes/SN-*.md`
2. Filter files where frontmatter `status` is `pending`
3. If none found, respond: "No pending sidenotes."
4. Otherwise display as numbered list:

```
Pending sidenotes:
1. SN-0001 (2026-04-04) — Research a good sidenote solution
2. SN-0002 (2026-04-04) — Prevent many open about:blank tabs
```

### 3. Conversion (convert sidenote SN-NNNN / tackle sidenote SN-NNNN)

1. Read `docs/sidenotes/SN-NNNN.md`. If file does not exist, respond: "Sidenote SN-NNNN not found."
2. If status is already `converted`, respond: "Sidenote SN-NNNN was already converted to <converted-to path>."
3. Present 3 options to the user: todo, brainstorm, plan
4. On user selection, create the artifact following the patterns in `references/conversion.md`
5. Update the sidenote file: set `status: converted`, add `converted-to: <artifact-path>` in frontmatter
6. Confirm: "Sidenote SN-NNNN converted to <artifact-path>."

### 4. Dismissal (dismiss sidenote SN-NNNN)

1. Read `docs/sidenotes/SN-NNNN.md`. If file does not exist, respond: "Sidenote SN-NNNN not found."
2. If status is already `dismissed`, respond: "Sidenote SN-NNNN is already dismissed."
3. Update the file: set `status: dismissed`. If the user provided a reason (e.g., "dismiss sidenote SN-0002 - not relevant anymore"), add `dismissed-reason` field to frontmatter
4. Confirm: "Sidenote SN-NNNN dismissed."

## COMMANDS

| Command                                                | Purpose             | When                                |
| ------------------------------------------------------ | ------------------- | ----------------------------------- |
| `sidenote: <text>`                                     | Capture inline      | Mid-conversation tangential thought |
| `/sidenote <text>`                                     | Capture via command | Explicit capture                    |
| `show sidenotes` / `list sidenotes`                    | List pending        | Ready to review backlog             |
| `convert sidenote SN-NNNN` / `tackle sidenote SN-NNNN` | Convert to task     | Ready to act on specific item       |
| `dismiss sidenote SN-NNNN`                             | Dismiss item        | No longer relevant                  |

## BOUNDARIES

### ALWAYS

- Create `docs/sidenotes/` if missing
- Use 4-digit sequential IDs (SN-0001, SN-0002, ...)
- Confirm capture in one line only
- Re-scan directory before each write to avoid ID collisions

### ASK FIRST

- Conversion target type — always present 3 options (todo, brainstorm, plan) and let the user choose

### NEVER

- Act on a sidenote immediately after capture
- Auto-suggest sidenotes to the user
- Modify existing sidenote text (only status and lifecycle fields change)
- Re-convert an already-converted sidenote

## CONTEXT

Sidenotes are a lightweight capture system for tangential ideas during active work sessions. They persist as individual markdown files in `docs/sidenotes/` with YAML frontmatter for metadata. The system is additive — it does not modify existing brainstorm, plan, or todo systems. Conversion bridges sidenotes into those systems when the user is ready to act.
