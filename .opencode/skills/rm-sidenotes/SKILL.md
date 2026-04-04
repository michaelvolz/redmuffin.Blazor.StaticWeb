---
name: rm-sidenotes
description: "Shortcut: rm:sn. Use for sidenote capture (sidenote:, /sidenote, /sidenotes) and management commands (sidenotes list, sidenotes show, sidenote list, sidenote show, sidenote convert SN-NNNN, sidenote tackle SN-NNNN, sidenote dismiss SN-NNNN, sidenotes dismiss SN-NNNN). Handles capture, storage, retrieval, conversion, and dismissal of sidenotes during active work sessions."
---

# rm-sidenotes

Capture tangential ideas mid-conversation without derailing the current task. Store as structured files. Retrieve and convert to actionable artifacts when ready.

## CRITICAL

- When you see `sidenote:`, `sidenotes`, `/sidenote`, `/sidenotes` in user input, ALWAYS use this skill — do not handle inline
- NEVER act on a sidenote immediately after capture — the current task continues uninterrupted
- NEVER ask follow-up questions about a captured sidenote
- NEVER auto-suggest sidenotes to the user — they explicitly request retrieval
- Trigger detection:
  - `sidenote:` must be the first non-whitespace token on a line (capture)
  - `/sidenote` or `/sidenote` at message start (capture/command)
  - `sidenote ` (singular, space-separated) for commands like "sidenote convert 5", "sidenote dismiss 1"
  - `sidenotes` (plural) for commands like "sidenotes list", "sidenotes show"
  - Do NOT trigger on casual prose containing the word "sidenote"

## FLOW

### 1. Capture (sidenote: / /sidenote)

**FIRE-AND-FORGET: Spawn async subagent and continue immediately.**

Do NOT process the sidenote content in the main agent. Spawn a background subagent and continue the conversation without waiting.

1. Extract the sidenote text from the user input
2. Spawn a subagent with `run_in_background: true`:
   - Description: "sidenote capture"
   - Prompt: Create the sidenote file using the provided text
   - Instructions for subagent:
     a. Glob `docs/sidenotes/SN-*.md` to find next available ID (max + 1, zero-pad to 4 digits)
     b. Create `docs/sidenotes/` if missing
     c. Use first 149 characters of text as title (preserve whitespace)
     d. Write `docs/sidenotes/SN-NNNN.md` with frontmatter (id, date, title, status: pending) and body
     e. Proofread: fix obvious typos (spelling, transposed letters) but NEVER change unclear areas — leave them as-is to preserve intent
     f. Verify file created with content
     g. If verification fails, retry write up to 2 times
     h. Return "done" when verified
3. Wait 1 second (non-blocking pause), then verify the file was created
   - If file exists: continue silently
   - If file missing: return to step 2 (spawn new subagent, up to 2 retries)
4. IMMEDIATELY continue the previous task — NO waiting, NO confirmation, NO message of any kind

The main agent never processes the sidenote content — it only spawns the subagent and continues.

### 2. Retrieval (show sidenotes / list sidenotes)

1. Glob `docs/sidenotes/SN-*.md`
2. Filter files where frontmatter `status` is `pending`
3. If none found, respond: "No pending sidenotes."
4. Otherwise display as numbered list

### 3. Verification (sidenotes verify)

1. Glob `docs/sidenotes/SN-*.md` to get expected files
2. Check which IDs from last capture attempt actually exist
3. Report any missing files with their IDs
4. If all captured: "All sidenotes verified."
5. If some missing: "Missing: SN-NNNN, SN-NNNN"

### 4. Conversion (convert sidenote SN-NNNN / tackle sidenote SN-NNNN)

```
Pending sidenotes:
1. SN-0004 (2026-04-04) — Add to AGENTS.md that I am a Trunk Based Developer and prefer staying on trunk if possible. If the risk is too high we can branch, otherwise I like master.
2. SN-0005 (2026-04-04) — We need to make sure sidenote always triggers and the note is written before reporting it was done. I have experienced data loss multiple times when the agent failed to load the rm-sidenotes skill and did not create the file.
```

### 4. Conversion (convert sidenote SN-NNNN / tackle sidenote SN-NNNN)

