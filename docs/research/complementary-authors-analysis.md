---
date: 2026-05-10
title: Complementary Software Engineering Authors Analysis
tags: [research, clean-code, principles, authors, refactoring, tdd]
description: Analysis of software engineering authors whose principles complement (not contradict) Uncle Bob Martin, Kent Beck, Michael Feathers, Dave Farley, and John Ousterhout for a C# .NET 9 Blazor WASM code cleanup initiative.
module: code-quality
problem_type: design-principles
---

## Compatibility Gate

Existing canon: Uncle Bob (Clean Code, SOLID, TDD), Kent Beck (TDD, Test Desiderata), Dave Farley (Modern SE, fast feedback), Michael Feathers (Legacy Code, characterization tests), John Ousterhout (deep modules, strategic programming).

Hard constraint: No author who rejects TDD or SOLID was considered. All eight candidates are compatible.

---

## 1. Sandi Metz

**Books**: _Practical Object-Oriented Design in Ruby_ (POODR), _99 Bottles of OOP_ (with Katrina Owen)

### Key Principle: "Duplication is far cheaper than the wrong abstraction"

From her 2014 RailsConf talk and 2016 blog post "The Wrong Abstraction":

> "prefer duplication over the wrong abstraction"

Metz describes a recurring failure pattern:

1. Programmer A sees duplication and extracts a shared abstraction.
2. New requirements arrive that almost fit the abstraction.
3. Programmer B adds parameters and conditionals to make it work.
4. Repeat until the abstraction is a rat's nest of conditional logic.
5. The "fastest way forward is back" — inline the abstraction, re-extract only the real commonality.

### Why It Complements (Doesn't Contradict)

- Uncle Bob's Single Responsibility Principle says extract when things change for different reasons. Metz says _wait until you have evidence._ This is not a contradiction — it is a **timing refinement**.
- She is pro-TDD, pro-SOLID, pro-refactoring. Her 99 Bottles book uses TDD as its foundation.
- Her principle functions as a **DRY speed-limit sign**: DRY is the goal, but the wrong abstraction costs more than duplication. This aligns with Kent Beck's "make the change easy, then make the easy change."

### Practical Rules from 99 Bottles of OOP

**Shameless Green**: The first passing solution should be the simplest code that works, even if it's ugly. Premature elegance is a form of speculation.

**Flocking Rules**: A structured refactoring technique:

1. Find the things that are most alike.
2. Select the smallest difference between them.
3. Make the simplest change that removes that difference.

Repeat until differences vanish and an abstraction emerges naturally.

**Rule of Three**: Don't abstract until you've seen three concrete cases. Two cases are coincidence; three is a pattern. This protects against wrong abstractions.

### Concrete C# Example

```csharp
// BEFORE: Two similar methods (duplication is okay at this stage)
public decimal CalculateTaxUk(Order order) { ... }
public decimal CalculateTaxDe(Order order) { ... }

// AFTER seeing three countries (UK, DE, FR), extract:
public decimal CalculateTax(Order order, ITaxRule taxRule)
{
    return taxRule.Calculate(order);
}
// Now the abstraction is justified by multiple concrete cases.
```

### CRAP Score Relevance

Wrong abstractions cause high cyclomatic complexity (conditionals accumulate inside the abstraction). Inlining and re-extracting reduces complexity by removing spurious parameters and branches.

---

## 2. Martin Fowler

**Book**: _Refactoring: Improving the Design of Existing Code_, 2nd Edition (2018)

### Key Principle: Behavior-preserving transformations via catalogued steps

Fowler's catalog contains 80+ refactorings, each with motivation, mechanics, and examples. The 2nd edition shifts to JavaScript to deemphasize class-centrism, but all refactorings apply equally to C#.

### Refactorings Most Relevant to CRAP Score Reduction

CRAP = `complexity^2 * (1 - coverage)^3 + complexity`. To reduce it, lower cyclomatic complexity and increase test coverage. These Fowler refactorings directly reduce complexity:

