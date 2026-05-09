---
title: "feat: Add CRAP Analyzer (First Quality Gate)"
type: feat
status: active
date: 2026-05-09
---

# feat: Add CRAP Analyzer (First Quality Gate)

## Summary

Build the first gate in the `quality-gates` dotnet tool: a CRAP analyzer that
computes `CC² × (1 − coverage)³ + CC` per method using Roslyn for cyclomatic
complexity and Cobertura XML for line coverage. Test-first with TUnit,
deployed as a local NuGet feed dotnet tool in a separate `tools/` solution.

---

## Problem Frame

The `rm-uncle-bob-martin-agentic-coding` skill mandates that every method
pass a CRAP ≤ 8 gate before work is considered done. No tool exists in the
repo to compute this metric. The first increment builds only the CRAP gate;
all other gates (SCRAP, duplication, architecture) are deferred.

---

## Requirements

### Computation

- R1. Compute CRAP score per method given a source project and a Cobertura XML coverage file

### CLI Behavior

- R2. Flag all methods with CRAP > 8, exit code 2 on breach
- R3. Support `--changed` flag for incremental analysis (only files modified since HEAD)

### Packaging & Installation

- R4. Install as a `dotnet tool` via local NuGet feed, callable as `dotnet quality-gates crap`

### Development Process

- R5. All production code written test-first using TUnit

---

## Scope Boundaries

- Build only the `crap` subcommand; `scrap`, `dupes`, `arch`, and `all` are deferred
- Mutation testing (Stryker.NET) is deferred
- CI hookup is deferred

### Deferred to Follow-Up Work

- SCRAP gate: test structural analyzer — separate plan
- Duplication gate: fuzzy structural duplicate scanner — separate plan
- Architecture gate: dependency graph and layer enforcement — separate plan
- `all` subcommand: runs all gates in sequence — separate plan after all gates exist
- CI integration: GitHub Actions quality gate stage — separate plan

---

## Context & Research

### Relevant Code and Patterns

- Existing dotnet tool pattern: `.config/dotnet-tools.json` with `libman` entry
- Test project pattern: `tests/redmuffin.Blazor.StaticWeb.Tests/` — TUnit, Exe output, Central Package Management
- Console app pattern: `src/SwaLauncher/` — minimal Exe project with no package references
- Coverage generation: `scripts/Generate-CoverageReport.ps1` produces Cobertura XML via TUnit native coverage
- NuGet config pattern: root `nuget.config` with package source mapping and `signatureValidationMode`

### Institutional Learnings

- ADR-0002: Quality Gates Toolchain — established separate solution, monolith, local NuGet feed, Roslyn + Cobertura decisions

### External References

- Uncle Bob's [crap4java](https://github.com/unclebob/crap4java): reference implementation with `--changed` flag, exit code 2
- Uncle Bob's [crap4clj](https://github.com/unclebob/crap4clj): original Clojure implementation with full differential logic
- CRAP formula: `CRAP(m) = CC(m)² × (1 − coverage(m))³ + CC(m)`

---

## Key Technical Decisions

- **Roslyn for CC, not Roslynator output**: Roslynator CLI format is not a stable machine-readable contract. Using Roslyn directly gives full control over CC computation and method span extraction needed for coverage mapping.
- **Cobertura XML from TUnit, not coverlet**: TUnit already produces Cobertura XML via `dotnet run --coverage --coverage-output-format cobertura`. No need to add coverlet as a dependency.
- **`--project` + `--coverage-file` as separate flags**: Tool does not run tests itself. Coverage XML must be generated separately (by `scripts/Generate-CoverageReport.ps1` or similar). This keeps the tool stateless and fast.
- **`--changed` compares against HEAD**: In trunk-based development, `git diff HEAD --name-only` identifies files modified since the last commit. No branch comparison needed. Git is a conditional runtime dependency (required only when `--changed` is used).

---

## Deferred to Implementation

- Exact XML element names in Cobertura format — discover during CoverageParser TDD red phase
- Exact CLI flag parsing library setup — System.CommandLine wiring details determined during U5
- Package version numbers for System.CommandLine and Microsoft.CodeAnalysis — resolve during U1 when adding to Directory.Packages.props

