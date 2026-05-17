---
date: 2026-05-12
last_updated: 2026-05-17
title: Functional C# Cleanup Patterns — CRAP Elimination via FrozenDictionary, LINQ, Builder Records, and Func<> Injection
tags:
  [
    crap,
    refactoring,
    functional-csharp,
    frozendictionary,
    linq,
    builder-pattern,
    depth-gate,
    best-practice,
  ]
description: >
  Catalog of functional C# patterns that eliminate CRAP violations by
  replacing imperative branching with declarative data structures and
  LINQ pipelines. Discovered and validated across two cleanup campaigns
  (May 12 DataAnnotations + May 17 DepthDetector).
module: quality-gates
problem_type: best-practices
---

## The CRAP-Driven Refactoring Process

CRAP is a **signal**, not a target. The process:

1. **Identify** — CRAP flags a method with high complexity
2. **Analyze** — what is this method's true purpose?
3. **Research** — what is the modern C# best practice for this problem?
4. **Implement** — apply the standard pattern
5. **Standardize** — use as the canonical example for all future cases

Each CRAP violation is an opportunity to discover and standardize an optimal
pattern. Over time, the codebase converges toward standardized, idiomatic C#
code that any .NET developer instantly recognizes.

## Structural-First Workflow (Depth → CRAP)

**Run Depth BEFORE CRAP during cleanup.** The Depth gate flags structural
defects (parameter bloat, wrong abstraction, dead parameters) that are the
root cause of many CRAP violations. Fixing them first eliminates CRAP
violations without writing tests — a structural fix cascades into free
CRAP wins.

Example from May 17: consolidating 6 shared parameters into a
`DuplicationContext` builder record eliminated 3 Depth FAIL signals and
simultaneously killed the CRAP violations on the consuming methods.
Zero new tests, zero behavior changes.

Always follow this order during cleanup: Architecture → Depth → CRAP →
SCRAP → Mutation → Dupes.

## Functional C# Pattern Catalog

When a CRAP or Depth violation appears, match the code smell to the pattern
below. These patterns delivered 91-100% CC reduction in the May 17 cleanup
campaign. See `rm-guide-csharp-features` for full pattern documentation.

### Pattern 1: FrozenDictionary (Switch → Lookup)

Switch expressions with ≥4 arms mapping to constant values. Replaced with
`FrozenDictionary<K,V>` + `GetValueOrDefault`. CC drops from N+1 to 1.
FrozenDictionary preferred over Dictionary for zero allocation in lookups.

**Before (CC=21, CRAP=462):**

```csharp
private static bool IsKnownPure(string methodName) => methodName switch
{
    "GetHashCode" => true,
    "Equals" => true,
    // ... 18 more arms
    _ => false,
};
```

**After (CC=1, CRAP=2):**

```csharp
private static readonly FrozenDictionary<string, bool> KnownPureMethods =
    new Dictionary<string, bool>
    {
        ["GetHashCode"] = true,
        ["Equals"] = true,
        // ... 18 more entries
    }.ToFrozenDictionary();

private static bool IsKnownPure(string methodName) =>
    KnownPureMethods.GetValueOrDefault(methodName, false);
```

### Pattern 2: LINQ `.Any()` Chain (foreach + Guards → Pipeline)

foreach loops with nested if-guard filtering. Each element tested against
multiple conditions. Replaced with `.Where().Where().Any()` chain. CC drops
to 1 — the branching is framework-level, not counted.

**Before (CC=8):**

```csharp
private static bool IsWrongAbstraction(SymbolGroup group)
{
    foreach (var method in group.Methods)
    {
        if (method.CyclomaticComplexity <= 5) continue;
        if (!IsKnownPure(method.Name)) continue;
        if (method.Parameters.Count == 0) continue;
        if (method.ReturnType == "void") return true;
    }
    return false;
}
```

**After (CC=1):**

```csharp
private static bool IsWrongAbstraction(SymbolGroup group) =>
    group.Methods
        .Where(m => m.CyclomaticComplexity > 5)
        .Where(m => IsKnownPure(m.Name))
        .Where(m => m.Parameters.Count > 0)
        .Any(m => m.ReturnType == "void");
```

### Pattern 3: Signal Array + LINQ (Composite Scoring → Declarative)

Methods that accumulate a score through multiple if-blocks. Each branch adds
weighted points based on a condition. Replaced with `(condition, points)[]`
signal array + `.Where().Sum()`. CC drops from N+1 to 2.

**Before (CC=14):**

```csharp
private static int ScoreMethod(MethodInfo method)
{
    int score = 0;
    if (method.CyclomaticComplexity > 10) score += 3;
    if (method.Parameters.Count > 5) score += 2;
    // ... 11 more branches
    return score;
}
```

