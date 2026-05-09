---
title: feat: Add SCRAP test structural analyzer and AllCommand composition
type: feat
status: completed
date: 2026-05-09
origin: docs/adr/0003-scrap-test-structural-analyzer.md
---

# feat: Add SCRAP Test Structural Analyzer and AllCommand Composition

## Summary

Adds the SCRAP (test structural analyzer) subcommand to `redmuffin.Tools.QualityGates` — the second quality gate after CRAP. SCRAP detects weak-test smells (zero-assertion, low-assertion, duplicated scaffolding) and computes extraction pressure using fuzzy Jaccard similarity on Roslyn-normalized test bodies. All thresholds are locked to Uncle Bob's scrap source `policy.clj` values. Also creates `AllCommand` to run CRAP and SCRAP sequentially with unified pass/fail, fulfilling the ADR-0002 requirement for one-command/one-report/one-exit-code.

---

## Problem Frame

CRAP gates production code quality (complexity × coverage). But tests themselves have structural quality that CRAP cannot see — zero-assertion "tests" that pass but verify nothing, copy-pasted Arrange blocks across dozens of examples, and files where test responsibilities are so blurred that local cleanup cannot fix them. The Uncle Bob discipline mandates a test structural analyzer (SCRAP) as the second gate. Without it, agents can produce tests that satisfy CRAP's coverage requirement while being unmaintainable and semantically hollow.

---

## Requirements

- R1. SCRAP subcommand discovers and analyzes TUnit test files in a test project directory
- R2. Per-example metrics: assertion count, complexity score, setup depth, line count, smell labels
- R3. Fuzzy structural duplication via Jaccard similarity (threshold 0.5) on Roslyn-normalized test bodies
- R4. Extraction pressure formula: `D_before = max(0, F-3) * (I-1)^1.5 / (V+1)` where F=0 if F≤3 or V>4
- R5. Per-file aggregation with STABLE/LOCAL/SPLIT recommendation and AI-actionability class
- R6. CLI parity with CrapCommand: `scrap --test-project <dir>` with `--verbose`, `--json`, `--changed`, `--write-baseline`, `--compare`
- R7. AllCommand subcommand runs CRAP then SCRAP sequentially, exits non-zero on any breach
- R8. Exit code 2 on SCRAP threshold breach, 1 on error, 0 on pass
- R9. All exact thresholds from scrap `policy.clj` (stability, SPLIT trigger, pressure levels, complexity curve)

---

## Scope Boundaries

- TUnit only — no xUnit or NUnit detection in this iteration
- Analyzes test method bodies only — does not analyze test class constructors or `[Before]`/`[After]` setup methods for extraction pressure (detected as complexity input, not duplication channels)
- No mutation testing integration — SCRAP is purely structural
- `--compare` mode requires `--write-baseline` to have been run first — no auto-baseline generation in compare mode

### Deferred to Follow-Up Work

- xUnit and NUnit support: separate plan when needed
- `AllCommand` stubs for future `dupes` and `arch` gates: wire them when those gates are implemented
- Baselines directory (`target/scrap/`): create when first `--write-baseline` is run, not in tool init

---

## Context & Research

### Relevant Code and Patterns

- `tools/src/redmuffin.Tools.QualityGates/Commands/CrapCommand.cs` — CLI pattern to replicate exactly (static class, `Create()` factory, `SetAction()`, `internal static int Execute()`, Handler with injectable `TextWriter`)
- `tools/src/redmuffin.Tools.QualityGates/Commands/CrapHandler.cs` — sort-results, print-table, return-exit-code pattern
- `tools/src/redmuffin.Tools.QualityGates/Analysis/CyclomaticComplexity.cs` — `CSharpSyntaxTree.ParseText()` per-file pattern, `CSharpSyntaxWalker` subclass, `DescendantNodes()` for method discovery
- `tools/src/redmuffin.Tools.QualityGates/Program.cs` — `RootCommand` + `Subcommands.Add()` registration
- `tools/tests/redmuffin.Tools.QualityGates.Tests/Analysis/CyclomaticComplexityTests.cs` — test pattern: temp-file-in-helper-method, `should_*` naming, `[Test]` + `public async Task`
- `tools/tests/redmuffin.Tools.QualityGates.Tests/Commands/CrapCommandTests.cs` — command test pattern
- Tool csproj already references `Microsoft.CodeAnalysis.CSharp` 5.3.0, `System.CommandLine` 2.0.7 — no new packages needed

### Institutional Learnings