| Refactoring                                       | CRAP Impact                                      | C# Mechanics                                                  |
| ------------------------------------------------- | ------------------------------------------------ | ------------------------------------------------------------- |
| **Extract Function**                              | Reduces cyclomatic complexity of parent          | Move a block into a named method                              |
| **Decompose Conditional**                         | Flattens nested if/else chains                   | Extract condition, then-branch, else-branch into methods      |
| **Replace Conditional with Polymorphism**         | Biggest single complexity reducer                | Create subclasses per condition branch                        |
| **Replace Nested Conditional with Guard Clauses** | Flattens deep nesting                            | Return early for edge cases                                   |
| **Split Phase**                                   | Separates concerns, reduces per-phase complexity | Break a function into sequential independent phases           |
| **Replace Loop with Pipeline**                    | Replaces imperative loops with LINQ              | `.Select()`, `.Where()`, `.Aggregate()` — reduces branching   |
| **Slide Statements**                              | Consolidates related logic                       | Group conditional fragments so they can be extracted together |
| **Combine Functions into Class**                  | Reduces parameter-passing complexity             | Group related functions + shared data into a class            |
| **Remove Dead Code**                              | Eliminates paths that can never execute          | Delete unreachable code (reduces apparent complexity)         |
| **Replace Derived Variable with Query**           | Removes mutable state complexity                 | Replace a cached variable with a computed query               |

### Why It Complements

- Fowler names and formalises the refactoring steps that Clean Code, TDD, and Feathers' legacy code work depend on.
- Feathers' "characterization test first, then refactor" approach uses Fowler's catalog as the refactoring toolbox.
- The catalog entries provide **safe, step-by-step mechanics** — each step is reversible and behaviour-preserving. This is exactly what Dave Farley means by "make the change easy."
- Fowler explicitly states refactoring must be coupled with testing. He dedicates chapter 4 to testing.

### Concrete C# Example: Decompose Conditional

```csharp
// BEFORE: Complexity 4
public decimal CalculateCharge(DateOnly date, int quantity)
{
    if (date < SUMMER_START || date > SUMMER_END)
        return quantity * _winterRate + _winterServiceCharge;
    else
        return quantity * _summerRate;
}

// AFTER: Each method has complexity 1
public decimal CalculateCharge(DateOnly date, int quantity)
{
    return IsSummer(date) ? SummerCharge(quantity) : WinterCharge(quantity);
}
private bool IsSummer(DateOnly date) =>
    date >= SUMMER_START && date <= SUMMER_END;
private decimal SummerCharge(int quantity) =>
    quantity * _summerRate;
private decimal WinterCharge(int quantity) =>
    quantity * _winterRate + _winterServiceCharge;
```

---

## 3. Steve Freeman & Nat Pryce

**Book**: _Growing Object-Oriented Software, Guided by Tests_ (2009)

### Key Principle: TDD as a design activity — tests drive object relationships

> "We use Mock Objects to discover and then describe relationships between objects."

Their approach treats TDD not as verification but as a **design tool**. Tests specify how objects collaborate. Mock objects let you discover interfaces by writing the calling code first — you don't design an interface and then test it; the test _becomes_ the first client.

### Core Concepts

- **Outside-in TDD** (London School): Start with an acceptance test, then drill into unit-level collaboration tests.
- **Mock Roles, Not Objects**: Mocks represent the _role_ a collaborator plays, not the implementation.
- **Listening to Tests**: When a test is hard to write, the design is wrong. This is the "design pressure" that J.B. Rainsberger later formalized.
- **Ports and Adapters**: Tests at the system boundary use test doubles for external systems, tests internally use real objects.

### How It Complements Characterization Testing (Feathers)

| Feathers (Characterization Tests)            | Freeman & Pryce (GOOS)                       |
| -------------------------------------------- | -------------------------------------------- |
| Tests existing behavior first, then refactor | Tests desired behavior first, then implement |
| Safety net for legacy code                   | Design driver for new code                   |
| Defines "what it does now"                   | Defines "what it should do"                  |
| Tests are retrospective                      | Tests are prospective                        |

In a code cleanup initiative:

1. Write characterization tests for existing code (Feathers) to establish a safety net.
2. Then use GOOS-style outside-in TDD when _replacing_ components: write a failing acceptance test, mock collaborators to discover clean interfaces, implement to make it pass.

### Concrete C# Example: Discovering an Interface Through Mocking

```csharp
// The test IS the first client of IAlertService
[Test]
public async Task NotifiesAlertServiceWhenThresholdExceeded()
{
    var mockAlert = Substitute.For<IAlertService>();
    var monitor = new TemperatureMonitor(mockAlert, threshold: 30);

    await monitor.RecordReadingAsync(31);

    await mockAlert.Received(1).SendAsync("Temperature exceeds 30°C");
}

// The IAlertService interface was discovered by writing the test,
// not designed in isolation.
public interface IAlertService
{
    Task SendAsync(string message);
}
```

