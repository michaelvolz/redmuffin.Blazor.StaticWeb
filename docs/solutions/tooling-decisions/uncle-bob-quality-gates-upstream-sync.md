---
title: "Uncle Bob quality gates — local ports vs upstream sync (2026)"
date: 2026-08-02
category: tooling-decisions
module: QualityGates
problem_type: tooling_decision
component: tooling
severity: medium
applies_when:
  - "Syncing tools/src/redmuffin.Tools.QualityGates against Uncle Bob public tools"
  - "Adding a new quality gate claimed to match an unclebob/* repo"
  - "Evaluating acceptance-pipeline or deintroverter-style tools for the cleanup suite"
  - "Correcting skill or ADR text about which upstream tools exist"
tags:
  - quality-gates
  - uncle-bob
  - upstream-sync
  - crap
  - scrap
  - mutation-testing
  - dry
  - dependency-checker
  - acceptance-pipeline
related_components:
  - development_workflow
  - testing_framework
  - documentation
---

# Uncle Bob quality gates — local ports vs upstream sync (2026)

## Context

`tools/src/redmuffin.Tools.QualityGates` ports Robert C. Martin’s agentic metric
suite into this repo as a .NET 10 CLI (separate solution, subcommand monolith —
see ADR-0002). The top requirement is **fidelity to the original tools**:
algorithms, thresholds, CLI shape, and scope should match upstream, not invent
repo-local bars (ADR-0003 locks SCRAP thresholds to scrap `policy.clj`).

A thorough sync pass against his public GitHub tools, AIR-J `AGENTS.md`, recent
X posts (@unclebobmartin), and the Clean Coder blog (no new tool posts there)
showed core formulas and thresholds mostly matched, with mid-2026 lag on
mutation operators and dependency-checker zones, plus false skill text denying
dry4* production DRY scanners. **Phase A (2026-08)** closed those local-port
gaps (operators, `--mutation-warning`, architecture zones/config, SCRAP
fidelity audit, doc/skill corrections). Still out of suite: acceptance
pipeline, deintroverter, swarm-forge, and mutate4java `--update-manifest`.

This learning records the map so agents do not re-research the whole stack
before every gates change.

## Guidance

### Local suite ↔ upstream map

