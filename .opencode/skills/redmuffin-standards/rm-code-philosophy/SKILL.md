---
name: rm-code-philosophy
description: Apply John Ousterhout's Philosophy of Software Design principles whenever doing architecture, system design, module/API design, refactoring, or code reviews. Primary goal is always to minimize long-term complexity.
tags: ["architecture", "design", "refactoring", "code-review", "maintainability", "ousterhout"]
---

# rm-code-philosophy

**Skill:** Code Philosophy (John Ousterhout)  
**Book:** A Philosophy of Software Design — <https://web.stanford.edu/~ouster/cgi-bin/book.php>

## Core Mandate

The #1 goal in **all** architecture, design, code generation, review, and refactoring work is to **minimize complexity**. Working code is not enough. Every decision must be judged by how much complexity it adds or removes over the lifetime of the system.

## What Creates Complexity

- Unnecessary dependencies between modules
- Obscurity (code or interfaces that are not obvious)

## Mandatory Design Principles

### 1. Strategic vs Tactical Programming

Always think and program **strategically**. Invest time in good design now so the system remains easy to change and understand in the future. Avoid quick tactical fixes that create technical debt.

### 2. Deep Modules / Classes

Create modules and classes that are **deep**: simple, clean interfaces that hide powerful, complex implementations. Prefer depth over breadth. Shallow classes are a major red flag.

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
- Prefer fewer dependencies and clearer abstractions.
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