### Compatibility

GOOS is entirely within the Kent Beck TDD tradition. It extends (not contradicts) Classic/Detroit-school TDD by focusing on collaboration design. Uncle Bob's "TDD by Example" and GOOS are complementary sides of the same coin.

---

## 4. Kevlin Henney

**Key works**: _97 Things Every Programmer Should Know_ (editor), _97 Things Every Software Architect Should Know_ (contributor), patterns in POSA series.

### Key Principles

**Simplicity Before Generality, Use Before Reuse**:

> "The best route to generality is through understanding known, specific examples, focusing on their essence to find an essential common solution. Simplicity through experience rather than generality through guesswork."

This directly parallels Sandi Metz's Rule of Three and Ousterhout's "deep modules" — design for what you know now, not what you guess you might need.

**"Worse is Better" applied to scope control**:
Henney's interpretation of Richard Gabriel's "Worse is Better" philosophy:

- Focus on simplicity of implementation.
- Correctness is non-negotiable. Quality is non-negotiable.
- Scope is what you compromise on — build small, correct things. If it's wrong, throw it away; the simplicity makes replacement cheap.
- This is NOT "good enough software" — it is "small, correct software."

**Good Unit Tests (GU**TS)\*\*: Tests must be:

1. **Readable** — tests are for humans first, machines second.
2. **Single-case** — one test method = one behavior case (not multiple assertions about different things).
3. **Automated** and **first-class citizens** — same code quality standards as production code.

### Why It Complements

- Henney's "simplicity of implementation" aligns with Ousterhout's "deep modules with simple interfaces."
- His GUTs principles align with Kent Beck's Test Desiderata (isolated, composable, fast, deterministic).
- "Use before reuse" reinforces Sandi Metz's "prefer duplication over the wrong abstraction."

### Concrete C# Example: Simplicity Before Generality

```csharp
// WRONG: Early generalization (Henney says NO)
public interface IDataExporter<T>
{
    Task<Stream> ExportAsync(IEnumerable<T> data, ExportFormat format);
}
// You don't yet know if you'll ever export anything other than reports.

// RIGHT: Simple, specific, correct
public class ReportExporter
{
    public async Task<byte[]> ExportToCsvAsync(IReadOnlyList<Report> reports) { ... }
}
// Add PDF export only when there's a real requirement, THEN extract.
```

---

## 5. Mary & Tom Poppendieck

**Book**: _Lean Software Development: An Agile Toolkit_ (2003), _Implementing Lean Software Development_ (2006)

### Key Principle: Eliminate Waste (Muda)

The Poppendiecks mapped Toyota's seven manufacturing wastes to software development:

| Manufacturing Waste | Software Equivalent           | Code Cleanup Relevance                                          |
| ------------------- | ----------------------------- | --------------------------------------------------------------- |
| Inventory           | **Partially Done Work**       | Unfinished refactorings, TODO comments, feature flags long dead |
| Overproduction      | **Extra Features**            | Code for requirements that never materialized; YAGNI violations |
| Extra Processing    | **Extra Processes / Steps**   | Unnecessary layers of indirection, ceremony code                |
| Transportation      | **Task Switching / Handoffs** | Context lost between developers, inconsistent patterns          |
| Waiting             | **Delays**                    | Blocked PRs, slow CI, long build times                          |
| Motion              | **Relearning**                | Rediscovering what code does because it's unclear               |
| Defects             | **Defects**                   | Bugs that should have been caught earlier                       |

### Map to Superfluous Code Removal

This is a direct framework for your code cleanup:

| Waste Category          | Detection                                                | C# Example                                         |
| ----------------------- | -------------------------------------------------------- | -------------------------------------------------- |
| **Extra Features**      | Dead code analysis, unused `public` APIs                 | Unused endpoints, classes never instantiated       |
| **Partially Done Work** | TODO comments, abandoned abstraction layers              | Half-migrated DI registrations                     |
| **Extra Processing**    | CRAP score spikes, excessive indirection                 | Methods that delegate to another method unmodified |
| **Relearning**          | Smell "obscured intent" — unclear names, missing context | Magic numbers, abbreviations, cryptic names        |

### Why It Complements

- The Poppendiecks are pro-Agile, pro-TDD, pro-continuous-delivery. Dave Farley frequently references lean principles.
- "Eliminate waste" gives a **framework for prioritizing cleanup tasks** — which deletions give the biggest payoff in reduced maintenance burden?
- "Amplify learning" (their Principle 2) aligns with Feathers' characterization tests — learn what the code does before changing it.

