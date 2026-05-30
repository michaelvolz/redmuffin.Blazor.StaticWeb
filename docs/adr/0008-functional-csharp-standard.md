---
date: 2026-05-30
status: accepted
---

# Functional C# as the Project's Preferred Coding Style

The project adopts functional C# — records, pattern matching, LINQ pipelines,
immutable collections, pure static methods, and `Func<>` injection — as the
standard coding style. Classical OOP with mutable state is used only where
Blazor's component model or Framework API forces it.

## Decision

Functional C# patterns are preferred at every opportunity, with a catalog of
standard transformations for common imperative patterns:

| Imperative pattern                          | Functional replacement                                          | Effect                   |
| ------------------------------------------- | --------------------------------------------------------------- | ------------------------ |
| `switch` 5+ arms                            | `FrozenDictionary<TKey, TResult>`                               | CC 8→1                   |
| `foreach` + `if` guards                     | `LINQ .Any()`, `.Where().Select()`                              | CC→1                     |
| Cumulative scoring with mutable accumulator | `signal array + .Where().Sum()`                                 | CC→1                     |
| Validation with `if` chain                  | `Array.TrueForAll(allOf)` with `static readonly Func<T,bool>[]` | CC→1                     |
| I/O coupled to pure logic                   | Extract pure function + optional `Func<T>` parameter            | CC↓, testable            |
| Mutable DTO                                 | `record` with `with` expressions                                | Immutable, equality-free |
| Long `if-else` chain                        | Pattern match expression                                        | CC→1                     |

## Considered Options

**Classical OOP with mutable state.**
Rejected. Mutable objects are harder to reason about (any caller can change
state), harder to test (setup requires correct initial state), and produce
higher CRAP scores (branching + mutation). Functional C# is not about
abandoning OOP — it is about preferring immutable data flows where they
make the code simpler and more testable.

**Reflection-heavy dispatch (e.g., `Activator.CreateInstance` by type name).**
Rejected. Unsafe under the Blazor WASM linker (IL trimming removes
reflection-accessed types) and produces opaque control flow. `FrozenDictionary`
dispatch is checked at compile time and survives trimming.

**Visitor pattern for dispatch.**
Rejected. Ceremonial — requires a visitor interface, accept methods on every
visited type, and a concrete visitor class. `FrozenDictionary<string, Func<>>`
does the same job in 3 lines and keeps dispatch logic local.

**"Best of both worlds" — no style preference.**
Rejected. Without a preference, code drifts into random imperativeness over
time. A declared standard means reviewers have a consistent baseline: default
to functional, use imperative only when Framework API requires it.

## Consequences

- The `rm-csharp-functional` skill catalogs 9 functional patterns with
  before/after code and existing codebase examples.
- Code review checks for functional refactoring opportunities — every new
  `switch` or `foreach`+`if` should trigger a "should this be a
  `FrozenDictionary` or `.Any()`?" question.
- CRAP refactoring first tries functional replacement (FrozenDictionary,
  LINQ, pattern matching) before extraction — often CC drops to 1 without
  adding a new method.
- Composition over inheritance is documented as a language-agnostic principle
  in multiple cross-referenced locations.
- Blazor component code (`OnInitializedAsync`, render fragments) is exempt
  from pure-function requirements — Blazor's lifecycle model is inherently
  imperative.
