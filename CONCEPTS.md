# Concepts

Shared domain vocabulary for this project — entities, named processes, and status concepts with project-specific meaning. Seeded with core domain vocabulary, then accretes as ce-compound and ce-compound-refresh process learnings; direct edits are fine. Glossary only, not a spec or catch-all.

## Grok Build CLI

The native Grok agent harness (distinct from OpenCode/Cursor adapter modes). Surfaces direct tools (`read_file`, `search_replace`, `lsp`, `run_terminal_command`, etc.) when the active model is `grok-build`. Requires explicit `[features] lsp_tools = true` and model selection (or pin) for native `lsp` surface.

## .grok/

Per-project (and user) configuration directory for the Grok Build CLI harness. Holds `lsp.json` (language server definitions), `config.toml` (features, models, permissions), skills/, hooks/, etc. Project-level files take precedence for the current workspace.

## .grok/lsp.json

Project-level (or user-level) definition of language servers for the Grok `lsp` tool. Each entry names a server id (e.g. "roslyn"), `command` (absolute path on Windows for .cmd shims), `args`, `extensionToLanguage` mappings, and timeouts. Merged with precedence (project > user > plugins). Changes require a full CLI restart because servers are spawned at host/session init.

## roslyn-language-server (Grok)

The external Roslyn stdio language server (`Microsoft.CodeAnalysis.LanguageServer.exe`, typically exposed on Windows via a `.cmd` shim at `~/.local/bin/windows/roslyn-language-server.cmd`). Configured for C# (`.cs` → `csharp`) and Razor (`.razor` → `razor`). Requires `--stdio --autoLoadProjects` (or equivalent) plus the absolute shim path under direct-spawn harnesses. Emits `window/logMessage` "Language server initialized" on successful startup.

## direct spawn (agent tools)

The non-shell process launch mechanism used by the Grok Build CLI host for stdio-controlled children (language servers, etc.). Equivalent to .NET `ProcessStartInfo { UseShellExecute = false, RedirectStandard* = true }`. Does not resolve bare command names via `PATH` + `PATHEXT` on Windows; absolute paths (or .exe) are required for shims that are .cmd/.bat files.

## session restart (Grok config)

Requirement after changes to `.grok/lsp.json`, `[features]`, or `[models]` that affect tool surfaces or child servers. `grok inspect` re-reads from disk live and shows the current state, but the running host binds `lsp` (and similar) child processes only at TUI/session launch time.

## Karpathy Change Gate

Mandatory pre-mutation discipline in this repo: externalize (1) provable problem with cited data, (2) hypothesis, (3) test command via tool call; apply one bounded mutation; run the test; return to INVESTIGATE. Enforced via `rm-karpathy` skill before edits/writes/installs/config changes (git staging/commits during COMMIT_BATCH are exempt).

## docs/solutions/

Searchable archive of past solutions, bugs, best practices, and workflow patterns. Entries use YAML frontmatter (`module`, `tags`, `problem_type`, `date`, `component`, `severity`, track-specific fields) and are organized under category subdirectories (`tooling-decisions/`, `developer-experience/`, `workflow-issues/`, etc.). Relevant when implementing features, debugging, or making decisions in areas that already have documented learnings.

*(Seeded from the 2026-06-20 Grok Build CLI Roslyn LSP Windows spawn + restart learning in tooling-decisions/ + prior session memory on agent harness enablement. Core nouns limited to the agent tooling / harness config area actually investigated.)*