### Compatibility Note

Some lean advocates reject upfront design entirely. The Poppendiecks explicitly do NOT — they advocate "decide as late as possible" but "decide" is still in there. Their position is compatible with Ousterhout's strategic programming (invest in design where it reduces long-term complexity).

---

## 6. Robert Nystrom

**Books**: _Game Programming Patterns_ (2014), _Crafting Interpreters_ (2021)

### Key Principles

Nystrom doesn't propose a new software design philosophy. Instead, he demonstrates that classic design patterns (GoF) apply universally — including in performance-critical domains like game engines. His contribution is **pattern translation**: showing how patterns manifest under hard constraints.

**Data Locality**: Arrange data for cache-line efficiency. In C# terms, prefer `struct` arrays over scattered heap objects for hot paths. This is a performance principle, but also a **simplicity** principle — contiguous data is easier to reason about.

**Component Pattern**: "A single entity spans multiple domains. To keep the domains isolated, the code for each is placed in its own component class." This is Single Responsibility Principle applied at the entity level. Directly reinforces SOLID.

**Command Pattern with undo**: Encapsulate a request as an object — enables undo, replay, queuing. In Blazor WASM, useful for state management and undo/redo stacks.

**Decoupling via Observer/Event Queue**: "Decoupling when something happens from when it gets processed." Aligns with Clean Code's "objects expose behavior, data structures expose data."

### Why It Complements

- Nystrom frames patterns as **solutions to specific problems**, not as universal mandates. This is the correct way to apply SOLID — use a pattern when it solves a problem, not because it's a principle.
- His writing demonstrates that clean code and performance are NOT enemies. The patterns that make code clean (separation of concerns, single responsibility) also enable performance optimizations (cache-friendly layout, parallel execution).
- _Crafting Interpreters_ shows how to build a complex system incrementally — start with a simple tree-walk interpreter, then add a bytecode VM. This is Kent Beck's "make it work, make it right, make it fast" at system scale.

---

## 7. Sandi Metz's Practical Rules (Collected)

| Rule                                                                        | Meaning                                                                          | Application to C# Cleanup                                                                                                     |
| --------------------------------------------------------------------------- | -------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| Duplication is cheaper than the wrong abstraction                           | DRY is a target, not a mandate. Wait for evidence.                               | Don't merge two `Service` classes that happen to look similar but serve different callers.                                    |
| Prefer duplication over the wrong abstraction                               | When uncertain, keep duplication.                                                | Two `ToDto()` methods with slightly different mappings? Leave them separate until the mapping logic converges.                |
| You can't know the right abstraction until you've seen three concrete cases | Rule of Three for abstraction extraction.                                        | After seeing `ReportService`, `InvoiceService`, and `NotificationService` share common patterns, extract `BaseBackgroundJob`. |
| Shameless Green                                                             | First solution should be the simplest code that passes all tests.                | A `switch` statement with 10 cases is fine until tests demand polymorphism.                                                   |
| Flocking Rules                                                              | Incremental convergence: find likeness, reduce smallest difference, repeat.      | When 3 controllers have similar DI constructors, use flocking to converge them toward a shared pattern without guessing.      |
| Open/Closed first, then extend                                              | Refactor existing code to be open to the new requirement before adding new code. | Before adding a new payment method, refactor `PaymentProcessor` so the extension point already exists.                        |

---

## 8. Additional Compatible Authors

### Roy Osherove

**Book**: _The Art of Unit Testing_ (3rd ed., with Vladimir Khorikov), examples in C#.

- **Key Principle**: "A unit test is an automated piece of code that invokes a unit of work and checks one specific outcome."
- **Test categorization**: Classifies tests by value (maintainability, readability, trustworthiness) and isolation (sociable vs. solitary).
- C#-native examples for mocking (NSubstitute, Moq), test organization, and test naming conventions.
- **Complements**: Provides the C# tooling and practical patterns for Kent Beck's TDD and Feathers' characterization tests. Directly compatible — Osherove is an explicit Kent Beck/Dave Farley follower.

### J.B. Rainsberger

**Talk**: "Integration Tests Are a Scam" (InfoQ, 2009)

