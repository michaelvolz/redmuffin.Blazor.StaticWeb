---
name: rm-guide-csharp-functional
description: "Functional C# patterns and modern language features. Covers records, pattern matching, LINQ, immutability, pure functions, expression bodies, higher-order functions, FrozenDictionary, with-expressions, tuple deconstruction, and collection abstractions. USE FOR: C# features, functional programming, immutability, pattern matching, LINQ, records, readonly, IReadOnlyList, IReadOnlySet, FrozenDictionary, with-expressions, expression-bodied members, pure functions, tuple deconstruction, switch expressions, aggregate, higher-order functions."
version: 2.0
guide-authors:
  - Simon J. Painter (Functional Programming with C#)
  - Enrico Buonanno (Functional Programming in C#, Second Edition)
---

# rm-guide-csharp-functional

## CRITICAL — Negative Constraints

These rules are non-negotiable. Every pattern below is an application of these
principles. When in doubt, the decision catalogs in §Patterns resolve the tension.

- **Never mutate data in place** when a non-destructive alternative exists.
  Use records with `with` expressions, `IReadOnly*` return types, and
  `FrozenDictionary`/`FrozenSet` for static lookup tables.
- **Never use `if-else` chains** when pattern matching (switch expressions,
  property patterns, relational patterns) expresses the same logic with
  fewer branches.
- **Never use `void` methods** when a pure `public static` function can
  return the result instead. Side effects are the exception, not the default.
- **Never expose mutable collections** in public signatures. Return
  `IReadOnlyList<T>`, `IReadOnlySet<T>`, or `IReadOnlyDictionary<K,V>`.
  Follow Postel's Law: `IEnumerable<T>`/`IReadOnlyList<T>` for parameters,
  `IReadOnlyList<T>`/`IReadOnlyCollection<T>` for returns. Internal
  collections (`List<T>`, `Dictionary<K,V>`) are fine.
  Use `.AsReadOnly()` at boundaries.
- **Never use `new List<T>()` in a field initializer** when `.AsReadOnly()`
  or `FrozenDictionary.ToFrozenDictionary()` can produce an immutable value.
- **Never use `class` when `record` is applicable.** Records are value
  semantics by default: equality, deconstruction, `with` expressions,
  and `init`-only properties.
- **Never use novelty just because it exists.** Every pattern below is
  triggered by a specific code smell. The pattern replaces a worse
  imperative equivalent.

## §1 Records — Immutable Data

**What it replaces:** Mutable `class` with setters, manual `Equals`/`GetHashCode`,
manual `ToString`, manual deconstruction.

**When to use:**

- Any DTO, value object, or analysis result type
- Any type where equality matters (caching, comparison, deduplication)
- Any type passed through a pipeline of transformations

**Before:**

```csharp
public class MethodCrap
{
    public string MethodName { get; set; }
    public double CrapScore { get; set; }
    public int Complexity { get; set; }
    // 20 lines of manual Equals/GetHashCode/ToString if needed
}
```

**After:**

```csharp
public sealed record MethodCrap(
    string MethodName,
    double CrapScore,
    int Complexity);
```

**With-expressions for non-destructive mutation:**

```csharp
var updated = result with { IsCoverageGap = true };
// Original result is unchanged. No mutation.
```

**Existing codebase examples:**

- `tools/` — 33 sealed records (MutationSite, MethodCrap, ArchResult, etc.)
- `src/` — 6 readonly record structs (PerformanceMetrics, TimingMetrics, etc.)

**Constraint:** Never add `set` to a record property. Use `{ get; init; }` if the
primary constructor shorthand is insufficient.

**Builder records for parameter consolidation:**

When 3+ methods pass the same 4+ shared collections as parameters,
extract a record to bundle them. This is a lightweight alternative to
the full Builder class pattern — the record is a descriptive bag of
shared state, not an active builder:

```csharp
// BEFORE: parameter bloat at every call site
private static void AddProjectDependencies(
    string project, IReadOnlyList<string> refs,
    ArchConfig config, HashSet<string> ignored,
    Dictionary<string, ISet<string>> deps, HashSet<string> unmapped)
{
    var component = config.ComponentMap.GetValueOrDefault(project, "Default");
    if (ignored.Contains(component)) return;
    // ...
}

// AFTER: record bundles shared state
private sealed record GraphBuilder(
    ArchConfig Config,
    HashSet<string> Ignored,
    Dictionary<string, ISet<string>> Deps,
    HashSet<string> Unmapped);

private static void AddProjectDependencies(
    string project, IReadOnlyList<string> refs, GraphBuilder b)
{
    var component = b.Config.ComponentMap.GetValueOrDefault(project, "Default");
    if (b.Ignored.Contains(component)) return;
    // ...
}
```

**Trigger:** Depth gate `params(1)` signal — method has >4 formal parameters,
and 3+ of those parameters are shared across multiple sibling methods.
**Benefit:** Eliminates params(1) Depth signal. CC unchanged but call-site
readability improves — 2 meaningful params instead of 6 opaque ones.

## §2 Pattern Matching — Replace if-else Chains

**What it replaces:** Nested `if-else`, `switch` statements, `is` chains,
type-testing cascades.

**When to use:**

- Dispatch on type (Roslyn SyntaxNode subclasses, enum values)
- Multi-condition branching where `if-else` would be ≥3 levels deep
- Property tests on nullable types (`is { Count: > 0 }`)

**Before (type dispatch with if-else):**

```csharp
if (node is ExpressionSyntax e)
    return NormalizeExpression(e);
else if (node is StatementSyntax s)
    return NormalizeStatement(s);
else if (node is MethodDeclarationSyntax m)
    return NormalizeMethod(m);
else
    return WalkChildren(node);
```

**After (switch expression):**

```csharp
return node switch
{
    ExpressionSyntax e => NormalizeExpression(e),
    StatementSyntax s => NormalizeStatement(s),
    MethodDeclarationSyntax m => NormalizeMethod(m),
    _ => WalkChildren(node),
};
```

**Property patterns (null-safe member access):**

```csharp
// Before: if (_articleItems != null && _articleItems.Count > 0)
// After:
if (_articleItems is { Count: > 0 })
```

**Relational patterns in switch expressions:**

```csharp
return value switch
{
    < 0 => "negative",
    0 => "zero",
    > 0 and < 10 => "small",
    >= 10 => "large",
};
```

**Existing codebase examples:**

- `DupesNormalizer.cs` — 12 switch expression dispatch arms for Roslyn AST
- `MutationApplicator.cs` — switch on `MutationCategory` enum

**Constraint:** Never nest switch expressions more than 2 levels deep.
Extract inner switches to named methods.

## §3 LINQ Pipelines — Data Transformation Declaratively

**What it replaces:** `foreach` with manual accumulation, nested loops with
temporary lists, manual filtering/grouping.

**When to use:**

- Filter + transform + collect patterns
- Aggregation (sum, average, fold)
- Flattening nested collections (SelectMany)
- Any data pipeline with ≥2 steps

**Before (imperative loop):**

```csharp
var result = new List<string>();
foreach (var line in lines)
{
    var trimmed = line.Trim();
    if (trimmed.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        result.Add(Path.GetFullPath(trimmed, projectPath));
}
return result;
```

**After (LINQ pipeline):**

```csharp
return lines
    .Select(f => f.Trim())
    .Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
    .Select(f => Path.GetFullPath(f, projectPath))
    .ToList();
```

**Aggregate (fold/reduce) for accumulation:**

```csharp
var stats = keys.Zip(sizes, (k, s) => (Key: k, Size: s))
    .Aggregate(new StatsAccumulator(), (acc, entry) =>
    {
        acc.Update(entry.Key, entry.Size);
        return acc;
    });
```

**SelectMany for flattening:**

```csharp
// Extract all attributes from all attribute lists
method.AttributeLists
    .SelectMany(al => al.Attributes)
    .Any(a => a.Name.ToString().Contains("Test"));
```

**Existing codebase examples:**

- `BrowserStorageService.cs:155` — Aggregate fold
- `GitFileFilter.cs:56` — Select/Where/Select chain
- `TestMethodParser.cs:48` — SelectMany

**Constraint:** Never mix LINQ and side effects (no `Console.WriteLine` or
`_logger.Log` inside a `.Select()` lambda). Use `foreach` for side-effecting
iteration. LINQ is for pure transformations.

## §4 Immutable Collection Interfaces — Safety at Boundaries

**What it replaces:** `List<T>`, `Dictionary<K,V>`, `HashSet<T>` in return types
and public properties.

**When to use:**

- Every method that returns a collection (no exceptions)
- Every public property or field holding a collection
- Every `static readonly` field that should not change after construction

**Before:**

```csharp
public List<MutationSite> Sites { get; set; }
public Dictionary<string, int> Counts { get; set; }
```

**After:**

```csharp
public IReadOnlyList<MutationSite> Sites { get; init; }
public IReadOnlyDictionary<string, int> Counts { get; init; }
```

**Frozen collections for static lookup tables:**

```csharp
// Singleton data that never changes — zero GC pressure after init
private static readonly FrozenDictionary<string, int> Lookup =
    new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 }
        .ToFrozenDictionary();
```

**Existing codebase examples:**

- `IReadOnlyList<T>` — 142 occurrences throughout `tools/`
- `FrozenDictionary` — `TestNormalizer.cs:22` LiteralMap
- `.AsReadOnly()` — 12 call sites at collection creation boundaries

**Constraint:** Never use `IEnumerable<T>` in return types when the caller
needs to index or count. Use `IReadOnlyList<T>` — it is still immutable
but preserves the list contract.

## §5 Higher-Order Functions — Func<> and Delegates

**What it replaces:** Interface injection for single-method dependencies,
hard-coded processing logic, Strategy pattern boilerplate.

**When to use:**

- Inject a single I/O dependency without creating an interface (Process.Start,
  File.ReadAllText, HttpClient)
- Replace a switch expression with a lookup: `Func<SyntaxKind, string>`
- Pass behavior as data (filter predicates, transformation lambdas)
- LoggerMessage delegates for structured, high-performance logging

**Before (hard-coded Process.Start):**

```csharp
private static async Task<string?> GenerateCoverage(string path)
{
    var process = Process.Start(new ProcessStartInfo("dotnet", ...));
    await process!.WaitForExitAsync().ConfigureAwait(false);
    // untestable — requires real dotnet installation
}
```

**After (injectable Func<> — testable without dotnet):**

```csharp
public static async Task<string?> GenerateCoverage(
    string path,
    Func<string, Task<string?>>? generate = null)
{
    generate ??= RealGenerateCoverage;
    return await generate(path).ConfigureAwait(false);
}

// Test:
var result = await GenerateCoverage("/fake",
    generate: _ => Task.FromResult<string?>(null));
```

**LoggerMessage.Define — compile-time generated delegates:**

```csharp
private static readonly Action<ILogger, string, long, Exception?> LogEvictedItem =
    LoggerMessage.Define<string, long>(LogLevel.Debug, new EventId(1),
        "Evicted {Key} with size {Size} bytes from LRU cache");
```

**Existing codebase examples:**

- `MutateHandler.cs` — `Func<string, Task<string?>>` injection
- `CrapCommand.cs` — `Func<string, string?>` injection
- `BrowserStorageService.cs` — 8 LoggerMessage delegates
- `Articles.razor.cs` — 14 LoggerMessage delegates

**Constraint:** Never add a `Func<>` parameter when the method has 5+
dependencies. At that point, proper DI with interfaces is clearer.

**Applied pattern — static orchestrator with context record:**

Func<> callbacks + a context record replace `ComponentBase` inheritance
for shared page orchestration. The orchestrator is a `static class` with
pure methods; the context record bundles mutable page state. Func<>
delegates serve as the seam for page-specific behavior (fetch, populate
images, state change notification). Zero interface declarations, zero DI
registration. Full pattern: `docs/solutions/architecture-patterns/
composition-over-inheritance-orchestrator-pattern-2026-05-23.md`

## §6 Pure Static Methods — Functions Without Side Effects

**What it replaces:** Instance methods that read internal state, methods
with hidden dependencies, untestable methods with embedded I/O.

**When to use:**

- Any transformation: input in, output out, no mutation
- Pipeline stages: normalize → analyze → format → output
- Validation: `bool IsValid(T input)`, no state access
- Lookups: `string Format(SyntaxKind kind)`, no I/O

**Before (hidden dependency on field):**

```csharp
public double Calculate()
{
    return _baseRate * _multiplier; // depends on instance state
}
```

**After (explicit, testable):**

```csharp
public static double Calculate(double baseRate, double multiplier)
    => baseRate * multiplier;
```

**Existing codebase examples:**

- `tools/` — 312 public static methods across ~60 static classes
- `MutationApplicator.Apply(string source, int index, MutationSite)` — pure string transformation
- `CrapHandler.WriteTable(...)` — pure formatting
- `CyclomaticComplexity.Analyze(string projectPath)` — pure analysis

**Constraint:** A public static method must have zero side effects. If it
writes to a file, spawns a process, or logs — it goes through an injected
`Func<>` or `TextWriter`, never directly.

## §7 Expression-Bodied Members — Reduce Boilerplate

**What it replaces:** Block-bodied methods, properties, and constructors
that are a single expression.

**When to use:**

- Single-expression methods and properties
- Constructors that only assign parameters
- Simple computed properties

**Before:**

```csharp
public string FullName
{
    get { return $"{FirstName} {LastName}"; }
}

public bool IsCovered()
{
    return Hits > 0;
}
```

**After:**

```csharp
public string FullName => $"{FirstName} {LastName}";

public bool IsCovered() => Hits > 0;
```

**Single-line switch expressions:**

```csharp
public static string ResolveFormat(bool json, string? formatOption)
    => json ? "json" : (formatOption ?? "text");
```

**Existing codebase examples:**

- 552 expression-bodied members across the codebase
- `DupesNormalizer.cs` — switch expression bodies
- `MutateHandler.cs:332` — `WasCoverageGenerated` guard

**Constraint:** Never use `=>` when the body is >1 expression. Switch to a
block body for readability if the expression wraps to multiple lines.

## §8 Tuple Deconstruction — Multi-Return Without Classes

**What it replaces:** `out` parameters, ad-hoc DTO classes for method returns,
temporary holder types.

**When to use:**

- A method returns 2-3 related values that don't merit a named type
- Destructuring a result into local variables at the call site
- `TryXxx` patterns: `(bool success, T value)` instead of `out T`

**Before (out parameter):**

```csharp
public static bool TryParse(string input, out int result)
{
    return int.TryParse(input, out result);
}
```

**After (tuple return — call site deconstructs):**

```csharp
var (success, result) = TryParse(input);

public static (bool Success, int Value) TryParse(string input)
    => (int.TryParse(input, out var v), v);
```

**Multi-return from pipeline methods:**

```csharp
var (sites, covered, uncovered, changed) =
    await DiscoverSitesAsync(...).ConfigureAwait(false);
```

**Existing codebase examples:**

- `MutateHandler.cs:23` — 7-tuple deconstruction from `DiscoverSitesAsync`
- `CoverageReader.cs:47` — `(Covered, Uncovered)` tuple return
- `MutationRunner.cs:14` — `(canProceed, timeout)` deconstruction
- `ArchHandler.cs:50` — `(exitCode, result)` deconstruction

**Constraint:** Never use a tuple with >3 unnamed elements. If you need 4+,
either name every element or create a record. Unnamed tuples with >3
elements are unreadable at the call site.

## §9 Collection Expressions — Declarative List Construction

**What it replaces:** `new List<T> { ... }`, `new[] { ... }`, `.ToArray()`.

**When to use:**

- Every list/array literal in initialization and test data
- Method arguments that take `IReadOnlyList<T>` or `IEnumerable<T>`

**Before:**

```csharp
var items = new List<string> { "a", "b", "c" };
var result = items.ToArray();
DoSomething(new[] { 1, 2, 3 });
```

**After:**

```csharp
List<string> items = ["a", "b", "c"];
int[] result = [1, 2, 3];
DoSomething([1, 2, 3]);
```

**In tests — fluent test data construction:**

```csharp
var coveredLines = new HashSet<int> { 10, 20, 30 };
// becomes:
HashSet<int> coveredLines = [10, 20, 30];
```

**Existing codebase examples:**

- Used pervasively in test data throughout `tools/tests/`

**Constraint:** Collection expressions work with any type that has a
collection-builder attribute. Use for `List<T>`, `HashSet<T>`, arrays,
`IReadOnlyList<T>`, and any custom collection with the builder pattern.

## WHEN TO LOAD

Load this skill when writing or reviewing any C# code that involves:
data modeling, control flow, collection handling, method signatures,
dependency injection, logging, or transformation pipelines. The
patterns below cover the full decision space.

## NEVER

- Do not use novelty just because it exists. Every pattern above replaces
  a worse imperative equivalent.
- Do not mix functional and imperative styles within a single method.
  A method is either a pure pipeline or a side-effecting procedure, never both.
- Do not use mutable collections in public signatures. The `MA0016` analyzer
  enforces this — do not suppress it.
- Do not add `set` to record properties. Records are immutable by convention.
- Do not nest LINQ chains deeper than 4 operations without extracting to
  a named method. Deep chains are un-debuggable.
- Do not use `IEnumerable<T>` as a return type when `IReadOnlyList<T>`
  is applicable. Lazy enumeration hides collection costs from the caller.
