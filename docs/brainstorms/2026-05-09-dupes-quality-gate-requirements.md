---
date: 2026-05-09
topic: dupes-quality-gate
status: superseded
superseded_by: >-
  The production structural DRY gate shipped as `duplicates` (port of dry4clj /
  dry4java / dry4go). SCRAP still owns test-body Jaccard duplication only.
  This brainstorm's premise that "there is no separate production-code
  duplication scanner" is false. Canonical inventory: tools/README.md;
  sync map: docs/solutions/tooling-decisions/uncle-bob-quality-gates-upstream-sync.md.
---

# Duplication Quality Gate (dupes)

## Summary

Add a `dupes` subcommand to the Quality Gates tool that scans all C# methods
across the source project, normalizes them via Roslyn, computes pairwise
Jaccard similarity, clusters structurally similar methods at ≥0.5 similarity,
and reports each cluster with file locations and scores.

---

## Problem Frame

The CRAP gate catches complex/untested production code; SCRAP catches
structural weaknesses in tests. Neither catches copy-pasted logic scattered
across the codebase — utility methods duplicated in `ImageService.cs`,
`VideoHelpers.cs`, and `ArticleUtils.cs` that should be consolidated into a
single shared module.

Uncle Bob's discipline requires near-zero structural duplication as a
quality gate. Without automated detection, duplication accumulates silently:
a developer copies a helper method, renames a variable, and the copy-paste
is invisible to CRAP (complexity is low) and SCRAP (it doesn't scan source).

The dupes gate closes this gap by detecting structurally similar method
bodies across the entire source tree, regardless of naming, namespace, or
file location.

---

## Actors

- A1. **Developer**: Runs the dupes gate locally or in CI to find duplication
  clusters before they accumulate.
- A2. **Quality Gates Pipeline Agent**: Runs `all` (which composes crap,
  scrap, and dupes) as part of a metrics-driven development cycle. Exits
  non-zero if any gate breaches.

---

## Key Flows

- F1. **Run dupes gate**
  - **Trigger:** Developer or CI runs `dotnet quality-gates dupes --project <path>`
  - **Actors:** A1
  - **Steps:** Parse all `.cs` files in the project → extract method
    declarations → normalize bodies via Roslyn → filter out methods with
    ≤3 normalized tokens → compute pairwise Jaccard on remaining n-gram
    tokens → cluster at ≥0.5 → report summary + cluster list → exit 0 or 2
  - **Outcome:** Duplication clusters are visible and actionable.
  - **Covered by:** R1, R2, R3, R4, R5, R6

- F2. **All-gate execution includes dupes**
  - **Trigger:** Developer or CI runs `dotnet quality-gates all --project <path> --test-project <path> --coverage-file <path>`
  - **Actors:** A2
  - **Steps:** Run crap → run scrap → run dupes → return worst exit code
    across all gates (all gates always execute regardless of prior
    failures)
  - **Outcome:** All three gates pass (0) or the worst breach is reported (2).
  - **Covered by:** R9

---

## Requirements

**CLI and execution**

- R1. The `dupes` subcommand accepts `--project <path>` (required) pointing
  to the source project directory.
- R2. Scans all `.cs` files recursively within the project, excluding
  generated code (`.Designer.cs`, `.g.cs`), migrations directories, and
  `bin`/`obj` output directories.
- R3. Extracts all method declarations (property accessors, operators,
  regular methods) as comparison units. Constructors are excluded — parameter
  assignment patterns normalize to identical structures across all classes and
  produce false-positive clusters.

**Analysis**

- R4. Normalizes each method body using Roslyn syntax-node rewriting
  (reusing the normalizer infrastructure from SCRAP), replacing identifiers
  and literals with typed placeholders while preserving AST structure.
- R5. Tokenizes each normalized body into 3-gram sequences and computes
  pairwise Jaccard similarity across all methods. Tokens are Roslyn
  `SyntaxKind` enum values collected via depth-first pre-order traversal of
  the normalized syntax tree.
- R6. Builds similarity clusters using union-find aggregation at threshold
  ≥0.5 (Uncle Bob's discipline). Transitive chaining is a known property of
  union-find — a bridge method can connect two dissimilar methods into one
  cluster (e.g., A↔B at 0.6 and B↔C at 0.55 produces a single cluster even
  if A↔C is 0.3). Developers should review clusters for bridge effects; a
  future iteration may add intra-cluster similarity floor reporting.
- R7. Filters out methods with ≤3 normalized tokens before comparison
  (trivial getters, setters, one-line delegates) to suppress noise.

**Output**

- R8. Default output: summary line (files scanned, methods analyzed, clusters
  found) followed by one cluster per group, each listing file path, method
  name, and average intra-cluster similarity score.
- R9. `--verbose` flag: includes the normalized body representation for each
  method in each cluster so the developer can assess the structural match.
- R10. `--json` flag: outputs all cluster data as structured JSON for
  programmatic consumption.
- R11. Exit codes: 0 when no clusters found, 2 when any cluster found
  (threshold breach), 1 on error. Matches CRAP and SCRAP convention.

**AllCommand integration**

- R12. `AllCommand` composes dupes after scrap, using worst-exit-code logic:
  if crap exits 2, dupes still runs; if scrap exits 2 and dupes exits 0,
  overall exits 2; if all exit 0, overall exits 0.

**Implementation workflow**

- R13. Implementation follows `rm-tdd` skill strictly: one failing test
  first, minimal code to pass, refactor, next test. Tests cover the Handler
  directly, not the Command. All new tests in
  `tools/tests/redmuffin.Tools.QualityGates.Tests/Commands/DupeCommandTests.cs`
  and matching analysis tests.

---

## Acceptance Examples

- AE1. **Covers R6, R11.** Given two methods with identical normalized
  bodies (different identifiers, same structure), when dupes runs, a cluster
  is reported and exit code is 2.
- AE2. **Covers R7, R11.** Given only methods with ≤3 normalized tokens
  (trivial getters), when dupes runs, no clusters are reported and exit code
  is 0.
- AE3. **Covers R5, R6.** Given three methods where A↔B is 0.6 similarity and
  B↔C is 0.55 similarity but A↔C is 0.3, when dupes runs, all three are in
  one cluster (transitive via union-find).
- AE4. **Covers R12.** Given crap exits 0, scrap exits 2, and dupes exits 0,
  when `all` runs, the overall exit code is 2 (scrap breach takes priority).
- AE5. **Covers R1, R8.** Given a project with 50 `.cs` files and 200
  methods, when dupes runs, the summary line reports "50 files, 200 methods,
  N clusters found."

---

## Success Criteria

- Running dupes against the current `redmuffin.Blazor.StaticWeb` source
  produces actionable clusters — methods that a developer would agree are
  structurally similar and worth consolidating.
- Zero clusters on an intentionally clean test fixture.
- Full test suite (104 existing + new dupes tests) passes with `dotnet run
--project tests/redmuffin.Tools.QualityGates.Tests`.
- `all` subcommand runs crap → scrap → dupes sequentially and returns correct
  worst exit code.
- A downstream agent reading `tools/README.md` can run dupes without
  rediscovering commands, patterns, or gotchas.

---

## Scope Boundaries

- Type-level duplication detection (identical DTO shapes or class signatures
  across namespaces) — out of scope. dupes analyzes method bodies only.
- String or text-level pattern matching outside the AST — out of scope.
  Normalized AST comparison only.
- Automatic refactoring or code generation from cluster results — out of
  scope. dupes reports clusters; the developer decides what to consolidate.
- Analyzing test files — out of scope. SCRAP covers test structural analysis.
- Pairwise O(n²) comparison is acceptable for the current codebase (~few
  hundred methods). A future iteration may add `--changed` incremental mode
  or locality-sensitive hashing for large codebases.
- Intentional-duplication suppression mechanism (allow-list file or
  `[SuppressDupe]` attribute) — deferred. V1 reports all clusters; developers
  manually review. A suppression mechanism will be added if false-positive
  noise from legitimate patterns (API adapters, facade layers) becomes
  unmanageable.

---

## Key Decisions

- **Methods as unit**: Methods (including properties, constructors, operators)
  as the comparison unit rather than arbitrary blocks or type declarations.
  All C# logic lives in methods; method boundaries are the natural structural
  unit. Reuses CRAP's method-discovery infrastructure.
- **Reuse SCRAP infrastructure**: Roslyn normalizer and union-find clustering
  port directly from SCRAP. The normalization code and Jaccard/clustering code
  are adapted from `TestNormalizer` and `ScrapDuplication` rather than rewritten.
- **CrapCommand pattern**: Follows `CrapCommand`/`CrapHandler` pattern
  exactly — thin CLI wrapper delegates to a testable Handler. No
  `InternalsVisibleTo`; handlers are `public static`.
- **rm-tdd workflow**: Implementation follows the `rm-tdd` skill: one failing
  test, minimal production code, refactor, next test. Tests cover the Handler
  (not the Command), matching SCRAP and CRAP test patterns.
- **N-gram size of 3**: Matches SCRAP's Jaccard tokenization precedent.
  Balances sensitivity (2-grams over-match) against noise (4-grams
  under-match).
- **No separate ADR for dupes**: The architecture decisions (separate
  solution, monolith subcommand, local NuGet feed, Roslyn workspace reuse)
  are already established in ADR-0002 and ADR-0003. dupes follows those
  existing decisions.

---

## Dependencies / Assumptions

- Depends on SCRAP's existing Roslyn normalization infrastructure
  (`TestNormalizer`) being adaptable to source method bodies (not just test
  methods).
- Depends on SCRAP's existing Jaccard similarity and union-find clustering
  code (`ScrapDuplication`) being portable to cross-codebase method
  comparison.
- Assumes the repo root `global.json` continues to pin SDK 9.0 and
  `tools/global.json` continues to require SDK 10.0.104. All tool commands
  must run from the `tools/` directory.
- Assumes `dotnet run --project tests/redmuffin.Tools.QualityGates.Tests`
  remains the test execution command (TUnit + AOT mode).

---

## Outstanding Questions

### Resolve Before Planning

_None._ All product decisions are resolved; the remaining questions are
technical and belong in planning.

### Deferred to Planning

- [Affects R4][Needs research] Can `TestNormalizer` be adapted to source
  method bodies with minimal changes, or does it need a separate
  `SourceNormalizer` class?
- [Affects R5, R6][Technical] What data structure for pairwise comparison —
  compare all pairs O(n²) or use a locality-sensitive hash for pre-filtering?
- [Affects R2][Technical] Exact exclusion rules for generated code — should
  we detect `[GeneratedCode]` attribute, file naming conventions, or both?
- [Affects R8][Technical] Cluster output format — how to display multi-file
  clusters compactly in the default table view?
