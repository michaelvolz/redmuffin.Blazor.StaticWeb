# Global Rules for OpenCode

## Safety Blocks — cc-safety-net / Pushblocker (CRITICAL — NEVER BYPASS)

When a tool call returns a safety alert from `cc-safety-net` or a custom
pushblocker (recognizable by messages like "you shouldn't have done that",
"BLOCKED by Safety Net", or any advisory that a command was intercepted for
data/code protection):

1. **STOP.** Do not retry, do not work around, do not substitute a different
   tool to achieve the same effect.
2. **READ** the alert completely. It describes what you attempted that was
   unsafe or unauthorized, and usually explains why.
3. **The alert may suggest an allowed alternative.** If it does, and the
   alternative is feasible, you may use it. Otherwise:
4. **ASK ME** what to do. Describe what you attempted, paste the alert
   message, and wait for explicit instruction. Do not assume.
5. **Never circumvent** a safety block by using a plain / unwrapped command
   (e.g., bare `git` instead of `rtk` git, or `curl` instead of the
   sandbox fetcher) to bypass the wrapper. The blocks exist to protect
   data, code, and system integrity. Bypassing them is a critical error.

## Communication Protocol (CRITICAL)

**Questions = Discussion Phase Only**

- When I ask a question, I ONLY want ANSWERS - NOT actions
- Question means discussion, not action
- It is NEVER okay to do/install/create/change something when asked a question
- I will explicitly say "proceed" or "go" when action can start
- If unsure whether something is a question or a request, ask for clarification first

## context-mode — MANDATORY routing rules

You have context-mode MCP tools available. These rules are NOT optional —
they protect your context window from flooding. A single unrouted command
can dump 56 KB into context and waste the entire session.

### Think in Code — MANDATORY

When you need to analyze, count, filter, compare, search, parse, transform,
or process data: **write code** that does the work via
`context-mode_ctx_execute(language, code)` and `console.log()` only the
answer. Do NOT read raw data into context to process mentally. Write robust,
pure JavaScript — no npm dependencies, only Node.js built-ins (`fs`, `path`,
`child_process`). Always use `try/catch`, handle `null`/`undefined`, and
ensure compatibility with both Node.js and Bun. One script replaces ten tool
calls and saves 100x context.

### BLOCKED commands — do NOT attempt these

- **curl / wget — BLOCKED**: Any shell command containing `curl` or `wget`
  will be intercepted and blocked by the context-mode plugin. Do NOT retry.
  Instead use: `context-mode_ctx_fetch_and_index(url, source)` or
  `context-mode_ctx_execute(language: "javascript", code: "const r = await fetch(...)")`
- **Inline HTTP — BLOCKED**: Any shell command containing `fetch('http`,
  `requests.get(`, `requests.post(`, `http.get(`, or `http.request(` will be
  intercepted and blocked. Do NOT retry with shell. Instead use sandbox
  equivalent.
- **Direct web fetching — BLOCKED**: Do NOT use any direct URL fetching tool.
  Use the sandbox equivalent.

### REDIRECTED tools — use sandbox equivalents

- **Shell (>20 lines output)**: Shell is ONLY for: `git`, `mkdir`, `rm`,
  `mv`, `cd`, `ls`, `npm install`, `pip install`, and other short-output
  commands. For everything else, use:
  `context-mode_ctx_batch_execute(commands, queries)` — run multiple
  commands + search in ONE call (each command:
  `{label: "descriptive header", command: "..."}`. Label becomes FTS5 chunk
  title). Or `context-mode_ctx_execute(language: "shell", code: "...")`
- **File reading (for analysis)**: If reading to **edit** → reading is
  correct. If reading to **analyze, explore, or summarize** → use
  `context-mode_ctx_execute_file(path, language, code)` instead. Only your
  printed summary enters context.
- **grep / search (large results)**: Search results can flood context. Use
  `context-mode_ctx_execute(language: "shell", code: "grep ...")` to run
  searches in sandbox. Only your printed summary enters context.

### Tool selection hierarchy

1. **GATHER**: `context-mode_ctx_batch_execute(commands, queries)` — Primary
   tool. Runs all commands, auto-indexes output, returns search results.
   ONE call replaces 30+ individual calls.
