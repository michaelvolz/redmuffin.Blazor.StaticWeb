---
date: 2026-06-08
last_updated: 2026-06-08T22:00:00
tags:
  - configureawait
  - ca2007
  - fixer
  - msbuildworkspace
  - roslyn
  - formatter-pipeline
  - opencode
  - analyzer
---

# ConfigureAwait Fixer — Current Status & Proven Facts

## What Belongs in This File

- **Viewpoint**: Developer/agent maintaining or extending the
  ConfigureAwait fixer automation. Assumes familiarity with the fixer
  architecture (MSBuildWorkspace, official CA2007 analyzer, formatter
  pipeline).
- **What belongs**: Proven facts verified via tool execution this
  session, confirmed dead ends with root causes, current deployment
  state, architecture decisions, open problems with status.
- **What does NOT belong**: Speculative solutions not yet tested,
  implementation instructions (those belong in the plan doc), CA2007
  policy rationale (belongs in `csharp-standards-final`), session
  logs or conversation transcripts.

---

## 1 — Proven Facts (Verified This Session)

### 1.1 The fixer works correctly when invoked manually

Tested 2026-06-08 against a deliberately created C# file with two
missing `.ConfigureAwait(false)` calls (`Task.Delay(100)` and
`Task.FromResult(42)`).

```powershell
dotnet "C:\Users\flynn\.local\bin\ConfigureAwaitFixer\ConfigureAwaitFixer.dll" `
  --file "path\to\ConfigureAwaitFixerTest.cs"
