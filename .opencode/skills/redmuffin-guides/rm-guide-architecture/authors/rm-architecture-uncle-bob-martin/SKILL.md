---
name: rm-architecture-uncle-bob-martin
description: Robert C. Martin's SOLID principles, Clean Architecture, and metrics-driven quality standards (CRAP ≤8, mutation kill rate 100%, high-90s coverage, dependency integrity). Loaded by rm-guide-architecture during architecture work. Do not load independently.
version: 1.0
author: Compiled from primary sources (Uncle Bob's GitHub repositories, X posts, and Clean AI series)
tags:
  [
    agentic-coding,
    clean-code,
    tdd,
    atdd,
    crap-metric,
    mutation-testing,
    metrics-driven-development,
    multi-agent-coordination,
    clojure-to-csharp-adaptation,
  ]
languages: [C#, PowerShell, TypeScript, Java, Clojure]
prerequisites: OpenCode agent with file I/O, test runner integration, and metric tooling execution capabilities
---

# Uncle Bob Martin Agentic Coding Discipline: Complete Reference Guide

Robert C. Martin has distilled four months of intensive experimentation into a rigorous, metrics-first discipline for agentic development. This guide synthesizes every publicly documented concept, pattern, workflow, tip, and tool he recommends. It is structured for direct use in OpenCode as an agent skill. All recommendations derive exclusively from his GitHub repositories[](https://github.com/unclebob), X activity under @unclebobmartin, and the Clean AI: Agentic Discipline video series on cleancoders.com. No external interpretations or secondary sources are included.

The core thesis is that agents multiply developer productivity by an order of magnitude provided the human enforces strict discipline. Vibe coding—letting agents generate code without measurement—produces unmaintainable legacy. Clean Code principles, SOLID design, and semantic stability remain mandatory; agents must be directed to uphold them through automated gates rather than human line-by-line review.

## 1. Foundational Principles

Agentic development succeeds only when the following axioms are enforced without exception:

- Tests force better structured code. Acceptance tests (ATDD via Gherkin-style Given-When-Then) precede any production implementation.
- Coverage alone is insufficient; pair it with cyclomatic complexity via the CRAP metric.
- Mutation testing eliminates false confidence in coverage.
- File and module sizes must remain small (target <50 mutation sites per file; refactor aggressively).
- Duplication must be driven to near zero using fuzzy structural matching.
- Dependency structure, module cohesion, and cyclomatic complexity must be measured and corrected automatically.
- Human role shifts from coder to orchestrator: measure, probe agents with targeted questions, and direct refactoring. Never perform manual code review if metrics pass.
- Static vs dynamic typing, OO vs functional, high vs low level—these are irrelevant. Scenarios, testing discipline, coverage, and token/context compression matter.
- Objects and clean architecture still apply; agents require them more than humans because agents lack intuition.

Counter to any assumption that agentic coding relaxes quality standards: the opposite holds. Discipline prevents the exponential growth of technical debt that agents accelerate when left unchecked.

### 1.1 TOP REQUIREMENT — Fidelity to Original Tools

All tool implementations in this project must replicate Uncle Bob's original
tools as closely as possible. This is the overriding requirement for every
quality gate:

- **Algorithm fidelity**: Thresholds, formulas, and clustering logic must
  match the original source code exactly. Never invent thresholds or tweak
  formulas without explicit evidence from the original repo.
- **CLI interface fidelity**: Flag names, exit codes, and output formats
  must match the original tools. If the original uses `--verbose`, `--json`,
  and exits 0/2, our tool must do the same.
- **Scope fidelity**: Implement only what Uncle Bob's tools actually do.
  SCRAP analyzes test files — not production source. There is no separate
  production-code duplication scanner. The dependency checker validates
  architecture, not code style.
- **Decision authority**: When in doubt, the original source code is the
  answer. User preference, convenience, or "best practice" are irrelevant
  unless the original tool explicitly supports them.
- **Research before implementing**: Never implement a tool or gate without first reading the original repo's README, AGENTS.md, and source code. Confirm existence,
  scope, thresholds, and CLI interface. Never build from a third-party
  summary or memory.

## 2. Core Workflows

### 2.1 Acceptance Test Driven Development (ATDD) + TDD Pipeline

1. Write Gherkin-style acceptance tests in plain-text .txt or .feature files using Given-When-Then format tailored to the domain.
2. Build or use a custom parser that converts Gherkin to language-specific test skeletons (e.g., Speclj for Clojure, xUnit/NUnit for C#, Jest for TS). Separate parser from generator with an intermediate JSON/EDN representation to prevent cheating.
3. Implement production code strictly via TDD: red-green-refactor loop driven by you.
4. Never execute the application until all acceptance and unit tests pass.
5. Upon completion, run full metric suite (coverage, CRAP, mutation, duplication, dependencies).
6. Refactor any violations; repeat until all gates pass.

This workflow produced zero-bug first-run systems in Uncle Bob’s public experiments (AIR-J language, custom wiki, Empire game updates).

### 2.2 Metrics-Driven Refactoring Cycle (Run After Every Significant Change)

- Coverage: Target high 90s percent (instruction-level where possible).
- CRAP score: ≤8.0 per method/function. Formula (exact):
  CRAP = CC² × (1 − coverage)³ + CC
  where CC = cyclomatic complexity, coverage = fraction from test runner.
- Mutation kill rate: 100% on all generated mutants (use differential mode for speed).
- Duplication: Run fuzzy structural duplicate scanner; reduce all high-score instances.
- Module size: Split any file exceeding ~50 mutation sites or token thresholds that inflate context windows.
- Dependency check: Enforce acyclic, layered architecture (use custom arch-view tool).
- Test quality (SCRAP equivalent): No long test chains, duplicated setup, hidden helpers, or zero-assertion examples.

Never proceed past a failing quality gate without fixing the violation and re-running the suite. Exit code conventions: 0 = pass, 2 = CRAP threshold breach.

### 2.3 Multi-Agent Coordination (Swarm Pattern)

Use swarm-forge[](https://github.com/unclebob/swarm-forge) or equivalent:

- Define roles (architect, coder, reviewer, tester) via role-specific prompt files.
- Assign separate git worktrees per agent.
- Coordinate via tmux panes and notification scripts.
- Coder implements slices; reviewer verifies metrics only.
- Message passing prevents race conditions.

For C#/PowerShell/TS: Replicate swarm-forge using PowerShell scripts for worktree management and named pipes or file-based messaging.

### 2.4 Differential Mutation and Incremental Checks

- Initial full mutation scan embeds a manifest footer.
- Subsequent runs mutate only changed code (differential strategy).
- This keeps CI fast while maintaining full coverage guarantees.

## 3. Tools and Latest Versions (May 2026)

All original tools are open-source under Uncle Bob’s GitHub. Convert them directly to C#/PowerShell/TS by feeding the repo README + source to your OpenCode agent with the prompt: “Replicate exact functionality and CLI interface for [language]; preserve CRAP formula and differential logic; output as native executable or module.”

- **CRAP Analyzers** (core risk metric):
  - crap4clj (Clojure original): https://github.com/unclebob/crap4clj
  - crap4java (Java port, latest): https://github.com/unclebob/crap4java
    - Builds with Maven; runs JaCoCo then applies formula; --changed flag for incremental; exits 2 on breach.
  - Existing TS port (inspired): https://github.com/sebassdc/crap4ts (use as reference).
  - C# adaptation prompt ready: Agent creates dotnet tool using Microsoft.CodeAnalysis for CC and coverlet for coverage.

- **Mutation Testing**:
  - clj-mutate (Clojure, with differential strategy): Pinned in AIR-J deps[](https://github.com/unclebob/AIR-J). Supports --scan (fast prepass), --max-workers, manifest-based differential mode.
  - mutate4java: Produced during public experiments; replicate from clj-mutate logic + Java bytecode mutation (use Roslyn for C# equivalent).

- **SCRAP (Test Structural Analyzer)**:
  - https://github.com/unclebob/scrap
  - Measures test smells (setup duplication, long chains, zero-assertion examples, extraction pressure via Jaccard similarity on normalized forms).
  - Outputs refactoring recommendations (STABLE/LOCAL/SPLIT) and AI-actionability classes.
  - Replicate for C# (xUnit/NUnit specs) or TS (Jest) using AST parsing (Roslyn or TypeScript compiler API).

- **Dependency / Architecture Checker**:
  - dependency-checker: Pinned in AIR-J deps (SHA `e8f3579`). Parses project references, checks for cycles, validates layered architecture rules (no upward references).
  - arch-view: https://github.com/unclebob/arch-view (agent-built; generates dependency graphs and projections).
  - Run with: `clj -M:check-dependencies`
  - Replicate for C# using Roslyn to parse project references and `dotnet-format` workspace analysis.

- **SCRAP** also handles test-code duplication internally: fuzzy Jaccard similarity (≥0.5) on normalized test bodies, three-channel classification (harmful, case-matrix, subject repetition), and extraction pressure. There is NO separate production-source-code duplication scanner in Uncle Bob's toolchain — SCRAP covers duplication detection for tests.

- **Multi-Agent Orchestrator**:
  - swarm-forge: https://github.com/unclebob/swarm-forge (tmux + worktrees + role prompts).

- **Custom Tools**:
  - AIR-J repo[](https://github.com/unclebob/AIR-J) contains full AGENTS.md documenting pinned toolchain, structure checks, and combined workflow commands:
    clj -M:check-structure spec
    clj -M:spec
    clj -M:cov
    clj -M:crap
    clj -M:mutate src/.../file.clj --scan
    clj -M:check-dependencies

## 4. Language-Specific Adaptation Guide (C#/PowerShell/TypeScript)

Feed this entire skill plus each original repo to your OpenCode agent. Explicit conversion instructions:

For C#:

- Use Roslyn for CC and duplication.
- coverlet + xUnit for coverage.
- Stryker.NET or custom mutant generator for mutation testing (preserve differential manifest).
- Output as dotnet global tool.
- Functional C# patterns (immutable records, LINQ pipelines, pure static methods,
  FrozenDictionary lookups, pattern-matching switch expressions) implement
  Uncle Bob's Clean Code principles — small functions, low complexity, zero
  side effects. Full catalog at `rm-guide-csharp-features`.

For PowerShell:

- Use PSScriptAnalyzer for complexity.
- Pester for tests/coverage.
- Custom AST visitor for mutation/duplication.

For TypeScript:

- Use existing crap4ts as base.
- ts-morph or TypeScript compiler API for CC/duplication.
- Jest + custom mutator (or ts-mutate equivalent).

Prompt template for agent:
“Replicate the exact behavior, CLI flags, exit codes, and differential logic of [tool repo]. Target [C#/PowerShell/TS]. Preserve CRAP formula verbatim. Output ready-to-use module with README.”

## 5. Agent Directives (Embed as Constitution Files)

Every OpenCode session begins with these immutable rules:

- You will never suggest or produce code without accompanying tests.
- Never accept code that violates CRAP ≤8, drops below 100% mutation kill, or contains harmful duplication.
- Never complete a change to source files without running the full metric suite.
- Never write acceptance tests without converting them through the Gherkin→JSON→test-skeleton pipeline.
- Never leave a module with high CRAP, elevated duplication, or excessive size unrefactored.
- Never answer a structural question about the codebase while any metric gate is failing.
- You will never rely on human code review; metrics and targeted probes suffice.
- Never pass over a Clean Code, SOLID, or dependency-inversion violation without citing it.

## 6. Tips, Tricks, and Hard-Earned Insights

- Run CRAP and mutation on every CI build and after every agent edit.
- Use --changed / differential modes to keep feedback loops under 30 seconds.
- Probe agents: “Show me the highest CRAP method and refactor it.” “List duplication clusters above score X.”
- Separate parser/generator pipelines to block shortcut cheating.
- Maintain a “source document” of domain rules; agents consult it before any edit.
- For large systems, swarm multiple specialized agents rather than one omniscient coder.
- Fan noise and 100% CPU are expected and desirable—agents are working.
- Semantic stability is the miracle: zero bugs on first run when discipline is followed.
- Non-technical stakeholders cannot drive high-quality code; disciplined developers remain essential.

This skill is exhaustive. Apply it verbatim in OpenCode. Any deviation reintroduces the very problems Uncle Bob’s discipline was engineered to eliminate. Update only when new primary sources appear on https://github.com/unclebob or @unclebobmartin.
