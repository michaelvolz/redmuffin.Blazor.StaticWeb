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

## Dual TFM stack

Intentional multi-target layout for this solution: the Azure Static Web Apps Functions API remains on **net9.0** until the host supports .NET 10 Functions, while the Blazor WASM app, launcher, and most tools target **net10.0**. Shared libraries used by the API stay on net9. Package *versions* (for example Microsoft.Extensions on the 10.x line) can still apply to net9 projects when packages multi-target; dual TFM alone is not a reason to leave sibling packages in the same family on mismatched patches.

## Shared Microsoft version property

`MicrosoftExtensionsVersion` in `Directory.Packages.props` — the MSBuild property that pins the Blazor/Components (and related Extensions) package family to one version under Central Package Management so Dependabot or manual bumps move the whole family together—including compile-time packages such as Components.Analyzers that participate in the restore graph.

## Central transitive pinning

CPM setting that applies central package versions to **transitive** dependencies as well as direct references. Without it, a safer direct pin (for example AngleSharp) does not override a vulnerable version pulled by a test framework such as bunit.

## Agent git boundary

Rule for this team’s agents: durable git writes (branch, stage, commit, push, PR) are never part of an automated skill finish unless the human separately and explicitly requests them. Skills that stock-default into branch+PR (for example ce-compound-refresh Phase 5) are overridden via vendor overlays so trunk-based workflow stays human-owned.

## Quality Gates toolchain

The local metrics suite that enforces agentic coding quality on this repo: CRAP, SCRAP, Architecture, Depth, Mutation, Duplicates, plus the Slopwatch pre-gate. Most gates are ports of Uncle Bob Martin’s public tools; Depth and Slopwatch are local. Ports are expected to stay algorithm-faithful to upstream rather than inventing repo-local thresholds.

## CRAP

Change-risk score for production methods: cyclomatic complexity combined with test coverage so complex under-tested code ranks highest. Used as a hard gate after significant changes; lower is better.

## SCRAP

Structural quality analyzer for **test** code (not production). Scores smells, fuzzy test-body duplication, and extraction pressure, then recommends STABLE, LOCAL, or SPLIT remediation and an AI actionability class. Complements CRAP the way tests complement production risk.

## STABLE / LOCAL / SPLIT

SCRAP remediation modes. STABLE means leave the file alone; LOCAL means clean individual examples or scaffolding in place; SPLIT means reorganize the test file by responsibility before local cleanup.

## Duplicates gate (dry)

Production-code structural duplicate detection using normalized AST fingerprints and Jaccard similarity. Distinct from SCRAP, which analyzes test structure. Ports the dry4* family (dry4clj / dry4java / dry4go).

## Differential mutation

Mutation-testing mode that reuses an embedded per-file manifest so later runs only re-test sites in changed scopes instead of re-mutating the whole file. Default workflow once a file has been mutated cleanly once.

## Module-size discipline

Rule of thumb that a source file with too many mutation sites should be split before further hardening. The mutation CLI exposes `--mutation-warning` (default 50) for this boundary so agents keep modules small enough for focused differential runs; treat the flag as the contract even when a given build only wires the option without emitting a hard gate.

## Acceptance mutation

Upstream (Acceptance-Pipeline-Specification) concept, not yet a local gate: mutating **acceptance example values** (for example Gherkin table data via an IR), not production source operators. Checks that acceptance tests actually depend on the specified data. Separate from unit-level mutation testing of production code.

## Architecture zones (main sequence)

Upstream dependency-checker concept, not yet implemented in the local architecture gate: classification of components into healthy, pain, or useless zones using abstractness and instability relative to the main sequence. Would extend plain cycle and allowed-edge checks with design-health metrics.

## Depth gate

Local structural quality gate (not an Uncle Bob port) that flags over-decomposition: shallow methods, parameter bloat, wrong abstractions, and entanglement. Peer to CRAP so “extract to fix CRAP” cannot create pointless thin methods without pushback.

## Slopwatch

Local LLM anti-cheat pre-gate that finds reward-hacking patterns agents introduce (disabled tests, suppressions, empty catches, arbitrary delays, project-file slop). Runs before the metric suite; not part of the unclebob/* toolchain.

## RiverBooks-shaped module

Bounded feature package using Ardalis RiverBooks **structure** only (not full DDD): Contracts project, module implementation project, and module tests. Host owns UI and composition. See the modular layout ADR and the module addition guide.

## Module Contracts

Public project for a module’s cross-boundary types: Mediator queries/responses and service interfaces. Implementation types stay in the module project (services internal). Consumers reference Contracts, not internals.

## Result (Result of T)

Shared success/failure value in Common for expected module outcomes. Factories and `Match` map success and failure as data; cancellation and programmer bugs remain exceptions. Used at service and Mediator response boundaries so pages do not use exception-driven control flow for normal API errors.

## Synthetic (module strategy)

Application implementation that returns artificially generated data instead of a live backend. Registered via the module DI extension when host policy selects synthetic (pure client host only — not every localhost). Distinct from test doubles, which live only in test projects. See also CONTEXT.md **Synthetic**.

## Azure Functions deployment boundary

The isolated-worker Api app is a **separate deployment unit** from the WASM host and RiverBooks modules. Modularization extracts **client** code from the host only; it never relocates Functions triggers, workers, or Api-only types into Modules (or the reverse). Cross-boundary contact is HTTP (and deliberately dual-consumed types in Common), not shared project source.

## Host-time Strategy registration

Composition-root choice of real vs synthetic module implementation via a host-computed boolean passed into `Add{Module}Module(...)`. Replaces NavigationManager-based factories that resolved concrete services at first use.

*(Seeded from the 2026-06-20 Grok Build CLI Roslyn LSP Windows spawn + restart learning in tooling-decisions/ + prior session memory on agent harness enablement. Package-management terms accreted from the 2026-07-22 NU1605/NU1902 CPM restore learning. Quality-gates terms accreted from the 2026-08-02 Uncle Bob upstream-sync learning. Modular monolith terms accreted 2026-08-03; Raindrop Phase 1 / Api boundary terms accreted 2026-08-03.)*
