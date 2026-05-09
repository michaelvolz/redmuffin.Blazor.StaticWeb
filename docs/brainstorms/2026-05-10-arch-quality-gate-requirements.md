---
date: 2026-05-10
topic: arch-quality-gate
---

# Architecture Quality Gate (arch)

## Summary

Add an `arch` subcommand that replicates Uncle Bob's `dependency-checker`. It
reads a YAML config defining component groups and their allowed dependencies,
parses `.csproj` `<ProjectReference>` elements, validates every project
reference against the allow-list, detects dependency cycles, and exits 2 on
any violation. Headless only; no GUI (arch-view's interactive mode is out of
scope for the quality gate).

## Uncle Bob Source

- Config format replicates `dependency-checker.edn` from AIR-J:
  `:allowed-dependencies`, `:ignored-components`, `:fail-on-cycles`,
  `:fail-on-violations`.
- Run with: `clj -M:check-dependencies`
- AIR-J AGENTS.md: "Treat dependency violations and cycles as design problems
  to fix, not warnings to ignore."

## Actors

- A1. **Developer**: Runs `arch` locally to validate project dependency
  structure.
- A2. **Quality Gates Pipeline Agent**: Runs `all` (crap → scrap → arch →
  mutate) with worst-exit-code logic.

## Key Flow

- **Trigger:** `dotnet quality-gates arch --project <path> --config <path>`
- **Steps:**
  1. Load config YAML from `--config` (component definitions, allowed
     dependencies, ignored components, cycle/violation fail flags)
  2. Parse all `.csproj` files in `--project`, extract
     `<ProjectReference>` elements → `(project-name → [dependencies])`
  3. Map each project to its component group (config-defined; unmapped
     projects are in an implicit default component)
  4. For each dependency, check: does the dependency's component appear in
     the source component's allowed-dependencies list? If not → violation.
  5. Run cycle detection on the component dependency graph (DFS or
     equivalent). If `--fail-on-cycles` (default true) and cycles found →
     violation.
  6. Report: violations grouped by component, cycle chains listed. Summary
     line.
  7. Exit 0 if clean, 2 if violations or cycles, 1 on error.

## Requirements

**CLI and execution**

- R1. `arch` accepts `--project <path>` (required, root of solution or
  project directory to scan) and `--config <path>` (required, YAML config
  file).
- R2. Scans all `.csproj` files recursively within `--project`, excluding
  `bin`/`obj` directories and test projects (unless explicitly included in
  config).

**Config format** (replicates dependency-checker.edn)

- R3. YAML structure:
  ```yaml
  allowed-dependencies:
    Core: []
    Api: [Core]
    Infrastructure: [Core]
  ignored-components: [Tests]
  fail-on-cycles: true
  fail-on-violations: true
  ```
- R4. `allowed-dependencies`: map of component-name → list of component-names
  it may depend on. Empty list means no dependencies allowed (leaf).
- R5. `ignored-components`: components to skip entirely (default: none).
- R6. `fail-on-cycles`: boolean, default `true`. When false, cycles are
  reported but do not exit 2.
- R7. `fail-on-violations`: boolean, default `true`. When false, violations
  are reported but do not exit 2.

**Project-to-component mapping**

- R8. Config defines an optional `component-map` section:
  ```yaml
  component-map:
    redmuffin.Blazor.StaticWeb: Web
    redmuffin.Blazor.StaticWeb.Api: Api
    redmuffin.Blazor.StaticWeb.Core: Core
  ```
  Projects not listed in the map belong to an implicit `Default` component
  which has no allowed dependencies (any dependency into or out of it is a
  violation). This forces explicit component assignment.

**Analysis**

- R9. Cycle detection: find all strongly connected components (SCCs) in the
  component dependency graph. Each SCC of size > 1 is a cycle.
- R10. Violation detection: for each project reference `A → B`, look up the
  component of A and the component of B. If B's component is not in A's
  component's `allowed-dependencies` list → violation.

**Output**

- R11. Default output: summary line (projects scanned, components defined,
  violations found, cycles found) followed by violation details (source
  project → target project, source component → disallowed target component)
  and cycle details (component chain).
- R12. `--json` flag: outputs all violation and cycle data as structured
  JSON.
- R13. Exit codes: 0 when no violations/cycles (or when both
  `fail-on-violations` and `fail-on-cycles` are false), 2 when violations or
  cycles found, 1 on error. Matches CRAP/SCRAP convention.

**AllCommand integration**

- R14. `AllCommand` composes arch after scrap: run crap → scrap → arch →
  return worst exit code.

**Implementation workflow (rm-tdd)**

- R15. Implementation follows `rm-tdd` skill strictly. Each requirement is
  implemented as one vertical slice:
  1. Write one failing test for the Handler
  2. Write minimal production code to pass
  3. Refactor both test and production code
  4. Run `dotnet run --project tests/redmuffin.Tools.QualityGates.Tests` to
     confirm green
  5. Next slice
