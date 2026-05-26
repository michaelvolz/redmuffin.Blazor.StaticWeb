---
name: rm-architecture
description: "Use when designing services, boundaries, patterns, or cross-layer C# changes."
---

# rm-architecture

See also: `rm-quality-gates` Gate 3 for the Architecture gate (`--arch-config`
flag, `arch-rules.yml`), `rm-code-quality` §1 for SLAP and method quality.

Notes that were previously loaded as separate sub-skills are now
inlined as sections below. Both were always loaded together during
architecture work — their only cost as separate files was 2 extra
names in every prompt.

Read the inlined sections directly rather than loading separately:

- §Ousterhout: deep modules, complexity elimination, information hiding
- §Uncle Bob Martin: SOLID, Clean Architecture, metrics-driven quality

---

## CRITICAL

- Never use inheritance where composition suffices.
- Keep dependencies flowing inward.
- Give each type one reason to change.

## WHEN TO LOAD

- Designing a new feature slice or service.
- Refactoring boundaries across components, services, and APIs.

## GUIDANCE

- Use small, explicit abstractions.
- Keep domain, application, infrastructure, and presentation concerns separate.
- Introduce patterns only when they reduce complexity.
- After structural changes, run `dotnet run -- arch --arch-config arch-rules.yml`
  to verify no dependency violations.
- When multiple components share a workflow, prefer a composed orchestrator
  (context record + static methods with `Func<>` callbacks) over a base class.
  Example: `docs/solutions/architecture-patterns/composition-over-inheritance-orchestrator-pattern-2026-05-23.md`

## NEVER

- Do not add architecture for hypothetical future use.
- Do not use Service Locator in business code.

## Feature Folder Structure

The project follows the Blazor feature-folder pattern (Giesel 2022,
Hilton 2021). Every feature lives as a top-level folder under `Features/`
with all its code co-located:

```
Features/
  Common/components/     ← shared by 2+ features
  Common/PageLoadSpeed/   ← cross-cutting domain
  Raindrop/               ← domain feature
  HomePage/               ← single-page feature
  DebugPage/              ← multi-page feature
  ...
```

### Rules

- **Feature isolation:** A component in one feature folder must never
  reference a component in a sibling feature. Pull shared components
  up to `Features/Common/Components/`.
- **Locality before reuse:** Do not extract a shared component after
  only 2 consumers. Wait for 3+ distinct features to prove the
  abstraction is real (Metz: Rule of Three).
- **No root `Services/`:** Services belong with their consumers:
  feature-specific in `Features/{Domain}/Services/`, cross-cutting in
  `Core/Services/`.
- **Dead code has no home:** If a model has zero consumers, delete it.
  Do not keep it in a generic bucket "in case we need it later."

### Reference structure (Hilton)

```
Features/Components/ (most abstract)  ← Features/ can reference
Features/Common/     (shared domain)
Features/Raindrop/   (domain feature)
Features/HomePage/   (leaf feature)    ← cannot reference siblings
Core/                (infrastructure)  ← everything can reference
```

`rm-naming` §Directory & Namespace Structure has the full folder-to-namespace mapping.

---

## §Ousterhout: Deep Modules and Complexity Elimination (inlined from rm-architecture-ousterhout)


# rm-architecture-ousterhout

**Skill:** Code Philosophy (John Ousterhout)  
**Book:** A Philosophy of Software Design — <https://web.stanford.edu/~ouster/cgi-bin/book.php>

## Core Mandate

The #1 goal in **all** architecture, design, code generation, review, and refactoring work is to **minimize complexity**. Working code is not enough. Every decision must be judged by how much complexity it adds or removes over the lifetime of the system.

## What Creates Complexity

- Unnecessary dependencies between modules
- Obscurity (code or interfaces that are not obvious)

## Mandatory Design Principles

### 1. Strategic vs Tactical Programming

Never think or program tactically. Invest time in good design now so the system remains easy to change and understand in the future. Avoid quick tactical fixes that create technical debt.

### 2. Deep Modules / Classes

Create modules and classes that are **deep**: simple, clean interfaces that hide powerful, complex implementations. Never create shallow modules. Shallow classes are a major red flag.

### 3. General-Purpose Modules

Design modules to be as **general-purpose** as reasonably possible. General-purpose code tends to be simpler, cleaner, and more reusable than highly specialized code.

### 4. New Layer = New Abstraction

Every new layer in the system must introduce a **clean, useful new abstraction**. Maintain clear separation of concerns and consistent abstraction levels.

### 5. Pull Complexity Downwards

Push complexity down into lower layers so that higher-level code remains simple and easy to understand.

### 6. Define Errors Out of Existence

Design interfaces so that common errors become **impossible** or are handled at the lowest appropriate level. Make the common case simple.

### 7. Comments

Write comments that describe things that are **not obvious** from the code itself. Focus on the "why" and high-level intent. Never just restate what the code does.

## Architecture Decision Framework

When designing or evaluating any architecture, module, or API:

- Ask: "Does this increase or decrease long-term complexity?"
- Never add dependencies when existing abstractions suffice.
- Choose the option that makes future changes easiest.
- Look for opportunities to deepen modules and reduce obscurity.
- Reject shallow wrappers, excessive parameters, high coupling, and proliferation of special cases.

## Red Flags (actively hunt for these)

- Shallow classes or modules (thin wrappers)
- Many tiny methods that do very little
- Complex interfaces with many parameters or special cases
- High coupling between modules
- Tactical workarounds instead of strategic solutions
- Comments that merely repeat the code
- Error handling that bubbles up unnecessarily
- Code that is hard to reason about ("unknown unknowns")

**Default stance:** When in doubt, choose the design that results in the **simplest possible code** for the people who will maintain it in the future.

---

## §Uncle Bob Martin: SOLID, Clean Architecture, Metrics-Driven Quality (inlined from rm-architecture-uncle-bob)


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
  side effects. Full catalog at `rm-csharp-functional`.

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
