---
date: 2026-05-14
title: Mutation Testing Decision Tree — Kill Rate Protocol
tags: [mutation, testing, decision-tree, protocol, uncle-bob, pit]
description: Defines the exact protocol for handling mutation testing survivors,
  based on Uncle Bob's blog post and PIT documentation. Covers equivalent mutants,
  when to fix tests vs code, and the step-by-step remediation workflow.
module: tools
problem_type: best_practice
---

# Mutation Testing Decision Tree — Kill Rate Protocol

## Sources

- **Uncle Bob**: "Mutation Testing" (June 2016, blog.cleancoder.com):
  _"There is no justifiable goal other than 100%. Every single line, and every
  single branch, should be tested by your unit tests. I realize that this goal
  is not practicably achievable."_
- **PIT (pitest)**: Official documentation (pitest.org), the tool Uncle Bob endorsed

## The Only Acceptable Survivor

From Uncle Bob:

> "If a mutant passes the test suite, it is said to have _survived_. This is a
> bad thing. Mutants should all be red!"

From PIT:

> "Manually review surviving mutants before drawing conclusions."

The one exception: **equivalent mutants** — mutations where the mutated code
behaves identically to the original for all possible inputs. These are false
positives in the mutation system.

## Decision Tree

```
Mutant survives
    │
    ├── Is it an EQUIVALENT MUTANT?
    │   │  (behavior unchanged — e.g., i > 1 vs i >= 2 when i=2 always)
    │   │
    │   ├── YES → Document as equivalent. Accept. Move on.
    │   │          ONLY acceptable survivor category.
    │   │
    │   └── NO  → Continue ↓
    │
    ├── Does a test COVER the mutated line?
    │   │
    │   ├── NO  → Write a test (TDD: red first, verify it kills the mutant)
    │   │          Category: NO COVERAGE
    │   │
    │   └── YES → Existing test has weak/no assertion for this behavior
    │              FIX THE TEST (add stronger assertions)
    │              Category: WEAK TEST
    │
    └── NEVER change production code to "fix" a survivor.
        The code is correct. The test is insufficient.
```

## What NEVER to Do

| Wrong                                                                   | Why                                                                                       |
| ----------------------------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| Remove the code that triggers the mutation                              | The code is correct. You're destroying production behavior to pass a test quality metric. |
| Remove assertions from tests to let mutants survive (for test fixtures) | Tests verify survivor reporting. Don't weaken real tests. Use separate fixtures.          |
| Change the mutation tool's thresholds to hide survivors                 | The tool measures test quality. Adjusting it to report success defeats the purpose.       |
| Delete tests to make room for survivor tests                            | Both must coexist. Create separate fixtures if needed.                                    |

## What to Do — Step by Step

### 1. Equivalent Mutant Check

Ask: "Does the mutated code produce the SAME output as the original for ALL
possible inputs?"

Examples of equivalent mutants:

- `a + 0` → `a - 0` (always identical)
- `x > 1` → `x >= 2` for integer x (always identical)
- `!!flag` → `flag` (double negation)
- `Math.Max(0, x)` where x is always ≥ 0

If yes: document in a comment, accept the survivor. Do NOT change code or tests.

### 2. Missing Test (No Coverage)

If no test exercises the mutated line: write one. Follow TDD:

1. Write a failing test that exercises the unmutated code path
2. Verify the test passes against unmutated code
3. Re-run mutation — verify the mutant is now KILLED

### 3. Weak Test (Covered but Survived)

A test covers the line but doesn't detect the semantic change. The test has
coverage but no meaningful assertion for this behavior.

Example: a test calls `Divide(10, 2)` but doesn't assert the result. Coverage
shows 100% but the `* → /` mutation survives because no assertion catches it.

Fix: add a concrete assertion that would fail under the mutation.

### 4. Fixture Design for MutationRunner Tests

The mutation runner tests need BOTH killed and survived scenarios. They cannot
share a fixture because 100% kill rate and deliberate survivors conflict.

**Rule**: Separate fixtures.

- `Calculator.cs` + `CalculatorTests.cs` → target 100% kill rate (all tested)
- `Survivor.cs` + `SurvivorTests.cs` → contains one intentionally untested method
  for the survivor assertion test

Never remove tests from the kill-rate fixture to create survivors.

## The Goal

100% kill rate. Every surviving mutant that is not equivalent is a test gap.
Close it.

Use the recursive quality gates loop: run mutation → fix survivors → re-run →
repeat until zero survivors (excluding documented equivalent mutants).
