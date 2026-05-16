---
title: "CRAP Formula vs. Cobertura Coverage Divergence — Measurement Gap at High CC"
date: 2026-05-16
category: developer-experience
module: QualityGates
problem_type: developer_experience
component: tooling
severity: low
applies_when:
  - "A method with high CC passes Cobertura coverage but fails CRAP gates"
  - "Debugging why CRAP scores do not match reported coverage percentages"
  - "Evaluating whether to refactor CC or accept a CRAP score near the threshold"
tags:
  [
    crap-formula,
    cobertura,
    cyclomatic-complexity,
    measurement-gap,
    coverage,
    branch-rate,
  ]
---

# CRAP Formula vs. Cobertura Coverage Divergence

## Context

Two methods (`AddClassLines` at CRAP 9.2, `DiscoverFromSlnx` at CRAP 9.6) remained as FAILs
despite Cobertura XML reporting 100% line coverage and branch-rate values of 1.0 or 0.833.
This exposes a measurement divergence: the CRAP formula's notion of coverage percentage does
not map cleanly to Cobertura's `branch-rate` attribute, and the cyclomatic complexity count
from Roslyn analyzers does not match Cobertura's instrumented branch count.

## Guidance

The CRAP formula is:

```
CRAP = CC² × (1 − coverage%)³ + CC
```

Two numbers determine the score, and both come from different sources:

| Component     | Source                                   | What it counts                                                   |
| ------------- | ---------------------------------------- | ---------------------------------------------------------------- |
| **CC**        | Roslyn analyzers (source-level)          | `or` patterns, `foreach`, `??`, `?.`, `switch` arms as branches  |
| **coverage%** | Cobertura XML (IL-level instrumentation) | Only branches that exist in IL — implicit branches may be absent |

These diverge because:

- `or` patterns in switch expressions: source CC counts each alternative; IL may produce a
  single compare+jump pair
- `foreach` with `IEnumerable<T>`: source CC=1 for the loop construct; IL has
  MoveNext/Current dispatch that Cobertura instruments separately
- `??` null-coalescing: source CC=1; IL may inline as a conditional or may not
- `?.` null-conditional: same divergence

**Practical threshold:** at CC=8, passing CRAP (strictly greater than 8.0) requires
coverage above ~96% due to the cubic term. At 74%: 64×0.26³+8 = 9.1. At 96%: 64×0.04³+8 =
8.004. So methods at CC=8 need >96% coverage by the formula's math to pass — even though
Cobertura may report 100% branch coverage.

## Why This Matters

Prevents chasing "fake" violations. A method well-covered by Cobertura's metrics may still
fail the CRAP formula. This is the third category in the coverage gap taxonomy beyond the
existing conductor and switch-dispatcher patterns handled by `CoverageGapDetector`:

1. **Conductor methods** — delegating to other production methods (coverage only hits callees)
2. **Switch-dispatcher methods** — every arm delegates to one sub-method
3. **Formula-bound methods** — CC ≥ 8 and coverage >80% but CRAP still FAILs due to
   CC counting differences

For category 3, the right response is either:

1. Apply FrozenDictionary to drop CC below the threshold
2. Apply I/O injection to extract pure logic
3. Accept as a formula limitation — document the false positive

## When to Apply

Diagnose when ALL of these conditions hold:

1. Method has CC ≥ 8
2. Cobertura reports branch-rate > 0.80 (or line-rate > 0.95)
3. CRAP score still > 8.0
4. Method is not a conductor or switch-dispatcher (those have their own gap patterns)

The root cause is usually one of: `or` patterns inflating CC, implicit `foreach` branches,
or `??`/`?.` operators adding CC that Cobertura does not instrument.

## Related

- [Multi-Test-Project Coverage Merge](/docs/solutions/workflow-issues/multi-test-project-coverage-merge-2026-05-13.md)
  — The other major class of CRAP accuracy issues: incomplete coverage data (fixable)
- [Quality Gates Tool Operational Gotchas](/docs/solutions/developer-experience/quality-gates-tool-operational-gotchas-2026-05-09.md)
  — Broader tool operational context
- [FrozenDictionary Switch Replacement](/docs/solutions/design-patterns/frozendictionary-switch-expression-replacement-2026-05-16.md)
  — The primary fix for formula-bound switch expressions
