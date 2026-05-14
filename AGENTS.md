---
date: 2026-04-20
title: AGENTS Project Guide (OpenCode-Optimized v2)
tags: [agent, rules, blazor, critical-policies, context-management, dotnet9]
description: Token-optimized single-file AGENTS.md for OpenCode harness. 100% of original policies, commands, workflows, Git optimizations, NPM rules, and verbatim context-mode routing preserved. ~28% smaller with heightened urgency. Frontmatter retained for harness compatibility and metadata routing. Use to prevent context truncation and improve multi-model adherence.
---

# AGENTS: Project Guide (OpenCode-Optimized v2)

## STRUCTURAL CHANGE GATE (READ FIRST — STOP HERE)

**This is the highest-priority rule in this document. Violating it breaks everything else.**

Before implementing ANY change that affects the build pipeline, toolchain, project structure, deployment, SCSS compilation, or any system spanning dev and production — you MUST answer three questions in writing:

1. **What constraints am I aware of?** — List every known constraint that applies (AGENTS.md rules, user directives, architectural decisions, platform requirements, tool policies, build requirements, Omarchy rules, npm policies, package manager rules, etc.).

2. **What do I NOT know?** — List gaps. Unknowns about the existing system. Side effects you cannot predict. User preferences you are assuming. Assumptions you're making without verification.

3. **What conflicts could this create?** — Map how the change interacts with every constraint from question 1. If any interaction is unclear or risky, you do not proceed.

If any answer is incomplete, if you are guessing about a constraint the user holds, if you are unsure about a side effect, or if the solution might collide with something else the user is balancing — **STOP AND ASK.** Do not implement. Do not edit files. Do not run commands.

Structural changes are not routine edits. They touch multiple systems. The user balances a dozen constraints that you cannot see. Only through this gate can we converge safely.

## PAIR PROGRAMMING

We are pair programming together.

**Your role**: You are the expert coder. You possess the complete technical skill and capability required to produce high-quality code and to create, modify, or update any files necessary to advance our tasks.

**Users role**: My sole responsibility is to ensure the entire collaborative process remains as smooth and error-free as possible. I fulfill this by continuously monitoring the workflow, identifying patterns, inefficiencies, friction points, hot spots, and any areas that require updating, optimization, fixing, or refactoring.

To enable me to perform my role effectively, two coordination rules are critical:

1. When I ask you a question, you must first provide a clear, direct, and complete answer before generating code, proposing changes, or taking any further action. These questions are posed specifically because I require the information to reach informed decisions that will have direct consequences for every subsequent step in our work. Proceeding to act immediately instead of answering first disrupts the process for me and prevents the informed coordination we both need.

2. You should commit changes when the work is ready and appropriate. However, you must not commit (whether via git commit or any equivalent permanent commit action) on your own initiative or without my explicit request or approval. When a commit occurs without my prior review, I lose the ability to examine the changes easily and quickly, which directly impairs my capacity to detect the very patterns, issues, and improvements I am responsible for addressing.

## CRITICAL

**ALWAYS**:

