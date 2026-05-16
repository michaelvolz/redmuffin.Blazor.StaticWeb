---
date: 2026-05-15
title: Mutation Testing Execution Protocol
tags: [mutation, testing, quality, uncle-bob, protocol, workflow]
description: Defines the target priority, per-survivor workflow, safety gate,
  definition of done, and scaling trigger for hardening tests with mutation
  testing. Captures decisions from the May 15, 2026 grilling session.
module: tools
problem_type: best_practice
---

# Mutation Testing Execution Protocol

Established during a `grill-with-docs` session on May 15, 2026.
This protocol extends the [mutation testing decision tree](./mutation-testing-decision-tree-2026-05-14.md)
and the [100% kill rate philosophy](./mutation-testing-100-percent-kill-rate-2026-05-14.md)
with concrete execution rules.

## Audit of Existing Work

The Calculator.cs mutation fixture was audited for production-code changes.
Result: **Clean.** All mutation fixes were test-only — Multiply test restored,
IsPositive boundary tests added (1, 0, -5). No production code was touched
between creation and fix commits.

## Target Priority

**Public API surface first.** Target methods that are `public static` and
called by external consumers. Within a solution, start with the public entry
points (CLI classes, handler public methods). Internal helpers are lower
priority — they are exercised indirectly through public API tests.

## Per-Survivor Workflow

One survivor at a time:

1. **Identify** the survivor — mutated line, mutation category (arithmetic,
   comparison, equality, boolean, conditional, constant).
2. **Apply the decision tree** — equivalent mutant / no coverage / weak test.
   See [mutation-testing-decision-tree-2026-05-14.md](./mutation-testing-decision-tree-2026-05-14.md).
3. **Fix with TDD**: write a failing test (red), verify it kills the mutant,
   verify the test passes against unmutated code.
4. **Re-run mutation** on that file — confirm the mutant is now KILLED.
5. **Move to the next survivor**.

Do not batch survivors. Each fix must be independently verified before
proceeding.

## Safety Gate

**Commit separation.** Production-code changes and test changes always go in
separate commits. Before declaring a mutation fix session done, run
`git diff --name-only` to confirm only test files were modified.

The unbreakable rule from the decision tree applies here: **NEVER change
production code to fix a survivor.** The code is correct. The mutation
exposes a test deficiency.

## Definition of Done

**100% kill rate per file.** Zero survivors excluding documented equivalent
mutants. Re-run mutation after all fixes to confirm zero survivors.

Equivalent mutants must be documented in a comment at the mutation site.

## Scaling Trigger

**Full tools solution pass before main solution.** Achieve 100% kill rate on
all targeted tools solution files first. The tools solution is the proving
ground — validate the process is smooth and optimal before scaling to the
main solution.

When the tools solution is clean:

1. The process is proven correct (no production code changes, clean workflow)
2. The decision tree is battle-tested
3. The safety gate (commit separation) is validated
4. Then apply the same protocol to main solution public API surface

## Separate Fixtures Rule

Survivor verification tests (proving MutationRunner reports survivors
correctly) and 100% kill rate targets MUST use separate fixture projects.
Never remove tests from a kill-rate fixture to create survivors.

Current state:

- `Calculator.cs` + `CalculatorTests.cs` → target 100% kill rate
- `Survivor.cs` + `SurvivorTests.cs` → intentionally untested Multiply for
  runner verification

## Reference Docs

- [Mutation Testing Decision Tree](./mutation-testing-decision-tree-2026-05-14.md)
- [100% Kill Rate Philosophy](./mutation-testing-100-percent-kill-rate-2026-05-14.md)
- `rm-gates-cleanup` skill §4 — Mutation Cleanup Workflow