**After (CC=2):**

```csharp
private static int ScoreMethod(MethodInfo method) =>
    new[]
    {
        (condition: method.CyclomaticComplexity > 10, points: 3),
        (condition: method.Parameters.Count > 5, points: 2),
        // ... remaining signals
    }
    .Where(s => s.condition)
    .Sum(s => s.points);
```

### Pattern 4: Builder Records (Parameter Bloat → Record)

When 3+ methods share 4+ threading parameters, consolidate into a
`sealed record`. Eliminates Depth's `params(1)` signal and cascades
into CRAP wins at every call site.

**Before (CC unchanged, params=6, Depth FAIL):**

```csharp
private static void AddProjectDependencies(
    string project, IReadOnlyList<string> refs,
    ArchConfig config, HashSet<string> ignored,
    Dictionary<string, ISet<string>> deps, HashSet<string> unmapped)
{
    var component = config.ComponentMap.GetValueOrDefault(project, "Default");
    if (ignored.Contains(component)) return;
    // ...
}
```

**After (params=3, Depth clean):**

```csharp
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

Three builder records emerged in one session: `GateRunResults` (11→2 params),
`GraphBuilder` (6→3), `DuplicationContext` (9→3).

### Pattern 5: Func\<\> Injection (I/O → Testable)

Methods with a single I/O dependency (Process.Start, Console.WriteLine,
HttpClient). Inject an optional `Func\<\>` parameter — tests supply a fake,
production uses the real implementation via null-coalescing default.

**Before (untestable, CRAP=30.0):**

```csharp
public static async Task<int> Execute(CommandLineArgs args)
{
    Console.SetOut(originalOut);  // side effect, cannot test
    // ...
}
```

**After (testable, CRAP=7.7):**

```csharp
public static async Task<int> Execute(
    CommandLineArgs args, TextWriter? output = null)
{
    var writer = output ?? Console.Out;
    writer.WriteLine(...);
    // ...
}
// Test: Execute(args, output: new StringWriter())
```

### Pattern 6: LINQ Pipeline (foreach → Select/Where/ToList)

foreach loops that filter and collect into a list. Replaced with
`.Select().Where().ToList()` pipeline.

### Pattern 7: Public Static Extraction (Private → Testable)

Private helper methods hiding pure, testable logic. Made `public static`
so characterization tests can target them directly. Only extract when
the helper is ≥5 lines of real logic (Feathers seam pattern).

### Pattern 8: Dead Code Removal

Methods with zero callers. Delete them — do not refactor, do not add tests,
do not "improve." Dead code is always a net negative.

## When NOT to Apply These Patterns

### Algorithm-Inherent Branching

Some methods are flagged by Depth's `wrong-abstraction(2)` signal because
their parameters control branching. But the branching IS the algorithm:

- **DFS cycle detection** — `Dfs(node, adj, visited, stack, path, cycles)`:
  the branching on visited/stack membership is inseparable from the algorithm
- **Conductor detection** — `TryClassifyAsConductor`: the branching on syntax
  node types IS the detection
- **Scoring functions** — `ComputeScore`: the branching on input values IS
  the scoring

**Test:** "If I removed this if-statement, would the method still do its job?"
If no, the branch stays. Document as algorithm-inherent.

### Shallow Method Decision Tree

For Depth's `shallow(3)` signal, apply this 5-question test before acting:

1. Name adds semantic value the raw body lacks? → **KEEP**
2. Called from ≥3 distinct callers? → **KEEP** (auto-suppressed by Phase 2)
3. Roslyn visitor/pattern override? → **KEEP** (pattern cost)
4. Extension method on framework type? → **KEEP**
5. All four NO? → **INLINE** (only if the body is as clear as the name)

In the May 17 campaign: 18 shallow(3) methods evaluated, 1 inlined
(`HasManifest` — name = body length), 17 kept.

## Original Case Study: DataAnnotations Validation (May 12)

### Original Code

```csharp
// PrunedRaindropItem.IsValid() — CC=12, CRAP 156.0, 0% coverage
public bool IsValid()
{
    if (Id <= 0) return false;
    if (Link is not null && !Uri.TryCreate(Link, UriKind.Absolute, out _)) return false;
    if (Cover is not null && !Uri.TryCreate(Cover, UriKind.Absolute, out _)) return false;
    if (Title?.Length > 500) return false;
    if (Excerpt?.Length > 2000) return false;
    return true;
}
```

### Investigation Path

| Approach                | Verdict      | Why                                                                    |
| ----------------------- | ------------ | ---------------------------------------------------------------------- |
| Extract helpers         | Rejected     | Shallow modules (Ousterhout) — one-liner wrappers that invert booleans |
| `&&` chain              | Partial      | CC 12→9, but still 13.3 CRAP                                           |
| LINQ `Array.TrueForAll` | Partial      | CC 1, CRAP 1.0, but non-standard pattern                               |
| Switch expression       | Rejected     | No single discriminant — 5 independent fields                          |
| `IValidatableObject`    | Rejected     | Same CC=12; community reserves it for cross-property checks            |
| **DataAnnotations**     | **Accepted** | Standard .NET pattern, every dev recognizes it                         |

### Final Solution

```csharp
public sealed class PrunedRaindropItem
{
    [Range(1, long.MaxValue, ErrorMessage = "ID must be a positive value.")]
    public long Id { get; set; }

