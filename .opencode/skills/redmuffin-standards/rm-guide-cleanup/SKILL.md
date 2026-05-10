---
name: rm-guide-cleanup
description: Universal code quality principles applied to every code change. Covers superfluous code removal, characterization tests, method simplicity, async patterns, Blazor-specific rules, collection abstractions, and coding standards for C# .NET 9 Blazor WASM. Use when writing new code, refactoring, or cleaning up existing functionality. USE FOR: code quality, cleanup, refactoring, code review, superfluous code, dead code, method size, async patterns, ConfigureAwait, Blazor lifecycle.
version: 1.0
guide-authors:
  - Robert C. Martin (Clean Code, SOLID, TDD)
  - Kent Beck (TDD, Test Desiderata, Extreme Programming)
  - Michael Feathers (Working Effectively with Legacy Code, characterization tests)
  - Dave Farley (Modern Software Engineering, fast feedback)
  - John Ousterhout (A Philosophy of Software Design — use for module structure)
  - Sandi Metz (Practical OOD, duplication vs wrong abstraction, Rule of Three)
  - Martin Fowler (Refactoring catalog, replace conditional with polymorphism)
  - Steve Freeman & Nat Pryce (Growing OO Software, mock-object TDD)
  - Kevlin Henney (simplicity before generality, use before reuse)
  - Mary & Tom Poppendieck (Lean Software Development, eliminate waste)
---

# rm-guide-cleanup

Universal code quality principles for every code change in this .NET 9 /
Blazor WASM / C# project. Not gate-specific — these govern all work.

## Core Principle

Every code change must make the code better. Not just fix a warning or
pass a gate. The change must improve simplicity, maintainability,
testability, or architecture per the principles below.

## 1. Superfluous Code Removal

### Taxonomy

