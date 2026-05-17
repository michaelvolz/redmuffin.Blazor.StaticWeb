---
date: 2026-05-17
title: "Depth Quality Gate: Design Decisions and Requirements"
tags: [quality-gates, depth, structural-quality, requirements, design]
description: "Full design documentation for the Depth structural quality gate — scope, signals, scoring, pipeline position, and every design decision made during the 2026-05-17 grilling session."
module: tools
problem_type: design
---

# Depth Quality Gate: Design Decisions and Requirements

Design decisions made during the 2026-05-17 grill-with-docs session. Every
option considered, every rationale recorded. Use this document when revisiting
any decision — it preserves the full context for future re-evaluation.

## Context

SN-0040 ("Audit tools solution for shallow methods, create severity metric for
refactoring priority") expanded into a full quality gate after researching
author positions on structural code quality. The existing gates (CRAP, SCRAP,
Architecture, Mutation, Duplicates) all focus on risk, test quality,
dependencies, thoroughness, or redundancy — none detect _over-decomposition_.

## Scope: Phase 1 vs. Phase 2

### Phase 1 (Implemented Now)

Four signals, each detectable via Roslyn:

1. **Shallow methods** (weight 3) — Ousterhout's core concern. A method where
   interface cost (name, signature, parameters, call site) equals or exceeds
   the implementation benefit. Detected as: LOC ≤ 4, single caller or private,
   trivial body (no loops, no branching, just delegation or simple arithmetic).

2. **Wrong abstraction** (weight 2) — Metz's signal. An extracted helper method
   containing if/switch statements that branch on parameter values, indicating
   the abstraction is handling multiple concerns gated by a parameter. Detected
   via Roslyn body inspection: conditional on formal parameter → wrong
   abstraction.

3. **Parameter bloat** (weight 1) — Martin, Fowler, NDepend, SonarQube. More
   than 4 parameters on any method. Uncle Bob: "Triadic should be avoided." The
   weakest signal alone but compounds with others.

4. **Entanglement proxy** (weight 2) — Ousterhout's entanglement signal,
   approximated. Detected as: a private helper method whose parameter count
   exceeds what the caller's expressiveness would suggest (caller passes
   multiple values that the callee combines in a way forcing the reader to
   flip back and forth).

### Phase 2 (Future Enhancement)

Full call-graph entanglement analysis. Builds a true call graph via Roslyn
semantic model, detects when caller and callee share mutable state, and flags
method chains where the reader must hold both implementations in memory
simultaneously. Replaces the Phase 1 proxy with precise detection.

**Why split:** The four Phase 1 signals cover ~80% of structural problems.
Full entanglement analysis requires building a cross-file call graph with
semantic resolution — substantial engineering effort. Phase 1 proves the gate
concept before investing in Phase 2.

## Scoring: Composite Weight Model

### Individual Signal Weights

| Signal             | Weight | Alone |
| ------------------ | ------ | ----- |
| Shallow method     | 3      | FAIL  |
| Wrong abstraction  | 2      | WARN  |
| Parameter bloat    | 1      | INFO  |
| Entanglement proxy | 2      | WARN  |

### Composite Thresholds

| Composite Score | Classification | Exit Code Impact    |
| --------------- | -------------- | ------------------- |
| ≥ 3             | FAIL           | Gate exits 2        |
| = 2             | WARN           | Advisory only       |
| = 1             | INFO           | Verbose output only |
| = 0             | CLEAN          | No output           |

### Rationale

Weight 3 for shallow methods: this is the gate's primary purpose. A single
shallow method alone justifies a FAIL — no need for compounding signals.

Weight 2 for wrong abstraction and entanglement: these are design-level concerns
that individually warn but don't fail. Combined with any other signal (≥3
composite), they fail.

Weight 1 for parameter bloat: the weakest signal. Informational alone. But
combined with shallow (3+1=4 FAIL) or wrong abstraction (2+1=3 FAIL), it
confirms the problem.

### Rejected Alternative

"All signals equal weight, fail on any single signal" — rejected because
parameter bloat alone is common in legitimate scenarios (external API wrappers,
serialization DTOs). Failing on `void Configure(int a, int b, int c, int d, int
e)` would generate noise.

