---
module: tools
tags: [structural-quality, shallow-methods, coding-standards, depth-gate]
problem_type: design-patterns
date: 2026-05-17
title: "When Are Shallow Methods Good Design? Author Research"
---

# When Are Shallow Methods Good Design? Author Research

## Context

The Depth structural quality gate flags all private methods with LOC ≤ 4
and no branching as shallow(3) FAIL. This blanket rule is too aggressive.
Five authors converge on a more nuanced view: shallow methods can be
excellent design when the name conveys intent better than the body.

## Author Positions

### Kent Beck — Intention Revealing Selector

> "The opportunity through intention revealing message names is the most
> compelling reason to keep methods small."

From _Smalltalk Best Practice Patterns_: The **Intention Revealing Selector**
test asks: _"Imagine a second very different implementation. Will the name
work for both implementations?"_ If yes, the name captures _what_ not _how_.

His 4th rule of simple design is _"Fewest possible classes and methods"_ —
the lowest-priority rule. Small methods are good, but only if they earn
their keep. A 1-line wrapper that replaces a comment is ideal. A 1-line
wrapper that just renames an operator is not.

### Martin Fowler — Inline Function Refactoring

From refactoring.com: _"When the function body is as self-descriptive as
the name, inline it."_

The **test**: look at the call site. If reading `moreThanFiveLateDeliveries(driver)`
takes MORE mental work than reading `driver.numberOfLateDeliveries > 5`,
inline. The function name should add semantic information the raw expression
doesn't convey.

### John Ousterhout — Deep Modules

> _"It's more important for a module to have a simple interface than a
> simple implementation."_

His "shallow module" critique targets **public API surfaces**, not private
helpers. A private method's interface cost is minimal — just its name.
Independent shallow helpers (e.g., `isAvailable()`, `hasDiscount()`) are fine.

> _"First make functions deep, then try to make them short enough to be
> easily read."_

### Sandi Metz — The Wrong Abstraction

> _"Duplication is far cheaper than the wrong abstraction."_

> _"You should not reach for abstractions, but instead, resist them until
> they absolutely insist upon being created."_

This warns against PREMATURE extraction, not against existing small helpers
that are already stable abstractions.

### Robert C. Martin — Clean Code Functions

> _"The first rule of functions is that they should be small. The second
> rule of functions is that they should be smaller than that."_

He sets NO minimum line count. The test: _"If a function does only those
steps that are one level below the stated name of the function, then the
function is doing one thing."_

## Decision Heuristic: Good vs Bad Shallow Methods

| Criterion           | GOOD (keep)                                                   | BAD (inline)                                        |
| ------------------- | ------------------------------------------------------------- | --------------------------------------------------- |
| Name adds info      | `isAvailable()` wraps `qty > 0` — conveys domain intent       | `moreThanFiveLateDeliveries()` wraps `n > 5`        |
| Caller count        | Called 3+ times — DRY applies                                 | Called once — no reuse value                        |
| Abstraction level   | Part of Composed Method decomposition — consistent level      | Body as clear as name — no indirection needed       |
| Domain concept      | `isApplicationInProduction(headers)` — captures business rule | `doubleValue()` wraps `(double)x`                   |
| Extension method    | On framework/scalar type, widely useful                       | On own type, only used in one caller                |
| Parameter stability | Stable parameter set                                          | Accumulating parameters + conditionals (Metz smell) |

## Application to Depth Gate

**Phase 1** (no caller analysis): Flag all private LOC ≤ 4 no-branching
methods as shallow(3). This is a survey signal, not a condemnation.

**Phase 2** (single-caller filter): For each flagged shallow method, walk
the project's Roslyn trees to count callers. If called from ≥ 3 distinct
methods, suppress the shallow signal — it's a legitimate abstraction.

**Decision tree for responding to a shallow(3) FAIL:**

1. Does the name add semantic value the raw expression doesn't? → KEEP
2. Called from ≥ 3 distinct places? → KEEP
3. Extension method on a framework/scalar type? → KEEP
4. Part of a visitor/override pattern (Roslyn walkers, interface impls)? → KEEP
5. All four NO? → INLINE into callers

## Extension Methods

Extension methods on framework types (`string`, `IEnumerable<T>`) follow
a different design convention — they're API surface for discoverability.
They should be excluded from the shallow signal or given a higher threshold.
C# LINQ is the canonical example: dozens of individually trivial extension
methods that are collectively powerful.

## References

- Beck, _Smalltalk Best Practice Patterns_
- Fowler, _Refactoring_, Chapter 6 "Inline Function"
- Ousterhout, _A Philosophy of Software Design_, Chapter 4 "Modules Should Be Deep"
- Metz, _The Wrong Abstraction_ (blog post, 2016)
- Martin, _Clean Code_, Chapter 3 "Functions"