```

**Result**: Both awaits fixed. Timer: ~0.93s. File content verified
post-fix via `read` — both awaits had `.ConfigureAwait(false)` added.

```csharp
// Before:
await Task.Delay(100);
// After:
await Task.Delay(100).ConfigureAwait(false);
```

### 1.2 The fixer finds zero fixes when all awaits are already annotated

Tested against the main src project and the tools test project. Both
returned `Completed in X.XXXs` with zero fixes. Confirmed: the fixer
does not produce false positives on already-clean code.

### 1.3 CA2007 is enabled in the project compilation

The root `Directory.Build.props` sets:

```xml
<AnalysisMode>recommended</AnalysisMode>
<AnalysisModeReliability>All</AnalysisModeReliability>
```

MSBuildWorkspace correctly loads the project with these settings.
`compilation.WithAnalyzers()` produces CA2007 diagnostics on files
missing `.ConfigureAwait(false)`. The `.editorconfig` does NOT need
an explicit `dotnet_diagnostic.CA2007.severity = warning` entry —
the `AnalysisModeReliability=All` setting is sufficient.

### 1.4 The formatter pipeline is broken by tilde path expansion

The `opencode.jsonc` formatter section (line 526) contains:

```json
"configureawait-fixer": {
  "command": [
    "dotnet",
    "~/.local/bin/ConfigureAwaitFixer/ConfigureAwaitFixer.dll",
    "--file",
    "$FILE"
  ],
  "extensions": [".cs"]
}
```

**Root cause (confirmed 2026-06-08):** OpenCode spawns formatter commands
directly via `Bun.spawn` (no shell). The `~` tilde is NOT expanded —
it is passed literally to `dotnet`. Manual test confirmed:

```
$ dotnet "~/.local/bin/ConfigureAwaitFixer/ConfigureAwaitFixer.dll" --file ...
→ dotnet-~/.local/bin/... does not exist.
$ dotnet "C:\Users\flynn\.local\bin\ConfigureAwaitFixer\ConfigureAwaitFixer.dll" --file ...
→ Works, fixes awaits.
```

The `dotnet-format` and `prettier` formatters work because their commands
do not contain `~`. Only `configureawait-fixer` uses a tilde path.

**Correction to earlier assumption:** The OpenCode formatter docs state
formatters fire on **all** writes ("when OpenCode writes or edits a
file"). The formatter pipeline DOES fire on agent tool writes — it just
fails silently because `dotnet` receives a broken path.

The fixer DLL directory exists with all dependencies (verified).

### 1.5 The Roslyn C# Language Server (v5.9.0) is active in OpenCode

Installed at `C:\Users\flynn\.local\bin\windows\roslyn-language-server.cmd`.
The `opencode.jsonc` has `"lsp": true` (line 3). The LSP provides
diagnostics and code actions (including CA2007 quick fixes) to the
editor.

---

## 2 — Architecture Decisions

### 2.1 Official CA2007 analyzer, never heuristics

Decision: Load `Microsoft.CodeAnalysis.NetAnalyzers.dll` via
`Assembly.LoadFrom` and run against the MSBuildWorkspace compilation.
The official analyzer is the sole source of truth for which awaits
need `.ConfigureAwait(false)`.

**Rationale**: Heuristic detection (syntax-only, "all awaits except
Assert.\*") corrupted 765 TUnit assertion chains with CS1929 errors.
The official CA2007 analyzer correctly excludes TUnit assertion types
(`ThrowsAssertion<T>`, `Bool_IsTrue_Assertion`, etc.) because they
do not return `Task`/`ValueTask`.

File: `AnalyzerLoader.cs` — loads both `Microsoft.CodeAnalysis.NetAnalyzers.dll`
and `Microsoft.CodeAnalysis.CSharp.NetAnalyzers.dll`.

### 2.2 MSBuildWorkspace for full compilation context

Decision: Use `MSBuildWorkspace.OpenProjectAsync()` to load the
project's cached compilation. Never `CSharpSyntaxTree.ParseText`
(no type information, no NuGet references).

**Rationale**: `OpenProjectAsync` reuses the MSBuild evaluation
cache from the last `dotnet build`. No second compilation needed.
Full type resolution including NuGet packages and cross-file
references.

### 2.3 Fixer runs outside MSBuild — no `.targets` hook

Decision: The `.targets` hook was removed from the NuGet package
(`1.0.2`). The fixer must never run during an active `dotnet build`.

**Rationale**: MSBuildWorkspace spawns a child MSBuild process
(BuildHost) to evaluate projects. When called from a `.targets` hook
during `dotnet build`, both parent and child evaluate the same
project simultaneously → **process deadlock**. This is documented
MSBuildWorkspace behavior — it was designed for IDE tools
(between-build analysis), not in-build hooks.

### 2.4 Fixer deployed to `~/.local/bin/ConfigureAwaitFixer/`

The fixer DLL and all dependencies (analyzer DLLs, BuildHost, Roslyn
assemblies) are deployed to a fixed path in the user's home directory.
This is separate from the NuGet package cache. The formatter pipeline
references this path.

---

## 3 — Confirmed Dead Ends

| Approach                                              | Result                                         | Root Cause                                                                                         |
| ----------------------------------------------------- | ---------------------------------------------- | -------------------------------------------------------------------------------------------------- |
| `.targets` hook + MSBuildWorkspace                    | Deadlock — `dotnet build` hangs indefinitely   | Two MSBuild processes evaluate the same `.csproj` simultaneously                                   |
| `AfterTargets="CoreCompile"`                          | CA2007 error already visible in build output   | Fixer runs after compilation — developer sees error, must build again                              |
| `BeforeTargets="CoreCompile"` + syntax-only detection | Works but corrupted 765 TUnit assertion chains | Pure syntax can't distinguish `Task` from `ThrowsAssertion<T>`                                     |
| `dotnet format analyzers`                             | Reports CA2007, never applies CodeFixProviders | `dotnet format` only formats whitespace and detects diagnostics — confirmed by 6 independent tests |
| Roslynator CLI (`roslynator fix`)                     | Crashes on .NET 10 SDK                         | `System.Composition.AttributedModel` removed from shared framework (roslynator#1748)               |
| Publish-during-pack via `<Exec>`                      | `dotnet publish` times out on Roslyn deps      | Roslyn dependency graph too large for in-pack publish step                                         |
| `<Analyzer>` MSBuild item hack                        | Breaks on `dotnet clean`, not portable         | Debug build output path not stable across machines                                                 |
| Pure syntax await detection (v1.0.0)                  | 0 awaits fixed on real projects                | Only 3 BCL references — can't resolve user-defined method return types                             |
| Agent instruction "always add ConfigureAwait(false)"  | LLM forgets ~100% of the time                  | 7 sessions, zero correct first tries across all sessions (session history)                         |

---

## 4 — Current Deployment State

| Component                     | Version | Location                                                                   | Status                                  |
| ----------------------------- | ------- | -------------------------------------------------------------------------- | --------------------------------------- |
| Fixer DLL                     | 1.0.2   | `~/.local/bin/ConfigureAwaitFixer/ConfigureAwaitFixer.dll`                 | Deployed                                |
| Analyzer DLLs (NetAnalyzers)  | 10.0.0  | `~/.local/bin/ConfigureAwaitFixer/Microsoft.CodeAnalysis.NetAnalyzers.dll` | Deployed                                |
| MSBuildWorkspace BuildHost    | 5.3.0   | `~/.local/bin/ConfigureAwaitFixer/BuildHost-netcore/`                      | Deployed                                |
| Formatter pipeline entry      | —       | `~/.config/opencode/opencode.jsonc` line 526                               | Exists, fires on editor save only       |
| NuGet package                 | 1.0.2   | `tools/nupkgs/redmuffin.Tools.ConfigureAwaitFixer.1.0.2.nupkg`             | Packaged (no `.targets`)                |
| Source code                   | 1.0.2   | `tools/src/redmuffin.Tools.ConfigureAwaitFixer/`                           | Complete                                |
| `.targets` file               | —       | `tools/src/redmuffin.Tools.ConfigureAwaitFixer/build/`                     | Exists in repo but NOT in NuGet package |
| Roslyn Language Server        | 5.9.0   | `~/.local/bin/windows/roslyn-language-server.cmd`                          | Active in OpenCode                      |
| Plugin (configureawait-fixer) | 1.0.0   | `~/.config/opencode/plugins/configureawait-fixer.ts`                       | Deployed 2026-06-08                     |

---

## 5 — OpenCode Plugin Hook Research (2026-06-08)

Online research of the OpenCode plugin system (official docs +
community guides) identified the following hooks relevant to auto-fix
automation:

### Available hooks

| Hook                   | Fires when                                   | Usefulness |
| ---------------------- | -------------------------------------------- | ---------- |
| `file.edited`          | Any file is written or modified              | HIGH       |
| `tool.execute.after`   | After any tool completes (edit, write, bash) | HIGH       |
| `tool.execute.before`  | Before any tool executes                     | MEDIUM     |
| `file.watcher.updated` | File watcher detects a change                | LOW        |

### Plugin capabilities

- TypeScript/JavaScript modules in `~/.config/opencode/plugins/`
  or `.opencode/plugins/`
- Access to Bun's shell API (`$`) for running commands
- `$` shell tag handles tilde expansion (unlike raw `spawn`)
- Access to `directory`, `worktree` context
- Built-in support for `package.json` dependencies

### Formatter pipeline internals

The official formatter docs state: "When OpenCode writes or edits a
file and formatters are enabled, it runs the appropriate formatter
command on the file." This means formatters SHOULD fire on agent
tool writes — they are not restricted to editor saves. The failure
is specifically the tilde expansion in the `configureawait-fixer`
command path.

---

## 6 — Root Cause & Solution Analysis

### Root cause

The formatter `configureawait-fixer` entry uses `~/.local/bin/...`
but OpenCode spawns commands directly (no shell). The tilde is
passed literally to `dotnet`, which cannot resolve it. The formatter
fires but `dotnet` exits with an error. The error is silently
swallowed by the formatter pipeline.

### Solution ranking by certainty

| #   | Approach                                                     | Certainty | Rationale                                                                                                            |
| --- | ------------------------------------------------------------ | --------- | -------------------------------------------------------------------------------------------------------------------- |
| 1   | **OpenCode plugin with `file.edited` hook**                  | 90%       | Fire-on-write is documented. Bun `$` resolves `~`. Debouncing possible. Full error visibility. Cross-platform.       |
| 2   | **Fix formatter path** (remove `~`, use absolute path)       | 70%       | Works on one platform. Cross-platform needs env var or platform-specific config. No debouncing. No error visibility. |
| 3   | **Plugin with `tool.execute.after`** (filter `edit`/`write`) | 85%       | More targeted than `file.edited`. Same plugin benefits. Slightly more complex filtering.                             |
| 4   | **LSP code-action auto-apply**                               | 15%       | OpenCode has no "code actions on save" mechanism. Requires upstream feature. No existing infrastructure.             |

### Verdict: Plugin with `file.edited`

The `file.edited` event hook in an OpenCode plugin is the approach
with highest certainty. Reasons:

1. **100% programmatic** — TypeScript code, not agent instructions
2. **Cross-platform** — Bun `$` handles tilde; `path.join` works
   on Windows and Linux
3. **Debouncing** — can wait 300ms after last edit before running
   fixer, avoiding redundant runs during rapid edits
4. **Error visibility** — plugin can log to stderr or
   `client.app.log()` for debugging
5. **No formatter dependency** — doesn't rely on the formatter
   pipeline's internal behavior
6. **Independence** — if the fixer path or args need to change,
   only the plugin code changes, not OpenCode config

### Risk: fixer runs during `dotnet build`

The fixer opens MSBuildWorkspace which spawns BuildHost. If the file
is edited (triggering the plugin) while `dotnet build` is running,
two MSBuild processes could evaluate the same project → deadlock.

**Mitigation:** Check for active `dotnet build` processes before
running the fixer. If a build is active, skip the fixer run — the
build will catch any missing `ConfigureAwait(false)` via CA2007.

### Performance

The fixer takes ~0.9s per invocation. With debouncing (300ms),
rapid edits (common during agent work) trigger only one fixer run
per burst. First save after a burst pays ~0.9s; subsequent saves
exit instantly (0 CA2007 violations → 0 fixes).

---

## 7 — Plugin Implementation (2026-06-08)

### File

`~/.config/opencode/plugins/configureawait-fixer.ts`

### Architecture

The plugin uses the `file.edited` event hook — fires whenever OpenCode
or the editor writes a file. Flow:

```
file written (.cs) → file.edited fires
  → debounce 300ms (cancel any pending timer for this file)
  → check for active dotnet build (skip if MSBuild running)
  → run fixer via Bun shell: dotnet <fixerPath> --file <path>
  → log stderr output to console