- **CRAP pipeline doc** (`docs/solutions/tooling-decisions/crap-quality-gates-pipeline-2026-05-09.md`): All gates share one Roslyn approach. SCRAP is listed as planned subcommand. Exit code 2 on breach.
- **ADR-0002**: Adding a new gate = subcommand class + hook into `all`. Must `dotnet pack` after tool code changes.
- **Uncle Bob skill** (`.opencode/skills/rm-uncle-bob-martin-agentic-coding/SKILL.md` lines 88-92): SCRAP reference at `https://github.com/unclebob/scrap`.
- **TDD guardrails doc**: Defines "good test" properties (one assertion, black-box, no implementation coupling) that SCRAP enforces structurally.

### External References

- Uncle Bob's scrap source: `https://github.com/unclebob/scrap` — `policy.clj` for thresholds, `pressure_stability.clj` for stability rules, `pressure_mode.clj` for SPLIT vs LOCAL logic, `actionability_modes.clj` for AI-actionability classes
- Scrap README: Jaccard threshold 0.5, extraction pressure formula, normalization strategy (symbols→`sym`, strings→`:string`, numbers→`:number`, preserve collection shape)

---

## Key Technical Decisions

- **Per-file `CSharpSyntaxTree.ParseText()` over `MSBuildWorkspace`**: The existing CRAP tool uses per-file parsing, not a full workspace. SCRAP follows the same pattern — load each `.cs` test file individually, parse, walk. No shared workspace to coordinate. Simpler and faster.
- **Syntax-node normalization**: Normalize `IdentifierNameSyntax` → placeholder, `LiteralExpressionSyntax` → type-based placeholder, preserve all other `SyntaxKind` structure. This mirrors Clojure's form normalization and preserves assertion shape (e.g., `IsNotNull()` vs `IsEqualTo(x)` remain distinguishable).
- **Channel-based duplication**: Follow scrap's three-channel model — harmful duplication (setup/assertion/arrange), case-matrix repetition (low-complexity coverage tables), subject repetition (same API focus, lightly penalized). Channels are implemented as separate detection passes sharing the same normalized bodies.
- **SCRAP complexity as saturating curve**: Use scrap's complexity formula (cap 25.0, rise-rate 0.18, floor 1.0) rather than cyclomatic complexity. This prevents huge test methods from exploding scores.
- **Static record types for scoring**: Follow the existing `MethodComplexity`/`MethodCrap` record pattern. Create `TestMethodMetrics` (per-example) and `FileScrapReport` (per-file) records.
- **AllCommand follows CrapCommand.Execute() pattern**: Rather than abstracting a shared `IGate` interface prematurely, AllCommand directly calls `CrapCommand.Execute()` and `ScrapCommand.Execute()` with their argument objects. This keeps the composition concrete until more gates motivate an abstraction.

---

## Open Questions

### Resolved During Planning

- **AllCommand creation timing**: Include in this plan — ADR-0002 explicitly requires it, and SCRAP is gate #2.
- **Shared Roslyn workspace**: Not needed — existing tool uses per-file parsing. SCRAP follows the same pattern.
- **Test framework scope**: TUnit only (from ADR-0003).

### Deferred to Implementation

- Exact method signature for `ScrapCommand.Execute()` — depends on how many analysis results the handler needs
- Exact table format for ScrapHandler output — depends on what per-file/per-example fields are most useful in practice
- Whether `--compare` needs the full baseline document or just the previous scores — implement the simpler version first, extend if needed

---

## High-Level Technical Design

> _This illustrates the intended approach and is directional guidance for review, not implementation specification. The implementing agent should treat it as context, not code to reproduce._

**SCRAP Analysis Pipeline (mirrors CRAP pipeline structure):**

```
TestMethodParser.FindTests(projectPath) → IReadOnlyList<TestMethod>
    │
    ├── TestNormalizer.Normalize(method) → NormalizedBody
    │       └── (SyntaxWalker: replace IdentifierName → placeholder,
    │           LiteralExpression → type placeholder, preserve kind)
    │
    ├── ScrapDuplication.Analyze(normalizedBodies) → ChannelResults
    │       ├── HarmfulDuplication (setup/assertion/arrange similarity ≥0.5)
    │       ├── CaseMatrixRepetition (low-complexity clusters)
    │       └── SubjectRepetition (same-API focus, lightly penalized)
    │
    ├── ExtractionPressure.Compute(channelResults) → double
    │       └── D_before = max(0, F-3) * (I-1)^1.5 / (V+1) for each cluster
    │
    └── ScrapScorer.Score(testMethods, duplication, pressure) → FileScrapReport
            ├── Per-example: complexity (saturating curve), assertion count,
            │   setup depth, line count, SCRAP score, smell labels
            ├── Per-file: avg/max SCRAP, extraction pressure, duplication scores,
            │   zero/low-assertion counts, branching ratio
            └── RecommendationEngine.Decide(fileReport) → (mode, actionability)
```

**Recommendation Decision Tree (from scrap source):**

