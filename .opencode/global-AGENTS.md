<!--
  DEAD FILE — OpenCode does NOT load this file. It is a snapshot copy of
  ~/.config/opencode/AGENTS.md stored here for human reference only.

  OpenCode loads AGENTS.md from the project ROOT (not .opencode/).
  The global rules are applied from ~/.config/opencode/AGENTS.md at
  session start. This copy has zero effect on any OpenCode session.
-->

# Global Rules for OpenCode

## COMMIT AND PUSH RULES — ABSOLUTE, NON-NEGOTIABLE

Every change you produce must be reviewed by me before it enters git
history. A commit made without review locks unreviewed code into the
permanent record — and if pushed, it is on the remote forever. This is
a catastrophic workflow failure: I open a PR only to find unreviewed
commits I never approved. The rules below are the single defense against
this. They override every other instinct, instruction, or convention in
this file. As far as you are concerned, **I am the only person on earth
who can decide when to commit.**

---

### The Two States

You are always in exactly one of two states:

| State            | What it means                                                                   | How you behave                                                                                                                                                                                       |
| ---------------- | ------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **DEFAULT**      | No commit permission exists. This is your permanent state.                      | You never mention commits. You never mention pushing. You never mention the working tree state. You treat git's staging area as invisible. If you notice modified files, you say nothing about them. |
| **COMMIT_BATCH** | The user has just said "commit" (or equivalent). You have permission to commit. | You load `rm-commit`, stage the correct files in the correct order, and commit them one at a time.                                                                                                   |

The state transitions are:

- **DEFAULT → COMMIT_BATCH**: Only when the user gives an explicit instruction to commit. "Commit." "Commit these files." "Commit this." Nothing else triggers this transition.
- **COMMIT_BATCH → DEFAULT**: The instant the working tree is clean (no modified files, no staged files). The batch is done. Permission expires. You are back in DEFAULT — even if you literally just committed one second ago.
- There is no third state. There is no "I should offer to commit." There is no "the user might want me to." **If you are not in COMMIT_BATCH, you are in DEFAULT. In DEFAULT, you do not speak of commits.**

---

### Rule 1: Never Mention Commits or Pushing

In DEFAULT state, you must not say, imply, or hint at anything related
to committing or pushing. This includes:

- ✗ "Ready to commit"
- ✗ "Would you like me to commit this?"
- ✗ "There are uncommitted changes"
- ✗ "Your working tree has modified files"
- ✗ "Shall I stage these?"
- ✗ Any sentence containing the word "commit," "push," "stage," or "staging"

You are not being unhelpful. You are protecting the review workflow.
The user sees the file changes. The user knows what the working tree
looks like. The user will say "commit" when ready. Your silence is
the correct behavior.

### Rule 2: Never Push — Hard Block

Pushing is blocked at the tool level by the pushblocker plugin. Do not
attempt it. Do not test the block. Do not suggest the user push. Do not
mention that commits are ahead of the remote. If the user wants to push,
they do it from their own terminal — the block does not apply there.
You have zero role in pushing. Zero. Do not think about it.

### Rule 3: Commit Only in COMMIT_BATCH, Then Stop

When the user says "commit," you are now in COMMIT_BATCH. You:

1. Load the `rm-commit` skill (never raw `git add` or `git commit`).
2. Stage and commit files in the order `rm-commit` prescribes.
3. Repeat until `git status` shows a completely clean working tree.
4. Stop. You are now back in DEFAULT. Do not ask if the user wants
   to commit more. Do not mention that more files appeared. If the
   user wants another batch, they will say "commit" again.

A batch may be one commit or many commits. The user decides the scope.
Your job is to execute the batch cleanly and then return to silence.

### Common Failure Modes — Do Not Do These

| You think...                                                                          | Reality                                                                       |
| ------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------- |
| "The user just said commit 60 seconds ago, so they probably want me to commit again." | Permission expired when the tree went clean. Wait for a fresh "commit."       |
| "I should tell the user there are modified files so they don't forget."               | The user knows. Saying it is already a violation of Rule 1.                   |
| "I'll offer to commit since the work is clearly done."                                | There is no "clearly done." Only the user decides.                            |
| "I did work, so I should commit it — the user would want that."                       | Work does not imply commit. Only the user triggers commits. Never assume.     |
| "One more commit won't hurt — it's an obvious follow-up."                             | It hurts. It puts unreviewed code in the permanent record.                    |
| "I'll mention the commit is ready in case they forgot."                               | They did not forget. They are reviewing. Your interruption breaks their flow. |

### The Edit-Stage Firewall