## Gate Priority: Peer to CRAP

### The Conflict Scenario

CRAP says "extract this method to reduce complexity." Depth says "that
extraction would be shallow — interface cost exceeds benefit." Who wins?

**Decision: Neither auto-wins.** Depth and CRAP are equal peers. The agent sees
both signals and must find a third path that satisfies both:

1. Don't extract shallow wrappers — collapse the call chain instead
2. Extract genuine seams (≥5 lines of real logic per Feathers)
3. Reduce CC through functional patterns (FrozenDictionary, Array.TrueForAll)
   rather than method extraction
4. Write characterization tests for the larger method — CRAP drops with
   coverage even without extraction

### Rejected Alternatives

- **Depth above CRAP:** Would auto-block valid extractions during incremental
  cleanup. A method that's temporarily shallow during a multi-step refactor
  shouldn't fail the gate.
- **CRAP above Depth:** Would allow CRAP-driven extraction to create shallow
  methods without pushback. Structural quality is not optional.

### Decision Tree (for rm-gates-cleanup)

When both CRAP and Depth flag the same method:

1. Can you reduce CC without extracting? (functional patterns, early returns,
   switch→dictionary) → Do that. Both gates pass.
2. Is there a genuine seam ≥5 lines with different responsibility? → Extract
   it. Depth won't flag genuine seams.
3. Neither possible? → Write characterization tests. CRAP drops with coverage.
   Depth stays clean because no extraction occurred.

## Pipeline Position

### Execution Order

```
Architecture → Depth → CRAP → SCRAP → Mutation → Duplicates
```

Architecture is first (structural dependency violations block everything).
Depth runs before CRAP so the agent sees structural problems before deciding
extraction targets. This prevents the cycle: CRAP says extract → extraction is
shallow → new untested method creates MORE CRAP → CRAP says extract again.

### AllCommand Integration

Depth is always-on in `AllCommand` (like CRAP, unlike Mutation which is
opt-in). It runs on all source projects discovered from the `.slnx`. No
special configuration required — auto-discovers files via `SlnxProjectDiscovery`.

No new CLI flags needed for Phase 1. Phase 2 entanglement analysis may add
`--depth-call-graph` flag.

## Exit Codes

| Exit | Meaning                                    |
| ---- | ------------------------------------------ |
| 0    | No FAIL methods found (WARN/INFO allowed)  |
| 1    | Tool error (file not found, parse failure) |
| 2    | FAIL methods found (composite ≥ 3)         |

Matches the existing gate convention. WARN and INFO are advisory — they appear
in output but don't trigger exit code 2.

## Output Format

Per-method severity with signal breakdown:

```
FAIL  ShallowMethod.cs:42  Helper()  composite=4  [shallow(3) + params(1)]
WARN  Service.cs:15        Wrapper() composite=2  [wrong-abstraction(2)]
INFO  DTO.cs:8             Config()  composite=1  [params(1)]
```

JSON mode outputs the same data structured per method with signal enumeration.

## Name

**Depth** — Ousterhout's exact term for the concept the gate measures. Matches
the existing naming convention (Architecture, Mutation, Duplicates — full
words, no abbreviations). CLI subcommand: `dotnet run -- depth`.

Rejected alternatives:

- StructuralDepth — verbose, adds no precision
- Shallow — names the problem, not the measurement. Negative naming
  (AntiPatternDetector-style) breaks the convention
- CodeStructure — too generic, overlaps with Architecture

## References

- [Author Research: Shallow Methods and Structural Quality](../research/structural-depth-author-research-2026-05-17.md)
- [ADR-0004: Depth Quality Gate](../adr/0004-depth-structural-quality-gate.md)
- [SN-0040: Audit tools solution for shallow methods](../sidenotes/SN-0040.md)
- John Ousterhout, _A Philosophy of Software Design_ (2021)
- Michael Feathers, _Working Effectively with Legacy Code_ (2004)
- Robert C. Martin, _Clean Code_ (2008)
- Martin Fowler, _Refactoring_ (2nd ed., 2018)
- Sandi Metz, "The Wrong Abstraction" (2016)
- Kent Beck, _Implementation Patterns_ (2007)