1. Read `docs/sidenotes/SN-NNNN.md`. If file does not exist, respond: "Sidenote SN-NNNN not found."
2. If status is already `converted`, respond: "Sidenote SN-NNNN was already converted to <converted-to path>."
3. Present 4 options to the user: **brainstorm** (ce:brainstorm), **plan** (ce:plan), **work** (ce:work), **todo** (todo-create)
4. On user selection, **load the appropriate skill** with the sidenote text as context using the `skill` tool:
   - **brainstorm** → Load skill `ce:brainstorm` with sidenote text as feature description
   - **plan** → Load skill `ce:plan` with sidenote text as feature description
   - **work** → Load skill `ce:work` with sidenote text as task description
   - **todo** → Load skill `todo-create` with sidenote text as context
   - **IMPORTANT**: These are SKILLS loaded via the `skill` tool, NOT agents loaded via the `Task` tool. Never use Task with agent types like `vendor/ce_brainstorm`.
5. After the skill completes and creates the artifact, update the sidenote file: set `status: converted`, add `converted-to: <artifact-path>` in frontmatter
6. Confirm: "Sidenote SN-NNNN converted to <artifact-path>."

### 5. Dismissal (dismiss sidenote SN-NNNN)

1. Read `docs/sidenotes/SN-NNNN.md`. If file does not exist, respond: "Sidenote SN-NNNN not found."
2. If status is already `dismissed`, respond: "Sidenote SN-NNNN is already dismissed."
3. Update the file: set `status: dismissed`. If the user provided a reason (e.g., "dismiss sidenote SN-0002 - not relevant anymore"), add `dismissed-reason` field to frontmatter
4. Confirm: "Sidenote SN-NNNN dismissed."

## COMMANDS

| Command                                                                                                           | Purpose             | When                                |
| ----------------------------------------------------------------------------------------------------------------- | ------------------- | ----------------------------------- |
| `sidenote: <text>`                                                                                                | Capture inline      | Mid-conversation tangential thought |
| `/sidenote <text>`                                                                                                | Capture via command | Explicit capture                    |
| `sidenotes list` / `sidenotes show` / `sidenote list` / `sidenote show`                                           | List pending        | Ready to review backlog             |
| `sidenotes verify` / `sidenote verify`                                                                            | Verify capture      | Check last capture succeeded        |
| `sidenote convert SN-NNNN` / `sidenote tackle SN-NNNN` / `sidenotes convert SN-NNNN` / `sidenotes tackle SN-NNNN` | Convert to task     | Ready to act on specific item       |
| `sidenote dismiss SN-NNNN` / `sidenotes dismiss SN-NNNN` / `dismiss sidenote SN-NNNN`                             | Dismiss item        | No longer relevant                  |

## BOUNDARIES

### ALWAYS

- Create `docs/sidenotes/` if missing
- Use 4-digit sequential IDs (SN-0001, SN-0002, ...)
- NEVER output any confirmation or acknowledgment after capture — file and continue silently
- Re-scan directory before each write to avoid ID collisions

### ASK FIRST

- Conversion target type — always present 4 options (brainstorm, plan, work, todo) and let the user choose

### NEVER

- Act on a sidenote immediately after capture
- Auto-suggest sidenotes to the user
- Modify existing sidenote text (only status and lifecycle fields change)
- Re-convert an already-converted sidenote

## CONTEXT

Sidenotes are a lightweight capture system for tangential ideas during active work sessions. They persist as individual markdown files in `docs/sidenotes/` with YAML frontmatter for metadata. The system is additive — it does not modify existing brainstorm, plan, or todo systems. Conversion bridges sidenotes into those systems when the user is ready to act.

## COMMIT MESSAGES

When committing sidenote changes, reference sidenotes as `SN-NNNN` (no `#` prefix) in the commit body. The `#` prefix triggers a bug in `conventional-commits-parser` that treats `#` + identifier as an issue reference, moving everything after it into the footer and causing commitlint's `body-empty` rule to fail.

**Good:**

```
feat(sidenotes): capture SN-0004

Agent instructions should surface es.exe as a primary search tool.
See SN-0004 for the full context.
```

**Bad (triggers commitlint body-empty):**

```
feat(sidenotes): capture SN-0004

Agent instructions should surface es.exe as a primary search tool.
See #SN-0004 for the full context.
```

For actual GitHub issue references, use the footer: `Refs: #1234`.