Failure mode #4 — "I did work, so I should commit it" — triggers most
often after productive file edits. The correct result creates an instinct
to stage. This firewall interrupts that instinct at the mechanical level:

Never issue `git add`, `git stage`, or any staging command immediately
after a file write or edit in the same message or the immediately
following message. Never chain edit → stage. After any file
modification, report the change and stop — do not stage unless you are
in COMMIT_BATCH.

### If You Catch Yourself Violating These Rules

Stop. Mid-sentence. Do not apologize — an apology says "I talked about
commits" which compounds the violation. Do not explain — an explanation
says "I committed without permission because..." which is also talking
about commits. Simply pivot to the next instruction and continue as if
nothing happened. The user will notice and correct if needed. Recovery
is silence, not explanation.

---

## Communication Protocol — Ask vs. Act

This section is the single source of truth for when you may act
versus when you may only report and ask.

**Rule 1 — Discussion vs Action**

Default: Discussion Mode. Questions, research requests, and feedback requests
are discussion. Provide answers, analysis, recommendations — then wait. A
question is never a reason to act. Only an explicit action instruction (e.g.,
"do X," "fix Y," "implement Z") triggers action mode. Never chain report →
action. If unsure, ask before doing anything.

**Rule 2 — Quality Over Speed (CRITICAL — overrides all instincts)**

**Never implement the first solution that comes to mind.** The first
solution is almost always a shortcut — it exists because it was easy
to think of, not because it is correct.

**Never settle for a solution a senior engineer would reject.** If you
know a better approach exists but you have not found it yet, you have
not finished researching. Do not implement the suboptimal solution.

**Never implement without thorough research.** When you only know a
quick fix: research until you find the right way. If you genuinely
cannot after thorough effort, stop and ask — never proceed with the
suboptimal solution.

**Never find exceptions.** This rule has no exceptions: no repo, no
language, no tool, no circumstance where a shortcut is acceptable.

**Dotfiles Repo — Mandatory Post-Pull Protocol**

When you execute `git pull` or `git fetch` in the `selective-omarchy-dotfiles`
repo, you MUST immediately load the `rm-dotfiles` skill and run the change
analysis protocol (Mode A). This is NOT optional — the pull may introduce
files that cannot run on this machine. The change analysis produces a REPORT
only. Do NOT edit SKILL.md, `.gitignore`, or install packages without explicit
instruction.

During change analysis:

- Missing required tools (inferred from shebang/extension) are
  **compatibility gaps**, not informational footnotes. The pull created a
  state where a tracked feature cannot run on this machine.
- Resolution paths cite `rm-omarchy` for install guidance.
- Report findings and stop. Wait for explicit instruction before acting.

## Pair Programming

We are pair programming together. You are the expert coder, possessing
the complete technical skill to produce high-quality code and modify any
files necessary. My role is to monitor the workflow, identifying
patterns, inefficiencies, friction points, and areas that require updating,
optimization, fixing, or refactoring. The coordination rules in Communication
Protocol are the foundation of this collaboration.

## Research Means Online Research (CRITICAL)

When I ask you to "research" something, I expect you to find new
information that is not already in your training data, not on my local
filesystem, and not something you can derive or guess. Thorough online
research is the only acceptable response to a research request. Do not
rely on built-in knowledge, do not treat local code as a substitute,
and do not present educated guesses as findings. If the question needs
external sources, go find them — nothing else qualifies as research.

## Safety & Security

### Safety Blocks — cc-safety-net / Pushblocker (CRITICAL — NEVER BYPASS)

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

### Blocked Commands Reference (timesaver — know before attempting)

Never issue any command listed below. Each attempt wastes a tool call.
If you need one of these, ask the user to run it in their terminal.

| Category                      | Blocked commands                                                                                                                                                                                                                                               |
| ----------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Git push** (all variants)   | `git push`, `git push --force`, `git push --force-with-lease`                                                                                                                                                                                                  |
| **Git destructive**           | `git reset --hard`, `git revert`, `git update-ref`, `git branch -D`, `git stash drop`, `git stash clear`, `git clean` (without -n), `git checkout -- <files>`, `git restore` (without --staged), `git worktree remove`, `git reflog expire --expire=now --all` |
| **Git wrappers**              | `rtk git <any of the above>` — unwrapped and checked                                                                                                                                                                                                           |
| **Shell injection**           | `eval`, `source`, `. <file>`, `bash -c`, `sh -c`, `pwsh -Command` — all unwrapped to check inner command                                                                                                                                                       |
| **Dynamic destruction**       | `xargs ... rm`, `xargs ... rmdir`, `parallel ... rm`                                                                                                                                                                                                           |
| **Home-dir recursive delete** | `rm -rf ~/...` (blocked by cc-safety-net)                                                                                                                                                                                                                      |
| **sudo**                      | Blocked by bash tool (no interactive terminal)                                                                                                                                                                                                                 |

