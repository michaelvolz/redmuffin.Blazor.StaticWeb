---
title: feat: Add mutation testing gate (clj-mutate → C# Roslyn)
type: feat
status: active
date: 2026-05-09
---

# feat: Add Mutation Testing Gate (clj-mutate → C# Roslyn)

## Summary

Port Uncle Bob's `clj-mutate` mutation testing tool to C# as the 4th quality gate. Discovers mutation sites in C# source files via Roslyn syntax walking, applies mutants one-at-a-time by rewriting the source file in-place, runs `dotnet run --project` on the test project, and classifies each mutant as killed or survived. Supports differential mode via embedded footer manifest, `--scan` for fast structural analysis, and parallel execution. Integrates into `AllCommand` as the final gate in the CRAP → SCRAP → Architecture → Mutation pipeline.

---

## Problem Frame

The quality gates toolchain is incomplete without mutation testing — the only way to verify that tests actually catch bugs rather than just executing code paths. Uncle Bob's `clj-mutate` proves this discipline works in Clojure; this plan ports it to C# with Roslyn-based mutation discovery, coverlet Cobertura XML coverage integration, and `dotnet run --project` execution. The gate must replicate the original tool's algorithm, CLI interface, exit codes, and output format exactly (§1.1 TOP REQUIREMENT).

---

## Requirements

- R1. Discover mutation sites across C# source files using 6 rule categories (arithmetic, comparison, equality, boolean, conditional, constant) adapted for C# syntax
- R2. Apply auto-suppression predicates for known-equivalent mutations (e.g., `> → >=` near `List.Count > 0` subvec boundaries)
- R3. Parse coverlet Cobertura XML coverage data to skip mutations on uncovered lines (reuse existing CoverageParser.cs pattern from CRAP gate)
- R3b. Support `--reuse-coverage` flag to reuse existing coverage data without regeneration (warn when stale or missing)
- R4. Run a baseline test (`dotnet run --project <test-project>`) to verify tests pass and measure timing before mutating
- R5. Apply each mutation sequentially, run `dotnet run --project <test-project>`, classify as killed/survived
- R6. Support `--scan` mode: fast structural site count without test execution
- R7. Support differential mode: embedded footer manifest with member hashes, only mutate changed members
- R8. Support `--max-workers N` for parallel mutation execution
- R9. Output header (total/covered/uncovered/changed counts, manifest status, differential surface) then survivors summary
- R10. Exit code 0 for completed runs (survivors informational), exit code 1 for errors (missing coverage with `--reuse-coverage`, baseline failure)
- R11. Integrate into `AllCommand` as 4th gate with `--mutate-*` flags

---

## Scope Boundaries

- Mutation operates on one source file per invocation (single-file scope, matching clj-mutate)
- Full-project mutation is out of scope for v1 — run one file at a time
- Manifest writes modify git-tracked source files (same behavior as clj-mutate) — this is intentional
- `--update-manifest` (rewrite manifest without testing) is deferred to follow-up
- `--test-command` override (custom test command) is deferred — default to `dotnet run --project` on the test project
- `--reuse-coverage`: reuse existing coverage data without regeneration, warn when stale or missing — in scope for v1

### Deferred to Follow-Up Work

- `--test-command` flag for custom test invocation — separate PR
- `--update-manifest` flag — separate PR
- Full-project mutation mode (scan all .cs files) — separate PR
- Stryker.NET-style HTML reports — out of scope

---

## Context & Research

### Relevant Code and Patterns

- **Command/Handler pattern**: `tools/src/redmuffin.Tools.QualityGates/Commands/CrapHandler.cs` (exit codes, TextWriter output), `ArchHandler.cs` (orchestration pattern)
- **Roslyn parsing**: `tools/src/redmuffin.Tools.QualityGates/Analysis/CyclomaticComplexity.cs` (CSharpSyntaxWalker, CSharpSyntaxTree.ParseText)
- **Roslyn rewriting**: `tools/src/redmuffin.Tools.QualityGates/Analysis/ScrapDuplication.cs` (syntax normalization via CSharpSyntaxRewriter)
- **Options pattern**: `tools/src/redmuffin.Tools.QualityGates/Commands/ScrapOptions.cs`
- **AllCommand composition**: `tools/src/redmuffin.Tools.QualityGates/Commands/AllCommand.cs` (run-all policy, CombineExitCodes)

### Institutional Learnings

- `docs/solutions/developer-experience/quality-gates-tool-operational-gotchas-2026-05-09.md` — SDK version trap (run from tools/), `dotnet test` doesn't discover TUnit tests, command-handler separation
- `docs/solutions/tooling-decisions/rm-tdd-llm-guarded-test-discipline-2026-05-09.md` — vertical slicing, black-box testing, one assertion per test
- `docs/solutions/test-failures/tunit-testing-platform-version-compatibility-2026-04-07.md` — TUnit + Microsoft.Testing.Platform version pinning

### External References

- `clj-mutate` source at SHA `a95a3352cd5fe104dac8e7a5c0b97547ba5718d5` ([GitHub](https://github.com/unclebob/clj-mutate)) — mutation rules, suppression predicates, manifest format, parallel worker design, workflow orchestration
- AIR-J AGENTS.md — mutation workflow: baseline → scan → mutate → kill survivors
- coverlet Cobertura XML format — line-level coverage data (already used by CRAP gate)

---

## Key Technical Decisions

- **In-place sequential mutation (with worker isolation for `--max-workers > 1`)**: When `--max-workers 1` (default), mutate the source file in-place (modify → run `dotnet run --project` → restore). When `--max-workers N > 1`, create N worker directories under `target/mutation-workers/run-<uuid>/worker-{0..N-1}` by cloning the test project directory, then dispatch each mutation site to a worker. Workers mutate their isolated copy independently. Worker dirs are cleaned up after the run. This matches clj-mutate's `workers.cljc` design exactly.
- **Roslyn syntax walking, not MSBuildWorkspace**: Per-file parsing via `CSharpSyntaxTree.ParseText()` — matches CRAP and SCRAP patterns, avoids workspace overhead for single-file mutation.
- **Cobertura XML coverage format**: The project already produces Cobertura XML via `scripts/Generate-CoverageReport.ps1` (used by the CRAP gate). The mutation gate extends the existing `CoverageParser.cs` pattern to extract line-level coverage data. No new NuGet packages or coverage infrastructure required.
- **Manifest as JSON comment block**: `// clj-mutate-manifest-begin` / `// clj-mutate-manifest-end` wrapping JSON (not Clojure EDN). Uses `System.Text.Json` for zero-dependency parsing. Contains `version`, `testedAt`, `moduleHash`, and per-member `forms` with line span and hash. Functionally equivalent to clj-mutate's EDN manifest, but formatted for C# interop.
- **Exit code 0 for survivors**: Matches clj-mutate exactly. Survivors are informational — the developer/agent reviews output and adds tests. This differs from CRAP/SCRAP/Arch which use exit code 2 for violations. `CombineExitCodes` treats exit code 0 as "no gate failure" — mutation survivors won't break the `all` pipeline.

---

## Open Questions

### Resolved During Planning

- Worker isolation strategy: In-place sequential for v1 (see Key Technical Decisions)
- Coverage format: Cobertura XML from coverlet (reuse existing CoverageParser.cs, already produced by Generate-CoverageReport.ps1)

### Deferred to Implementation

- Exact C# `SyntaxKind` mappings for each mutation category — determined when writing MutationRules
- C#-specific suppression predicates (e.g., `List.Count > 0` boundary guards) — derived from Clojure equivalents during implementation
- Baseline `dotnet run --project` command construction — depends on whether user provides `--test-project` or infers from source file location

---

## Implementation Units

### Phase 1: Core Engine

- U1. **Mutation rules engine and site discovery**

**Goal:** Define 6 mutation categories with C# `SyntaxKind` mappings, auto-suppression predicates, and Roslyn walker that discovers all mutation sites in a file.

**Requirements:** R1, R2

**Dependencies:** None

**Files:**

- Create: `tools/src/redmuffin.Tools.QualityGates/Analysis/MutationRules.cs`
- Create: `tools/src/redmuffin.Tools.QualityGates/Analysis/MutationDiscoverer.cs`
- Test: `tools/tests/redmuffin.Tools.QualityGates.Tests/Analysis/MutationDiscovererTests.cs`

**Approach:**

- `MutationRules` defines a static list of rules. Each rule: original `SyntaxKind`, mutant `SyntaxKind`/token, category enum, position (`Head` or `Any`), optional suppression predicate list.
- C# syntax mapping from clj-mutate rules:
  - Arithmetic: `+` ↔ `-` (AddExpression/SubtractExpression), `*` → `/` (MultiplyExpression → DivideExpression), `++` ↔ `--` (PreIncrementExpression ↔ PreDecrementExpression + Post variants)
  - Comparison: `>` ↔ `>=` (GreaterThanExpression ↔ GreaterThanOrEqualExpression), `<` ↔ `<=` (LessThanExpression ↔ LessThanOrEqualExpression)
  - Equality: `==` ↔ `!=` (EqualsExpression ↔ NotEqualsExpression)
  - Boolean: `true` ↔ `false` (TrueLiteralExpression ↔ FalseLiteralExpression)
  - Conditional: `if` body negation — apply `!` to condition (if `x` then A else B → if `!x` then A else B, effectively swapping branches). C# lacks `if-not`.
  - Constant: `0` ↔ `1` (NumericLiteralExpression with value 0/1)
- `MutationDiscoverer.FindSites(string source)` returns `IReadOnlyList<MutationSite>`. Each site: `Index` (sequential), `Category`, `Line`, `Column`, `Description`, `Original` SyntaxKind, `Mutant` SyntaxKind/token.
- Walker uses `CSharpSyntaxWalker` visiting all expression nodes. For each node, checks if any rule matches. Suppression predicates receive context (parent node, grandparent node) and skip equivalent mutations.
- Warning: `FindSites` and `ApplySite` must walk the tree identically so indices match. Any change to traversal order must be mirrored in both.

**Execution note:** Start with a failing test — parse a simple method with `+` operator, assert one mutation site found.

**Patterns to follow:**

- `tools/src/redmuffin.Tools.QualityGates/Analysis/CyclomaticComplexity.cs` — CSharpSyntaxWalker subclass pattern
- `tools/src/redmuffin.Tools.QualityGates/Analysis/ScrapDuplication.cs` — normalization and analysis pipeline

**Test scenarios:**

- Happy path: Method with `a + b` → one arithmetic site found (line/column correct)
- Happy path: Method with `x > y` → one comparison site found
- Happy path: Method with `flag == true` → one equality site found
- Happy path: Method with `return true` → one boolean site found
- Happy path: Method with `x = 0` → one constant site found
- Happy path: Method with `if (condition)` → one conditional site found
- Edge case: `++` and `--` (both prefix and postfix) properly discovered
- Edge case: Compound expression `(a + b) * c` → two arithmetic sites
- Suppression: Comparison on `list.Count > 0` should be suppressed (subvec boundary)
- Suppression: Constant inside random-seed context should be suppressed
- Edge case: Method with no mutation sites → empty list returned
- Edge case: Indices are sequential and stable across repeated calls

**Verification:**

- `MutationDiscoverer.FindSites(source)` returns correct count, types, and line numbers for a test file with known mutation sites
- All 6 categories produce at least one site in an appropriate test fixture

---

- U2. **Coverage reader (Cobertura XML parsing)**

**Goal:** Parse coverlet-generated Cobertura XML files and partition mutation sites by coverage status. Reuse the existing `CoverageParser.cs` pattern from the CRAP gate, extended to extract line-level coverage.

**Requirements:** R3

**Dependencies:** U1

**Files:**

- Create: `tools/src/redmuffin.Tools.QualityGates/Analysis/CoverageReader.cs`
- Test: `tools/tests/redmuffin.Tools.QualityGates.Tests/Analysis/CoverageReaderTests.cs`

**Approach:**

- `CoverageReader.LoadCoverage(string coberturaPath)` returns `IReadOnlySet<int>` of covered line numbers
- Parses Cobertura XML: `<packages>/<package>/<classes>/<class>/<lines>/<line hits=">0" number="N"/>`
- `CoverageReader.PartitionByCoverage(...)` same signature — pure data transformation
- Supports `--reuse-coverage` flag: if coverage XML missing and `reuseCoverage` is true, throws with message instructing user to generate coverage first
- Checks coverage freshness (file timestamp vs source timestamp) — warns when stale

**Patterns to follow:**

- `tools/src/redmuffin.Tools.QualityGates/Analysis/CoverageParser.cs` — CRAP gate Cobertura parsing
- Scalar analysis: no state, pure functions, return data

**Test scenarios:**

- Happy path: Parse valid Cobertura XML with 5 covered lines → correct set
- Happy path: Partition 10 sites with 7 on covered lines → (7 covered, 3 uncovered)
- Edge case: Empty Cobertura (no lines covered) → empty set
- Edge case: XML with `<classes/>` but no `<line>` elements → empty set
- Error path: Missing coverage file with `reuseCoverage=true` → throws with actionable message
- Integration: Coverage line numbers match mutation site line numbers correctly (1-indexed)

**Verification:**

- Cobertura XML parsing round-trips correctly for test fixture files
- Partitioned sites are disjoint and union equals original

---

- U3. **Mutation applicator (Roslyn rewriter)**

**Goal:** Apply a specific mutation by index to a C# source file, producing the mutated text.

**Requirements:** R5

**Dependencies:** U1

**Files:**

- Create: `tools/src/redmuffin.Tools.QualityGates/Analysis/MutationApplicator.cs`
- Test: `tools/tests/redmuffin.Tools.QualityGates.Tests/Analysis/MutationApplicatorTests.cs`

**Approach:**

- `MutationApplicator.Apply(string source, int siteIndex, MutationSite site)` returns mutated source text
- Uses `CSharpSyntaxRewriter` subclass that walks the tree identically to `MutationDiscoverer.FindSites`
- When it reaches the node at `siteIndex`, replaces the node with the mutant form:
  - Arithmetic: replace binary expression operator token (e.g., `PlusToken` → `MinusToken`)
  - Comparison: replace binary expression operator token (e.g., `GreaterThanToken` → `GreaterThanEqualsToken`)
  - Equality: replace binary expression operator token (e.g., `EqualsEqualsToken` → `ExclamationEqualsToken`)
  - Boolean: replace literal expression token (e.g., `TrueLiteralExpression` → `FalseLiteralExpression`)
  - Conditional: wrap condition in `PrefixUnaryExpression(SyntaxKind.LogicalNotExpression)`
  - Constant: replace numeric literal token value
- SYNC WARNING: `MutationApplicator` and `MutationDiscoverer` must walk the tree identically so site indices match. `CSharpSyntaxWalker` and `CSharpSyntaxRewriter` have incompatible base classes — they cannot share a base walker. Strategy: `MutationDiscoverer` collects `SyntaxNode` references alongside each site. `MutationApplicator` identifies the target node by `Span` position in the tree (node identity decoupled from counting visits), then rewrites via `SyntaxNode.ReplaceNode`. This is testable: a conformance test for a fixture file iterates all sites, applies each, and verifies exactly one change.

**Execution note:** Write a test that verifies index correspondence between FindSites and Apply — for a fixture file, iterating through all sites, applying each, and checking the output differs from the original.

**Patterns to follow:**

- `tools/src/redmuffin.Tools.QualityGates/Analysis/ScrapDuplication.cs` — CSharpSyntaxRewriter usage

**Test scenarios:**

- Happy path: Apply site 0 (arithmetic: `a + b` → `a - b`) → output contains `-` not `+`
- Happy path: Apply site 0 (comparison: `x > y` → `x >= y`) → output contains `>=` not `>`
- Happy path: Apply site 0 (equality: `a == b` → `a != b`) → output contains `!=` not `==`
- Happy path: Apply site 0 (boolean: `return true` → `return false`)
- Happy path: Apply site 0 (conditional: `if (x)` → `if (!x)`)
- Happy path: Apply site 0 (constant: `x = 0` → `x = 1`)
- Edge case: Apply last site in multi-site file → only that site changed, others untouched
- Edge case: Apply index out of range → throw with clear message
- Critical: For a fixture with 5 sites, apply each and verify exactly one change per application (index sync verified)

**Verification:**

- Applying site N changes only site N, no other mutations introduced
- Output compiles (Roslyn can parse the output)
- Running all sites through Apply produces distinct outputs for each

---

- U4. **Mutation runner (baseline + test orchestration)**

**Goal:** Run baseline (`dotnet run --project`), then for each covered mutation site, apply the mutation, run the test project, classify as killed or survived, and restore the original source.

**Requirements:** R4, R5

**Dependencies:** U1, U2, U3

**Files:**

- Create: `tools/src/redmuffin.Tools.QualityGates/Analysis/MutationRunner.cs`
- Test: `tools/tests/redmuffin.Tools.QualityGates.Tests/Analysis/MutationRunnerTests.cs`

**Approach:**

- `MutationRunner.Run(string sourcePath, IReadOnlyList<MutationSite> sites, string testProjectPath, MutationOptions options)` returns `IReadOnlyList<MutantResult>`
- Steps:
  1. Run baseline `dotnet run --project <test-project>` on test project, measure elapsed time
  2. If baseline fails → return empty list (tests don't pass unmodified)
  3. Compute timeout = `baselineElapsed * options.TimeoutFactor`
  4. Read original source from `sourcePath`
  5. Write backup to `sourcePath.backup` before mutating (crash-safe: Handler checks for orphaned `.backup` on startup and auto-restores)
  6. For each covered site: apply mutation → write to `sourcePath` → run `dotnet run --project <test-project>` with timeout → classify → restore original source from memory
  7. Delete `sourcePath.backup` on successful completion
  8. Classification: `dotnet run --project` exit code ≠ 0 → killed; exit code 0 → survived; timeout → killed
- Uses `System.Diagnostics.Process` to invoke `dotnet run --project` with working directory set to test project
- Reads coverlet Cobertura XML output path from test project configuration
- MutantResult record: `SiteIndex`, `Category`, `Line`, `Column`, `Description`, `Result` (Killed/Survived/Error), `DurationMs`

**Execution note:** This is the hardest unit to test — it requires a real .NET test project with real tests. Add a pre-built fixture project at `tools/tests/redmuffin.Tools.QualityGates.Tests/Fixtures/MutationTarget/` containing a C# source file with known mutation sites and TUnit tests that are intentionally weak against specific mutations (so some survive, some are killed). This avoids test-time `.csproj` generation and SDK dependency issues.

**Patterns to follow:**

- `System.Diagnostics.Process` for external command invocation
- Timeout handling via `WaitForExit(milliseconds)`

**Test scenarios:**

- Happy path: Project with test that catches mutation → mutation killed
- Happy path: Project with weak test that doesn't catch mutation → mutation survived
- Edge case: Baseline test fails → empty results returned, error logged
- Edge case: Mutation site on uncovered line → skipped (handled by handler, not runner)
- Edge case: Mutation causes compilation error → classified as killed (test can't run)
- Error path: Test process times out → classified as killed with timeout note
- Error path: Source file doesn't exist → throw FileNotFoundException

**Verification:**

- Runner produces correct killed/survived counts for a fixture project with known-weak tests
- Source file is restored to original content after run (verify via SHA comparison)

---

### Phase 2: Differential + Handler

- U5. **Mutation manifest (embedded footer + differential)**

**Goal:** Embed a footer manifest in mutated source files tracking member hashes, and compute differential mutation sites (only changed members since last run).

**Requirements:** R7

**Dependencies:** U1

**Files:**

- Create: `tools/src/redmuffin.Tools.QualityGates/Analysis/MutationManifest.cs`
- Test: `tools/tests/redmuffin.Tools.QualityGates.Tests/Analysis/MutationManifestTests.cs`

**Approach:**

- Manifest format (JSON, wrapped in C# comment block):
  ```
  // clj-mutate-manifest-begin
  // {"version":1,"testedAt":"2026-05-09T...","moduleHash":"12345","forms":[...]}
  // clj-mutate-manifest-end
  ```
- `MutationManifest.Extract(string source)` → `Manifest?` (parses embedded JSON via `System.Text.Json`)
- `MutationManifest.Strip(string source)` → source without manifest block
- `MutationManifest.Build(string source, DateTime testedAt)` → computes `module-hash` (hash of all top-level members), per-member `:forms` (id, line, end-line, hash of normalized body)
- `MutationManifest.Embed(string source, Manifest manifest)` → source with manifest appended
- `MutationManifest.ChangedFormIndices(Manifest prior, Manifest current)` → set of form indices whose hashes changed
- Top-level members: classes, structs, interfaces, enums, delegates, methods, properties, fields at namespace level
- Hash: SHA256 of normalized member text (whitespace-insensitive via Roslyn NormalizeWhitespace)

**Test scenarios:**

- Happy path: Build manifest for simple file with 2 methods → 2 forms with distinct hashes
- Happy path: Round-trip: embed → extract → same manifest data
- Happy path: Strip removes manifest block correctly
- Happy path: Compute changed form indices → only modified method's index returned
- Edge case: File with no previous manifest → null from Extract
- Edge case: File with only one top-level member → single form
- Edge case: Whitespace-only change → hash unchanged (normalized)
- Edge case: Empty file → empty forms list

**Verification:**

- Manifest survives embed-extract round-trip without corruption
- Changed-form detection correctly identifies modified methods

---

- U6. **MutateHandler (orchestration + output + exit codes)**

**Goal:** Orchestrate the full mutation pipeline: parse options → discover sites → load coverage → run baseline → mutate → report. Produce clj-mutate-compatible output and exit codes.

**Requirements:** R9, R10

**Dependencies:** U1, U2, U3, U4, U5

**Files:**

- Create: `tools/src/redmuffin.Tools.QualityGates/Commands/MutateHandler.cs`
- Create: `tools/src/redmuffin.Tools.QualityGates/Commands/MutateOptions.cs`
- Test: `tools/tests/redmuffin.Tools.QualityGates.Tests/Commands/MutateHandlerTests.cs`

**Approach:**

- `MutateHandler.Run(string sourcePath, string testProjectPath, MutateOptions options, TextWriter? output)` returns `int`
- MutateOptions record: `bool Scan`, `bool MutateAll`, `bool SinceLastRun`, `int MaxWorkers`, `int MutationWarning` (default 50), `int TimeoutFactor` (default 10), `bool ReuseCoverage`, `IReadOnlySet<int>? Lines`
- Pipeline:
  1. Read source, strip manifest, discover sites
  2. Load Cobertura XML coverage data, partition sites
  3. Apply manifest differential filtering (if `--since-last-run` or manifest present and no `--mutate-all`)
  4. Apply `--lines` filtering
  5. If `--scan`: print header (total/changed sites, manifest status), return 0
  6. Run baseline via `dotnet run --project` via runner
  7. If baseline fails: print "FAIL — tests do not pass without mutations", return 1
  8. Run mutations via runner with timeout from baseline
  9. Print header (total/covered/uncovered/changed sites, manifest status, differential surface area)
  10. Print survivors summary (killed/total count, percentage, survivor list)
  11. Write updated manifest
  12. Return 0 (survivors are informational per clj-mutate)

**Output format** (matching clj-mutate exactly):

```
=== Mutation Testing: path/to/file.cs ===
Previous mutation test: 2026-05-09T...
Total mutation sites: 25
Covered mutation sites: 20
Uncovered mutation sites: 5
Changed mutation sites: 3
Manifest exists: yes
Module hash changed: yes
Differential surface area: 3 mutations in new top-level forms
Manifest-violating surface area: 0 mutations

=== Summary ===
18/20 mutants killed (90.0%)
5 uncovered mutations skipped
Survivors:
  #3  L42   > → >=
  #18 L156  return true → return false
```

**Patterns to follow:**

- `tools/src/redmuffin.Tools.QualityGates/Commands/CrapHandler.cs` — TextWriter output, exit codes, table formatting
- `tools/src/redmuffin.Tools.QualityGates/Commands/ScrapOptions.cs` — sealed record options

**Test scenarios:**

- Happy path: Handler returns 0 when all mutants killed
- Happy path: Handler returns 0 when survivors exist (informational)
- Happy path: `--scan` prints header with site counts, does not run tests
- Happy path: `--since-last-run` filters to changed forms only
- Happy path: `--lines 10,15` filters to specific lines only
- Error path: Missing source file → exit 1
- Error path: Missing test project → exit 1
- Error path: Baseline test fails → exit 1
- Error path: `--reuse-coverage` with missing coverage XML → exit 1 with actionable message

**Verification:**

- Handler output matches clj-mutate format for a fixture project
- Exit codes: 0 for completed runs, 1 for errors, never 2

---

### Phase 3: CLI + Integration

- U7. **MutateCommand (CLI wiring)**

**Goal:** Wire System.CommandLine options (`--project`, `--scan`, `--max-workers`, `--since-last-run`, `--mutate-all`, `--lines`, `--mutation-warning`, `--timeout-factor`, `--reuse-coverage`) and delegate to MutateHandler.

**Requirements:** R8, R11

**Dependencies:** U6

**Files:**

- Create: `tools/src/redmuffin.Tools.QualityGates/Commands/MutateCommand.cs`
- Modify: `tools/src/redmuffin.Tools.QualityGates/Program.cs`

**Approach:**

- `MutateCommand.Create()` returns System.CommandLine `Command("mutate")`
- Options: `--project` (required, path to source file), `--test-project` (required, path to test project directory), `--scan`, `--max-workers`, `--since-last-run`, `--mutate-all`, `--lines`, `--mutation-warning`, `--timeout-factor`, `--reuse-coverage`
- `Execute()` validates paths, constructs MutateOptions, calls MutateHandler.Run
- Wire in Program.cs: `rootCommand.Subcommands.Add(MutateCommand.Create());`

**Patterns to follow:**

- `tools/src/redmuffin.Tools.QualityGates/Commands/ArchCommand.cs` — option definitions, SetAction, Execute

**Test scenarios:**

- No command-level unit tests (commands are thin wrappers; handler is tested directly)

**Verification:**

- `dotnet run --project src/redmuffin.Tools.QualityGates -- mutate --help` shows usage
- `dotnet run --project src/redmuffin.Tools.QualityGates -- mutate --project src/Foo.cs --test-project tests/Foo.Tests` invokes handler

---

- U8. **AllCommand integration + README**

**Goal:** Extend AllCommand to run mutation gate as 4th step, update README gates table.

**Requirements:** R11

**Dependencies:** U7

**Files:**

- Modify: `tools/src/redmuffin.Tools.QualityGates/Commands/AllCommand.cs`
- Modify: `tools/tests/redmuffin.Tools.QualityGates.Tests/Commands/AllCommandTests.cs`
- Modify: `tools/README.md`

**Approach:**

- Add mutation-specific AllCommand options: `--mutate-source`, `--mutate-test-project`, `--mutate-scan`, `--mutate-max-workers`
- Add `MutateCommand.Execute(...)` call in the run-all pipeline
- Extend `CombineExitCodes` to accept 4 parameters
- Add mutate status line to AllCommand output
- Update README gates table: Mutation row → Status: Done
- Update README project structure tree

**Test scenarios:**

- Happy path: CombineExitCodes(0, 0, 0, 0) → 0
- Happy path: CombineExitCodes(0, 0, 0, 2) → 2 (mutation uses 0 not 2, but test that arch+crap still dominate)
- Edge case: CombineExitCodes(1, 0, 0, 0) → 1 (error propagates)

**Verification:**

- `dotnet run --project src/redmuffin.Tools.QualityGates -- all --help` shows mutation flags
- AllCommandTest for 4-parameter CombineExitCodes passes

---

## System-Wide Impact

- **Interaction graph:** MutateCommand flows into MutateHandler which orchestrates MutationDiscoverer → CoverageReader → MutationRunner. No callbacks or middleware affected.
- **Error propagation:** Handler catches FileNotFoundException, FormatException (coverage XML parse), Process failure (test runner crash). Returns exit 1 for all errors.
- **State lifecycle risks:** Source file mutation is in-place with in-memory restore. If the process crashes mid-mutation, the source file may be left mutated. The manifest-based backup/restore pattern from clj-mutate (save-backup! / restore-from-backup! / cleanup-backup!) will be implemented.
- **API surface parity:** MutateCommand follows the same `Create()` / `internal static Execute()` pattern as all other gates.
- **Unchanged invariants:** Other gates (CRAP, SCRAP, Architecture) are untouched. AllCommand's run-all policy (all gates execute regardless of failures) is preserved.

---

## Risks & Dependencies

| Risk                                                                                                               | Mitigation                                                                                                        |
| ------------------------------------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------- |
| Test project may compile slowly (Roslyn incremental build invalidation)                                            | Baseline time already captured; timeout-factor accounts for this. Acceptable for single-file mutation             |
| In-place mutation may leave file corrupted if process killed mid-write                                             | Implement backup/restore pattern from clj-mutate: save backup before mutation, restore on startup if backup found |
| Cobertura XML parsing fragile across coverlet versions                                                             | Pin to known coverlet Cobertura format; add version check in parser                                               |
| Some C# constructs resist simple token replacement (e.g., pattern matching `is > 5`)                               | Start with basic binary expressions; defer advanced syntax to follow-up. Document unsupported constructs          |
| Differential mode may produce false positives if Roslyn NormalizeWhitespace doesn't handle all whitespace variants | Use Roslyn's built-in normalizer; accept small false-positive rate as clj-mutate does                             |

---

## Sources & References

- **Original tool:** [clj-mutate](https://github.com/unclebob/clj-mutate) at SHA `a95a3352cd5fe104dac8e7a5c0b97547ba5718d5`
- **AIR-J AGENTS.md:** mutation workflow and pinned toolchain SHAs
- **Prior art:** `tools/src/redmuffin.Tools.QualityGates/Analysis/CyclomaticComplexity.cs`, `ScrapDuplication.cs` — Roslyn patterns
- **Operational gotchas:** `docs/solutions/developer-experience/quality-gates-tool-operational-gotchas-2026-05-09.md`
- **TDD discipline:** `docs/solutions/tooling-decisions/rm-tdd-llm-guarded-test-discipline-2026-05-09.md`
- **Package compatibility:** `docs/solutions/test-failures/tunit-testing-platform-version-compatibility-2026-04-07.md`