| Category                  | Definition                                                  | When to Remove                                                                                                    |
| ------------------------- | ----------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| **Dead code**             | Unreachable, uncallable, never executed                     | Always. Run `dotnet test` before and after.                                                                       |
| **Speculative code**      | Built for a future that never arrived (YAGNI)               | Remove. If the future arrives, we have git history.                                                               |
| **Over-abstraction**      | Interface with 1 implementation, base class with 1 subclass | Collapse (Fowler's _Collapse Hierarchy_) unless the interface serves interface segregation or external consumers. |
| **Unused generalization** | Generic type parameter used in exactly 1 concrete way       | Remove the generic. Simpler code wins.                                                                            |
| **Comment compensation**  | Comments explaining what bad code does                      | Remove the comment AND fix the code (Clean Code ch. 4).                                                           |
| **Unused usings/imports** | IDE0051, IDE0052 violations                                 | Remove immediately.                                                                                               |

### The Rule of Three (Sandi Metz)

Do NOT abstract duplicated code seen only twice. Wait for **three**
occurrences before extracting a shared abstraction. "Duplication is
cheaper than the wrong abstraction."

### What is NOT superfluous

- **Blazor lifecycle methods** (`OnInitializedAsync`, `OnAfterRender`) —
  the runtime calls them, static analysis false-positives on them.
- **`[Inject]` properties** — injected by the DI container.
- **Public API surface** — even if unused internally, may be consumed
  externally.
- **Intentional duplication** — when coupling two modules via a shared
  abstraction would be worse than the duplication itself (decoupling).

## 2. Characterization Tests (Michael Feathers)

Before refactoring any method without adequate tests:

1. Write **characterization tests** — test ONLY observable inputs/outputs.
   Never test internal implementation details.
2. Use the golden master pattern: capture current output, refactor, verify
   output unchanged.
3. C# tools: `Verify` NuGet (snapshot testing), `ApprovalTests`.
4. After refactoring, graduate to proper unit tests on extracted pieces.

## 3. Method Quality Standards

### Size and complexity

- Target cyclomatic complexity (CC) ≤ 4 per method.
- If a method has >4 decision points, extract helper methods.
- Methods with CRAP ≥ 20 are critical failures; CRAP ≥ 8 are warnings.

### Single Responsibility

- A method should do ONE thing. If the name needs "and" or "or", split it.
- A method should operate at a SINGLE level of abstraction.

### Guard clauses (Fowler's _Replace Nested Conditional with Guard Clauses_)

Prefer early returns over nested if-else. Every early return reduces CC by 1.

## 4. Async Patterns

### ConfigureAwait(false) rules

| Context                              | Use ConfigureAwait(false)?                  |
| ------------------------------------ | ------------------------------------------- |
| Blazor WASM (single-threaded)        | No — no SynchronizationContext exists       |
| Library code (no UI context)         | Yes — avoid capturing unknown context       |
| Blazor Server (SignalR circuit)      | No — need the Blazor SynchronizationContext |
| ASP.NET Core (no HttpContext needed) | Yes — avoid capturing HttpContext           |
| Console apps, background services    | Yes                                         |
| Azure Functions (isolated)           | Not needed — no SynchronizationContext      |

In **our** Blazor WASM project: do NOT use `ConfigureAwait(false)` in
component code-behind. It's unnecessary and a noise indicator.

### Fire-and-forget

Blazor lifecycle methods cannot be async void. If you must fire and
forget, use `InvokeAsync(() => ...)` and call `StateHasChanged()`.

## 5. Blazor-Specific Rules

### Component disposal

Implement `IDisposable` / `IAsyncDisposable` when subscribing to events
or using resources. Blazor calls `Dispose` when the component is removed.

### [Inject] properties

- Must be `public` or `internal` (DI container access).
- Must have a default value or nullable annotation.
- Analyzer warning MA0015 on [Inject] properties is a **false positive**
  for Blazor. These are NOT method parameters — the container injects them.
  Do not suppress; add a comment `// Injected by DI container`.

### Lifecycle methods (static analysis false positives)

- `OnInitialized`, `OnInitializedAsync`, `OnParametersSet`,
  `OnAfterRender`, `OnAfterRenderAsync` are called by the runtime.
  Analyzers may flag them as dead code — they are not.

### StateHasChanged

Call `StateHasChanged()` after async operations that modify state
outside of Blazor lifecycle events (e.g., event handlers, timer callbacks).

## 6. Collection Abstractions

Follow Postel's Law: be conservative in what you send, liberal in what
you accept.

| Direction                     | Use                                                            |
| ----------------------------- | -------------------------------------------------------------- |
| Method **parameters** (input) | `IEnumerable<T>`, `IReadOnlyList<T>`, `IReadOnlyCollection<T>` |
| Method **returns** (output)   | `IReadOnlyList<T>`, `IReadOnlyCollection<T>`                   |
| Internal collections          | `List<T>`, `Dictionary<K,V>` — fine, no need to abstract       |
| Public API surface            | `IReadOnlyList<T>` out, `IEnumerable<T>` in                    |

Never expose `List<T>` or `Dictionary<K,V>` in public return types.

## 7. Logging

Use `LoggerMessageAttribute` source generators (compile-time, no
allocations, structured logging):

```csharp
private static partial class Log
{
    [LoggerMessage(Level = LogLevel.Warning,
        Message = "Image load failed for {Url}: {Error}")]
    public static partial void ImageLoadFailed(
        ILogger logger, string url, string error);
}
```

Dynamic messages (with `$` interpolation) defeat structured logging.
Use format templates with named placeholders.

## 8. No Pragma Suppression (Zero-Tolerance Policy)

Never add `#pragma warning disable` to new code. Warnings are signals
that code is not following best practices. Fix the root cause.

If a fix is truly impossible and the warning is a known false positive
for our context (e.g., MA0015 on `[Inject]` properties), document it
here as an explicit exception rather than suppressing per-file.

## 9. When to Refactor (Decision Flow)

```
Is the code correct?  → No → Fix with TDD (red-green-refactor)
         ↓ Yes
Is there a gate violation?  → Yes → Fix with characterization tests first
         ↓ No
Is the code simple and clear?  → No → Simplify (extract method, rename)
         ↓ Yes
Leave it alone.
```

Do not refactor for refactoring's sake. Every change must have a reason
traceable to either a gate violation, a bug, or a clarity problem.

## 10. Code Review Checklist

Before any change is complete, verify:

- [ ] Method CC ≤ 4 (or justified with tests)
- [ ] No public `List<T>` / `Dictionary<K,V>` return types
- [ ] Async methods have `ConfigureAwait(false)` in library code
- [ ] No new `#pragma warning disable`
- [ ] Logging uses `LoggerMessageAttribute` source generators
- [ ] No speculative code (YAGNI)
- [ ] No comment explaining what code does — code should be self-documenting
- [ ] Tests pass (`dotnet run --project tests/redmuffin.Blazor.StaticWeb.Tests`)
