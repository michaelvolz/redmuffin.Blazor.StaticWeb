---
name: rm-gates-cleanup
description: Quality gates cleanup workflows for systematically fixing CRAP violations, SCRAP LOCAL files, architecture issues, and mutation survivors. Step-by-step remediation patterns with verification checkpoints. Load rm-guide-cleanup alongside this skill — it defines the universal coding principles applied during cleanup. USE FOR: CRAP, SCRAP, architecture gate, mutation testing, code cleanup, quality gates remediation.
version: 1.0
prerequisite-skills:
  - rm-guide-cleanup
---

# rm-gates-cleanup

## §0 Recursive Quality Loop (Governing Principle)

Quality gates are not one-shot. The process is inherently recursive:
run gates, fix the worst violations, re-run gates, and repeat until the
solution is clean across every dimension. Never stop at "good enough."

**The loop:**

1. **Run all gates** — CRAP → SCRAP → Architecture → Mutation → Dupes.
   Every gate must execute. A build failure on one gate does not excuse
   skipping the rest.
2. **Fix the worst violations first** — sort by severity: CRAP score
   (highest first), then structural duplicates (Dupes), then LOCAL test
   files (SCRAP), then architectural violations, then mutation survivors.
3. **Re-run all gates** — verify every fix moved the needle. A fix that
   reduces CRAP but introduces a Duck finding is not a net improvement.
4. **Repeat until zero violations** — each iteration should converge
   toward a narrower gap. If you're stuck on the same violations after
   three passes, stop and reassess the approach.
5. **When all gates are clean, you are done.** Not before.

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
  rm-guide-cleanup §2.1.
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
exceptions per rm-guide-cleanup §3.

**Why this works:** The Feathers Seam Pattern (characterize → extract →
test) is deterministic. If extracted correctly, CRAP drops without
re-verification. Per-method re-checking adds I/O overhead but zero
decision value — it only confirms what §2.1 already guarantees.

Systematic cleanup workflows for each quality gate. Every remediation
follows the same pattern: gate reveals problem → characterize behavior →
fix → re-gate to verify.

Load `rm-guide-cleanup` alongside this skill. It defines the universal
coding principles applied during cleanup.

## Gate Execution Order

Always run gates in Uncle Bob's order: CRAP → SCRAP → Architecture →
Mutation. Each gate validates a different quality dimension.

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

| Technique                             | CC Reduction                               | When to Use                                            |
| ------------------------------------- | ------------------------------------------ | ------------------------------------------------------ |
| Guard clause                          | -1 per early return                        | `if (x) { ... entire method }` → `if (!x) return; ...` |
| Extract method                        | Redistributes CC                           | Long method with distinct concerns                     |
| Decompose conditional                 | Makes CC countable per method              | Complex `if` with multiple conditions                  |
| Replace conditional with polymorphism | Removes conditionals entirely              | Switch/if-else on type codes                           |
| Introduce Null Object                 | Removes null checks (-1 per check removed) | Repeated `if (x != null)` patterns                     |
| Table-driven method                   | Replaces branching with lookup             | Methods with many `return "fixed string"` branches     |

### 0%-coverage method classification

Many methods at CRAP 6.0 (CC=2, 0% coverage) are structural, not real
violations. Classify before acting:

| Category                                     | Treatment                                                                                                                                                               |
| -------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Interface stubs (I\* interface methods)      | Check implementer count. 1 implementer → flag for collapse (superfluous code). 2+ implementers → add tests to one implementation, interfaces get covered transitively.  |
| Logging delegates (LoggerMessage source gen) | Keep. These are compile-time generated. The source generator is the test target, not the delegate.                                                                      |
| Blazor lifecycle (OnInitializedAsync, etc.)  | Ignore CRAP score. Runtime-called methods are tested via integration/blazor-renderer tests, not CRAP's line coverage.                                                   |
| Factory/Creation methods                     | If single-line `new X()` → test the callers. If complex creation logic → write characterization tests.                                                                  |
| True dead code                               | Remove per superfluous code rules in rm-guide-cleanup §1.                                                                                                               |
| Conductor/orchestrator (CC ≤ 3, 0% cov)      | Auto-detected by CoverageGapDetector. Body is only delegation + guards + try/catch. Shown as COVERAGE GAP, excluded from exit code.                                     |
| Switch dispatcher (CC > 3, >50% cov)         | Auto-detected by CoverageGapDetector. Body is return switch where every arm delegates to one sub-method. Shown as COVERAGE GAP. Sub-dispatchers verified independently. |

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

## Gate 4: Mutation Cleanup Workflow

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

**Definition of done** — 100% kill rate per file. Zero survivors
excluding documented equivalent mutants. Re-run mutation after all
fixes to confirm zero survivors.

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

**Separate fixtures rule:** When writing tests for the mutation runner itself,
survivor tests and kill-rate tests MUST use separate fixtures. Never remove
tests from a kill-rate fixture to create survivors for assertion tests.

Reference: `docs/research/mutation-testing-decision-tree-2026-05-14.md`

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
- [ ] Mutation: survivors reviewed and addressed
- [ ] `dotnet run --project tests/redmuffin.Blazor.StaticWeb.Tests` — all 265 tests pass
- [ ] Build: `dotnet build src/redmuffin.Blazor.StaticWeb --verbosity quiet`
- [ ] No new `#pragma warning disable`

## Related

- `rm-guide-testing` — test patterns for CRAP fixes (pure functions, fakes, scopes)
- `rm-guide-cleanup` — universal code quality principles
- `docs/pure-function-extraction-testing-guide-2026-05-10.md` — pure function pattern
