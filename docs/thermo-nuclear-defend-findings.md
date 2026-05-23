---
date: 2026-05-23
module: tools
tags: [thermo-nuclear, audit, defend, refactoring]
problem_type: code review
---

# Thermo-Nuclear Audit — Complete DEFEND Log

All 20 findings classified as DEFEND during the tools solution audit.
Each includes the author-cited rationale so decisions can be reversed with
full context. Ordered by batch then severity.

---

## B1 — Mutation Pipeline (4 defended)

### B1-W5: `MutateConstant` silently passes through

**Decision:** By design. No current mutation rule matches `const`/literal
expressions — the class generates zero sites.
**Trigger to revisit:** Add a constant-mutation rule (e.g., swap `true`/`false`).
Then `VisitLiteralExpression` will produce sites with zero code changes.

### B1-CJ1: Delegate transform to `MutationRule` itself

**Decision:** Cross-batch architectural change. Touches 18 sites across 6 rules.
Ceremony > benefit for a single-batch audit.
**Trigger to revisit:** When adding a new mutation category that needs custom
transform logic — refactor the visitor dispatch then.

### B1-CJ2: In-memory mutation + temp file instead of disk backup/restore

**Decision:** Requires validating `dotnet build` behavior with in-memory
compilation. High risk of silent build differences between in-memory and
on-disk compilation (Roslyn emits slightly different IL than csc).
**Trigger to revisit:** When mutation runner performance is measured as the
bottleneck. The disk I/O per mutant is negligible compared to `dotnet run`.

### B1-CJ3: `IAsyncEnumerable<MutationResult>` return type

**Decision:** Changes public API contract of `MutationRunner.RunAsync`.
No simplification at the single call site — caller materializes all results
anyway. The `IAsyncEnumerable` would add ceremony (CancellationToken,
disposal) without enabling streaming consumption.

---

## B2 — Scrap Pipeline (2 defended)

### B2-W3: Syntax-only attribute detection (no semantic model)

**Decision:** Intentional design choice. Semantic model adds 30-60s startup per
file. The normalized syntax tree comparison (Jaccard similarity) doesn't need
type resolution — it compares AST shapes, not type identities.
**Trigger to revisit:** If semantic info becomes needed (e.g., detecting
methods that call identical library functions with different overloads).

### B2-W5: Union-by-rank cluster merging for ≤dozens of methods

**Decision:** The algorithm operates on ≤50 methods per file. O(n) merge with
a dictionary is equivalent to union-by-rank at this scale.
**Trigger to revisit:** If SCRAP is run on files with 1000+ test methods
(large integration test suites). The current approach handles everything
we've seen in the codebase.

---

## B3 — Dupes + Coverage Pipeline (5 defended)

### B3-B1: Untyped `List<object>` in `CSharpTreeNormalizer`

**Decision:** Mirrors dry4clj's original algorithm which uses untyped
Clojure data. All consumers of the normalized tree are in the same file,
~30 lines apart. A typed wrapper record adds ceremony with zero
bug-catching benefit.
**Trigger to revisit:** If normalizer output is consumed from outside
the file (>30 lines away, different class).

### B3-O5: Coverage threshold `CC≤3 && coverage≥1%` → conductor

**Decision:** Designed threshold calibrated against real code. A method with
CC=3 and any coverage is structurally a delegation method (calls sub-methods,
each call adds a branch). The 1% threshold exists because Cobertura
attributes a single line hit to the entire method body.
**Trigger to revisit:** If false positives emerge (methods classified as
conductor that clearly contain business logic).

### B3-CJ3: File grouping in coverage gap detector

**Decision:** Pre-optimization. The detector processes ~50 methods across
~15 files. Grouping by file path before reading eliminates O(m) file reads
but adds code complexity for negligible performance difference.
**Trigger to revisit:** If the CRAP gate is run on a solution with 500+
source files — then the O(files) file reads matter.

### B3-CJ4: Typed normalizer record instead of `List<object>`

**Decision:** Same rationale as B3-B1. The normalizer produces an intermediate
tree consumed only by the fingerprint computer 20 lines later. Ceremony > benefit.

### B3-W4/Jaccard: Repeated file reads in DupesDetector

**Decision:** Same analysis as B3-CJ3. For the current scale (~20 files per
tool project), grouping adds code complexity with no measurable performance gain.

---

## B4 — CRAP Pipeline (7 defended)

### B4-B2: `TestClassDiscovery.Discover` returns bare class name

**Decision:** Correct for its sole caller. The return value is used as a
TUnit `--treenode-filter` argument (`"/*/*/{ClassName}/*"`). Returning a
full path would break the filter format — TUnit expects just the class name.
**Trigger to revisit:** If the method gets a second caller that needs
a full file path. Then split into two methods.

### B4-W4: Line-number coordinate comment should be a value type

**Decision:** Adding a `LineNumber` struct with `FromRoslyn`/`FromCobertura`
constructors for exactly 2 usages (both in `PartitionByCoverage`) adds
ceremony with no error-prevention benefit. The `+ 1` offset is a single
line with a comment — self-documenting.
**Trigger to revisit:** If line-number offsets appear in 5+ locations
across the codebase.

### B4-O2: `MethodMapper` should be renamed `CrapCalculator`

**Decision:** The call site reads `MethodMapper.Map(complexity, coverage)` —
the name is honest. CRAP is already in the return type `MethodCrap`.
Renaming breaks call-site readability.

### B4-O4: `CyclomaticComplexity.Analyze` mixes IO, parsing, analysis

