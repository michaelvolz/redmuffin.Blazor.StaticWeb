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
dotnet run --project src/redmuffin.Tools.QualityGates -- <gate> [options]
```

### Full: run all gates

```bash
cd tools
dotnet run --project src/redmuffin.Tools.QualityGates -- all \
  --project ../src/redmuffin.Blazor.StaticWeb \
  --test-project ../tests/redmuffin.Blazor.StaticWeb.Tests \
  --coverage-file <path-to-cobertura.xml> \
  --arch-config ../arch-rules.yml
```

### Prerequisite: generate coverage

Generate Cobertura XML before running CRAP, Mutation, or `all`:

```bash
# From repo root (NOT tools/ directory)
dotnet run --project tests/redmuffin.Blazor.StaticWeb.Tests \
  --coverage \
  --coverage-output-format cobertura \
  --coverage-output coverage/blazor-cobertura.xml
```

Coverage file lands at:
`tests/redmuffin.Blazor.StaticWeb.Tests/bin/Debug/net9.0/TestResults/coverage/blazor-cobertura.xml`

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

| Category                                     | Treatment                                                                                                                                                              |
| -------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Interface stubs (I\* interface methods)      | Check implementer count. 1 implementer → flag for collapse (superfluous code). 2+ implementers → add tests to one implementation, interfaces get covered transitively. |
| Logging delegates (LoggerMessage source gen) | Keep. These are compile-time generated. The source generator is the test target, not the delegate.                                                                     |
| Blazor lifecycle (OnInitializedAsync, etc.)  | Ignore CRAP score. Runtime-called methods are tested via integration/blazor-renderer tests, not CRAP's line coverage.                                                  |
| Factory/Creation methods                     | If single-line `new X()` → test the callers. If complex creation logic → write characterization tests.                                                                 |
| True dead code                               | Remove per superfluous code rules in rm-guide-cleanup §1.                                                                                                              |

## Gate 2: SCRAP Cleanup Workflow

SCRAP catches test structural problems: duplicated setup, long chains,
zero-assertion tests. Output: STABLE (good), LOCAL (needs helper extract),
SPLIT (needs restructuring).

### Workflow for LOCAL files

```
1. Run SCRAP gate → identify LOCAL files needing AutoRefactor
2. Read the test file. Find duplicated Arrange blocks across tests
3. Extract duplicated setup into shared helper methods:
   - Create a static helper class in the test project (e.g.,
     `TestFixtureFactory`)
   - Move duplicated object construction into factory methods
   - Keep helpers close to the test class when possible
4. Re-run SCRAP gate → verify file is now STABLE
```

### SCRAP principles

- Extract helpers for duplicated Arrange blocks (not for single-use setup)
- Table-drive tests: when multiple tests differ only in data, use
  `[MethodDataSource]` (TUnit) or `[ClassData]` (xUnit equivalent)
- Never extract Assert sections — assertions should be explicit per test
- Target: all test files STABLE, zero LOCAL or SPLIT

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
2. Run mutation on the fixed file:
   dotnet run --project tools/src/redmuffin.Tools.QualityGates -- mutate \
     --project <source-file> \
     --test-project tests/redmuffin.Blazor.StaticWeb.Tests
3. Review survivors:
   - Each survivor means a mutant passed tests — the tests missed it
   - Survivor at line X means the test at line X doesn't catch the change
4. For each survivor, strengthen existing tests or add new tests
5. Re-run mutation → verify all mutants killed
```

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
- [ ] SCRAP: all files STABLE
- [ ] Architecture: zero violations, zero cycles
- [ ] Mutation: survivors reviewed and addressed
- [ ] `dotnet run --project tests/redmuffin.Blazor.StaticWeb.Tests` — all 265 tests pass
- [ ] Build: `dotnet build src/redmuffin.Blazor.StaticWeb --verbosity quiet`
- [ ] No new `#pragma warning disable`

## Related

- `rm-guide-testing` — test patterns for CRAP fixes (pure functions, fakes, scopes)
- `rm-guide-cleanup` — universal code quality principles
- `docs/pure-function-extraction-testing-guide-2026-05-10.md` — pure function pattern