    [AbsoluteUrl(ErrorMessage = "Link must be a valid absolute URI.")]
    public string? Link { get; set; }

    [MaxLength(500, ErrorMessage = "Title cannot exceed 500 characters.")]
    public string? Title { get; set; }

    public bool IsValid() =>
        Validator.TryValidateObject(this, new(this), null, validateAllProperties: true);
}
```

## Results Across Both Campaigns

| Campaign                     | Methods fixed | CRAP FAILs eliminated             | Key patterns                                                                     |
| ---------------------------- | ------------- | --------------------------------- | -------------------------------------------------------------------------------- |
| May 12 — DataAnnotations     | 2             | 2 (both 156.0)                    | DataAnnotations, custom ValidationAttribute                                      |
| May 17 — DepthDetector       | 10            | 10 (462→2, 56→COVERAGE GAP, etc.) | FrozenDictionary, LINQ .Any(), signal array, builder records, Func\<\> injection |
| May 17 — Tools depth cleanup | 7             | 7 (structural)                    | Builder records, flag guard pushes, dead code removal                            |

| Method                 | Before CRAP  | After CRAP        | Pattern                         |
| ---------------------- | ------------ | ----------------- | ------------------------------- |
| `IsValid`              | 156.0        | 1.1               | DataAnnotations                 |
| `ValidateOrThrow`      | 156.0        | 2.1               | DataAnnotations                 |
| `IsKnownPure`          | 462.0        | 2.0               | FrozenDictionary                |
| `IsWrongAbstraction`   | 17.1 (CC=8)  | Eliminated (CC=1) | LINQ .Any()                     |
| `AnalyzeMethod`        | 14.9 (CC=14) | Eliminated (CC=2) | Signal array + LINQ             |
| `DepthCommand.Execute` | 12.4         | COVERAGE GAP      | Func\<\> injection              |
| `DiscoverFromSlnx`     | 9.6 (CC=8)   | 5.4 (CC=5)        | LINQ pipeline                   |
| `AddClassLines`        | 9.2 (CC=8)   | 3.2 (CC=3)        | public static extraction        |
| `WriteSummaryAsync`    | Depth FAIL   | Depth CLEAN       | Builder record (GateRunResults) |

## Repo Standard

For CRAP violations in this repo, the standard approach is:

1. **Survey first** — run all gates once, classify violations
2. **Fix structural defects first** — Depth → Architecture → CRAP
3. **Match the pattern** — use the functional catalog above, not generic
   "extract method" advice
4. **Functional C# first** — reach for FrozenDictionary, LINQ, signal arrays,
   and builder records before extraction
5. **Test the extractable seams** — only extract when ≥5 lines of pure logic
   (Feathers seam pattern)
6. **Document false positives** — algorithm-inherent branching, visitor
   overrides, structural patterns that should never be refactored

CRAP is a byproduct — standardized, functional C# code naturally has low
complexity.

## Related

- [I/O Injection Pattern](/docs/solutions/design-patterns/io-injection-optional-func-parameter-2026-05-16.md) — injectable Func\<\> seam for process-spawning methods
- [FrozenDictionary Switch Replacement](/docs/solutions/design-patterns/frozendictionary-switch-expression-replacement-2026-05-16.md) — switch → FrozenDictionary for CC reduction
- [Design Changes Are the Point](/docs/solutions/conventions/design-changes-are-the-point-cleanup-philosophy-2026-05-16.md) — the philosophy document codifying design changes over mechanical extraction
- [CRAP Formula vs Cobertura Coverage Divergence](/docs/solutions/developer-experience/crap-formula-cobertura-coverage-divergence-2026-05-16.md) — why CC=8 at 100% coverage can still be formula-bound
- `rm-guide-csharp-features` — full functional C# pattern documentation (FrozenDictionary, LINQ, Func\<\>, records, pattern matching)
- `rm-gates-cleanup` — cleanup workflows, functional catalog lookup table, Depth cleanup decision tree