**Decision:** The method IS a file-system walker by design. Extracting a
`ParseSourceFiles` method to enable in-memory testing would add abstraction
for a tool where file I/O is the purpose. Tests inject via temp directories.
**Trigger to revisit:** When writing characterization tests for the CC
calculator — then a parse-only extraction becomes test infrastructure.

### B4-O7: No XML namespace handling in Cobertura parsing

**Decision:** Our pipeline exclusively uses `Microsoft.Testing.Extensions.
CodeCoverage` which produces namespace-less Cobertura XML. Adding
namespace-agnostic queries is defensive coding against a format we
never encounter.
**Trigger to revisit:** If the coverage pipeline changes to use
ReportGenerator or coverlet (both add `xmlns` attributes).

### B4-J2: Replace `CcWalker` with `FrozenSet<SyntaxKind>`

**Decision:** Pattern-based syntax nodes (`AndPattern`, `OrPattern`,
`NotPattern`, `SuppressNullableWarning`, `CoalesceAssignment`) can't be
expressed as simple `node.IsKind()` checks. Each needs per-node logic
(checking the operator kind within the pattern/assignment). The walker
with per-node overrides is the correct abstraction.
**Trigger to revisit:** If C# adds simpler decision-point syntax that
maps 1:1 to `SyntaxKind` values. The walker approach handles both
simple and composite decision points.

### B4-J3: Drop `ComputeDefore` — inline into `ComputeExtractionPressure`

**Decision:** Kept as documentation. The method name matches Uncle Bob's
formula naming (`D_before`). Debugging extraction pressure values benefits
from seeing the `D_before` component separately (before helper costs).
**Trigger to revisit:** If the method grows unused after extraction
pressure computation matures.

---

## B5 — Command Orchestration (2 defended)

### B5-W3: Sequential orchestration when gates are independent

**Decision:** CRAP auto-generates coverage (spawns `dotnet run`, writes to
shared coverage file in temp). Running it concurrently with file-scanning
gates (Architecture, Depth, Duplicates) creates IO contention on the
temporary coverage file. Sequential is correct until coverage generation
is separated from analysis.
**Trigger to revisit:** After extracting CoverageRunner (surfaced finding) —
then all file-scanning gates CAN run in parallel.

### B5-W6: First-file-wins on multiple `.slnx` in directory

**Decision:** Multiple `.slnx` files in the same directory is a rare scenario.
The `--solution` flag already provides explicit override for ambiguity.
**Trigger to revisit:** If a CI/CD scenario produces multiple `.slnx`
files in the same directory.

---

## B6 — Command + Handler Layer (1 defended)

### B6-8: 3 different `SetAction` lambda styles

**Decision:** Cosmetic. Field vs inline lambda has zero behavioral difference.
Normalizing is a whitespace-only change. No author has a strong position
on field lambda vs inline lambda style (it's team convention).
**Trigger to revisit:** If the inconsistency causes an actual bug
(e.g., closure capture difference, reuse vs reallocation).

---

## B7 — Architecture + Mutation (2 defended)

### B7-6: Hardcoded `/` and `\\` path separators in `ProjectGraph`

**Decision:** The method checks for `/bin/` and `/obj/` in paths. Both
separators are covered. Using `Path.DirectorySeparatorChar` would
produce the same check in practice for any OS.
**Trigger to revisit:** If .NET introduces a platform where `/bin/`
and `\bin\` are both valid but `Path.DirectorySeparatorChar` is neither.

### B7-3: Hand-rolled JSON serialization

**Decision:** Author: Uncle Bob (single source of truth). Replacing 60 lines
of hand-rolled JSON with `JsonSerializer.Serialize` is architecturally
correct. Deferred because: (1) the formatter output is user-facing text
not API contract — changing the JSON structure requires coordination,
(2) hand-rolled JSON has no known bugs in current usage.
**Trigger to revisit:** When any new field is added to `ArchResult`
requiring manual JSON wiring. Then the maintenance cost becomes real
and `JsonSerializer` should replace it.

---

## B8 — Program.cs (1 defended)

### B8-W2: No top-level exception handling

**Decision:** Each subcommand's `Create()` factory and `Execute` handler
already catches its own errors and returns non-zero exit codes. A top-level
catch-all would only fire for framework-level failures (e.g.,
`System.CommandLine` internal exceptions) which provide their own
diagnostics.
**Trigger to revisit:** If a framework exception produces a misleading
error message (raw stack trace with no exit code).

---

## Summary

| Batch | Defended | Categories                                                                                           |
| ----- | -------- | ---------------------------------------------------------------------------------------------------- |
| B1    | 4        | Future-rule infrastructure, cross-batch scope, high-risk optimization, API contract                  |
| B2    | 2        | Intentional design, scale-appropriate algorithm                                                      |
| B3    | 5        | Original algorithm fidelity, calibrated threshold, scale-appropriate, ceremony > benefit             |
| B4    | 7        | Caller-correct behavior, scale-appropriate abstraction, intentional architecture, algorithm fidelity |
| B5    | 2        | Sequential-by-necessity, rare-scenario override exists                                               |
| B6    | 1        | Cosmetic (no behavioral impact)                                                                      |
| B7    | 2        | Both separators covered, deferred architectural improvement                                          |
| B8    | 1        | Subcommands handle their own errors                                                                  |

**Pattern:** Most DEFENDs fall into three categories:

1. **Scale-appropriate** — optimization/abstraction that would help at 10x size but adds complexity at current scale
2. **Intentional design** — behavior correct for the caller, reviewer's fix would break the existing contract
3. **Deferred architectural improvement** — correct refactoring judged worth doing later, not in this batch