```
STABLE?  → max-scrap ≤12, dup ≤3, zero-assertions=0, low-assertions ≤35%
         → OR: ≤2 examples with tighter bounds (max-scrap ≤10, dup ≤1)
    ↓ no
SPLIT?   → (avg-scrap ≥10 OR dup ≥20 OR subject-rep ≥12 OR helper-hidden>0)
         → AND examples ≥12 AND (high-pressure-blocks ≥2 OR max-scrap ≥35)
    ↓ no
LOCAL    → keep file together, fix assertions/duplication in place
```

---

## Implementation Units

- U1. **TestMethodParser — TUnit Test Discovery**

**Goal:** Discover and parse TUnit test methods from a project directory into `TestMethod` records with line spans and body syntax.

**Requirements:** R1, R2

**Dependencies:** None

**Files:**

- Create: `tools/src/redmuffin.Tools.QualityGates/Analysis/TestMethodParser.cs`
- Create: `tools/tests/redmuffin.Tools.QualityGates.Tests/Analysis/TestMethodParserTests.cs`

**Approach:**

- `public static IReadOnlyList<TestMethod> FindTests(string projectPath)` — mirrors `CyclomaticComplexity.Analyze()`
- Walk each `.cs` file via `CSharpSyntaxTree.ParseText()`, find `MethodDeclarationSyntax` nodes with `[Test]` attribute
- Extract method name, file path, start line, end line, full body syntax, and parent class name (for subject grouping)
- Record type `TestMethod(MethodName, FilePath, StartLine, EndLine, BodySyntax, ContainerClassName)`
- Ignore non-test methods (helpers, constructors, `[Before]`/`[After]` methods)
- Respect `--changed` filtering (via git diff, same pattern as `CrapCommand.FilterChangedFiles`)

**Patterns to follow:**

- `tools/src/redmuffin.Tools.QualityGates/Analysis/CyclomaticComplexity.cs` — per-file parsing, `DescendantNodes()`, file enumeration
- `tools/src/redmuffin.Tools.QualityGates/Commands/CrapCommand.cs` — `FilterChangedFiles` git-diff pattern

**Test scenarios:**

- Happy path: file with `[Test]` methods → returns them with correct spans; non-`[Test]` methods excluded
- Happy path: multiple test files in project → aggregates all
- Edge case: file with zero test methods → empty list for that file
- Edge case: empty test project directory → empty overall result
- Edge case: `[Test]` method inside a non-partial class → still discovered
- Error path: unparseable `.cs` file → skip with warning, continue

**Verification:**

- Parser returns correct method count for a sample TUnit test file with known structure
- Line spans match actual method boundaries

---

- U2. **TestNormalizer — Syntax-Node Normalization**

**Goal:** Normalize `MethodDeclarationSyntax` bodies into structural feature sets for fuzzy comparison, preserving AST shape while abstracting identifier names and literal values.

**Requirements:** R3

**Dependencies:** None (consumes raw Roslyn syntax, no U1 dependency for its own logic — U1 provides the input at integration time)

**Files:**

- Create: `tools/src/redmuffin.Tools.QualityGates/Analysis/TestNormalizer.cs`
- Create: `tools/tests/redmuffin.Tools.QualityGates.Tests/Analysis/TestNormalizerTests.cs`

**Approach:**

