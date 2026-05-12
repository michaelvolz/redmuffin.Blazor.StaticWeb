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

### 2.1 Feathers Seam Pattern (Extraction Discipline)

These rules govern WHEN and HOW to extract methods during cleanup.
Violating them produces fragmented code with no readability or CRAP benefit.

**The characterization-first rule (non-negotiable):**

```
1. Characterize → 2. Extract → 3. Test → 4. Verify CRAP
```

Never extract first and characterize later. Every extraction starts with:
"Do I know what this method currently does? Prove it with one golden-master
test."

**Extraction Decision Tree (Ousterhout + Feathers + Metz):**

Before extracting a method, answer these questions. Stop at the first NO:

**Q1: Is the extracted block ≥5 lines of actual logic?** (not counting braces,
`return`, or `throw` pedantry)
→ If NO: Inline. Small extractions are shallow modules (Ousterhout).
Their interface cost exceeds their abstraction benefit.

**Q2: Does the inline code read clearly as-is?**
Example: `Title?.Length > 500` translates instantly. Extracting to
`!IsValidLength(Title, 500)` forces a jump-to-definition to understand
null handling and the inverted boolean.
→ If YES: Leave inline. Fowler's Inline Function refactoring exists
for this exact case — when the body is clearer than the name.

**Q3: Does the extraction invert boolean logic?**
Example: `x is not null && Valid(x)` → `!IsValidX(x)` where IsValidX
returns `x is null || Valid(x)`. The inversion creates cognitive
indirection. Every reader must mentally negate the function.
→ If YES: Do not extract. Boolean inversion is a cognitive cost.

**Q4: Is the pattern duplicated ≥3 times?** (Sandi Metz's Rule of Three)
→ If NO (< 3): Duplication is cheaper than the wrong abstraction.
Two occurrences of a URI check are coincidence, not a pattern.

**Q5: Does the extraction hide meaningful complexity?** (Ousterhout's deep module)
A deep module has a simple interface hiding complex implementation.
A shallow module has a complex interface wrapping trivial implementation.
Examples:

- Deep: `GC.Collect()` — zero parameters, hides generational collection
- Shallow: `IsValidUri(string?)` — reader must know null→true, hides nothing
  → If the extracted method is a thin wrapper: do not extract.

**Concrete example — bad extraction (2026-05-12):**

```csharp
// BEFORE — clear, self-contained guard clauses
if (Title?.Length > 500)
    return false;

// AFTER — DO NOT DO THIS
if (!IsValidLength(Title, 500))
    return false;

// IsValidLength is a shallow module:
// Interface: reader must know null→true, max length param
// Implementation: value is null || value.Length <= maxLength
// Cost of indirection > benefit of abstraction
```

Only extract when ALL of Q1-Q5 pass. When in doubt, inline.

A seam is a place where behavior can be replaced WITHOUT editing in that
place. In C#, the primary seams are:

| Seam type                | Example                           | When to use                        |
| ------------------------ | --------------------------------- | ---------------------------------- |
| Pure function extraction | `public static int Parse(string)` | Default — no side effects          |
| Extract and override     | `protected virtual bool Check()`  | When side effects prevent purity   |
| Interface injection      | `IValidator` via constructor      | Only when multiple implementations |

Extract a method ONLY when it represents a seam — a distinct, replaceable
unit of behavior. Do NOT extract:

- **Algorithmic fragments** — a 3-line conditional that computes a value
  is part of the method's algorithm, not a seam. Leave it inline.
- **Single-line delegations** — `return DoTheThing(x)` is not a seam, it's
  indirection.
- **Switch arms** — unless the arm has significant logic (>5 lines), the
  switch IS the method's responsibility.

**Size threshold:** An extracted method must be ≥ 5 lines of actual logic
(not counting braces and `return`). If it's smaller, inline it.

**One seam per edit cycle:**

```
Extract one method → write its characterization test → run CRAP → verify
the violation count dropped → commit mentally → next seam.
```

Jumping between seams without verifying each one produces regressions
and file corruption from overlapping edits.

**Survey first, then extract:**

Before touching any code, read ALL CRAP violations in the target file.
Plan all seams as a list. The first extraction might affect line numbers
for subsequent ones. Order them top-to-bottom in the file to avoid drift.

**Example — good extraction:**

```csharp
// BEFORE: 25-line method, CC=8, CRAP 18
public int Analyze(Report r) {
    var baseline = ComputeBaseline(r);         // 5 lines — seam
    var adjusted = ApplyAdjustments(baseline); // 8 lines — seam
    return Math.Max(0, adjusted);
}

// AFTER: extracted seams
public static int ComputeBaseline(Report r) { ... }    // 5 lines, testable
public static int ApplyAdjustments(int baseline) { ... } // 8 lines, testable
public int Analyze(Report r) => Math.Max(0, ApplyAdjustments(ComputeBaseline(r)));
// CC=1, CRAP=2
```

**Example — bad extraction:**

```csharp
// BEFORE
return scoreCmp != 0 ? scoreCmp : Tiebreak(a, b);

// AFTER — DO NOT DO THIS
private static int Tiebreak(DupesCandidate a, DupesCandidate b) {
    var fileCmp = string.CompareOrdinal(a.LeftFile, b.LeftFile);
    return fileCmp != 0 ? fileCmp : a.LeftStartLine.CompareTo(b.LeftStartLine);
}
// This is algorithm logic, not a seam. It belongs inline.
```

### 2.2 Extraction Order in Cleanup Sessions

When reducing CRAP violations across multiple files:

1. **Survey** — list all methods with CRAP ≥ 8, sorted by score descending
2. **Group by file** — extractions in one file don't affect another
3. **Within a file, work top-to-bottom** — avoids line number drift
4. **One seam per commit cycle** — characterize → extract → test → verify
5. **Re-run all gates after each file** — extraction can create Dupes matches

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

## Related

- `rm-guide-testing` — comprehensive test patterns, test doubles, and file structure
- `rm-guide-naming` — naming conventions for types, members, and test doubles
- `rm-gates-cleanup` — CRAP, SCRAP, Architecture, Mutation gate remediation
