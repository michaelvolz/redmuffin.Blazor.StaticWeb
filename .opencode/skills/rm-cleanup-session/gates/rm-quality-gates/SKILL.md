---
name: rm-quality-gates
description: Quality gate tool protocols — CRAP, SCRAP, Depth, Architecture, Mutation, Dupes. Step-by-step remediation patterns with verification checkpoints. Loaded by rm-cleanup-session during cleanup. Do not load independently.
version: 1.0
prerequisite-skills:
  - rm-guide-code-quality
---

# rm-quality-gates

## §0.0 Cleanup Philosophy — All Improvements Welcome

Quality gates reveal problems. Cleanup fixes them — through ANY means that
make the code better. Extraction, seam testing, and mutation hardening are
tools in the arsenal. They are not exclusive methods. They are not the only
path.

When a CRAP violation or mutation survivor appears, ask the authors:

- **Feathers**: Is there a seam here? Can I characterize the behavior and
  test through the public API?
- **Ousterhout**: Is this module deep or shallow? Would extracting make it
  deeper, or am I creating interface overhead without benefit?
- **Uncle Bob**: Is the code clean? Does it read like well-written prose?
  Is there a SOLID violation?
- **Fowler**: Is there a refactoring that eliminates duplication without
  creating the wrong abstraction? (Rule of Three)
- **Metz**: Is the duplication cheaper than the wrong abstraction?

Cleanup is a search for ALL improvements. Every category is fair game:

| Category                | Example                                                  | When                                   |
| ----------------------- | -------------------------------------------------------- | -------------------------------------- |
| **Extraction**          | Pull pure logic out of I/O methods                       | Method is ≥5 lines, pure, calls no I/O |
| **Design change**       | Inject dependency instead of hard-coding `new Process()` | Coupling prevents testing              |
| **Architecture**        | Interface segregation, dependency inversion              | Module boundaries are wrong            |
| **Pure function**       | Convert side-effecting method to return-value            | Side effects prevent characterization  |
| **Pattern application** | Replace switch with polymorphism, introduce Null Object  | Pattern reduces complexity             |
| **Dead code removal**   | Delete unused methods, collapse 1-implementer interfaces | Code serves no purpose                 |

**The only constraint on structural changes**: Before implementing a design
change that touches multiple systems, load the STRUCTURAL CHANGE GATE in
AGENTS.md. Answer the three questions. Get approval. Then proceed.

**Recommend design changes aggressively.** When you see a hard-coded
dependency, a missing abstraction, or a SOLID violation — say so. Propose
the fix. Ask for permission. The guidelines that follow (§2.1 extraction
gates, §4 mutation decision tree) are ENABLERS that prevent bad extractions
and metric gaming. They are NOT a ceiling on what kind of improvement is
allowed.

## §0 Recursive Quality Loop (Governing Principle)

Quality gates are not one-shot. The process is inherently recursive:
run gates, fix the worst violations, re-run gates, and repeat until the
solution is clean across every dimension. Never stop at "good enough."

**The loop:**

1. **Run all gates** — Depth → Architecture → CRAP → SCRAP → Mutation → Dupes.
   Every gate must execute. A build failure on one gate does not excuse
   skipping the rest.
2. **Fix structural issues first** — Depth and Architecture gates catch
   structural defects (parameter bloat, wrong abstraction, dependency
   violations) that cascade into CRAP and Dupes. Consolidating shared
   state into a builder record (Depth fix) can eliminate 3+ CRAP
   violations without writing a single test. Fix structural problems BEFORE
   behavioral problems — the order matters.
3. **Fix the worst violations first** — sort by severity: CRAP score
   (highest first), then structural duplicates (Dupes), then LOCAL test
   files (SCRAP), then architectural violations, then mutation survivors.
4. **Re-run all gates** — verify every fix moved the needle. A fix that
   reduces CRAP but introduces a Depth finding is not a net improvement.