```

### Key design decisions

| Decision                                      | Rationale                                                               |
| --------------------------------------------- | ----------------------------------------------------------------------- |
| `file.edited` over `tool.execute.after`       | Captures ALL writes (agent tools + editor saves), simpler filtering     |
| 300ms debounce per file                       | Agent writes multiple files rapidly — one fixer run per burst           |
| Build-active guard (`tasklist` / `pgrep`)     | Prevents MSBuildWorkspace deadlock if file edited during `dotnet build` |
| `os.homedir()` for fixer path                 | Cross-platform — no tilde expansion issues                              |
| `console.error` for logging                   | Visible in terminal; survives plugin crashes                            |
| Global plugin (`~/.config/opencode/plugins/`) | Project-agnostic — works for any .NET project                           |
| `.quiet()` on subprocess calls                | Suppresses Bun shell framing output — only stderr from fixer surfaces   |

### Transparent upgrade path

If the formatter pipeline adds tilde expansion or OpenCode adds LSP
code-action-on-save, the plugin can be deleted with zero code changes
elsewhere. The plugin is a bridge, not a permanent architectural
commitment.

---

## 8 — `dotnet watch` Integration (2026-06-08)

### Design decision

The `dotnet watch` loop should never be blocked by a mechanical
formatting issue that a tool fixes in sub-second time. A two-pronged
architecture (plugin + `.targets` syntax-only fixer) was considered
and **rejected** — it introduced a second detection mechanism
(syntax heuristic), false-positive risk, double-build overhead, and
maintenance burden.

Instead: **disable `TreatWarningsAsErrors` during watch.**

### Implementation

`Directory.Build.props` line 122 (root, main solution):

```xml
<TreatWarningsAsErrors Condition="'$(DotNetWatchBuild)' != 'true'">true</TreatWarningsAsErrors>
```

`Directory.Build.props` line 11 (tools solution):

```xml
<TreatWarningsAsErrors Condition="'$(DotNetWatchBuild)' != 'true'">true</TreatWarningsAsErrors>
```

The `DotNetWatchBuild=true` property is set by `dotnet watch` during
design-time builds. The inner compilation uses the `-- -p:` flag:

```
dotnet watch -- -p:TreatWarningsAsErrors=false
```

### Why this works

1. **Plugin is primary:** Agent writes are fixed by the plugin
   (official CA2007 analyzer, 100% accurate). Watch loop sees clean
   files in the common case.

2. **Watch tolerates misses:** If the plugin somehow misses an await,
   CA2007 is a warning — watch loop continues. Agent sees the warning
   and fixes it on the next edit.

3. **Pre-commit enforcement:** `dotnet build` (without watch) uses
   `TreatWarningsAsErrors=true`. Nothing unfixed reaches the repo.

### Rejected approach: `.targets` syntax-only fixer

A second fixer in a `.targets` hook (`BeforeTargets="CoreCompile"`)
using pure Roslyn syntax was rejected because:

- **False positive risk:** Without `SemanticModel`, cannot perfectly
  distinguish `Task.Delay()` from `Assert.That().IsTrue()`. A
  conservative heuristic catches ~95% but the remaining risk on
  TUnit assertions is unacceptable.
- **Double-build during watch:** The `.targets` modifies files on
  disk, triggering a second `dotnet watch` build cycle.
- **Two detection mechanisms:** The plugin already has the official
  analyzer. A second syntax-based one adds maintenance burden with
  marginal benefit.

---

## 9 — Related

- `docs/solutions/developer-experience/automated-configureawait-fixer.md` — full fixer journey log (6 dead ends, architecture, MSBuild integration)
- `docs/solutions/tooling-decisions/configureawait-auto-fix-research.md` — official-analyzer + MSBuildWorkspace architecture research
- `docs/solutions/tooling-decisions/configureawait-msbuild-hook-incompatibility.md` — `.targets` deadlock analysis and save-time architecture decision
- `docs/plans/2026-05-17-002-refactor-configureawait-fixer-official-analyzer-plan.md` — implementation plan (U1-U3, appears code-complete)
- `tools/CONTEXT.md` — domain language glossary (ConfigureAwait Fixer section)
- `tools/src/redmuffin.Tools.ConfigureAwaitFixer/` — current fixer source code