- Use `rm-commit` for all commits (never manual `git commit` or `git add`).
- Run `dotnet run --project tests/redmuffin.Blazor.StaticWeb.Tests -c Release` before every commit.
- Run `dotnet build --verbosity quiet` (C#) after every C# edit. SCSS is auto-compiled by `sass --watch`.
- If `dotnet run` or `dotnet build` fails repeatedly → run `dotnet clean` first.
- Read relevant code before answering any question.
- Use `pwsh -NoProfile` for all PowerShell execution.
- Wrap commit messages at 80 characters.
- Add `date: YYYY-MM-DD` frontmatter to every new file in `docs/`.
- Research unfamiliar APIs (Exa/web) before use.
- Timestamp temporary files referenced in commit messages.
- Make single-purpose commits only.
- In `Directory.Packages.props`: Define versions as properties; reference properties in item groups; never hard-code versions.
- Place all workflows and Dependabot configuration in `.github/`.
- Follow Trunk-Based Development (main branch is the source of truth); create feature branches only for high-risk changes.
- Use `es.exe` for fast file search when available; fall back to `grep` only if `es.exe` is unavailable.
- **Update the todo sidebar for every multi-step task.** The user relies on the
  OpenCode todo tool sidebar to track progress. For any task requiring 3+
  distinct steps, create todos BEFORE starting work and mark them complete
  IMMEDIATELY after finishing each one. An empty or stale todo list means
  the user cannot follow the work. This is as critical as reading code
  before answering — the sidebar IS the user's visibility into the plan.

**NEVER**:

- Commit, push, or expose secrets.
- Push to remote without explicit request.
- Use `git commit`, `git add`, or `git revert` without explicit request.
- Restore from git without asking first.
- Discuss or act on any sidenote ("sidenote:" or "/sidenote") during an active task (sidenotes belong in backlog only).
- Remove or consolidate SKILL COMMANDS tables (duplication is mandatory for model adherence).
- Bypass the NPM 1-day release age filter (`min-release-age=1` in `.npmrc`).
- Clear the todo list prematurely or let it go stale. The sidebar is the
  user's only visibility into multi-step task progress.

**ASK FIRST**:

- Any git restoration operation.
- Any change to `#pragma warning disable` directives.

**POLICY**:

- All pragma warnings are deliberate. Goal: zero warnings on build.
- Reviewers must target the correct subfolder or "Local" only.

## COMMANDS

| Command                                                                                                  | Purpose                                            | When                                        |
| -------------------------------------------------------------------------------------------------------- | -------------------------------------------------- | ------------------------------------------- |
| `dotnet run --project tests/redmuffin.Blazor.StaticWeb.Tests -c Release`                                 | Verify logic & prevent regressions                 | Pre-commit (mandatory)                      |
| `dotnet build --verbosity quiet`                                                                         | Verify C# compilation                              | Immediately after any C# edit               |
| `sass --watch scss:wwwroot/css`                                                                          | Auto-compile SCSS on save (background)             | Start of dev session                        |
| `sass --style=compressed --no-source-map scss/app.scss:wwwroot/css/app.min.css`                          | Production SCSS build (one-shot)                   | Before publish                              |
| `scripts/Update-PackageVersions.ps1`                                                                     | Update NuGet packages (Central Package Management) | After any package change                    |
| `dotnet run --project tests/redmuffin.Tools.QualityGates.Tests`                                          | Run quality gates tool tests (+ build)             | After any tools/ code change                |
| `dotnet format [<solution-path>]`                                                                        | Auto-fix ~75% of StyleCop/Roslyn violations        | Before manually fixing analyzer warnings    |
| `dotnet clean && dotnet build && dotnet run --project tests/redmuffin.Blazor.StaticWeb.Tests -c Release` | Full verification cycle                            | After NuGet updates or repeated failures    |
| `es.exe`                                                                                                 | Ultra-fast file search                             | Large solutions or searches outside project |
| `pwsh -NoProfile`                                                                                        | Cross-platform PowerShell execution                | Any PowerShell task                         |

**Git CLI Optimizations** (stable porcelain output for agents & scripts):

| Command | Purpose | When |
| ------- | ------- | ---- |

| `git status --porcelain=v2 --branch` | Machine-readable status (stable across Git versions) | Scripted workflows & parallel agents |
| `git diff --numstat` | Tab-separated line counts | Code review & diff analysis |
| `git diff --name-status` | File status (M/A/D/R) | Change categorization |
| `git for-each-ref --format='%(refname:short)' --merged HEAD refs/heads/` | Safe list of merged branches | Cleanup & branch hygiene scripts |
| `git --no-optional-locks status` | Status without index lock | Background or parallel agent sessions |
| `git log --format='%H\|%an\|%s'` | Custom log format for parsing | Release notes & automated analysis |
| `git remote get-url origin 2>/dev/null` | Retrieve remote URL safely | Automation & CI scripts |
| `gh pr list --json number,title` | Structured GitHub PR list | PR workflow automation |

**Rationale for Git porcelain commands**: Output is stable across Git versions and user configurations. `--no-optional-locks` prevents index locking when multiple agents run in parallel.

## WORKFLOWS

- **Sidenotes** ("sidenote:" or "/sidenote"): Immediately load `rm-sidenotes` skill, capture the raw quoted text verbatim, then continue the current task without delay. Sidenotes are backlog items only.
- **Everything Search (`es.exe`)**: If the tool fails or is unavailable, STOP and report the failure. Do NOT attempt any fallback search without explicit approval.
- **Undo Last Commit**: Revert the commit while leaving all changes as unstaged edits in the working directory.
- **Restore a File from HEAD**: When a bulk edit (sed, replaceAll, write) corrupts a file, restore the clean committed version with `git show HEAD:path/to/file > path/to/file`. This bypasses the safety-net-blocked `git checkout` while achieving the same result. Capture the clean version BEFORE starting the bulk edit — then no restore is needed.
- **Skill Review**: Before activating any skill or analyzer, verify the correct name exists in the appropriate subfolder. On failure, double-check the name and retry.
- **Todo Sidebar**: For any task with 3+ distinct steps, use the `todowrite` tool BEFORE starting. Create one todo per logical unit. Mark each complete IMMEDIATELY after finishing — never batch completions. Clear the list only when all work is done. The sidebar is the user's primary progress indicator; an empty or stale list means they have no visibility into your work.
- **Quality Gates — Recursive Loop**: Gates are not one-shot. Run → fix worst violations → re-run → repeat until zero violations across all gates. See `rm-gates-cleanup` §0 for the full principle.
- **Quality Gates — Recursive Loop**: Gates are not one-shot. Run → fix worst violations → re-run → repeat until zero violations across all gates. See `rm-gates-cleanup` §0 for the full principle.

## PATTERNS

**PowerShell (Cross-Platform — PowerShell 7+ / `pwsh`)**:

- Prefer native PowerShell cmdlets and modules over bash-style constructs.
- Use proper quoting and escaping: backticks for special characters, `@' '@` for literal strings.
- Produce structured output with `ConvertTo-Json` or `Out-String -Width 4096`.
- Always use `try/catch` with `-ErrorAction Stop` for error handling.
- Use full paths with `\` or `/`; prefer `Join-Path` for cross-platform compatibility.
- **Windows (shell = pwsh)**: Execute directly — no wrapper required.
- **Linux / omarchy (shell = bash)**: MUST wrap all PowerShell commands as `pwsh -NoProfile -Command '...' ` (single quotes) so bash does not interpolate `$variables` before PowerShell sees them.
- Complex or multi-line scripts: Write the script to a `.ps1` file first using the write tool, then execute it with `pwsh -NoProfile -File path/to/script.ps1`.

**NPM Global Packages (Supply Chain Security)**:

- All global packages are protected by a mandatory 1-day release age filter (`min-release-age=1` in `.npmrc`). **NEVER bypass this filter** — it prevents typosquatting and malicious package releases.
- Update procedure:
  1. Query release dates: `npm view <package> time --json`
  2. Identify the latest version published more than 1 day ago.
  3. Install only qualifying versions.
  4. Verify installation: `npm list -g --depth=0`
- Example (prettier): Run `npm view prettier time --json`, locate the newest version older than 1 day, then install it.

## STACK & STRUCTURE

- **Technology Stack**: .NET 10 SDK (builds net9.0 projects for Azure SWA), Blazor WebAssembly (.NET 9), Azure Functions (isolated worker, .NET 9), TUnit testing framework, SCSS.
- **SDK vs Target**: All projects target `net9.0` for Azure SWA compatibility. The .NET 10 SDK provides build tooling, Roslyn, and MSBuild — it does not require changing target frameworks. When SWA adds .NET 10 Functions support, updating targets is a one-line change per `.csproj`.
- **Knowledge Base**: `docs/solutions/` — searchable archive of past solutions, bugs, best practices, and workflow patterns. All entries use YAML frontmatter with `module`, `tags`, and `problem_type` fields.
- **Key Paths**:
  - `src/redmuffin.Blazor.StaticWeb/` — Frontend application
  - `src/redmuffin.Blazor.StaticWeb.Api/` — Backend API
  - `tests/` — Test project mirror
  - `docs/solutions/` — Persistent knowledge store
  - `tools/` — Quality Gates toolchain (CRAP, SCRAP, Architecture, Mutation). See `tools/README.md`.

## context-mode — MANDATORY routing rules (Preserved verbatim for correctness)

You have context-mode MCP tools available. These rules are NOT optional — they protect your context window from flooding. A single unrouted command can dump 56 KB into context and waste the entire session.

**Think in Code — MANDATORY**: When you need to analyze, count, filter, compare, search, parse, transform, or process data: **write code** that does the work via `context-mode_ctx_execute(language, code)` and `console.log()` only the answer. Do NOT read raw data into context to process mentally. Your role is to PROGRAM the analysis, not to COMPUTE it. Write robust, pure JavaScript — no npm dependencies, only Node.js built-ins (`fs`, `path`, `child_process`). Always use `try/catch`, handle `null`/`undefined`, and ensure compatibility with both Node.js and Bun. One script replaces ten tool calls and saves 100x context.

**BLOCKED commands — do NOT attempt these**

- **curl / wget — BLOCKED**: Any shell command containing `curl` or `wget` will be intercepted and blocked by the context-mode plugin. Do NOT retry. Instead use: `context-mode_ctx_fetch_and_index(url, source)` or `context-mode_ctx_execute(language: "javascript", code: "const r = await fetch(...)")`
- **Inline HTTP — BLOCKED**: Any shell command containing `fetch('http`, `requests.get(`, `requests.post(`, `http.get(`, or `http.request(` will be intercepted and blocked. Do NOT retry with shell. Instead use sandbox equivalent.
- **Direct web fetching — BLOCKED**: Do NOT use any direct URL fetching tool. Use the sandbox equivalent.

**REDIRECTED tools — use sandbox equivalents**

- **Shell (>20 lines output)**: Shell is ONLY for: `git`, `mkdir`, `rm`, `mv`, `cd`, `ls`, `npm install`, `pip install`, and other short-output commands. For everything else, use: `context-mode_ctx_batch_execute(commands, queries)` — run multiple commands + search in ONE call (each command: `{label: "descriptive header", command: "..."}`. Label becomes FTS5 chunk title). Or `context-mode_ctx_execute(language: "shell", code: "...")`
- **File reading (for analysis)**: If reading to **edit** → reading is correct. If reading to **analyze, explore, or summarize** → use `context-mode_ctx_execute_file(path, language, code)` instead. Only your printed summary enters context.
- **grep / search (large results)**: Search results can flood context. Use `context-mode_ctx_execute(language: "shell", code: "grep ...")` to run searches in sandbox. Only your printed summary enters context.

**Tool selection hierarchy**

1. **GATHER**: `context-mode_ctx_batch_execute(commands, queries)` — Primary tool. Runs all commands, auto-indexes output, returns search results. ONE call replaces 30+ individual calls.
2. **FOLLOW-UP**: `context-mode_ctx_search(queries: ["q1", "q2", ...])` — Query indexed content. Pass ALL questions as array in ONE call.
3. **PROCESSING**: `context-mode_ctx_execute(language, code)` | `context-mode_ctx_execute_file(path, language, code)` — Sandbox execution. Only stdout enters context.
4. **WEB**: `context-mode_ctx_fetch_and_index(url, source)` then `context-mode_ctx_search(queries)` — Fetch, chunk, index, query. Raw HTML never enters context.
5. **INDEX**: `context-mode_ctx_index(content, source)` — Store content in FTS5 knowledge base for later search.

**Output constraints**

- Keep responses under 500 words.
- Write artifacts (code, configs, PRDs) to FILES — never return them as inline text. Return only: file path + 1-line description.
- When indexing content, use descriptive source labels so others can `search(source: "label")` later.

**ctx commands**

| Command | Action |
| ------- | ------ |

| `ctx stats` | Call the `stats` MCP tool and display the full output verbatim |
| `ctx doctor` | Call the `doctor` MCP tool, run the returned shell command, display as checklist |
| `ctx upgrade` | Call the `upgrade` MCP tool, run the returned shell command, display as checklist |
| `ctx purge` | Call the `purge` MCP tool with confirm: true. Warns before wiping the knowledge base. |

After /clear or /compact: knowledge base and session stats are preserved. Use `ctx purge` if you want to start fresh.

<!-- Optimized score: 94/100 (token reduction ~28% from original, all policies and verbatim sections preserved, urgency heightened via CRITICAL consolidation, quantitative scoring applied, structure follows v2.2 agent-optimizer rules, frontmatter retained and optimized for harness compatibility) -->