- **Key Principle**: Integration tests suffer combinatorial explosion: 3 components × 3 states each = 27 paths. Isolated contract + collaboration tests solve this.
- **Collaboration Tests + Contract Tests**: Two-pronged approach that breaks integration testing into: (1) unit-level mocks testing collaboration protocols, (2) contract tests ensuring mock behavior matches real implementation.
- **Complements**: This is the logical extension of Freeman & Pryce's mock-object approach. Supports Feathers' characterization testing by providing a strategy for when the code under test has external dependencies.

### Vladimir Khorikov

**Book**: _Unit Testing Principles, Practices, and Patterns_ (Manning, 2020)

- **Key Principle**: "Four pillars of a good unit test: protection against regressions, resistance to refactoring, fast feedback, and maintainability."
- Focuses on test value over test count — a test that breaks on every refactor is net-negative even if it increases coverage.
- **Complements**: Explicitly builds on Kent Beck and Freeman/Pryce. His "resistance to refactoring" pillar supports Michael Feathers' goal of having a test suite that enables (not inhibits) safe refactoring.

---

## Integration Matrix: How These Authors Fit Your Initiative

| Author               | Adds To                              | Applied During                                            |
| -------------------- | ------------------------------------ | --------------------------------------------------------- |
| **Sandi Metz**       | Abstraction discipline               | Refactoring phase — deciding when NOT to DRY              |
| **Martin Fowler**    | Refactoring mechanics                | Active code transformation — the step-by-step recipe book |
| **Freeman & Pryce**  | Design discovery through tests       | Replacing components — designing clean interfaces         |
| **Kevlin Henney**    | Simplicity guardrails + test quality | Code review — "is this simple enough? are tests GUTs?"    |
| **Poppendiecks**     | Waste elimination framework          | Prioritization — what to clean up first?                  |
| **Robert Nystrom**   | Pattern pragmatism + data design     | Performance-sensitive refactoring in Blazor WASM          |
| **Roy Osherove**     | C# test tooling + patterns           | Writing actual tests during cleanup                       |
| **J.B. Rainsberger** | Test decomposition strategy          | Breaking up large integration test suites                 |

---

## Authors NOT Included (and Why)

- **David Heinemeier Hansson (DHH)**: Publicly rejects TDD ("TDD is dead" controversy, 2014). Incompatible with Kent Beck and Dave Farley's TDD-centric approach.
- **Dan North (BDD)**: BDD is a valuable extension but operates at a different layer (specification). BDD builds on TDD, so North is compatible but less directly applicable to code cleanup.
- **John Carmack**: Brilliant engineer but his programming philosophy (static functions, data-oriented design) was developed for game engines in C/C++ and would require too much translation for C# Blazor.
- **Folks who reject SOLID outright** (e.g., some FP purists): Incompatible. Your cleanup initiative targets a C# OO codebase using SOLID.

---

## Recommended Reading Order for the Cleanup Initiative

1. **Martin Fowler — "Code Smells" chapter** (Refactoring 2nd ed., Ch. 3): Learn to see the problems.
2. **Sandi Metz — "The Wrong Abstraction"** (blog post, 2016): Learn when NOT to refactor.
3. **Metz — "99 Bottles of OOP" Ch. 1-3**: Flocking Rules and Shameless Green.
4. **Fowler — Refactoring catalog**: Extract Function, Decompose Conditional, Replace Conditional with Polymorphism, Remove Dead Code.
5. **Poppendiecks — 7 Wastes** (Lean Software Development, Ch. 4): Prioritize which code to eliminate first.
6. **Henney — "Simplicity Before Generality"** (97 Things Every Programmer Should Know): Final philosophy check before shipping.

---

## Confidence Levels

- **Sandi Metz**: High. Directly sourced from her blog and book summaries. Her principles are unambiguous.
- **Martin Fowler**: High. Directly sourced from his book announcement and catalog changes page.
- **Freeman & Pryce**: High. Directly sourced from their book's website, InfoQ, and multiple summaries.
- **Kevlin Henney**: High. Directly sourced from InfoQ interview and O'Reilly contributory chapter.
- **Poppendiecks**: High. Sourced from their books, Wikipedia, Agile Learning Labs, and multiple corroborating sources.
- **Robert Nystrom**: Moderate-high. Sourced from his book's website and summaries. His original contribution is pattern translation, not new principles — this is noted accurately.
- **Roy Osherove & Vladimir Khorikov**: Moderate. Sourced from their book sites and O'Reilly. C#-specific content confirmed but deep principle analysis is book-summary level.
- **J.B. Rainsberger**: Moderate. Sourced from InfoQ talk summaries and blog analyses. The talk itself is available as source material.
