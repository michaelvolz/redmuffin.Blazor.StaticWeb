---
title: "Design Changes Are the Point — Code Cleanup Philosophy"
date: 2026-05-16
category: conventions
module: QualityGates
problem_type: convention
component: development_workflow
severity: high
applies_when:
  - "Choosing between a mechanical cleanup fix and a structural design change"
  - "A quality gate violation reveals a deeper design issue rather than a surface problem"
  - "Evaluating whether to refactor or to redesign during cleanup workflows"
tags:
  [
    design-changes,
    cleanup-philosophy,
    author-canon,
    code-quality,
    refactoring-strategy,
  ]
---

# Design Changes Are the Point — Code Cleanup Philosophy

## Context

Multiple cleanup sessions got stuck chasing formula-edge cases with shallow extractions
that satisfied the CRAP/SCRAP rules technically but did not actually improve code.
The user gave a direct instruction:

> "We are not focused on one type of code changes. We are in search of ALL OF THEM.
> If a pattern emerges and a design change is the recommended way to make our code
> much better, WE DO THAT."

This principle was codified into `rm-gates-cleanup` §0 and `rm-guide-cleanup` §9 as
a permanent cleanup philosophy.

## Guidance

The principle (as embedded in the cleanup skills):

> Code cleanup is not about one type of change. When a pattern emerges and a design
> change makes the code substantially better (per our author canon — Uncle Bob,
> Feathers, Ousterhout, Beck, Fowler, Farley), recommend it first and ask permission.
> Procedural cleanup rules are tools in the arsenal, not exclusive constraints.

There is also a **pause-and-reflect checkpoint**: after every few cleanup steps,
explicitly ask _"Are we making the code better here?"_ If yes, continue. If the answer
is "we are making the metrics better but the code is the same shape," step back and
consult the author canon for a design change.

**Author canon (for reference):**

| Author           | Key work                             | Cleanup contribution                        |
| ---------------- | ------------------------------------ | ------------------------------------------- |
| Robert C. Martin | Clean Code, The Clean Coder          | CRAP metric, Clean Architecture             |
| Michael Feathers | Working Effectively with Legacy Code | Seam identification, characterization tests |
| John Ousterhout  | A Philosophy of Software Design      | Deep modules, shallow module rejection      |
| Kent Beck        | Extreme Programming, TDD by Example  | Red-green-refactor, YAGNI                   |
| Martin Fowler    | Refactoring                          | Extraction patterns, code smells            |
| David Farley     | Modern Software Engineering          | Continuous delivery, fast feedback          |

**Decision framework:**

1. Diagnose the violation (what is actually wrong)
2. Is the fix a mechanical extraction? Execute it.
3. Does the fix require a design change? Propose it with rationale from the canon.
4. **Always ask permission before executing a design change.**

## Why This Matters

Without this principle, cleanup becomes a treadmill: extract a method, re-run metrics,
extract another method — CRAP drops incrementally but the design does not improve.

Two examples from the session where this principle was created:

- **I/O injection pattern**: `ResolveCoverageLinesAsync` (CRAP 30.0). Shallow extraction
  would have left it with `Process.Start` still untested. The design change (injectable
  `Func<string, Task<string?>>` parameter) dropped CRAP to 7.7.
- **FrozenDictionary switch replacement**: `LiteralFeature` (CRAP 8.0, CC=8). No
  extraction could reduce CC below 8. The design change (switch → FrozenDictionary)
  dropped CRAP to ~1.0.

Both were design changes, not mechanical extractions. Both required this philosophy
to be explicitly permitted.

## When to Apply

- **Beginning of session**: "What patterns do I see across these violations?" If a
  pattern emerges, propose design change first.
- **After 3-5 fixes**: "Is the design better than when we started?"
- **When stuck**: "Am I extracting methods just to hit a number without improving
  the code?"
- **When a fix touches 3+ files**: pause and evaluate whether there is a deeper
  abstraction waiting to be extracted.

## Examples

**Session: CRAP cleanup for tools/QualityGates**

| Step | Method                    | CRAP | Mechanical fix                                    | Design change                                         | Chosen                     |
| ---- | ------------------------- | ---- | ------------------------------------------------- | ----------------------------------------------------- | -------------------------- |
| 1    | ResolveCoverageLinesAsync | 30.0 | Extract guard clauses (CRAP→28, still untestable) | Func<> injection + pure logic extraction (CRAP 7.7)   | Design change              |
| 2    | LiteralFeature            | 8.0  | Inline at call sites (avoids CRAP, degrades code) | FrozenDictionary (CRAP 1.0)                           | Design change              |
| 3    | AddClassLines             | 9.2  | Nothing works — Cobertura says 100% covered       | None available (foreach over IEnumerable unavoidable) | Accept as formula artifact |

**Pause and reflect:** code is measurably better — two methods went from untestable to
fully tested with clean APIs. One artifact remains but is understood and documented.
The design changes produced real improvements; mechanical extraction would not have.

## Related

- [CRAP-Driven Functional Refactoring](/docs/solutions/best-practices/crap-driven-functional-refactoring-2026-05-12.md)
  — Concrete case study applying this philosophy to domain model validation
- [Superfluous Code Principles](/docs/solutions/superfluous-code-principles.md)
  — WHAT to remove (sibling doc); this doc covers WHEN and WHY to intervene
- [I/O Injection Pattern](/docs/solutions/design-patterns/io-injection-optional-func-parameter-2026-05-16.md)
  — First design change enabled by this philosophy
- [FrozenDictionary Switch Replacement](/docs/solutions/design-patterns/frozendictionary-switch-expression-replacement-2026-05-16.md)
  — Second design change enabled by this philosophy
