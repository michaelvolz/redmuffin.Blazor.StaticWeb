---
date: 2026-05-14
title: Mutation Testing Philosophy — Uncle Bob's 100% Kill Rate Imperative
tags: [mutation, testing, quality, uncle-bob, philosophy]
description:
  Uncle Bob's position on mutation testing is absolute — every surviving
  mutant signals a test deficiency. There are no acceptable survivors. The only
  justifiable kill rate goal is 100%. Documents the source, our current gaps,
  and the path forward.
module: tools
problem_type: best_practice
---

# Mutation Testing — 100% Kill Rate

## Uncle Bob's Position (Source)

From his June 2016 blog post "Mutation Testing"
([source](https://blog.cleancoder.com/uncle-bob/2016/06/10/MutationTesting.html)):

> "If a mutant passes the test suite, it is said to have _survived_. This is
> a bad thing. It means that the tests do not check for that semantic change.
> Strangely, the sense for mutant tests is _inverted_; we expect them to
> fail. A passing mutant test is bad. **Mutants should all be red!**"

> "There is no justifiable goal other than 100%. Every single line, and
> every single branch, should be tested by your unit tests. I realize that
> this goal is not practicably achievable. So I think of it as an asymptotic
> goal — one that we are always pushing towards, but never quite achieving."

> "A surviving (green) mutant might be the result of tests that have been
> `@ignore`d, or commented out, or when asserts have been removed, or never
> added. It can also happen if TDD discipline got a little lax at some
> point, and some code got added without a corresponding test."

## Implication

There are **no conditions where a surviving mutant is acceptable**. Every
survivor is a test deficiency — a place where the code could change
semantically and our tests wouldn't notice.

This overrides my earlier casual acceptance of "expected for untested
paths." The mutation tool reveals which paths are untested. The response
must be to write tests for them, not to accept the survivors.

## Our Current Survivors

### Calculator.cs (tools fixture) — 3 survivors

| #   | Line | Mutation                                 | Category   | Root Cause                                                                         |
| --- | ---- | ---------------------------------------- | ---------- | ---------------------------------------------------------------------------------- |
| 1   | 5    | `a * b` → `a / b`                        | Arithmetic | `Multiply` method has no test in `CalculatorTests`                                 |
| 2   | 6    | `greater-than` → `greater-than-or-equal` | Comparison | `IsPositive` boundary case not tested (test only covers `IsPositive(5)`, not edge) |
| 3   | 6    | `0` → `1`                                | Constant   | Same method — constant value change not caught                                     |

### RaindropListFetcher.cs (main solution) — 6 survivors

All six are `false → true` mutations on boolean conditions in a chain:

| #   | Line | Mutation       | Root Cause                                                             |
| --- | ---- | -------------- | ---------------------------------------------------------------------- |
| 0   | 36   | `false → true` | Items check — condition true path tested, false path not               |
| 1   | 37   | `false → true` | API error fallback — only tested when earlier condition already caught |
| 4   | 45   | `false → true` | OperationCanceledException handler — condition true path tested        |
| 5   | 50   | `false → true` | General exception handler — condition true path tested                 |
| 6   | 57   | `false → true` | Redirect check — only one branch tested                                |
| 7   | 68   | `false → true` | Authorize check — only one branch tested                               |

These are chain-of-responsibility conditions where our tests only exercise
the first matching arm. Tests need to cover each branch independently.

## Path to 100%

For each survivor, the fix is the same: write a test that covers the
semantic change. The mutation tool identifies the gap precisely — the
test just needs to exercise the mutated code path.

This is a recursive process:

1. Run mutation → identify survivors
2. Write test for each survivor
3. Run mutation again → verify killed
4. Repeat until 0 survivors