5. **Repeat until zero violations** — each iteration should converge
   toward a narrower gap. If you're stuck on the same violations after
   three passes, stop and reassess the approach.
6. **When all gates are clean, you are done.** Not before.

**Why recursion matters**: Every code change can introduce new quality
issues. Extracting a method to reduce CRAP score may create a structural
duplicate the Dupes gate catches. Changing a collection type for MA0016
may expose a new mutation survivor. The gates reinforce each other —
skip the loop and you ship half-verified code.

**Dogfooding rule**: The tools that enforce quality on production code
must themselves pass every gate. Run the gates against `tools/` before
committing gate changes. If the gates can't pass on themselves, they're
not trustworthy for anything else.

### §0.1 Optimized Single-Pass Cleanup (Focused Sessions)

For sessions targeting a specific violation class (CRAP, SCRAP, Dupes),
the recursive loop is too slow — each gate run takes 17-600 seconds.
Use this 4-step single-pass workflow instead:

**Step 1: SURVEY** — run all gates ONCE, save output, classify.

```bash
pwsh -NoProfile -File scripts/Run-QualityGates.ps1
```

The script generates coverage, runs all gates, and saves the full output
to `/tmp/gates-output.txt`. Working directory must be the repo root.

**Step 2: EXTRACT** — work through fixable violations methodically.

- Characterize FIRST (golden-master test), THEN extract seams per
  rm-guide-code-quality §2.1.
- One seam per edit cycle within a file. Work top-to-bottom.
- Write all unit tests for extracted methods (rm-guide-testing pattern).
- Do NOT re-run gates between methods — trust the Feathers pattern.

**Step 3: VERIFY** — re-run the survey script ONCE at the end.

```bash
pwsh -NoProfile -File scripts/Run-QualityGates.ps1
```

Compare with the saved output from Step 1. Every violation that
disappeared is a win. Regressions are rare if §2.1 was followed.

**Step 4: DOCUMENT** — any remaining violations are either:

- Tool limitations (Cobertura attribution gaps, structural formula limits)
- Design decisions (semantic duplicates dry4clj can't distinguish)
- Infrastructure (git-spawned methods, System.CommandLine factories)

Document them in `tools/README.md` under Known Issues as justified
exceptions per rm-guide-code-quality §3.

**Why this works:** The Feathers Seam Pattern (characterize → extract →
test) is deterministic. If extracted correctly, CRAP drops without
re-verification. Per-method re-checking adds I/O overhead but zero
decision value — it only confirms what §2.1 already guarantees.

Systematic cleanup workflows for each quality gate. Every remediation
follows the same pattern: gate reveals problem → characterize behavior →
fix → re-gate to verify.

Load `rm-guide-code-quality` alongside this skill. It defines the universal
coding principles applied during cleanup.

## Gate Execution Order

Always run gates in order: Architecture → Depth → CRAP → SCRAP →
Mutation → Dupes. Each gate validates a different quality dimension.

### Quick: run a single gate

```bash
cd tools
dotnet run -- all --solution ../redmuffin.Blazor.StaticWeb.slnx
```

Zero flags. Auto-coverage generates and merges coverage from all test projects
discovered in the `.slnx`. All 5 gates run in one pass. Coverage merges via
`CoberturaMerger` — no manual per-project commands needed.

For per-gate runs or custom options:

```bash
cd tools
dotnet run --project src/redmuffin.Tools.QualityGates -- <gate> [options]
```

### Prerequisite: none (auto-coverage)

Coverage is generated automatically by `--auto-coverage` (default ON). When
the solution has multiple test projects (e.g., Blazor WASM + API Functions),
each generates Cobertura XML independently, then `CoberturaMerger` merges
them into a single file before CRAP analysis. No manual coverage commands
needed.

To generate coverage manually (for debugging):

```bash
dotnet run --project tests/redmuffin.Blazor.StaticWeb.Tests \
  --coverage --coverage-output-format cobertura \
  --coverage-output /tmp/manual-coverage.xml
```

## Gate 1: CRAP Cleanup Workflow

CRAP = CC² × (1 − coverage)³ + CC. Fix by raising coverage or lowering
complexity. The optimal approach is characterization tests first, then
simplify.

### Workflow for a single CRAP violation

```
1. Run CRAP gate → identify method with CRAP ≥ 8
2. Read the method. Understand its current behavior through its tests
   (or lack thereof)
3. Write characterization tests (Michael Feathers):
   - Test ONLY observable inputs → outputs
   - Use the golden master pattern: capture current behavior, refactor,
     verify output unchanged
   - NEVER test internal implementation details
4. Add the characterization tests to the test project, verify they pass
5. Re-run CRAP gate — coverage may now be high enough to drop CRAP below 8
6. If CRAP still ≥ 8, simplify the method:
   a. Add guard clauses (early returns) — each one reduces CC by 1
   b. Extract helper methods for decision branches
   c. Replace nested conditionals with guard clauses
   d. Replace conditional with polymorphism (Fowler) if justified
7. After simplification, characterization tests still pass (behavior preserved)
8. Write proper unit tests for each extracted method
9. Run mutation testing on the fixed file (Gate 4 below)
10. Re-run CRAP gate — verify CRAP < 8
```

### Specific CRAP reduction techniques

| Technique                             | CC Reduction                                   | When to Use                                                                                                                                                                                      |
| ------------------------------------- | ---------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Guard clause                          | -1 per early return                            | `if (x) { ... entire method }` → `if (!x) return; ...`                                                                                                                                           |
| Extract method                        | Redistributes CC                               | Long method with distinct concerns                                                                                                                                                               |
| Decompose conditional                 | Makes CC countable per method                  | Complex `if` with multiple conditions                                                                                                                                                            |
| Replace conditional with polymorphism | Removes conditionals entirely                  | Switch/if-else on type codes                                                                                                                                                                     |
| Introduce Null Object                 | Removes null checks (-1 per check removed)     | Repeated `if (x != null)` patterns                                                                                                                                                               |
| Table-driven method                   | Replaces branching with lookup. CC drops to 1. | Switch expression with ≥4 arms mapping to constant values. Use `FrozenDictionary<K,V>` + `GetValueOrDefault` for allocation-free lookup. Split `or` patterns into individual dictionary entries. |

### Functional C# Pattern Catalog (rm-guide-csharp-functional)

When a CRAP or Depth violation appears, match the code smell to the pattern.
These are the patterns that delivered 91-100% CC reduction this session
(2026-05-17). See `rm-guide-csharp-functional` for full pattern documentation.

| Problem pattern                          | Functional pattern                      | CC reduction     | Example                                                                             |
| ---------------------------------------- | --------------------------------------- | ---------------- | ----------------------------------------------------------------------------------- |
| Switch with ≥5 arms mapping to constants | FrozenDictionary (§1)                   | CC → 1           | `IsKnownPure`: 21-arm switch → `FrozenDictionary<string,bool>`, CC 21→1, CRAP 462→2 |
| foreach + multiple if guards filtering   | LINQ .Any() chain (§3)                  | CC → 1           | `IsWrongAbstraction`: foreach + 4 if guards → `.Where().Any()`, CC 8→1              |
| Cumulative scoring with many branches    | Signal array + LINQ .Where().Sum() (§3) | CC → 2           | `AnalyzeMethod`: 14 branches → `(bool,int,string)[]` + `.Where().Sum()`, CC 14→2    |
| 6+ params sharing state across methods   | Builder record (§1)                     | Params → 2-3     | `AddProjectDependencies`: 6 params → `GraphBuilder` record, 6→3                     |
| I/O boundary blocking testability        | Func<> injection (§5)                   | CC → 7.7 PASS    | `ResolveCoverageLinesAsync`: Process.Start embedded → optional `Func<>` param       |
| foreach + 3+ guard conditions            | LINQ pipeline (§3)                      | CC → 5           | `DiscoverFromSlnx`: foreach + 3 guards → `.Select().Where().ToList()`, 8→5          |
| Private method hiding pure logic         | public static extraction (§6)           | CC redistributed | `GetClassKey`: private 3-branch extraction → public static, CRAP 9.2→3.2            |
| Nested method inside untestable context  | Extract conductor method                | CC redistributed | `CollectMethodsFromFile`: extraction from `Analyze` with IOException test           |

For the full functional C# pattern catalog that directly reduces CC
(LINQ `.Any()`/`.All()` chains, signal arrays with `.Where()`/`.Sum()`,
pattern-matching dispatchers), see `rm-guide-csharp-functional`. Every
technique in that catalog is a CRAP reduction tool.

### 0%-coverage method classification

Many methods at CRAP 6.0 (CC=2, 0% coverage) are structural, not real
violations. Classify before acting:

| Category                                                     | Treatment                                                                                                                                                                                                                                                                                                                                                                                                                                                                        |
| ------------------------------------------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Interface stubs (I\* interface methods)                      | Check implementer count. 1 implementer → flag for collapse (superfluous code). 2+ implementers → add tests to one implementation, interfaces get covered transitively.                                                                                                                                                                                                                                                                                                           |
| Logging delegates (LoggerMessage source gen)                 | Keep. These are compile-time generated. The source generator is the test target, not the delegate.                                                                                                                                                                                                                                                                                                                                                                               |
| Blazor lifecycle (OnInitializedAsync, etc.)                  | Ignore CRAP score. Runtime-called methods are tested via integration/blazor-renderer tests, not CRAP's line coverage.                                                                                                                                                                                                                                                                                                                                                            |
| Factory/Creation methods                                     | If single-line `new X()` → test the callers. If complex creation logic → write characterization tests.                                                                                                                                                                                                                                                                                                                                                                           |
| True dead code                                               | Remove per superfluous code rules in rm-guide-code-quality §1.                                                                                                                                                                                                                                                                                                                                                                                                                        |
| Conductor/orchestrator (CC ≤ 3, 0% cov)                      | Auto-detected by CoverageGapDetector. Body is only delegation + guards + try/catch. Shown as COVERAGE GAP, excluded from exit code.                                                                                                                                                                                                                                                                                                                                              |
| Switch dispatcher (CC > 3, >50% cov)                         | Auto-detected by CoverageGapDetector. Body is return switch where every arm delegates to one sub-method. Shown as COVERAGE GAP. Sub-dispatchers verified independently.                                                                                                                                                                                                                                                                                                          |
| Formula-bound (CC ≥ 8, cov >80%, not a conductor/dispatcher) | CRAP score exceeds 8.0 despite high Cobertura coverage. Caused by CC counting differences: `or` patterns, `foreach`, `??`, `?.` operators that source CC counts but Cobertura instruments differently. Two fixes: (1) replace switch with FrozenDictionary to drop CC, or (2) extract I/O boundary via Func<> injection. If neither applies, accept as measurement artifact. See `docs/solutions/developer-experience/crap-formula-cobertura-coverage-divergence-2026-05-16.md`. |

### Algorithmic gap detection

The CRAP pipeline automatically classifies infrastructure methods via
`CoverageGapDetector`. Two patterns:

- **Conductors**: CC ≤ 3, 0% coverage, body is delegation/guards/try-catch
  only. No loops, no if/else, no switches, no data transforms.
  Detected via Roslyn statement-level analysis.
- **Switch dispatchers**: CC > 3, coverage > 50%, body is a single return
  switch expression where every arm is a single delegation (method call
  or simple literal). Detected via Roslyn switch-arm analysis.

When a method matches, it shows `COVERAGE GAP` instead of `FAIL` and is
excluded from the exit code. No manual exclusion lists, config files,
or attributes. Purely algorithmic per Uncle Bob philosophy.

## Gate 2: SCRAP Cleanup Workflow

SCRAP catches test structural problems: duplicated setup, long chains,
zero-assertion tests. Output: STABLE (good), LOCAL (consider extraction),
SPLIT (needs restructuring).

### LOCAL vs SPLIT — gate failure distinction

Only **SPLIT** is a gate failure. **LOCAL** is informational guidance —
the tool detected repeated Arrange blocks, but extracting them may not
improve the tests.

**When LOCAL helpers help:**

- Same object construction repeated 3+ times with the same shape
- Test data that's long and obscures the test intent
- Setup that's reused across multiple test files

**When LOCAL helpers hurt (extraction is worse):**

- The inline code IS the test specification (e.g., inline Roslyn
  source strings, inline XML, inline JSON)
- Extracting hides the test data behind a factory — "What does
  `MakeReport()` actually test?"
- Single-use setup that appears duplicated only because SCRAP's
  structural normalizer sees similar patterns in string literals

### SCRAP principles

- Extract helpers for duplicated Arrange blocks (not for single-use setup)
- Table-drive tests: when multiple tests differ only in data, use
  `[MethodDataSource]` (TUnit) or `[ClassData]` (xUnit equivalent)
- Never extract Assert sections — assertions should be explicit per test
- Never extract test data that IS the test specification
- Target: zero SPLIT files. LOCAL is guidance, not a gate failure.

## Gate 3: Architecture Cleanup Workflow

Architecture gate checks dependency direction against `arch-rules.yml`.

### Workflow

```
1. Configure arch-rules.yml with component-map and allowed-dependencies
2. Run arch gate → identify violations or cycles
3. Fix violations:
   a. Extract shared types into a Common/Shared assembly
   b. Use dependency inversion (interface in low-level, impl in high-level)
   c. Restructure project references
4. Run arch gate → verify zero violations, zero cycles
```

### Our current architecture (clean — 0 violations, 0 cycles)

```
Frontend → Shared ← Backend
         Launcher (ignored)
```

If adding new components: update `arch-rules.yml` BEFORE adding
cross-component references.

## Gate 4: Depth Cleanup Workflow

Depth catches structural quality issues grounded in Ousterhout's
deep-module philosophy: shallow methods, parameter bloat, wrong
abstractions (Metz conditional proliferation), and entanglement.
Shallow(3) = FLAG for evaluation, not a mandatory inline. Good shallow
methods exist — the decision tree distinguishes them.

### Classification

Each method receives a composite score from four signals:

| Signal            | Weight | Check                                                                         |
| ----------------- | ------ | ----------------------------------------------------------------------------- |
| Shallow           | 3      | Private, LOC ≤ 4, no branching                                                |
| Wrong abstraction | 2      | Private, if/switch on formal parameter (Metz signal)                          |
| Parameter bloat   | 1      | More than 4 formal parameters                                                 |
| Entangled         | 2      | Private, ≥ 3 parameters with side effects (member assignment, external calls) |

Composite score = sum of triggered signals. Thresholds:

| Score | Output               |
| ----- | -------------------- |
| ≥ 3   | FAIL — exit code 2   |
| 2     | WARN — informational |
| 1     | INFO — informational |
| 0     | Not shown in output  |

Exit code 2 only when FAIL present. WARN and INFO are advisory — they
do not affect the exit code unless a FAIL co-exists.

### Conflict resolution: Depth vs CRAP

Depth and CRAP are equal peers. When CRAP says "extract this" and
Depth says "that would be shallow" — the agent must exercise judgment:

1. If extraction would create a method ≤ 4 lines with no branching →
   Depth will flag it. Don't extract. Find another CRAP reduction
   technique (guard clause, table-driven replacement).
2. If extraction creates a method with real logic (≥ 5 lines or has
   branching) → Depth won't flag it. Extraction is valid.
3. When in doubt, CRAP reduction wins — CRAP is battle-tested in
   Uncle Bob's pipeline. But if the extraction is clearly shallow,
   Depth's signal overrides.

### Cleanup Decision Tree (Shallow Methods)

For each shallow(3) FAIL, apply this decision tree BEFORE making any
code changes. The default response is NOT to inline. Evaluate first.

1. **Name adds semantic value?** — Does the method name communicate intent
   the raw body doesn't? `isApplicationInProduction(headers)` wrapping
   `headers["env"] == "prod"` conveys domain meaning. `IsPositive(x)`
   wrapping `x > 0` just restates the code. → If YES: **KEEP**

2. **Called from ≥ 3 distinct places?** — Phase 2 caller-count filtering
   automatically suppresses shallow(3) for multi-caller methods. For
   manual evaluation: if the method is called from multiple locations,
   DRY wins. → If YES: **KEEP**

3. **Extension method on a framework/scalar type?** —
   `string.IsNullOrEmpty()`, `IEnumerable<T>.NotEmpty()` are API surface
   design for discoverability. They follow a different convention from
   internal helpers. → If YES: **KEEP**

4. **Part of a structural pattern?** — Roslyn visitor overrides
   (`VisitIfStatement`), interface implementations, abstract method
   overrides, `[LoggerMessage]` source-generated partial methods.
   These ARE the pattern — the partial declaration is a compile-time
   contract, not a logic seam. Depth flagging them is a structural
   false positive. → If YES: **KEEP**

5. **All four NO?** — The method body is as self-descriptive as the name,
   called from one place, not an extension method or pattern override.
   → **INLINE** into the caller (Fowler's Inline Function refactoring).

**Phase 2 caller-count filtering:** When the gate runs, it automatically
suppresses shallow(3) for methods called from ≥ 3 distinct locations.
This makes the cleanup decision tree primarily for Phase 1 survey runs
where single-caller data isn't yet available.

### Shallow Methods vs Wrong Abstractions

| Signal               | Problem                                     | Fix                                                                     |
| -------------------- | ------------------------------------------- | ----------------------------------------------------------------------- |
| Shallow(3)           | Name doesn't earn its keep over inline body | Inline (decision tree above)                                            |
| Wrong-abstraction(2) | Method branches on formal parameter (Metz)  | Inline + re-extract at cleaner boundaries, or accept as defensive guard |
| Params(1)            | > 4 formal parameters                       | Introduce parameter object, or accept for CLI/framework methods         |
| Entangled(2)         | Side effects + ≥ 3 params                   | Extract pure logic from I/O, inject Func<> boundary                     |

### Documented false positives

- Public API methods are excluded from shallow detection (private-only).
- Roslyn visitor overrides (`NormalizeReturn`, `VisitIfStatement`, etc.)
  are pattern-structural — the visitor pattern REQUIRES per-node methods.
  Accept as pattern cost, document as known issue.
- `[LoggerMessage]` source-generated partial methods — the partial declaration
  is a compile-time contract. The body is generated IL. The source generator is
  the test target, not the method. Always **KEEP** — apply Q4 (structural
  pattern) from the decision tree. Applies to both `LoggerMessageAttribute` and
  `LoggerMessage.Define` delegate patterns.
- Extension methods on framework types should be excluded in a future
  refinement (not yet implemented — currently flagged as shallow).
- Shared private utility methods with multiple callers at LOC ≤ 4 are
  auto-suppressed by Phase 2 caller-count filtering.
- **Algorithm-inherent branching** — methods where `wrong-abstraction(2)`
  flags parameter branching that IS the algorithm. DFS must branch on
  visited-set membership. Conductor detection must branch on syntax node
  types. Scoring functions must branch on input values. Accept these as
  "the algorithm is the branching" — restructuring would split
  inseparable logic. The test: "If I removed this if-statement, would
  the method still do its job?" If no, the branch is essential.

## Gate 5: Mutation Cleanup Workflow

Mutation testing verifies that tests actually catch logic errors.
After fixing a CRAP violation with new tests, run mutation on that
file to verify test quality.

### Workflow

```
1. Fix CRAP violation for a file (add tests + simplify)
2. Run mutation on the fixed file (auto-coverage generates if no coverage file)
3. For each survivor, apply the DECISION TREE (see below)
4. Re-run mutation → verify the mutant is now killed
5. Repeat until zero survivors (excluding documented equivalent mutants)
```

### Execution Protocol (May 2026)

This protocol applies when hardening tests with mutation testing.

**Target priority** — Public API surface first. Target methods that are
`public static` and called by external consumers. Within a solution,
start with the public entry points (CLI classes, handler public methods).

**Per-survivor workflow** — One survivor at a time:

1. Identify the survivor (mutated line + mutation category)
2. Apply the Survivor Decision Tree (below) — determine: equivalent /
   no coverage / weak test
3. Fix with TDD: write a failing test (red), verify it kills the mutant
4. Re-run mutation on that file — confirm the mutant is now KILLED
5. Move to the next survivor

**Safety gate** — NEVER change production code to fix a survivor.
Enforce with commit separation: production and test changes go in
separate commits. Before declaring a mutation fix session done, run
`git diff --name-only` to confirm only test files were modified.

**Definition of done — 100% kill rate per file. Zero survivors.**

Uncle Bob (June 2016, blog.cleancoder.com): _"There is no justifiable goal
other than 100%. Every single line, and every single branch, should be
tested by your unit tests."_

This is an asymptotic goal — not every file will reach 100% on first pass.
But every survivor must be addressed:

- **Equivalent mutants**: The ONLY acceptable survivor category. Document
  and move on.
- **Missing coverage**: Write integration tests through the public API.
  Never extract a shallow wrapper just to make a constant testable.
- **Weak assertions**: Strengthen the test. If the test doesn't distinguish
  the mutation from the original, the test isn't doing its job.
- **I/O-bound / CLI-harness**: Document as infrastructure gaps. They
  represent real test deficiencies, not acceptable survivors. Plan to
  address them when mocking infrastructure exists.

Zero survivors means zero unaddressed survivors. Every survivor has a
classification and a plan. Ignoring survivors is not acceptable.

Re-run mutation after all fixes to confirm the kill rate. Re-run after
any production code change — a refactor can expose new survivors.

**Scaling trigger** — Achieve 100% kill rate on all targeted tools
solution files before touching the main solution. The tools solution
is the proving ground — validate the process is smooth and optimal
before scaling.

**Separate fixtures rule** — Survivor verification tests and 100%
kill rate targets MUST use separate fixture projects. Never remove
tests from a kill-rate fixture to create survivors.

### Survivor Decision Tree (Uncle Bob + PIT)

Every surviving mutant MUST be investigated. These steps are ordered —
start at step 1 for each survivor.

**Step 1 — Equivalent mutant check**

Ask: "Does the mutated code produce the EXACT SAME output as the original
for ALL possible inputs?"

Examples of equivalent mutants:

- `a + 0` → `a - 0` (always identical)
- `x > 1` → `x >= 2` for integer x (always identical)
- `!!flag` → `flag` (double negation)
- `return value;` → `return value ?? 0;` when value is never null

→ If YES: **Accept.** Document as equivalent. This is the ONLY acceptable
survivor category. Do NOT change code or tests.
→ If NO: continue to Step 2.

**Step 2 — Missing test (no coverage)**

→ If NO test exercises the mutated line: **Write a test.** Follow TDD:
red first, verify it kills the mutant, verify output matches unmutated code.
→ If YES a test covers the line but still survived: continue to Step 3.

**Step 3 — Weak test (covered but survived)**

A test covers the line but has no meaningful assertion for the mutated
behavior. Example: test calls `Divide(10, 2)` but doesn't assert the result.
Coverage shows 100% but `* → /` mutation survives.

→ **Fix the test.** Add concrete assertions that would fail under the mutation.
Do NOT change the production code — the code is correct, the test is insufficient.

**The unbreakable rule: NEVER change production code to fix a survivor.**
The code is correct. The mutation exposes a test deficiency. Fixing the code
to make a mutant pass is destroying production behavior to appease a metric.

### Pause-and-Reflect Checkpoint (After Every 3-5 Survivors)

After fixing 3-5 survivors, pause and ask two questions:

1. **Is the code better?** — Did you extract a method just to make it
   testable? Does the new method pass the Extraction Decision Tree in
   `rm-guide-code-quality` §2.1 (Q1: ≥5 lines, Q2: reads clearly inline)?
   If the extraction created a shallow module, **revert it.** Code
   quality comes first. Mutation kill rate is a measure of test quality,
   not a license to damage production code.

2. **Are the tests better?** — Do the new tests exercise observable
   behavior through the public API, or do they test extracted wrapper
   methods that exist only to be testable? Tests coupled to
   implementation details (shallow wrappers) are worse than no tests
   at all — they entrench bad structure and prevent future refactoring.

If either answer is NO: step back. Ask: "What would Feathers, Ousterhout,
and Uncle Bob recommend here?"

The answer is never "extract a shallow wrapper to kill a mutant." It is
never "ignore the survivor and move on." It is always one of:

- Write a proper integration test through the public API (Feathers:
  characterize behavior, then test)
- Strengthen existing test assertions so the mutation is caught
  (Uncle Bob: the test wasn't good enough)
- Classify as infrastructure gap (I/O-bound, CLI-harness) and document
  the plan to address it when tooling exists
- Classify as equivalent and document the proof

**Code first. Tests second. Kill rate is the byproduct of both.
The goal is zero survivors, achieved through quality.**

### Mutation behavior

- Exit code 0 = completed (survivors are informational per clj-mutate)
- Exit code 1 = error (tests don't pass, missing coverage, etc.)
- Uses `dotnet run --project` (not `dotnet test` — TUnit AOT compatibility)

### Scan-only mode (fast check)

```bash
dotnet run --project tools/src/redmuffin.Tools.QualityGates -- mutate \
  --project <source-file> \
  --test-project tests/redmuffin.Blazor.StaticWeb.Tests \
  --scan
```

Reports mutation site count without running tests. Use to estimate effort.

## Gate Exit Codes

| Code | Meaning                                                    |
| ---- | ---------------------------------------------------------- |
| 0    | Pass — no violations, or completed successfully (mutation) |
| 1    | Error — tool failure, missing files, tests don't pass      |
| 2    | Violations — threshold breached (CRAP/SCRAP/Architecture)  |

## Cleanup Session Checklist

After each cleanup session:

- [ ] CRAP: zero violations ≥ 8
- [ ] SCRAP: zero SPLIT files (LOCAL is informational)
- [ ] Architecture: zero violations, zero cycles
- [ ] Depth: zero FAIL methods (WARN/INFO advisory)
- [ ] Mutation: survivors reviewed and addressed
- [ ] `dotnet run --project tests/redmuffin.Blazor.StaticWeb.Tests` — all 265 tests pass
- [ ] Build: `dotnet build src/redmuffin.Blazor.StaticWeb --verbosity quiet`
- [ ] No new `#pragma warning disable`

## Related

- `rm-guide-testing` — test patterns for CRAP fixes (pure functions, fakes, scopes)
- `rm-guide-code-quality` — universal code quality principles
- `rm-guide-csharp-functional` — functional C# patterns (LINQ pipelines, FrozenDictionary,
  pattern matching) that eliminate branching and reduce CC during CRAP cleanup
- `docs/pure-function-extraction-testing-guide-2026-05-10.md` — pure function pattern
