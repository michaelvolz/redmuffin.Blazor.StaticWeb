---
title: feat: Add Architecture Quality Gate (arch subcommand)
type: feat
status: active
date: 2026-05-10
origin: docs/brainstorms/2026-05-10-arch-quality-gate-requirements.md
---

# feat: Add Architecture Quality Gate (arch subcommand)

## Summary

Add an `arch` subcommand that replicates Uncle Bob's `dependency-checker`. Reads a YAML config defining component-to-allowed-dependency maps, parses `.csproj` `<ProjectReference>` elements from the source solution, validates every reference against the allow-list, detects cycles, and exits 2 on any violation. Implemented via 9 TDD vertical slices following the established `CrapCommand`/`CrapHandler` pattern.

---

## Problem Frame

CRAP and SCRAP gate code quality within files. Neither catches architectural decay across the project graph — a `Core` project referencing `Web` breaks layering, a cycle between `Api` and `Infrastructure` makes deployment ordering fragile. Uncle Bob's `dependency-checker` catches these at CI time: config-defined component boundaries with explicit allow-lists, cycle detection, and configurable fatal/non-fatal violation behavior.

---

## Requirements

- R1. `arch` subcommand accepts `--project <path>` (required) and `--config <path>` (required). `all` subcommand accepts `--arch-config <path>` (required when arch gate is present) to pass through to arch
- R2. Scans `.csproj` files recursively, extracts `<ProjectReference>` elements
- R3. YAML config: `allowed-dependencies`, `component-map`, `ignored-components`, `fail-on-cycles`, `fail-on-violations`
- R4. `component-map` maps C# project names to architectural component groups
- R5. Unmapped projects assigned to implicit `Default` component with no allowed dependencies
- R6. Violation detection: dependency's target component not in source component's allow-list
- R7. Cycle detection: SCCs > 1 in component dependency graph
- R8. Exit codes: 0 clean, 2 violations/cycles, 1 error; `fail-on-*: false` flags downgrade
- R9. `--json` flag: structured JSON output
- R10. `AllCommand` integration after scrap with worst-exit-code logic. `AllCommand` accepts `--arch-config <path>` flag to pass through to the arch gate
- R11. Follows `rm-tdd`: 9 vertical slices, each red-green-refactor. Handler tested directly (`public static`), not Command
- R12. All tests use synthetic YAML/XML — no real project files in test fixtures

**Origin actors:** A1 (Developer), A2 (Quality Gates Pipeline Agent)
**Origin flows:** F1 (Run arch gate)
**Origin acceptance examples:** AE1 (allowed dep → 0), AE2 (disallowed dep → 2), AE3 (cycle → 2), AE4 (unmapped project → 2), AE5 (fail-on-cycles: false → 0)

---

## Scope Boundaries