- `SyntaxWalker` subclass that visits `IdentifierNameSyntax` → replaces with `"$id"`, `LiteralExpressionSyntax` → `"$str"`/`"$num"`/`"$bool"` by kind
- `InvocationExpressionSyntax` → preserves member access chain shape (e.g., `Assert.That(...).IsNotNull()` normalizes to `$id.$id(...).$id()` — distinguishable from `Assert.That(...).IsEqualTo(...)` which normalizes to `$id.$id(...).$id($id)`)
- `BlockSyntax` / statement boundaries preserved — structure matters for Jaccard
- Output: `NormalizedBody` record with a list of feature tokens (syntax-kind-preserving strings)
- Comments and whitespace are NOT included in normalized output (mirrors Clojure scrap's comment/wrapping independence)

**Patterns to follow:**

- `tools/src/redmuffin.Tools.QualityGates/Analysis/CyclomaticComplexity.cs` — `CSharpSyntaxWalker` subclass, override `Visit*` pattern

**Test scenarios:**

- Happy path: two methods with same structure, different variable names → normalize to identical feature sets
- Happy path: two methods with different assertion shapes → normalize to different feature sets
- Edge case: empty method body → empty feature set
- Edge case: method with only comments → empty feature set (comments excluded)
- Edge case: nested lambda expressions → structure preserved in normalized output
- Error path: (none — normalization should not fail on any valid syntax)

**Verification:**

- Normalized output for `Assert.That(x).IsNotNull()` differs from `Assert.That(x).IsEqualTo(y)`
- Same-structured methods with different local variable names produce identical normalized output

---

- U3. **ScrapDuplication — Jaccard Similarity and Channel Detection**

**Goal:** Compute pair-wise Jaccard similarity on normalized test bodies, classify matches into three duplication channels (harmful, case-matrix, subject), and produce channel results used by extraction pressure.

**Requirements:** R3, R4

**Dependencies:** U2 (TestNormalizer provides normalized bodies)

**Files:**

- Create: `tools/src/redmuffin.Tools.QualityGates/Analysis/ScrapDuplication.cs`
- Create: `tools/tests/redmuffin.Tools.QualityGates.Tests/Analysis/ScrapDuplicationTests.cs`

**Approach:**

- `JaccardSimilarity(setA, setB)` → `intersection.Count / union.Count` — threshold 0.5 per scrap `policy.clj`
- For each pair of test methods in the same file, compute Jaccard on normalized feature sets
- Cluster connected components (transitive pairs all ≥0.5)
- Classify each cluster:
  - **Harmful duplication**: shared setup/assertion/arrange scaffolding — cluster has ≥3 shared forms AND ≤4 variable points. Split into sub-scores: setup-duplication, assertion-duplication, arrange-duplication (based on which sections of the normalized bodies overlap)
  - **Case-matrix repetition**: low-complexity examples (scrap ≤18, lines ≤12, assertions ≤1, branches ≤0, setup-depth ≤2 per `duplication-policy`) that form a coverage table
  - **Subject repetition**: cluster focuses on the same `ContainerClassName` — lightly penalized per `policy.clj` (subject-repetition-score, split threshold 12)
- Record types: `DuplicationChannel(ClusterId, Methods, SharedForms F, VariablePoints V, InstanceCount I, ChannelType)`, `DuplicationResults(HarmfulDuplication[], CaseMatrixRepetition[], SubjectRepetition[], EffectiveDuplicationScore)`
- Effective duplication score = sum of harmful cluster D_before values (computed in U4) minus matrix credit (1.5 per case-matrix cluster per `pressure-policy`)

**Patterns to follow:**

- Use `HashSet<string>` for feature sets — Jaccard is set-based
- Sort results by cluster size descending (for handler output)

**Test scenarios:**

- Happy path: two structurally identical tests (different values) → Jaccard ≥0.5, harmful duplication cluster
- Happy path: three low-complexity tests with same structure → case-matrix cluster
- Happy path: four tests in same class with different structures → subject repetition, not harmful
- Edge case: single test in file → no clusters, no pressure
- Edge case: Jaccard exactly 0.5 → treated as match (≥ threshold)
- Edge case: tests across different files → NOT compared (per-file analysis only, matching scrap behavior)
- Edge case: cluster with F ≤3 → excluded from harmful (D_before = 0 guard)

**Verification:**

- Known similar test pair produces Jaccard ≥0.5
- Known dissimilar test pair produces Jaccard <0.5
- Case-matrix classification fires on low-complexity clusters matching matrix policy thresholds

---

- U4. **ExtractionPressure — D_before Formula and Net Benefit**

**Goal:** Compute extraction pressure per duplication cluster and per file using Uncle Bob's formula, estimating whether helper extraction is net beneficial.

**Requirements:** R4

**Dependencies:** U3 (ScrapDuplication provides cluster data with F, I, V)

**Files:**

- Create: `tools/src/redmuffin.Tools.QualityGates/Analysis/ExtractionPressure.cs`
- Create: `tools/tests/redmuffin.Tools.QualityGates.Tests/Analysis/ExtractionPressureTests.cs`

**Approach:**

- `ComputeDefore(F, I, V)` → `0` if `F ≤ 3` or `V > 4`, else `(max(0, F - 3) * (I - 1)^1.5) / (V + 1)`
- `ComputeExtractionPressure(cluster)` → `max(0, D_before - D_after - H)` where:
  - `D_after` = 0 (current scrap treats post-extraction cost as 0)
  - `H` = helper cost estimated from shared and variable structure: `(F * 0.5 + V * 0.3)` — a conservative estimate for the cost of the extracted helper itself
- `ComputeFilePressure(duplicationResults)` → sum of extraction pressure across all harmful clusters, minus matrix credit (1.5 × case-matrix-cluster-count)
- Output: `FilePressure(TotalExtractionPressure, ClusterPressures[], MatrixCredit, NetPressure)`

**Patterns to follow:**

- Pure static computation — no I/O, no Roslyn
- Use `double` for all values (extraction pressure is continuous)

**Test scenarios:**

- Happy path: F=5, I=3, V=2 → D_before = (2 \* 2^1.5) / 3 ≈ 1.886
- Happy path: F=3, I=4, V=1 → D_before = 0 (F ≤ 3 guard)
- Happy path: F=6, I=2, V=5 → D_before = 0 (V > 4 guard)
- Edge case: F=4, I=1 → D_before = 0 ((I-1) = 0)
- Edge case: large cluster F=20, I=10, V=3 → D*before = (17 * 9^1.5) / 4 = (17 \_ 27) / 4 = 114.75
- Edge case: single cluster with negative net pressure after helper cost → pressure = 0 (max with 0)
- Integration: two harmful clusters → total pressure = sum of individual pressures, minus matrix credits

**Verification:**

- Manual computation matches for known inputs
- Guard conditions (F≤3, V>4) produce zero

---

- U5. **ScrapScorer — Per-Example and Per-File Metrics**

**Goal:** Compute per-example SCRAP metrics (complexity, assertion count, setup depth, smell labels, SCRAP score) and aggregate into per-file summaries matching Uncle Bob's scrap report structure.

**Requirements:** R2, R5

**Dependencies:** U1 (TestMethodParser), U3 (ScrapDuplication), U4 (ExtractionPressure)

**Files:**

- Create: `tools/src/redmuffin.Tools.QualityGates/Analysis/ScrapScorer.cs`
- Create: `tools/tests/redmuffin.Tools.QualityGates.Tests/Analysis/ScrapScorerTests.cs`

**Approach:**

- Per-example metrics:
  - **Line count**: `EndLine - StartLine + 1`
  - **Raw line count**: total lines including blank lines (for smell detection)
  - **Complexity score**: saturating curve: `min(cap, floor + riseRate * structuralComplexity)` where cap=25.0, riseRate=0.18, floor=1.0
  - **Structural complexity**: number of decision points in the test body (branches, loops, try/catch) — reuses `CyclomaticComplexity` logic scoped to the method body
  - **Assertion count**: count `Assert.That(...)` invocations via Roslyn syntax walk
  - **Setup depth**: count nested using/constructor calls before first assertion
  - **Zero-assertion**: assertion count = 0
  - **Low-assertion**: assertion count = 1 (per scrap, one-assertion tests are flagged)
  - **Branching**: boolean — does the test have branches
  - **SCRAP score**: `complexityScore + smellPenalties` where smell penalties are summed from: zero-assertion (+5), low-assertion (+2), branching (+1 per branch), high-setup-depth (+1 per level above 2)
- Per-file aggregation:
  - Example count, avg/max SCRAP, branching/low/zero assertion counts
  - Duplication scores (harmful, effective, subject, case-matrix)
  - Extraction pressure score
  - Worst examples (top 5 by SCRAP score)
- Record types: `TestMethodMetrics(TestMethod, LineCount, ComplexityScore, AssertionCount, SetupDepth, BranchCount, ScrapScore, SmellLabels[])`, `FileScrapReport(FilePath, ExampleCount, AvgScrap, MaxScrap, Metrics[], DuplicationResults, ExtractionPressure, SmellCounts)`

**Patterns to follow:**

- `tools/src/redmuffin.Tools.QualityGates/Analysis/MethodCrap.cs` — record pattern for per-method results

**Test scenarios:**

- Happy path: simple test with 3 assertions, no branches → low scrap score, no smells
- Happy path: test with 0 assertions → scrap score includes zero-assertion penalty (+5), smell label "ZERO_ASSERTION"
- Happy path: test with 1 assertion → low-assertion smell (+2), NOT zero-assertion
- Edge case: empty test method → assertion count = 0, zero-assertion smell, complexity = floor (1.0)
- Edge case: test with 50 lines and 10 branches → complexity saturates at cap (25.0), not quadratic
- Edge case: file with 2 examples, both zero-assertion → zero-assertion ratio = 1.0, NOT stable
- Integration: file with 20 examples, avg-scrap=12 → meets SPLIT threshold (avg-scrap ≥10)

**Verification:**

- Complexity never exceeds 25.0 (cap)
- Zero-assertion test scores higher than same test with 2 assertions
- Smell labels match expected for known test structures

---

- U6. **RecommendationEngine — STABLE/LOCAL/SPLIT and AI-Actionability**

**Goal:** Classify each test file as STABLE, LOCAL, or SPLIT, and assign an AI-actionability class (LEAVE_ALONE, AUTO_TABLE_DRIVE, AUTO_REFACTOR, MANUAL_SPLIT, REVIEW_FIRST) using exact scrap source thresholds.

**Requirements:** R5

**Dependencies:** U5 (ScrapScorer provides FileScrapReport)

**Files:**

- Create: `tools/src/redmuffin.Tools.QualityGates/Analysis/ScrapRecommender.cs`
- Create: `tools/tests/redmuffin.Tools.QualityGates.Tests/Analysis/ScrapRecommenderTests.cs`

**Approach:**

- `ClassifyStability(fileReport)` → STABLE if:
  - Small files (≤2 examples): max-scrap ≤10, effective-duplication ≤1, zero-assertions=0, no helper-hidden
  - General files: max-scrap ≤12, effective-duplication ≤3, zero-assertion ratio = 0.0, low-assertion ratio ≤0.35
- `ClassifySplit(fileReport)` → SPLIT if:
  - NOT stable, AND
  - (avg-scrap ≥10 OR effective-duplication ≥20 OR subject-repetition ≥12 OR helper-hidden > 0), AND
  - example-count ≥12, AND
  - (high-pressure-blocks ≥2 OR max-scrap ≥35)
- Otherwise → LOCAL
- `ClassifyActionability(mode, fileReport)` → per `actionability-modes.clj`:
  - STABLE → LEAVE_ALONE
  - LOCAL + (zero-assertion>0 OR low-assertion>40% OR harm-dup>0 OR max-scrap>20) AND branching≤30% AND mocking<35% → AUTO_REFACTOR
  - SPLIT → MANUAL_SPLIT
  - Case-matrix heavy: candidates>0, case-matrix-repetition ≥ max(2, harm-dup/3), max-scrap≤12, branching≤15%, mocking<20% → AUTO_TABLE_DRIVE
  - Fallthrough → REVIEW_FIRST
- Output: `Recommendation(Mode, AiActionability, ActionabilityMessage, TopBlocks[], TopExamples[])`

**Patterns to follow:**

- Pure static logic — no I/O
- All thresholds as named constants (matching scrap `policy.clj` field names)
- `ScrapRecommender` as static class with `Decide(FileScrapReport)` entry point

**Test scenarios:**

- Happy path: file with 5 examples, max-scrap=8, dup=2, zero-assert=0, low-assert=20% → STABLE
- Happy path: file with 3 examples, one zero-assertion → not STABLE (zero-assertion ratio > 0), LOCAL → AUTO_REFACTOR
- Happy path: file with 15 examples, avg-scrap=11, 3 high-pressure blocks → SPLIT → MANUAL_SPLIT
- Edge case: file with 2 examples, max-scrap=11 → not STABLE (small-file threshold ≤10)
- Edge case: file that is LOCAL but has branching ratio >30% → REVIEW_FIRST (not AUTO_REFACTOR)
- Edge case: case-matrix candidate file → AUTO_TABLE_DRIVE
- Integration: STABLE file with 0 examples → should not be possible (parser returns empty, not 0-count), but guard anyway

**Verification:**

- Boundary value: max-scrap=12 exactly → STABLE (≤12)
- Boundary value: low-assertion ratio=0.35 exactly → STABLE (≤0.35)
- Boundary value: avg-scrap=10 exactly → triggers SPLIT (≥10)

---

- U7. **ScrapCommand and ScrapHandler — CLI Layer**

**Goal:** Wire the SCRAP analysis pipeline behind a System.CommandLine subcommand, following the exact CrapCommand/CrapHandler pattern.

**Requirements:** R6, R8

**Dependencies:** U1, U3, U4, U5, U6 (all analysis components)

**Files:**

- Create: `tools/src/redmuffin.Tools.QualityGates/Commands/ScrapCommand.cs`
- Create: `tools/src/redmuffin.Tools.QualityGates/Commands/ScrapHandler.cs`
- Create: `tools/tests/redmuffin.Tools.QualityGates.Tests/Commands/ScrapCommandTests.cs`

**Approach:**

- `ScrapCommand.Create()` → returns `Command("scrap", "Analyze test structural quality")`
  - `--test-project` (`DirectoryInfo`, Required) — path to test project
  - `--verbose` (`bool`, default false) — full metric dump
  - `--json` (`bool`, default false) — machine-readable output
  - `--changed` (`bool`, default false) — only test files changed since HEAD
  - `--write-baseline` (`bool`, default false) — write baseline to target/scrap/
  - `--compare` (`string?`) — path to baseline for comparison
  - `--stability-threshold` (`double`, default scrap policy value) — override with note that policy values are canonical
- `ScrapCommand.Execute(projectPath, verbose, json, changed, writeBaseline, comparePath)` → pipeline:
  1. `TestMethodParser.FindTests(projectPath)` (filter with git diff if `--changed`)
  2. Normalize bodies, run duplication analysis, compute extraction pressure
  3. Score per-example and per-file via `ScrapScorer`
  4. Classify via `ScrapRecommender`
  5. Route to `ScrapHandler.Run(reports, options)`
- `ScrapHandler`:
  - `Run(IReadOnlyList<FileScrapReport>, ScrapOptions, TextWriter?)` → exit code
  - Default output: one line per file (path, mode, actionability, avg-scrap, worst example)
  - `--verbose`: per-example table + per-file summary
  - `--json`: serialize as JSON
  - `--write-baseline`: write `target/scrap/{file-hash}.json`
  - `--compare`: load baseline, compute deltas, output comparison verdict (improved/worse/mixed/unchanged)
  - Exit code: 2 if any file is SPLIT or has actionability != LEAVE_ALONE, 1 on error, 0 on all STABLE

**Execution note:** Implement U7 last (after U1-U6 pass their own tests), but test with integration scenarios that wire the full pipeline.

**Patterns to follow:**

- `tools/src/redmuffin.Tools.QualityGates/Commands/CrapCommand.cs` — option creation, `SetAction`, `Execute` structure
- `tools/src/redmuffin.Tools.QualityGates/Commands/CrapHandler.cs` — `Run` with `TextWriter?` parameter, sort + table output
- `tools/tests/redmuffin.Tools.QualityGates.Tests/Commands/CrapCommandTests.cs` — command test pattern

**Test scenarios:**

- Happy path: `scrap --test-project tests/Foo` on a test project with well-structured tests → exit 0, STABLE for all files
- Happy path: `scrap --verbose` → per-example metrics printed
- Happy path: `scrap --json` → valid JSON output with file reports
- Happy path: `scrap --changed` → only tests modified since HEAD analyzed
- Edge case: `--test-project` points to non-existent directory → exit 1, error message
- Edge case: `--test-project` points to directory with no test files → exit 0, "no tests found"
- Edge case: `--compare` without prior `--write-baseline` → exit 1, error message
- Error path: unparseable source file → skip with warning, continue analyzing other files

**Verification:**

- Running against `tests/redmuffin.Blazor.StaticWeb.Tests/` produces a report with file-level recommendations
- `--json` output is parseable JSON matching the `FileScrapReport` structure

---

- U8. **AllCommand — Composite Gate Runner**

**Goal:** Run CRAP and SCRAP sequentially with unified pass/fail, fulfilling the ADR-0002 requirement for one command, one report, one exit code.

**Requirements:** R7

**Dependencies:** U7 (ScrapCommand), existing `CrapCommand`

**Files:**

- Create: `tools/src/redmuffin.Tools.QualityGates/Commands/AllCommand.cs`
- Create: `tools/tests/redmuffin.Tools.QualityGates.Tests/Commands/AllCommandTests.cs`

**Approach:**

- `AllCommand.Create()` → returns `Command("all", "Run all quality gates")`
  - `--project` (`DirectoryInfo`, Required) — source project for CRAP
  - `--test-project` (`DirectoryInfo`, Required) — test project for SCRAP
  - `--coverage-file` (`FileInfo`, Required) — Cobertura XML for CRAP
  - `--changed` (`bool`, default false) — incremental mode for both gates
  - `--verbose` (`bool`, default false) — pass through to both gates
  - No `--json` for `all` — each gate's output is its own (CRAP uses table, SCRAP uses text/JSON)
- `AllCommand.Execute(projectPath, testProjectPath, coveragePath, changed, verbose)`:
  1. Run `CrapCommand.Execute(projectPath, coveragePath, maxCrap: 8, changed)` — capture exit code, print CRAP section header
  2. Run `ScrapCommand.Execute(testProjectPath, verbose: verbose, json: false, changed, writeBaseline: false, comparePath: null)` — capture exit code, print SCRAP section header
  3. Print summary: "CRAP: PASS/FAIL | SCRAP: PASS/FAIL | Overall: PASS/FAIL"
  4. Return non-zero exit code if any gate failed (2 if any failed with threshold breach, 1 if any errored)
- Stub output for future gates: print "Duplication: NOT YET IMPLEMENTED" and "Architecture: NOT YET IMPLEMENTED" with exit 0 (not blocking)

**Patterns to follow:**

- Direct `CrapCommand.Execute()` and `ScrapCommand.Execute()` calls — no `IGate` abstraction yet
- Section headers in output to distinguish gate results

**Test scenarios:**

- Happy path: both CRAP and SCRAP pass → exit 0, "Overall: PASS"
- Happy path: CRAP passes, SCRAP fails (SPLIT file) → exit 2, "Overall: FAIL"
- Edge case: CRAP fails with exit 2, SCRAP passes → exit 2
- Edge case: CRAP errors (exit 1), SCRAP passes → exit 1
- Edge case: `--changed` passed through to both gates → each gate filters independently
- Integration: `all` output clearly separates CRAP and SCRAP sections

**Verification:**

- `dotnet quality-gates all --project src/... --test-project tests/... --coverage-file coverage.xml` runs both gates
- Exit code reflects worst-case result

---

- U9. **Wire into Program.cs and Update Documentation**

**Goal:** Register ScrapCommand and AllCommand in Program.cs, update tools/README.md gates table.

**Requirements:** R6, R7

**Dependencies:** U7 (ScrapCommand), U8 (AllCommand)

**Files:**

- Modify: `tools/src/redmuffin.Tools.QualityGates/Program.cs`
- Modify: `tools/README.md`

**Approach:**

- Program.cs: add `rootCommand.Subcommands.Add(ScrapCommand.Create());` and `rootCommand.Subcommands.Add(AllCommand.Create());`
- README.md: update gates table — SCRAP status from "Planned" to "Done", All status from "Planned" to "Done", add SCRAP CLI example
- No test file needed — registration is integration wiring

**Test expectation:** none — registration is declarative wiring. Build verification confirms it compiles.

**Verification:**

- `dotnet build tools/src/redmuffin.Tools.QualityGates` succeeds
- `dotnet quality-gates --help` lists `scrap` and `all` subcommands
- `dotnet quality-gates scrap --help` shows correct options

---

- U10. **Pack, Install, and Integration Smoke Test**

**Goal:** Pack the updated tool, install locally, and run against the repo's actual test project to verify end-to-end.

**Requirements:** R1-R8 (all)

**Dependencies:** U1-U9 (complete implementation)

**Files:**

- No new source files — this is a verification unit

**Approach:**

1. `dotnet pack tools/src/redmuffin.Tools.QualityGates --output tools/nupkgs`
2. `dotnet tool update redmuffin.Tools.QualityGates --tool-manifest .config/dotnet-tools.json --add-source ./tools/nupkgs`
3. Run `dotnet quality-gates scrap --test-project tests/redmuffin.Blazor.StaticWeb.Tests` — verify produces meaningful per-file recommendations
4. Run `dotnet quality-gates all --project src/redmuffin.Blazor.StaticWeb --test-project tests/redmuffin.Blazor.StaticWeb.Tests --coverage-file <path>` — verify unified output
5. `dotnet test tools/tests/redmuffin.Tools.QualityGates.Tests` — all tests pass
6. Address any threshold-tuning issues surfaced by real repo data

**Execution note:** Run this after code review, before declaring the gate operational. Real repo test data may reveal threshold sensitivity that unit tests don't capture.

**Patterns to follow:**

- `tools/README.md` quick start and pack/install instructions

**Test expectation:** none — this is manual integration verification against real repo code.

**Verification:**

- All tool tests pass
- `scrap` produces plausible recommendations against real test files
- `all` runs both gates without errors

---

## System-Wide Impact

- **Interaction graph:** `Program.cs` gains two new subcommand registrations. `AllCommand` calls into `CrapCommand.Execute()` and `ScrapCommand.Execute()` directly — no callback or middleware surface.
- **Error propagation:** Each gate handles its own errors. `AllCommand` captures exit codes and propagates the worst. Gate-level errors do not prevent subsequent gates from running.
- **State lifecycle risks:** None — the tool is stateless per invocation. Baselines (`--write-baseline`/`--compare`) write to `target/scrap/` only.
- **API surface parity:** ScrapCommand CLI is parallel to CrapCommand CLI (same option style, same `--changed` behavior, same exit code convention).
- **Integration coverage:** Full pipeline integration smoke test (U10) verifies CRAP + SCRAP composition and real-repo data handling.
- **Unchanged invariants:** CrapCommand behavior is unchanged. `dotnet tool` install/update workflow is unchanged. Tool csproj is unchanged (no new packages). `Directory.Packages.props` is unchanged.

---

## Risks & Dependencies

| Risk                                                                                                                                                | Mitigation                                                                                                                                                  |
| --------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------- |
| SCRAP thresholds from scrap `policy.clj` are tuned for Clojure/Speclj, not C#/TUnit — may produce false positives/negatives against real repo tests | U10 smoke test against actual test files; `--stability-threshold` and `--split-*` overrides exposed in CLI for tuning                                       |
| Jaccard on C# syntax-node features may produce different similarity distributions than Clojure form-based Jaccard                                   | TestNormalizer preserves AST structure (not token stream) which is the closest analog; verify with known-similar and known-different test pairs in U3 tests |
| `CrapCommand.Execute()` is `internal static` — AllCommand must be in same assembly or use `InternalsVisibleTo`                                      | AllCommand lives in same `redmuffin.Tools.QualityGates` project — no assembly boundary                                                                      |
| Tool must be re-packed after every change (`dotnet pack`) before testing — easy to test stale version                                               | U10 explicitly calls out pack + install; add reminder to README                                                                                             |

---

## Sources & References

- **Origin document:** `docs/adr/0003-scrap-test-structural-analyzer.md`
- Uncle Bob scrap source: `https://github.com/unclebob/scrap` (policy.clj, pressure_stability.clj, pressure_mode.clj, actionability_modes.clj)
- Uncle Bob skill: `.opencode/skills/rm-uncle-bob-martin-agentic-coding/SKILL.md`
- ADR-0002: `docs/adr/0002-quality-gates-toolchain.md`
- CRAP plan: `docs/plans/2026-05-09-001-feat-quality-gates-crap-analyzer-plan.md`
- Related code: `tools/src/redmuffin.Tools.QualityGates/Commands/CrapCommand.cs`, `.../Analysis/CyclomaticComplexity.cs`
