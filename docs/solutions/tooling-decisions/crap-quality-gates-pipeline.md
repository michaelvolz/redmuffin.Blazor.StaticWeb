---
title: CRAP Quality-Gates Pipeline with Uncle Bob Discipline
date: 2026-05-09
category: tooling-decisions
module: tools
problem_type: tooling_decision
component: tooling
severity: high
applies_when:
  - Adding automated quality gates to a .NET solution
  - Configuring CI to enforce code quality on agent-produced code
  - Evaluating Roslyn-based analysis tools for Blazor WASM projects
tags:
  [
    crap,
    quality-gates,
    uncle-bob,
    roslyn,
    cyclomatic-complexity,
    mutation-testing,
    metrics-driven,
  ]
---

# CRAP Quality-Gates Pipeline with Uncle Bob Discipline

## Context

The repo lacked automated quality gating for agent-produced code. Agents multiply productivity 10x but also accelerate technical debt unless constrained by metric gates. Coverage alone is insufficient — a 100% covered method with cyclomatic complexity of 50 is high-risk. The CRAP metric (Change Risk Analyzer and Predictor) pairs complexity with coverage to expose methods that are both complex and poorly tested.

Adding Roslyn-heavy tool projects to the main Blazor WASM solution would make every `dotnet build` compile tool dependencies — unacceptable for already-slow AOT compilation.

## Guidance

**Architecture pattern: Separate solution + subcommand monolith + local NuGet feed.**

Created `tools/redmuffin.Tools.slnx` with one project (`redmuffin.Tools.QualityGates`) using subcommands: `crap`, `scrap`, `dupes`, `arch`, `all`. Installed via local NuGet feed (`tools/nupkgs/`) that never leaves the repo. All gates share one Roslyn workspace — cyclomatic complexity computed once, reused across CRAP and SCRAP.

**Core gates:**

- CRAP ≤8 per method: `CC² × (1 − coverage)³ + CC`
- 100% mutation kill rate
- High-90s statement/branch/path coverage
- Near-zero structural duplication
- Acyclic layered architecture

**Workflow:** ATDD (Gherkin Given-When-Then) precedes all production code. Run `dotnet quality-gates all` on every build. Use `--changed` for incremental feedback under 30 seconds. On any gate breach, direct the agent to fix and re-run.

The full discipline is codified in `rm-guide-metrics`. See `docs/adr/0002-quality-gates-toolchain.md` for architecture rationale.

## Why This Matters

Coverage alone is a vanity metric — a method with CC=50 at 100% coverage is still a maintenance nightmare. CRAP gives a single number that gates on both dimensions. Metric gates are the only scalable way to enforce quality when agents write code at machine speed. The separate-solution pattern keeps tool dependencies from bloating app build times.

## When to Apply

- Every CI build and after every significant agent edit
- When evaluating whether agent-produced code meets quality thresholds
- When adding new Roslyn-heavy analysis to a slow-building solution — use the separate solution + local NuGet feed pattern

## Examples

```bash
# Full gate check
dotnet quality-gates all

# Incremental (changed files only, <30s)
dotnet quality-gates --changed

# CRAP only
dotnet quality-gates crap --threshold 8

# Install/update the tool
dotnet tool update --local redmuffin.Tools.QualityGates
```

## Related

- `docs/adr/0002-quality-gates-toolchain.md` — Architecture Decision Record
- `rm-guide-metrics` — Metrics-driven development standards
- `tools/README.md` — Tool usage and development guide