---

## Implementation Units

- U1. **Scaffold Tool Project Structure**

**Goal:** Create the `tools/` solution, project, and test project with correct build configuration.

**Requirements:** R4

**Dependencies:** None

**Files:**

- Create: `tools/redmuffin.Tools.sln`
- Create: `tools/src/redmuffin.Tools.QualityGates/redmuffin.Tools.QualityGates.csproj`
- Create: `tools/tests/redmuffin.Tools.QualityGates.Tests/redmuffin.Tools.QualityGates.Tests.csproj`
- Create: `tools/nuget.config`
- Create: `tools/Directory.Build.props`
- Modify: `Directory.Packages.props` (add System.CommandLine, Microsoft.CodeAnalysis.CSharp, Microsoft.CodeAnalysis.CSharp.Workspaces, TUnit for tool)

**Approach:**

- Solution references both `src/` and `tests/` projects
- `src` project: `OutputType=Exe`, `PackAsTool=true`, `ToolCommandName=quality-gates`, `TargetFramework=net9.0`
- `tests` project: follows repo pattern — `OutputType=Exe`, `IsTestProject=true`, TUnit + Microsoft.Testing.Platform references, ProjectReference to src
- `tools/nuget.config`: adds `./nupkgs/` as local package source
- `tools/Directory.Build.props`: inherits from root where needed. Sets `<RestoreLockedMode>false</RestoreLockedMode>` for simpler tool development. Root analyzer packages (Meziantou, Roslynator, StyleCop, etc.) are inherited and apply to tool code — this is intentional, tool code must pass the same static analysis bar
- New package versions follow Central Package Management convention: properties in `Directory.Packages.props` VersionOverrides group

**Patterns to follow:**

- `src/SwaLauncher/SwaLauncher.csproj` for console app structure
- `tests/redmuffin.Blazor.StaticWeb.Tests/redmuffin.Blazor.StaticWeb.Tests.csproj` for test project structure
- `nuget.config` at repo root for package source configuration pattern

**Verification:**

- `dotnet build tools/redmuffin.Tools.sln` exits 0
- `dotnet test` on the test project discovers the test runner
- `dotnet pack` produces a `.nupkg` in `tools/nupkgs/`

---

- U2. **CyclomaticComplexity: Roslyn-Based CC Walker**

**Goal:** Parse C# source files via Roslyn and compute cyclomatic complexity per method.

**Requirements:** R1

**Dependencies:** U1 (project scaffold)

**Files:**

- Create: `tools/src/redmuffin.Tools.QualityGates/Analysis/CyclomaticComplexity.cs`
- Create: `tools/tests/redmuffin.Tools.QualityGates.Tests/Analysis/CyclomaticComplexityTests.cs`

**Approach:**

- Use `Microsoft.CodeAnalysis.CSharp` with `AdhocWorkspace` to load source files into syntax trees (fast, syntax-only — sufficient for all required branching constructs)
- Walk each `MethodDeclarationSyntax`, counting decision points: `if`, `while`, `for`, `foreach`, `case`, `catch`, `&&`, `||`, `??`, `? :`, `?.` (null-conditional), `??=` (null-coalescing assignment), switch expressions (count arms), pattern matching combinators (`and`, `or`, `not`)
- CC = decision points + 1 (baseline)
- Return a collection of `(MethodName, FilePath, StartLine, EndLine, CC)` tuples
- Extract line spans from Roslyn's `SyntaxNode.GetLocation().GetLineSpan()` (exposes `StartLinePosition` and `EndLinePosition`)
- Accept a directory path or project path; recursively find all `.cs` files

**Patterns to follow:**

- Roslyn `CSharpSyntaxWalker` or `CSharpSyntaxRewriter` pattern
- TUnit `[Test]` attribute and assertion patterns from existing test projects

**Test scenarios:** CC counting must handle all branching constructs (`if`, `while`, `for`, `foreach`, `switch`/`case`, `catch`, `&&`, `||`, `??`, `?:`, `?.`, `??=`), switch expressions (count arms), pattern matching combinators (`and`/`or`/`not`), and edge cases (abstract methods, throw-only, lambdas, local functions). CC = decision points + 1.