- R16. Tests cover the Handler directly (`public static` methods), not the
  Command. Follows `CrapCommand`/`CrapHandler` pattern.
- R17. All new tests in
  `tools/tests/redmuffin.Tools.QualityGates.Tests/Commands/ArchCommandTests.cs`
  and matching analysis tests.
- R18. Test data uses embedded YAML config strings and synthetic `.csproj`
  XML — no real project files in test fixtures.

## Vertical Slices (TDD Order)

1. **Config parsing** — `ArchConfig.Parse(yaml)` returns structured config
   with allowed-dependencies, component-map, flags. Test: valid YAML →
   correct object. Test: missing required keys → error.
2. **Project reference extraction** — `ProjectGraph.From(path)` scans `.csproj`
   files, returns `(project-name → [referenced-project-names])`. Test: single
   project with one reference. Test: empty directory → empty graph.
3. **Component mapping** — `ComponentGraph.From(projectGraph, config)` maps
   projects to components, builds component-level dependency edges. Test:
   mapped projects. Test: unmapped project → Default component.
4. **Violation detection** — `ArchHandler.FindViolations(componentGraph, config)`
   returns list of disallowed dependencies. Test: allowed dep → no violation.
   Test: disallowed dep → violation reported. Test: self-dependency in same
   component.
5. **Cycle detection** — `ArchHandler.FindCycles(componentGraph)` returns
   list of SCCs > 1. Test: acyclic graph → empty. Test: A→B→C→A → one cycle.
6. **Exit code logic** — `ArchHandler.Run(...)` return 0/2/1 based on
   violations, cycles, and config flags. Test: clean graph → 0. Test:
   violations with fail-on-violations=false → 0. Test: cycle → 2.
7. **Output formatting** — default text output and `--json`. Test: summary
   line format. Test: JSON schema.
8. **Command wiring** — `ArchCommand` thin CLI wrapper. Test: correct
   option binding.
9. **AllCommand integration** — wire arch into `all` after scrap. Test:
   worst-exit-code logic with arch in pipeline.

## Acceptance Examples

- AE1. Config allows `Api → Core`. Solution has `Api.csproj` referencing
  `Core.csproj`. `arch` exits 0.
- AE2. Config allows `Api → Core`. Solution has `Core.csproj` referencing
  `Api.csproj`. `arch` exits 2 with violation: "Core → Api (Core is not
  allowed to depend on Api)".
- AE3. Config has `Web → Api` and `Api → Core`. Solution has `Core`
  referencing `Web`. `arch` exits 2 with cycle: "Core → Web → Api → Core".
- AE4. Unmapped project `SomeLib.csproj` → exits 2 with violation (Default
  component has no allowed dependencies).
- AE5. Config has `fail-on-cycles: false`. Cycle exists but no other
  violations. `arch` exits 0 (cycles reported but not fatal).

## Scope Boundaries

- GUI / interactive viewer (arch-view's primary mode) — out of scope. Quality
  gate is headless only.
- Package references (NuGet) — out of scope. Only `<ProjectReference>`
  dependencies within the solution.
- Transitive dependency depth analysis — out of scope. Direct references
  only. Cycles naturally emerge from the graph structure.
- Auto-fixing or suggesting fixes — out of scope. Reports only.

## Key Decisions

- **YAML config** (not EDN): EDN has no standard .NET parser. YAML (via
  YamlDotNet) is idiomatic for .NET configuration and preserves the map/list
  structure of the original EDN.
- **Project-to-component mapping is explicit**: Unlike AIR-J where components
  are named through module conventions, our C# projects need explicit
  grouping. The `component-map` section in config bridges this gap.
- **Implicit Default component with no permissions**: Any project not
  explicitly mapped is caught as a violation. This forces the config to be
  complete — mirroring Uncle Bob's principle that unplanned dependencies are
  the ones that cause architectural decay.
- **CrapCommand pattern**: Thin CLI wrapper → `public static` Handler. No
  `InternalsVisibleTo`. Tests cover Handler directly.
- **rm-tdd workflow**: 9 vertical slices, each red-green-refactor. Tests
  use synthetic YAML/XML — no real project files.

## Config Example (for this repo)

```yaml
allowed-dependencies:
  Web: [Core]
  Api: [Core]
  Core: []
  Tools: []

component-map:
  redmuffin.Blazor.StaticWeb: Web
  redmuffin.Blazor.StaticWeb.Api: Api
  redmuffin.Blazor.StaticWeb.Core: Core
  redmuffin.Tools.QualityGates: Tools

ignored-components: [Tests]

fail-on-cycles: true
fail-on-violations: true
```

## Dependencies / Assumptions

- Requires YamlDotNet NuGet package for config parsing.
- Assumes `tools/global.json` pins SDK 10.0; commands run from `tools/`
  directory.
- Assumes `dotnet run --project tests/redmuffin.Tools.QualityGates.Tests`
  remains the test execution command.
- No dependency on CRAP or SCRAP — `arch` is self-contained (only shares CLI
  framework through `AllCommand`).
