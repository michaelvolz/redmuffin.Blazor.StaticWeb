---
title: "Grok Build native `lsp` tool — Roslyn language server on Windows requires absolute command path in .grok/lsp.json"
date: 2026-06-20
category: tooling-decisions
module: grok-build-lsp
problem_type: tooling_decision
component: tooling
severity: high
applies_when:
  - "Using the Grok Build CLI harness (`grok-build` model) with native `lsp` tool on Windows for .NET/Blazor projects (C# + .razor)"
  - "roslyn-language-server is installed (exposed via .cmd shim at ~/.local/bin/windows/roslyn-language-server.cmd wrapping versioned Microsoft.CodeAnalysis.LanguageServer.exe)"
  - "Project .grok/lsp.json exists with roslyn entry; user ~/.grok/config.toml has [features] lsp_tools = true and [models] default = \"grok-build\""
  - "Seeking full solution load, Razor support, goToDefinition/findReferences/workspaceSymbol etc. via the native tool (vs. ast-grep + dotnet build fallback)"
tags:
  - grok
  - grok-build
  - lsp
  - roslyn
  - language-server
  - windows
  - blazor
  - agent-tooling
  - dx
  - config
  - path-resolution
---

# Grok Build native `lsp` tool — Roslyn language server on Windows requires absolute command path in .grok/lsp.json

## Context

Prior sessions in the same workspace (documented in memory files under `~/.grok/memory/redmuffin-blazor-staticweb-8960be8f`) had already captured the earlier gaps required to surface the native `lsp` tool at all:

- "Gap: No `.grok/lsp.json` and `lsp_tools` false → no `lsp` tool in Grok sessions despite installed Roslyn server. **Fix:** add project `lsp.json` and enable `lsp_tools`."
- "Symptom: Agent cannot use `lsp` in Grok Build CLI despite `.grok/lsp.json`, `lsp_tools = true`, and `roslyn-language-server` on PATH; `grok inspect` shows LSP config OK."
- Cause tied to model: only `grok-build` (not composer family) exposes native tools including `lsp`; "Default trap: `grok models` may show `default: grok-composer-2.5-fast`, so new sessions never get native `lsp` without changing model." Pin via `~/.grok/config.toml` `[models] default = "grok-build"`.
- "Need new session after config changes."

User-level config: `~/.grok/config.toml` had `[features] lsp_tools = true` and `[models] default = "grok-build"`.

Project `.grok/lsp.json` was created with bare command `"roslyn-language-server"`, args `["--stdio", "--autoLoadProjects"]`, mappings `.cs=csharp .razor=razor`, `startupTimeout` 60000.

User installed roslyn-language-server (shows as .cmd shim at `C:\Users\flynn\.local\bin\windows\roslyn-language-server.cmd` wrapping the Microsoft.CodeAnalysis.LanguageServer.exe under a versioned .store path). `grok inspect` showed the LSP server entry and "Project trusted: no".

**Observable symptoms of the remaining blocker (this session):** Calling the `lsp` tool (workspaceSymbol etc.) failed with "Tool `lsp` failed: LSP startup failed: No LSP servers started successfully." (elapsed_ms:0 in logs).

Diagnosis steps that succeeded: `Get-Command` found the .cmd; manual invocation of "roslyn-language-server" --help worked in pwsh. .NET `ProcessStartInfo` with `UseShellExecute=false` and bare name failed ("cannot find the file"); the same with full absolute path to .cmd succeeded and server responded to --help.

Further manual spawn test (with stdin/stdout/stderr redirect, no full handshake) showed server starts, stays alive until stdin closed, emits "Language server initialized" logMessage on stdout (no stderr errors).

**Root cause (per session analysis, after full externalization):** Windows PATHEXT vs direct ProcessStartInfo spawn (the mechanism used by the LSP host for stdio pipes); session-lifetime of LSP child processes (servers started at session init based on lsp.json at launch time; post-edit `grok inspect` reflected the change but running children did not).

**What was tried:** PATH shim + shell success, bare name in json, manual ProcessStartInfo probes, minimal handshake tests.

**Working solution (after Karpathy Change Gate externalization via `run_terminal_command` print of provable problem + hypothesis + test command):** edited `.grok/lsp.json` command to the full absolute Windows path `"C:\\Users\\flynn\\.local\\bin\\windows\\roslyn-language-server.cmd"` (using `search_replace`). Post-fix `grok inspect` (same session) updated to show the full path, but `lsp` tool calls still failed (expected). User restarted the Grok Build CLI.