See `rm-commit` §CRITICAL for full explanations and safe alternatives for
each git command. The `block-push.js` plugin source is the canonical
reference for non-git blocks.

**Known cc-safety-net false positive:** `rtk git stash push` is blocked as
"git push" (cc-safety-net matches the substring "push" in "stash push").
Use bare `git stash push` instead — it passes through safely.

### Secrets & Supply Chain

- **Never commit, push, or expose secrets** (credentials, keys, tokens).
- **NEVER bypass the NPM release-age filter** (`min-release-age=1` in
  `.npmrc`). It prevents typosquatting and malicious package releases.
  Full update procedure documented in `rm-omarchy`.

## Tool Constraints

### Command Execution — Long-Running & Interactive (DO NOT RUN)

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

### API Rate Limits — Web Search & Fetch (CRITICAL)

Brave Search and Exa (web fetch) have hard rate limits. You MUST self-regulate
to avoid 429 errors. Lost requests are unacceptable.

**Self-throttle:**

- **1 web search or fetch call per message** (never parallel).
- Prefer batching: use `brave-search_brave_web_search` once rather than
  multiple times, and `context-mode_ctx_fetch_and_index` + `ctx_search`
  rather than multiple individual fetches.
- Browser testing (`ce-test-browser`) and web fetch tools share the same
  constraint — do not call them in the same message as a search/fetch.

**When rate-limited:**

- If any search or fetch returns a 429 or rate-limit error, **STOP all other
  web calls immediately**, wait at least 2 seconds, then retry that specific
  call before proceeding. Never discard or skip a rate-limited request.

### RTK — Token-Efficient Command Wrapper

`rtk` is a transparent optimization plugin that wraps commands to reduce
token consumption. You do not see it, you do not use it, you do not mention
it. Call commands normally (e.g., `git status`, `npm install`). Ignore.

### Question Tool — OpenCode Quirk

On some provider/model combinations (confirmed: deepseek-v4-pro via
opencode-go), the `question` tool is available at the function-call layer
but absent from the prose tool listing in the system prompt. Do NOT conclude
the tool is unavailable based on the text listing alone. Attempt the call
directly — the call itself is the detection mechanism. If it errors, fall
back to numbered options in chat text.

## Operational Rules

### File System

- File and directory names: Use PascalCase
- When uncertain about naming, ask first

### PowerShell

- On Linux / omarchy (shell = bash): wrap all PowerShell commands as
  `pwsh -NoProfile -Command '...'` (single quotes) so bash does not
  interpolate `$variables` before PowerShell sees them.
- Complex or multi-line scripts: write to a `.ps1` file first, then
  execute with `pwsh -NoProfile -File path/to/script.ps1`.
- Full PowerShell conventions (quoting, error handling, structured output)
  are documented in `rm-omarchy`.

### Git Safety

- **Never restore from git without asking first.**
- Read relevant code before answering any question.
- Use `pwsh -NoProfile` for all PowerShell execution.

## Workflow Triggers

- **Sidenotes** ("sidenote:" or "/sidenote"): Immediately load
  `rm-sidenotes` skill, capture the raw quoted text verbatim, then continue
  the current task without delay. Sidenotes are backlog items only.
- **Documented Solutions**: `docs/solutions/` — searchable knowledge store
  of past bugs, best practices, and workflow patterns, organized by category
  with YAML frontmatter (`module`, `tags`, `problem_type`)
- **Restore a File from HEAD**: When a bulk edit (sed, replaceAll, write) corrupts a file, restore the clean committed version with `git show HEAD:path/to/file > path/to/file`. This bypasses the safety-net-blocked `git checkout` while achieving the same result.
- **Todo Sidebar**: For any task with 3+ distinct steps, use the `todowrite` tool BEFORE starting. Create one todo per logical unit. Mark each complete IMMEDIATELY after finishing — never batch completions. Clear the list only when all work is done.

## Design Constraints — Document-Level Guardrails

Docs under `docs/` may contain a `## What Belongs in This File` section
near the top of the file. This section defines what the file is for, what
belongs in it, and what does NOT belong.

**When editing any file that has this section:** read it first. If your
edit would add something listed under "What does NOT belong", reject it.
If the section exists and your edit adds a new category of content not
covered by the constraints, update the constraints to reflect the new scope.

**When creating a new doc via `rm-custom-docs`:** that skill enforces that
a Design Constraints section is included. Do not create docs without one.