- GUI / interactive viewer (arch-view's interactive mode) — out of scope
- NuGet package references — out of scope. `.csproj` `<ProjectReference>` only
- Transitive dependency depth analysis — out of scope. Direct references only
- Auto-fixing or suggested refactoring — out of scope. Reports only
- `--in-edn`, `--no-gui`, SVG/EDN export (arch-view features) — out of scope

---

## Context & Research

### Relevant Code and Patterns

- `tools/src/redmuffin.Tools.QualityGates/Commands/CrapCommand.cs` — CLI wiring pattern (thin wrapper → Handler)
- `tools/src/redmuffin.Tools.QualityGates/Commands/CrapHandler.cs` — `public static` exit-code handler, tested directly
- `tools/src/redmuffin.Tools.QualityGates/Program.cs` — System.CommandLine `RootCommand` registration
- `tools/src/redmuffin.Tools.QualityGates/Commands/AllCommand.cs` — worst-exit-code composition
- `tools/tests/redmuffin.Tools.QualityGates.Tests/Commands/CrapCommandTests.cs` — TUnit test pattern
- YAML config replicates `dependency-checker.edn` structure from AIR-J

### Institutional Learnings

- `docs/solutions/developer-experience/quality-gates-tool-operational-gotchas-2026-05-09.md`:
  - Handlers must be `public static` for testability; no `InternalsVisibleTo`
  - `dotnet run --project tests/redmuffin.Tools.QualityGates.Tests` for test execution
  - Run commands from `tools/` directory for SDK 10.0
  - Command/handler separation pattern

---

## Key Technical Decisions

- **YAML config (not EDN)**: No standard .NET EDN parser. YamlDotNet is the .NET YAML standard and preserves the map/list structure of the original EDN
- **Explicit `component-map`**: Bridges C# project naming (PascalCase with dots) to architectural component names (short, lowercase, layer-derived). Required because C# projects are named by namespace convention, not architecture layer
- **Implicit Default component with no permissions**: Unmapped projects trigger violations. Forces complete config — matches Uncle Bob's principle that ungoverned dependencies cause architectural decay
- **Handler-only testing**: Tests call `ArchHandler` methods directly with synthetic YAML and XML strings. No real `.csproj` files in test fixtures — avoids filesystem coupling and makes tests fast and deterministic
- **YamlDotNet NuGet package**: Already established pattern from CRAP's use of Cobertura XML parsing via `XmlReader`

---

## Open Questions

### Deferred to Implementation

- Exact YAML schema class names (`ArchConfig`, `ComponentMap`, `AllowedDependencies`) — finalize during implementation
- Whether `ignored-components` skips validation for dependencies FROM ignored components, TO ignored components, or both — clarify from dependency-checker source during implementation
- Exact JSON output schema fields — finalize during U7 (output formatting) when the data model is concrete

---

## Implementation Units

- U1. **Config parsing — `ArchConfig` and YAML deserialization**

**Goal:** Parse the YAML config file into structured C# objects usable by the Handler

**Requirements:** R3

**Dependencies:** None

**Files:**

- Create: `tools/src/redmuffin.Tools.QualityGates/Models/ArchConfig.cs`
- Create: `tools/src/redmuffin.Tools.QualityGates/Models/ArchOptions.cs` (CLI options record)
- Test: `tools/tests/redmuffin.Tools.QualityGates.Tests/Commands/ArchCommandTests.cs` (or dedicated `Models/ArchConfigTests.cs`)

**Approach:**

- `ArchConfig` record with `Dictionary<string, List<string>> AllowedDependencies`, `Dictionary<string, string> ComponentMap`, `List<string> IgnoredComponents` (default empty), `bool FailOnCycles` (default true), `bool FailOnViolations` (default true)
- Static `Parse(string yaml)` method using `YamlDotNet.Serialization`
- `ArchOptions` record with `string Project { get; }` and `string Config { get; }` for System.CommandLine binding

**Patterns to follow:** `ScrapOptions.cs` (CLI options record pattern), `CrapHandler` (static method entry point)

**Test scenarios:**

- Happy path: Valid YAML with all fields → correct `ArchConfig` with defaults populated
- Happy path: Minimal YAML (only `allowed-dependencies`) → defaults for `IgnoredComponents` (empty), `FailOnCycles` (true), `FailOnViolations` (true)
- Happy path: YAML with `component-map` → correct mapping deserialized
- Edge case: Empty config → error
- Edge case: Missing `allowed-dependencies` → error
- Error path: Invalid YAML syntax → parse exception surfaced
- Error path: YAML with wrong types (string where list expected) → deserialization error

**Verification:** `ArchConfig.Parse(yamlString)` returns correct object; invalid YAML throws with message referencing the config file. Build succeeds.

---

- U2. **Project reference extraction — `ProjectGraph`**

**Goal:** Scan a directory for `.csproj` files and extract `<ProjectReference>` dependencies

**Requirements:** R2

**Dependencies:** None (can be developed in parallel with U1)

**Files:**

- Create: `tools/src/redmuffin.Tools.QualityGates/Models/ProjectGraph.cs`
- Test: `tools/tests/redmuffin.Tools.QualityGates.Tests/Commands/ArchCommandTests.cs`

**Approach:**

- `ProjectGraph` record with `Dictionary<string, List<string>> Dependencies` (project-name → list of referenced project names)
- Static `From(string projectPath)` method:
  1. Find all `.csproj` files recursively, excluding `bin/`, `obj/`
  2. For each, parse XML with `XDocument`, extract `ProjectReference/@Include` attributes
  3. Extract project name from Include path (strip directory, remove `.csproj` extension)
  4. Build dependency dictionary
- Tests use in-memory temp directories with synthetic `.csproj` XML — no real project files

**Patterns to follow:** `ScrapHandler` (directory scanning + Roslyn parsing pattern), `CrapHandler` (static method, no constructor)

**Test scenarios:**

- Happy path: Directory with one `.csproj` referencing one other → correct dependency pair
- Happy path: Directory with multiple `.csproj` files, cross-references → correct graph
- Happy path: Solution-style references like `..\Core\Core.csproj` → correctly resolves to `Core`
- Edge case: Empty directory (no `.csproj` files) → empty graph, not an error
- Edge case: Project with no `<ProjectReference>` elements → empty dependency list for that project
- Edge case: Excludes `bin/` and `obj/` directories (create temp dirs with `.csproj` in bin → excluded)
- Edge case: `.Designer.cs` and `.g.cs` files ignored (`.csproj` only)

**Verification:** `ProjectGraph.From(tempDir)` returns correct graph; builds and runs in test sandbox.

---

- U3. **Component mapping — `ComponentGraph`**

**Goal:** Map project references to component-level edges using the config's `component-map`

**Requirements:** R4, R5

**Dependencies:** U1 (config), U2 (project graph)

**Files:**

- Create: `tools/src/redmuffin.Tools.QualityGates/Models/ArchViolation.cs`
- Create: `tools/src/redmuffin.Tools.QualityGates/Commands/ArchHandler.cs` (main handler, first created here with FindViolations)
- Test: `tools/tests/redmuffin.Tools.QualityGates.Tests/Commands/ArchCommandTests.cs`

**Approach:**

- `ArchViolation` record: `string SourceProject`, `string TargetProject`, `string SourceComponent`, `string TargetComponent`, `string Reason`
- `ArchHandler.FindViolations(ComponentGraph graph, ArchConfig config)` → `List<ArchViolation>`
  1. For each component → target-component edge in graph
  2. Skip if source and target are the same component (internal references within a component are always allowed — no need to list yourself in your own allow-list)
  3. Look up target-component in source-component's `AllowedDependencies` list
  4. If not found → violation with reason like "Core is not allowed to depend on Web"
  5. Unmapped projects → violation: "Project X is not assigned to any component"
  6. Projects in `ignored-components` → skip entirely

**Test scenarios:**

- Happy path: Allowed dependency (Core in Api's allowed list) → no violation
- Happy path: Disallowed dependency (Api depends on Web, not in list) → violation with correct reason
- Error path: Unmapped source project → violation: "unassigned to any component"
- Error path: Unmapped target project → violation: "depends on unassigned project"
- Edge case: Same-component reference (Core project A → Core project B) → no violation, always allowed
- Edge case: Ignored component's dependencies → skipped, no violations from or to it

**Verification:** `ArchHandler.FindViolations(...)` returns expected violation list for known good/bad graphs.

---

- U5. **Cycle detection**

**Goal:** Find all cycles (SCCs > 1) in the component dependency graph

**Requirements:** R7

**Dependencies:** U3 (component graph)

**Files:**

- Create: `tools/src/redmuffin.Tools.QualityGates/Models/ArchCycle.cs`
- Modify: `tools/src/redmuffin.Tools.QualityGates/Commands/ArchHandler.cs`
- Test: `tools/tests/redmuffin.Tools.QualityGates.Tests/Commands/ArchCommandTests.cs`

**Approach:**

- `ArchCycle` record: `List<string> Components` (the cycle chain), `int Length`
- `ArchHandler.FindCycles(ComponentGraph graph)` → `List<ArchCycle>`
- DFS-based cycle detection:
  1. Build adjacency list from component graph
  2. Standard DFS with `visited` and `recursionStack` sets
  3. When back-edge found (node in recursionStack), extract cycle path
  4. Return deduplicated cycles (normalize: rotate so smallest component name is first, canonicalize direction)
- Ignores self-loops (single-component cycles cannot exist in a project-reference graph — a project can't reference itself via `<ProjectReference>` to its own `.csproj`)

**Test scenarios:**

- Happy path: Acyclic graph (A→B, B→C) → empty cycle list
- Happy path: Simple cycle (A→B→A) → one cycle [A, B]
- Happy path: Three-component cycle (A→B→C→A) → one cycle [A, B, C]
- Edge case: Diamond with no cycles (A→B, A→C, B→D, C→D) → empty
- Edge case: Two independent cycles in same graph → both reported
- Edge case: Self-loop (A→A) → not reported as cycle (impossible via project references)
- Edge case: Cycle involving ignored component → ignored component's edges excluded, cycle may or may not form

**Verification:** `ArchHandler.FindCycles(...)` returns correct cycles for known graphs.

---

- U6. **Exit code logic and Handler orchestration**

**Goal:** Wire violations and cycles into the exit code decision with config flag respect

**Requirements:** R8

**Dependencies:** U4 (violations), U5 (cycles)

**Files:**

- Create: `tools/src/redmuffin.Tools.QualityGates/Models/ArchResult.cs`
- Modify: `tools/src/redmuffin.Tools.QualityGates/Commands/ArchHandler.cs` (add Run method to existing handler from U4)
- Test: `tools/tests/redmuffin.Tools.QualityGates.Tests/Commands/ArchCommandTests.cs`

**Approach:**

- `ArchResult` record: `int ExitCode`, `List<ArchViolation> Violations`, `List<ArchCycle> Cycles`, `int ProjectsScanned`, `int ComponentsDefined`
- `ArchHandler.Run(string projectPath, string configPath)` → `ArchResult`:
  1. Read config file → parse YAML
  2. Build `ProjectGraph` from project path
  3. Build `ComponentGraph` from project graph + config
  4. Find violations and cycles
  5. Determine exit code:
     - Error during parsing/IO → 1
     - `FailOnViolations && violations.Count > 0` → 2
     - `FailOnCycles && cycles.Count > 0` → 2
     - Otherwise → 0
  6. Build and return `ArchResult` with exit code, violations, cycles, and summary counts
- Console output NOT in Handler — Handler returns result struct; Command (U8) calls formatter

**Test scenarios:**

- Happy path: Clean graph → 0
- Happy path: Violations with `fail-on-violations: true` → 2
- Happy path: Violations with `fail-on-violations: false` → 0 (violations still reported)
- Happy path: Cycles with `fail-on-cycles: false` and no violations → 0
- Happy path: Both violations and cycles with both flags true → 2
- Error path: Config file not found → 1
- Error path: Invalid YAML → 1
- Error path: Project path not found → 1

**Verification:** `ArchHandler.Run(...)` returns correct exit code for fixture configs and graphs.

---

- U7. **Output formatting — text and JSON**

**Goal:** Produce default text output and `--json` structured output

**Requirements:** R9

**Dependencies:** U6 (result struct)

**Files:**

- Create: `tools/src/redmuffin.Tools.QualityGates/Commands/ArchOutputFormatter.cs`
- Test: `tools/tests/redmuffin.Tools.QualityGates.Tests/Commands/ArchCommandTests.cs`

**Approach:**

- `ArchOutputFormatter.Format(ArchResult result, bool json)` → `string`
- Default text output:

  ```
  Architecture Gate Results
  -------------------------
  5 projects scanned, 3 components defined
  2 violations found, 1 cycle found

  Violations:
    redmuffin.Blazor.StaticWeb.Core → redmuffin.Blazor.StaticWeb.Web
      Reason: Core is not allowed to depend on Web

  Cycles:
    Web → Api → Core → Web
  ```

- `--json` output: JSON array with `violations` and `cycles` arrays, each with typed fields
- Summary line always present even when zero violations/cycles

**Patterns to follow:** `CrapHandler` output format (summary line, followed by details), `ScrapOptions` (`--json` flag pattern)

**Test scenarios:**

- Happy path: Default text output for result with violations → correct summary line and violation details
- Happy path: Default text output for clean result → summary shows zero violations/cycles, no detail sections
- Happy path: `--json` produces valid JSON with correct structure
- Happy path: `--json` for clean result → valid JSON with empty arrays

**Verification:** Formatter output matches expected text/JSON strings.

---

- U8. **Command wiring — `ArchCommand` and `Program.cs`**

**Goal:** Create the CLI command, bind options, wire to Handler

**Requirements:** R1

**Dependencies:** U6 (Handler), U7 (formatter)

**Files:**

- Create: `tools/src/redmuffin.Tools.QualityGates/Commands/ArchCommand.cs`
- Modify: `tools/src/redmuffin.Tools.QualityGates/Program.cs`
- Test: `tools/tests/redmuffin.Tools.QualityGates.Tests/Commands/ArchCommandTests.cs`

**Approach:**

- `ArchCommand` class with `Configure(Command parent)` static method (matches `CrapCommand` pattern):
  ```csharp
  var archCommand = new Command("arch", "Check project dependency architecture against component rules");
  var projectOpt = new Option<string>("--project", "Path to solution or project root") { IsRequired = true };
  var configOpt = new Option<string>("--config", "Path to YAML architecture config") { IsRequired = true };
  var jsonOpt = new Option<bool>("--json", "Output as JSON");
  archCommand.AddOption(projectOpt);
  archCommand.AddOption(configOpt);
  archCommand.AddOption(jsonOpt);
  archCommand.SetHandler(async (project, config, json) => { ... }, projectOpt, configOpt, jsonOpt);
  ```
- Handler call: `ArchHandler.Run(project, config)` → exit code; if non-zero, format and write output
- Register `archCommand` in `Program.cs` root command, ordered after scrap
- `--json` flag routed to formatter

**Patterns to follow:** `CrapCommand.cs` (exact structure: static `Configure`, `Command` with `Option<T>`, `SetHandler` async lambda)

**Test scenarios:**

- Happy path: `--project` and `--config` bind correctly
- Edge case: Missing `--project` → CLI error from System.CommandLine
- Edge case: Missing `--config` → CLI error from System.CommandLine

**Verification:** `dotnet run --project tools/src/redmuffin.Tools.QualityGates -- arch --help` shows correct usage. Smoke test with real fixture returns expected exit code.

---

- U9. **AllCommand integration**

**Goal:** Wire `arch` into the `all` subcommand pipeline after scrap

**Requirements:** R10

**Dependencies:** U8 (command wired)

**Files:**

- Modify: `tools/src/redmuffin.Tools.QualityGates/Commands/AllCommand.cs`
- Test: `tools/tests/redmuffin.Tools.QualityGates.Tests/Commands/AllCommandTests.cs`

**Approach:**

- Add `--arch-config` option to `AllCommand` (required string, passed through to arch gate's Handler)
- Add `arch` after scrap in `AllCommand` execution order: crap → scrap → arch
- Follow existing worst-exit-code pattern: track max exit code across all gates
- Each gate runs even if prior gates fail (no short-circuit)
- Arch gate called via `ArchHandler.Run(project, archConfig)` using the `--arch-config` flag value
- Mock Handler method for all-gate composition test (existing pattern from SCRAP integration)
- Gate runs: `crap_result`, `scrap_result`, `arch_result` — all three executed, worst code returned

**Test scenarios:**

- Happy path: ALL clean (crap=0, scrap=0, arch=0) → 0
- Happy path: arch fails alone (crap=0, scrap=0, arch=2) → 2
- Happy path: scrap fails, arch clean (crap=0, scrap=2, arch=0) → 2
- Happy path: multiple fail (crap=2, scrap=0, arch=2) → 2
- Edge case: arch errors (crap=0, scrap=0, arch=1) → 1
- Edge case: Covers AE4. crap=0, scrap=2, arch=0 → 2

**Verification:** AllCommand tests pass; `dotnet run -- all --help` shows arch in the description.

---

## System-Wide Impact

- **Interaction graph:** New `arch` subcommand joins `crap`, `scrap`, and `all` in `Program.cs` root command. No effect on existing commands
- **API surface parity:** `ArchCommand.Configure(Command parent)` matches `CrapCommand.Configure(Command parent)` and `ScrapCommand.Configure(Command parent)` — same static void pattern
- **Unchanged invariants:** Existing CRAP and SCRAP behavior, exit codes, test patterns untouched. `AllCommand` handles 3 gates instead of 2
- **Error propagation:** `arch` exits 1 on config/project errors (bad path, invalid YAML), exits 2 on architecture violations, exits 0 on clean. Matches existing gate conventions

---

## Risks & Dependencies

| Risk                                                                 | Mitigation                                                                                                            |
| -------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------- |
| YamlDotNet not already in project dependencies                       | Add via `rm-nuget-manager` skill; YamlDotNet is a mature, widely-used package                                         |
| `component-map` requires manual maintenance for each new project     | Document in `tools/README.md`; unmapped projects fail the gate → forces config updates as part of adding projects     |
| Current project structure may have existing architectural violations | First run will report violations; config can be adjusted, or violations can be addressed as separate refactoring work |

---

## Sources & References

- **Origin document:** [docs/brainstorms/2026-05-10-arch-quality-gate-requirements.md](../brainstorms/2026-05-10-arch-quality-gate-requirements.md)
- Uncle Bob's dependency-checker: [AIR-J/dependency-checker.edn](https://github.com/unclebob/AIR-J/blob/master/dependency-checker.edn)
- Uncle Bob's arch-view: [github.com/unclebob/arch-view](https://github.com/unclebob/arch-view)
- AIR-J AGENTS.md: [github.com/unclebob/AIR-J/blob/master/AGENTS.md](https://github.com/unclebob/AIR-J/blob/master/AGENTS.md)
- Operational gotchas: [docs/solutions/developer-experience/quality-gates-tool-operational-gotchas-2026-05-09.md](../solutions/developer-experience/quality-gates-tool-operational-gotchas-2026-05-09.md)