| Local gate | Subcommand | Upstream | Sync status |
| --- | --- | --- | --- |
| CRAP | `crap` | [crap4clj](https://github.com/unclebob/crap4clj) / [crap4java](https://github.com/unclebob/crap4java) | Aligned — formula + ≤8 |
| SCRAP | `scrap` | [scrap](https://github.com/unclebob/scrap) | Thresholds aligned; Phase A fidelity audit (no code change; no invented bands) |
| Architecture | `architecture` | [dependency-checker](https://github.com/unclebob/dependency-checker) | Zone metrics + richer config (2026-08 Phase A) |
| Mutation | `mutation` | [clj-mutate](https://github.com/unclebob/clj-mutate) / [mutate4java](https://github.com/unclebob/mutate4java) | Operators + `--mutation-warning` (2026-08 Phase A); `--update-manifest` still deferred |
| Duplicates | `duplicates` | [dry4clj](https://github.com/unclebob/dry4clj) / [dry4java](https://github.com/unclebob/dry4java) | Defaults aligned (0.82 / 4 / 20) |
| Depth | `depth` | **Local only** (Ousterhout / Fowler / Metz peer to CRAP) | Keep; not an unclebob repo |
| Slopwatch | external | **Local only** (LLM anti-cheat) | Keep; not an unclebob repo |

Canonical command inventory and exit codes: `tools/README.md`. Architecture
config keys: `component-map`, `allowed-dependencies` (supports `all` /
`:all`), `forbidden-dependencies`, `allowed-exceptions`,
`ignored-components`, `healthy-threshold` (default 0.3), `fail-on-cycles`,
`fail-on-violations`.

### Still aligned (low drift)

- **CRAP:** `CRAP = CC² × (1 − coverage)³ + CC`, keep changed methods **≤ 8**
  (AIR-J `AGENTS.md`).
- **SCRAP policy constants** in `ScrapRecommender` match scrap `policy.clj`
  for the values that are ported: stable max-scrap 12, effective-duplication ≤ 3,
  low-assertion ratio ≤ 0.35; SPLIT triggers (avg 10 / dup 20 /
  subject-repetition 12 / min examples 12 / high-pressure blocks 2 /
  max-scrap 35); high-pressure block cutoff 18. Upstream policy.clj also has
  pressure bands 55 / 35 that are not separate local constants (only the
  high-pressure cutoff 18 is used for block counting). Jaccard 0.5 for
  test-body fuzzy match.
- **Duplicates:** `DupesOptions` defaults threshold `0.82`, min-lines `4`,
  min-nodes `20` — same as dry4clj / dry4java / dry4go.
- **Mutation workflow shape:** differential manifest, `--scan`,
  `--mutation-warning` default 50, max-workers — same discipline as
  clj-mutate / mutate4java.

### Phase A complete (2026-08) — remaining drift

1. **Mutation operators — done for mutate4java parity targets.**  
   Local now includes logical `&&`/`||`, unary strip (`!` / `-`), and
   null-rvalue discovery on reference-like return/assignment/equals values,
   plus wired `--mutation-warning` (default 50). Remaining UX gap:
   `--update-manifest` (local still uses `--mutate-all`).

2. **Architecture zones — done.**  
   Local reports abstractness, instability, main-sequence distance, and
   zones healthy / pain / useless (`healthy-threshold` default 0.3), plus
   `forbidden-dependencies`, `allowed-exceptions`, `ignored-components`,
   and `all` / `:all` allowlists.

3. **SCRAP fidelity audit — done (no code change).**  
   STABLE/SPLIT/actionability constants in `ScrapRecommender` match the
   ported `policy.clj` values (stable max-scrap 12, dup ≤ 3, low-assert
   ≤ 0.35; SPLIT avg 10 / dup 20 / subject-rep 12 / min examples 12 /
   high-pressure blocks ≥ 2 at cutoff 18 / max-scrap 35). Jaccard 0.5 and
   `--write-baseline` / `--compare` are present. No `HelperHidden` charge
   path in the C# port. Upstream pressure bands 55 / 35 are not separate
   local decision constants — only cutoff 18 is used for SPLIT block
   counting. Do **not** invent weighted pressure bands without a proven
   port of that policy path.

4. **Doc / skill correction — done.**  
   dry4* production DRY scanners are documented; `duplicates` is their
   port. `uncle-bob-metrics-discipline.md`, ADR-0002 subcommand names,
   brainstorm superseded text, and `tools/README.md` mutation/architecture
   rows were corrected.

### New upstream tools — include or not

| Tool | Role | Recommendation |
| --- | --- | --- |
| [Acceptance-Pipeline-Specification](https://github.com/unclebob/Acceptance-Pipeline-Specification) | Gherkin → JSON IR → acceptance entrypoints + **acceptance mutation** (example-value mutants) + IR-DRY | **Largest missing discipline layer** if the project wants full agentic ATDD. Capability expansion, not a one-line subcommand. |
| [deintroverter4clj](https://github.com/unclebob/deintroverter4clj) | Flags introverted tests (assertions not grounded in SUT) | Optional **advisory** C# port only. Upstream README: **not for CI gates**. |
| [swarm-forge](https://github.com/unclebob/swarm-forge) | Multi-agent two/four/six-pack orchestration | Process tooling, **not** a quality metric gate. |
| [arch-view](https://github.com/unclebob/arch-view) | Interactive dependency viewer | Optional UX; not pass/fail. |
| [speclj-structure-check](https://github.com/unclebob/speclj-structure-check) | Speclj nesting sanity | Clojure-specific; scrap absorbed much of it. Only consider a TUnit structure lint if needed. |
| crap4go / mutate4go / dry4go / dry4java | Same metrics in other languages | Reference implementations, not install targets for this repo. |

### Priority sync plan

**Done (Phase A, 2026-08):**

1. Mutation operator parity from mutate4java (`&&`/`||`, unary strip,
   null-rvalue) + tests.
2. Architecture zone metrics and richer config (forbidden edges, exceptions,
   ignored components, healthy-threshold, `all` allowlists).
3. Module-size discipline: `--mutation-warning` wired (default 50, warn-only).
4. SCRAP fidelity audit against `policy.clj` / extraction pressure (no invented
   thresholds; bands 55/35 not ported as decision inputs).
5. Skill/docs that denied dry4* production DRY scanners corrected.

**Still open (not Phase A):**

6. Acceptance pipeline (if product wants Gherkin ATDD + acceptance mutation).
7. Optional advisory deintroverter-for-C#.
8. mutate4java-style `--update-manifest` (local still has `--mutate-all`).
9. **Do not** hard-gate swarm-forge or deintroverter.

### Operating philosophy (public 2026 stance)

Measure rather than line-review: coverage, dependency structure, cyclomatic
complexity, module size, mutation kill rate. Bars he repeatedly states: CRAP
≤ 8, coverage in the high 90s, kill survivors, split large modules. Agents
write comprehensive tests; humans manage via tools and interrogation.

## Why This Matters

ADR-0002/0003 make “match upstream” the definition of correctness for these
gates. When mutate4java adds operators and the local port does not, mutants
that upstream would catch stay green here. When dependency-checker adds zone
metrics and the local architecture gate does not, pain/useless components
never surface. Wrong skill text about dry4* forces every agent into a
re-research cycle and can block a legitimate `duplicates` gate.

## When to Apply

- Before editing any port under `tools/src/redmuffin.Tools.QualityGates/`
  (especially mutation, scrap, architecture, dupes analysis and commands).
- When proposing a new gate “because Uncle Bob has it” — verify the upstream
  repo, CI-vs-advisory intent, and research-before-implement rule.
- When cleaning up or writing skills/ADRs that name the Uncle Bob toolchain.
- When planning acceptance-level discipline beyond unit CRAP/mutation.

## Examples

### Mutation gap

Local short-circuit logic such as `if (x != null && x.Id > 0)` is not exercised
by an `&&`→`||` mutant until that rule exists. Porting logical operators and
null-rvalue replacement from mutate4java closes that blind spot without
changing the CRAP formula.

### Architecture config today vs target

Today (`tools/quality-gates/architecture-rules.yml`):

```yaml
component-map: { ... }
allowed-dependencies: { ... }
fail-on-cycles: true
fail-on-violations: true
```

Upstream-shaped extension (illustrative):

```yaml
healthy-threshold: 0.3
forbidden-dependencies:
  - from: Presentation
    to: Infrastructure
allowed-exceptions:
  - from-ns: Some.Legacy.Ns
    to-ns: Some.Other.Ns
ignored-components:
  - TestInfra
```

### DRY documentation correction

**Wrong:** “SCRAP covers all duplication; there is no production DRY scanner.”  
**Right:** SCRAP covers **test** structural duplication and extraction pressure;
**dry4\*** / local `duplicates` covers **production** structural candidates at
Jaccard threshold 0.82.

## Related

- `docs/solutions/tooling-decisions/crap-quality-gates-pipeline.md` — original
  pipeline architecture decision
- `docs/solutions/developer-experience/quality-gates-tool-operational-gotchas.md`
  — operational patterns for gates
- `docs/solutions/tooling-decisions/slopwatch-integration-analysis.md` — local
  pre-gate (not Uncle Bob)
- `docs/adr/0002-quality-gates-toolchain.md`
- `docs/adr/0003-scrap-test-structural-analyzer.md`
- `docs/adr/0004-depth-structural-quality-gate.md` (local Depth peer to CRAP)
- Refresh candidates (stale “no dry” claims):
  `docs/brainstorms/2026-05-09-dupes-quality-gate-requirements.md`

## Upstream references

- [unclebob GitHub](https://github.com/unclebob)
- [AIR-J AGENTS.md](https://github.com/unclebob/AIR-J/blob/master/AGENTS.md)
- [crap4java](https://github.com/unclebob/crap4java), [scrap](https://github.com/unclebob/scrap),
  [clj-mutate](https://github.com/unclebob/clj-mutate), [mutate4java](https://github.com/unclebob/mutate4java),
  [dry4clj](https://github.com/unclebob/dry4clj), [dry4java](https://github.com/unclebob/dry4java),
  [dependency-checker](https://github.com/unclebob/dependency-checker)
- [Acceptance-Pipeline-Specification](https://github.com/unclebob/Acceptance-Pipeline-Specification)
- [deintroverter4clj](https://github.com/unclebob/deintroverter4clj)
- [swarm-forge](https://github.com/unclebob/swarm-forge)
- [@unclebobmartin](https://x.com/unclebobmartin)