After restart: `grok inspect` confirms the full-path roslyn entry. Verification with `lsp` tool succeeded: `workspaceSymbol "Program"` succeeded (returned Class Program location); `workspaceSymbol "Raindrop"` returned 100+ hits (classes, methods, tests, generated .g.cs serializer contexts); `documentSymbol` on RaindropItem.cs (absolute B:/... path, 0-based pos) returned the record + all properties; `documentSymbol` on RaindropItemList.razor returned BuildRenderTree(); `goToDefinition` from a MediaItem usage site in RaindropItem.cs returned the definition in MediaItem.cs; `findReferences` on RaindropItem class declaration returned 406 locations across source, tests, generated code, etc.

"The --autoLoadProjects enabled full solution load. .razor mapping worked for Razor support. Project .grok/lsp.json takes precedence."

Related Docs Finder (ce-compound Full mode) confirmed no high or moderate overlap in `docs/solutions/`. Low tangential hits exist on agent-process and tooling docs (Windows Start-Process patterns, pre-commit LSP misuse note which is superseded to AGENTS.md, configureawait MSBuild/Roslyn process trees). The closest high-level guidance lives in root `AGENTS.md` (Grok Build `lsp` requirements + `.grok/lsp.json` + "use LSP operations instead of grep for semantic queries") and a configureawait status guide that mentions the exact roslyn-language-server.cmd path (but for OpenCode, not Grok). Memory files for this workspace (2026-06-20-interval-*) capture the complete progression (basic enablement gaps + this session's Windows spawn + restart detail) with 5/5 dimensional overlap for the new learning.

## Guidance

For any external language server shim on Windows inside the Grok Build CLI (or similar direct-spawn hosts), the `"command"` value in `.grok/lsp.json` **must** be the absolute path to the `.cmd` (or `.exe`) wrapper. The harness does not perform shell / `PATHEXT` / `PATH` resolution.

Project-level `.grok/lsp.json` wins over user-level configuration. Use the Windows-specific full shim path when the installation method (e.g. `dotnet tool install -g` or custom `~/.local/bin` setups) produces a `.cmd` wrapper.

Changes to `.grok/lsp.json` require a complete CLI restart (new session). `grok inspect` reads from disk and will show updates, but the active language server host binds servers at launch time and does not hot-reload children.

The `grok-build` model is required to surface the native `lsp` tool (composer-family models use a different adapter without it).

Working configuration for the Roslyn server (exact values that produced real results):

```json
{
  "roslyn": {
    "command": "C:\\Users\\flynn\\.local\\bin\\windows\\roslyn-language-server.cmd",
    "args": ["--stdio", "--autoLoadProjects"],
    "extensionToLanguage": {
      ".cs": "csharp",
      ".razor": "razor"
    },
    "startupTimeout": 60000
  }
}
```

Apply via one bounded `search_replace` after printing the provable problem/hypothesis/test (Karpathy gate). Then restart.

## Why This Matters

A bare command name creates a silent total failure: the server appears configured in `grok inspect` yet the `lsp` tool is unusable for the entire session. All semantic operations (cross-file definitions, references, symbols, Razor support) are blocked behind the generic "No LSP servers started successfully" message.

Once the absolute `.cmd` path is set and the CLI is restarted, the Roslyn server (Microsoft.CodeAnalysis.LanguageServer) delivers high-quality results: workspace-wide symbol search, document symbols on both `.cs` and `.razor` (Razor language server active), `goToDefinition` across files, and `findReferences` that correctly surface hundreds of locations including real source, tests, and generated `.g.cs` files. `--autoLoadProjects` enables full solution load; the `.razor` mapping activates correct language support.

## When to Apply

- Configuring or repairing the `lsp` tool for C# / Razor / .NET projects in the Grok Build CLI harness on Windows.
- The language server was installed via `dotnet tool install -g` or a `.local/bin/windows/` layout that produces `.cmd` shims.
- `lsp` operations fail with "LSP startup failed: No LSP servers started successfully" (or return instantly with no data) even though bare commands succeed from PowerShell/cmd and `grok inspect` lists an entry.
- After editing `"command"`, `"args"`, or `extensionToLanguage` in `.grok/lsp.json`.
- Using project-specific configuration (`.grok/lsp.json` present in repo root) rather than (or in addition to) user-level settings.
- The environment requires `grok-build` model + `[features] lsp_tools = true`.

## Examples

**Before (bare command value — fails under harness direct spawn):**

```json
"command": "roslyn-language-server"
```

**After (absolute path to the .cmd shim — succeeds):**

```json
"command": "C:\\Users\\flynn\\.local\\bin\\windows\\roslyn-language-server.cmd"
```

**Full working `.grok/lsp.json` (edit the file at the relative project path `.grok/lsp.json`):**

```json
{
  "roslyn": {
    "command": "C:\\Users\\flynn\\.local\\bin\\windows\\roslyn-language-server.cmd",
    "args": ["--stdio", "--autoLoadProjects"],
    "extensionToLanguage": {
      ".cs": "csharp",
      ".razor": "razor"
    },
    "startupTimeout": 60000
  }
}
```

**Activation sequence (post-edit, copy-paste ready):**

```text
# After the single search_replace (Karpathy gate already externalized):
grok inspect

# (inspect will show the full path, but lsp calls will still fail until restart)

# 1. Fully exit the current Grok Build CLI session
# 2. Start a fresh Grok Build CLI session (new host launch)

grok inspect
# Confirm: command now shows the absolute .cmd path
```

**Post-restart verification `lsp` calls (all succeed with real data after the fix):**

```text
lsp workspaceSymbol query="Program"
lsp workspaceSymbol query="Raindrop"
lsp workspaceSymbol query="MediaItem"
# Large result sets returned, including generated source files

lsp documentSymbol file_path="B:/redmuffin.Blazor.StaticWeb/src/redmuffin.Blazor.StaticWeb.Common/Raindrop/RaindropItem.cs"
# Returns class/record + properties (use absolute forward-slash form on Windows)

lsp documentSymbol file_path="B:/redmuffin.Blazor.StaticWeb/src/redmuffin.Blazor.StaticWeb/App.razor"
# Returns BuildRenderTree and other members (Razor language server is active)

lsp goToDefinition file_path="B:/redmuffin.Blazor.StaticWeb/src/redmuffin.Blazor.StaticWeb.Common/Raindrop/RaindropItem.cs" line=28 character=24
# From the MediaItem usage inside RaindropItem.cs — returns the correct target file:line:col (MediaItem.cs)

lsp findReferences file_path="B:/redmuffin.Blazor.StaticWeb/src/redmuffin.Blazor.StaticWeb.Common/Raindrop/RaindropItem.cs" line=4 character=15
# On RaindropItem — returns 406 locations (real code + tests + .g.cs generated files)
```

**Diagnostic reproduction that exposed the root cause (for future similar shims):**

- Shell resolution: `& "roslyn-language-server" --help` succeeds.
- Harness-equivalent direct spawn (bare name): "The system cannot find the file specified."
- Harness-equivalent direct spawn (full `.cmd` path): process starts, stdio works, server emits initialization logMessage.

## Related

- Root `AGENTS.md` — High-level Grok Build `lsp` requirements (`[features] lsp_tools = true` + `.grok/lsp.json` configures Roslyn), explicit list of native `lsp` operations to prefer over `grep`/`read` for semantic queries, structural search fallback to `ast-grep` + `rm-structural-search`, and "Use LSP tools for call graphs and references". Also documents the broader agent workflow (pre-commit verification, cleanup sessions, etc.). Related Docs Finder noted this is the closest project-level guidance; the new details (Windows absolute .cmd + restart) would improve it.
- `~/.grok/memory/redmuffin-blazor-staticweb-8960be8f/sessions/2026-06-20-interval-*.md` (ephemeral session memory, "this session only") — Captures the complete arc for this workspace: initial gaps (no lsp.json + lsp_tools=false; model grok-build only; restart after changes), then the Windows-specific diagnosis and absolute-path fix, post-restart verification with concrete `lsp` results (406 references, documentSymbol on .cs/.razor, etc.), and the "compound this full mode this session only" trigger itself. Used as primary evidence here (5/5 dimensional overlap).
- `docs/configureawait-fixer-status-guide-2026-06-08.md` — Documents the exact roslyn-language-server.cmd shim path (for OpenCode). Related Docs Finder flagged it as a refresh candidate to add Grok harness contrast (absolute path requirement for direct spawn; restart semantics).
- Prior low-overlap tangential docs in `docs/solutions/` (e.g. `workflow-issues/pre-commit-verification-workflow.md` with "lsp-diagnostics" tag — superseded to AGENTS.md; agent dev-server process docs in `developer-experience/` and `tooling-decisions/`) — No direct match on Grok `lsp` enablement or .grok/lsp.json Windows spawn. Create new (no update needed).
- `.grok/lsp.json` (this repo) — The live edited project-level configuration using the absolute command.
- Related session memory also references `rm-csharp`, `rm-structural-search` (ast-grep fallback when `lsp` unavailable or insufficient), and AGENTS.md updates for Grok vs. OpenCode split.

---

*Documentation produced via ce-compound Full mode, "this session only" (current transcript + narrow recent memory files 2026-06-20-interval-* used as source; no broad historical ce-sessions sweep). Low overlap per Related Docs Finder — new file created. Related refresh opportunities noted for AGENTS.md and the configureawait status guide (call /ce-compound-refresh with narrow scope if desired).*

*Karpathy Change Gate followed for the config edit. All `lsp` verifications used absolute forward-slash paths (relative paths rejected with "invalid file path").*