**Verification:**

- All test scenarios pass
- CC matches manual count for several real methods from `src/redmuffin.Blazor.StaticWeb/`

---

- U3. **CoverageParser: Cobertura XML Deserializer**

**Goal:** Parse a Cobertura XML file and extract per-file, per-line coverage data.

**Requirements:** R1

**Dependencies:** U1 (project scaffold)

**Files:**

- Create: `tools/src/redmuffin.Tools.QualityGates/Analysis/CoverageParser.cs`
- Create: `tools/tests/redmuffin.Tools.QualityGates.Tests/Analysis/CoverageParserTests.cs`

**Approach:**

- Parse Cobertura XML (generated by TUnit's `--coverage-output-format cobertura`)
- Extract `<package>` → `<class>` → `<line>` elements
- Build a data structure: `Dictionary<(FilePath, LineNumber), HitCount>`
- Handle `hits` attribute: 0 = uncovered, >0 = covered
- Return structured coverage data keyed by file path and line number

**Patterns to follow:**

- `System.Xml.Linq` (`XDocument`, `XElement`) for XML parsing — available in .NET 9 without additional packages

**Test scenarios:** Parse `<package>` → `<class>` → `<line>` elements into a `Dictionary<(FilePath, LineNumber), HitCount>`. Handle `hits` attribute (0 = uncovered, >0 = covered), missing attributes (default uncovered), malformed XML, empty files, and file-not-found. Parse a real TUnit Cobertura output as integration smoke test.

**Verification:**

- All test scenarios pass
- Parser correctly reads coverage for a known test run of the main project

---

- U4. **MethodMapper: Line Coverage to Method Span Mapping**

**Goal:** Map line-level coverage data to individual methods, computing per-method coverage percentages.

**Requirements:** R1

**Dependencies:** U2 (CC walker provides method spans), U3 (coverage parser provides line data)

**Files:**

- Create: `tools/src/redmuffin.Tools.QualityGates/Analysis/MethodMapper.cs`
- Create: `tools/tests/redmuffin.Tools.QualityGates.Tests/Analysis/MethodMapperTests.cs`

**Approach:**

- Input: list of methods with file paths and line spans (from CC walker) + coverage data (from CoverageParser)
- For each method, count covered lines within its span vs total lines in its span
- Method coverage = covered lines / total lines
- Return `(MethodName, FilePath, CC, Coverage, CRAP)` tuples, where CRAP = CC² × (1 − coverage)³ + CC
- Handle edge case: method with 0 lines in span → coverage = 0

**Patterns to follow:**

- Pure function: no I/O, only data transformation — easy to test
- CRAP formula from Uncle Bob's [crap4java](https://github.com/unclebob/crap4java) reference implementation

**Test scenarios:**

- Happy path: CC=1, 100% coverage → CRAP = 1² × (0)³ + 1 = 1
- Happy path: CC=2, 100% coverage → CRAP = 4 × 0 + 2 = 2
- Happy path: CC=2, 50% coverage → CRAP = 4 × (0.5)³ + 2 = 4 × 0.125 + 2 = 2.5
- Happy path: CC=3, 0% coverage → CRAP = 9 × 1 + 3 = 12 (should breach threshold of 8)
- Happy path: CC=5, 100% coverage → CRAP = 25 × 0 + 5 = 5
- Happy path: CC=5, 80% coverage → CRAP = 25 × (0.2)³ + 5 = 25 × 0.008 + 5 = 5.2
- Edge case: method with 0 lines in span → coverage = 0, CRAP = CC² + CC
- Edge case: coverage data missing for a file entirely → coverage = 0 for all methods in that file
- Edge case: method lines partially covered → coverage = covered / total within span

**Verification:**

- CRAP formula matches manual calculation for all test scenarios
- Integer and decimal CC values handled correctly

---

- U5. **CrapCommand: CLI Integration**

**Goal:** Wire System.CommandLine with `crap` subcommand, wire analysis classes, produce output table.

**Requirements:** R2, R3

**Dependencies:** U2, U3, U4 (all analysis classes)

**Files:**

- Create: `tools/src/redmuffin.Tools.QualityGates/Commands/CrapCommand.cs`
- Create: `tools/src/redmuffin.Tools.QualityGates/Program.cs`
- Modify: `tools/src/redmuffin.Tools.QualityGates/redmuffin.Tools.QualityGates.csproj` (add System.CommandLine reference)
- Create: `tools/tests/redmuffin.Tools.QualityGates.Tests/Commands/CrapCommandTests.cs`

**Approach:**

- Root command: `quality-gates`
- Subcommand: `crap` with options: `--project <path>` (required), `--coverage-file <path>` (required), `--max-crap <int>` (default 8), `--changed` (flag)
- `--changed` runs `git diff HEAD --name-only` to find modified `.cs` files, filters analysis to those files only
- Output: table of methods sorted by CRAP descending: `CRAP | CC | Coverage | Method | File:Line`
- Exit code 0 if all methods ≤ max-crap, exit code 2 if any method breaches
- Error handling: missing project file, missing coverage file, unparseable coverage XML

**Execution note:** Start with a test that invokes the command handler with mock analysis results and verifies exit code and output format.

**Patterns to follow:**

- System.CommandLine `Command` + `Option<T>` pattern
- Exit code conventions from crap4java (0 = pass, 2 = CRAP breach)

**Test scenarios:** Verify exit codes: 0 (all methods pass), 2 (any method breaches), 1 (invalid args, missing files, unparseable XML). Verify `--max-crap` threshold, `--changed` flag (including clean tree, no git repo, git not installed). Verify table output format sorted by CRAP descending.

**Verification:**

- `dotnet run --project tools/src/redmuffin.Tools.QualityGates -- crap --project src/redmuffin.Blazor.StaticWeb --coverage-file path/to/coverage.xml` produces a CRAP table
- Exit code 0 when all methods pass
- Exit code 2 when a method breaches

### Post-U5 Verification

- `dotnet pack tools/src/redmuffin.Tools.QualityGates --output tools/nupkgs` produces a `.nupkg`
- `dotnet tool install redmuffin.Tools.QualityGates --tool-manifest .config/dotnet-tools.json --add-source ./tools/nupkgs` succeeds
- `dotnet quality-gates crap --help` prints usage
- Update `tools/README.md`: CRAP gate status from "Planned" to "Done"
- Add `tools/nupkgs/` to `.gitignore` if not already covered

**Post-U5 milestone:** Run the tool against `src/redmuffin.Blazor.StaticWeb`. Any methods breaching CRAP 8 will be surfaced and must be remediated — there are no baseline exemptions. Every breach will be fixed.

---

## System-Wide Impact

Tool is stateless, invoked manually or via scripts. Exit codes (0/1/2) are the only contract.

---

## Risks & Dependencies

| Risk                                                                    | Mitigation                                                                                                                                                                              |
| ----------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Roslyn workspace loading is slow for large solutions                    | U5 `--changed` flag limits analysis to modified files; full scan is intentional for CI                                                                                                  |
| Cobertura XML format differs from TUnit version to version              | U3 parser is defensive — handles missing attributes gracefully                                                                                                                          |
| `RestoreLockedMode` in Directory.Build.props blocks adding new packages | `tools/Directory.Build.props` sets `RestoreLockedMode=false` for tool development; root packages remain locked                                                                          |
| Tool version management across updates                                  | Single version in csproj; `dotnet tool update` after repack                                                                                                                             |
| Stale coverage XML produces wrong CRAP scores                           | U4 documents that line-level coverage is approximate (method spans include non-statement lines). Consider timestamp check between source files and coverage XML as a future enhancement |

---

## Documentation / Operational Notes

- `tools/README.md` updated with CRAP gate status change and verified install commands
- ADR-0002 already covers the architectural decisions; no new ADR needed
- The `tools/` solution is intentionally not referenced by the main `.sln` — documented in ADR-0002

---

## Sources & References

- Origin document: `.opencode/skills/rm-uncle-bob-martin-agentic-coding/SKILL.md`
- ADR: `docs/adr/0002-quality-gates-toolchain.md`
- Tool README: `tools/README.md`
- Reference implementation: [crap4java](https://github.com/unclebob/crap4java)
- Coverage generation script: `scripts/Generate-CoverageReport.ps1`