2. **FOLLOW-UP**: `context-mode_ctx_search(queries: ["q1", "q2", ...])` —
   Query indexed content. Pass ALL questions as array in ONE call.
3. **PROCESSING**: `context-mode_ctx_execute(language, code)` |
   `context_mode_ctx_execute_file(path, language, code)` — Sandbox
   execution. Only stdout enters context.
4. **WEB**: `context-mode_ctx_fetch_and_index(url, source)` then
   `context_mode_ctx_search(queries)` — Fetch, chunk, index, query. Raw
   HTML never enters context.
5. **INDEX**: `context-mode_ctx_index(content, source)` — Store content in
   FTS5 knowledge base for later search.

### Output constraints

- Keep responses under 500 words.
- Write artifacts (code, configs, PRDs) to FILES — never return them as
  inline text. Return only: file path + 1-line description.
- When indexing content, use descriptive source labels so others can
  `search(source: "label")` later.

### ctx commands

| Command       | Action                                                                                |
| ------------- | ------------------------------------------------------------------------------------- |
| `ctx stats`   | Call the `stats` MCP tool and display the full output verbatim                        |
| `ctx doctor`  | Call the `doctor` MCP tool, run the returned shell command, display as checklist      |
| `ctx upgrade` | Call the `upgrade` MCP tool, run the returned shell command, display as checklist     |
| `ctx purge`   | Call the `purge` MCP tool with confirm: true. Warns before wiping the knowledge base. |

After /clear or /compact: knowledge base and session stats are preserved.
Use `ctx purge` if you want to start fresh.

## Command Execution — Long-Running & Interactive (DO NOT RUN)

OpenCode's bash tool has no interactive terminal support and commands
will timeout if they run too long. For anything that might be slow or
interactive:

- **Long-running commands** — large compilations (`cargo build`,
  `cargo run` on big projects), heavy downloads, full test suites.
  Warn the user and let them run it in their own terminal.
- **Interactive commands** — anything prompting for input (`sudo`,
  `ssh`, `gcloud auth login`, etc.). The bash tool cannot respond to
  prompts. Skip these and tell the user to run them manually.
- **Default rule:** If unsure whether a command is too slow or
  interactive, **ask the user to run it themselves.** A missed
  assumption is worse than an extra round-trip.

## Git Rules

### CRITICAL

- Use `rm-commit` for all commits (never manual `git commit` or `git add`).
- Batch commits by concern (config, agents, skills, docs) even when "all
  changes" requested.
- Read relevant code before answering any question.
- Use `pwsh -NoProfile` for all PowerShell execution.
- Wrap commit messages at 80 characters.
- Timestamp temporary files referenced in commit messages.
- Make single-purpose commits only.

### NEVER

- Commit, push, or expose secrets.
- Push to remote without explicit request.
- Use `git commit`, `git add`, or `git revert` without explicit request.
- Restore from git without asking first.
- Suggest, recommend, or offer to commit or push — the user
  knows when they want these and will ask explicitly.
- Discuss or act on any sidenote ("sidenote:" or "/sidenote") during an
  active task (sidenotes belong in backlog only).
- Remove or consolidate SKILL COMMANDS tables (duplication is mandatory
  for model adherence).
- Bypass the NPM 7-day release age filter (`min-release-age=7` in `.npmrc`).

### ASK FIRST

- Any git restoration operation.

### Commits — load `rm-commit` skill

For all commit formatting, staging workflows, here-string patterns, commit
message conventions, and porcelain command references, load the `rm-commit`
skill. Every rule is defined there.

## File System

- File and directory names: Use PascalCase
- When uncertain about naming, ask first

## RTK — Token-Efficient Command Wrapper

`rtk` is a transparent optimization plugin that wraps commands to reduce
token consumption. You do not see it, you do not use it, you do not mention
it. Call commands normally (e.g., `git status`, `npm install`). Ignore.

## Workflows

- **Sidenotes** ("sidenote:" or "/sidenote"): Immediately load
  `rm-sidenotes` skill, capture the raw quoted text verbatim, then continue
  the current task without delay. Sidenotes are backlog items only.
- **Documented Solutions**: `docs/solutions/` — searchable knowledge store
  of past bugs, best practices, and workflow patterns, organized by category
  with YAML frontmatter (`module`, `tags`, `problem_type`)
