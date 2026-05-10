---
date: 2026-05-10
last_updated: 2026-05-10
tags:
  - testing
  - unit-testing
  - crap
  - cleanup
  - patterns
---

# Pure Function Extraction — Testing Without Interfaces

## What Belongs in This File

- **Viewpoint**: Developer writing unit tests for a class that contains
  complex private logic. You want to test the logic directly without
  introducing interfaces, subclassing, or DI boilerplate that serves
  only tests.
- **What belongs**: How to extract pure functions from private methods
  for direct unit testing. When the pattern applies and when it does not.
  The decision framework for choosing between pure function extraction,
  testing through public APIs, and interface-based mocking.
- **What does NOT belong**: General TDD workflow (use `rm-tdd`).
  Interface design guidelines (use `rm-guide-di`). Mocking frameworks.
  Integration or end-to-end testing patterns.

---

## Problem

You have a private method with complex logic:

```csharp
private static CacheHealthStatus DetermineHealthStatus(
    double storageUtilization, CacheStats stats)
{
    if (storageUtilization > 90) return CacheHealthStatus.Critical;
    if (storageUtilization > 80) return CacheHealthStatus.Warning;
    if (stats.TotalExpiredItemsCount > stats.TotalItems * 0.1)
        return CacheHealthStatus.Warning;
    return CacheHealthStatus.Healthy;
}
```

This method is:

- Private (can't test directly)
- Pure (receives everything as parameters, returns a value, no side effects)
- Called from a public method that has dependencies (`IBrowserStorageService`)

**Anti-pattern**: Create `IHealthCalculator` + `HealthCalculator` just
so you can mock it. This pollutes the domain model with test-only
abstractions.

**Better pattern**: Make the pure function public so tests can call it
directly.

## Pattern: Pure Function Extraction

1. **Identify** a private method that receives all state as parameters
   and produces a return value (no side effects).
2. **Make it `public static`** (or `internal static` if you prefer
   limiting visibility — but `public` is simpler).
3. **Test it directly** in the test project. No mocking. No DI. No
   subclassing.

```csharp
// Production code
public static CacheHealthStatus DetermineHealthStatus(
    double storageUtilization, CacheStats stats) { ... }

// Test code
[Test]
public async Task DetermineHealthStatus_Critical_WhenAbove90()
{
    var stats = new CacheStats { TotalItems = 100, TotalExpiredItemsCount = 0 };
    var result = CacheMonitoringService.DetermineHealthStatus(95, stats);
    await Assert.That(result).IsEqualTo(CacheHealthStatus.Critical);
}
```

## Why This Is Better

| Approach                     | Code changes                | Test complexity      |
| ---------------------------- | --------------------------- | -------------------- |
| Mock via interface           | New interface + class + DI  | High (mock setup)    |
| Subclass (protected virtual) | `protected virtual` keyword | High (test subclass) |
| **Pure function extraction** | `private` → `public`        | Low (call directly)  |

The method was already a pure function — changing one keyword is the
minimum possible change.

## When NOT to Use This Pattern

- Method has side effects (writes to files, calls APIs, mutates state)
- Method depends on instance state (`this._field`) — extract the
  field-dependent part instead
- Method is a trivial one-liner (CRAP score < 3)
- The class has too many public methods and this would worsen the API
  surface — consider nesting in a `public static partial class` instead

## Decision Framework

```
Is the logic complex enough to justify testing?
  → No: Test through the public caller (black-box)
  → Yes: ↓
Does the method modify state or have side effects?
  → Yes: Test through the public caller with real dependencies
  → No: ↓
Can you pass all dependencies as parameters?
  → Yes: Pure function extraction (public static)
  → No: Extract the parameterized part, test that
```

## Relationship to Uncle Bob's Clean Code

Robert C. Martin (Clean Code, ch. 3): "Functions should either do
something or answer something, but not both." A pure function "answers
something" — it takes inputs and returns a value. This makes it
ideal for direct unit testing.

Michael Feathers (Working Effectively with Legacy Code, ch. 25):
"When you have a method that you want to test, but it's private ...
see if you can make it public. If the method is a pure function, making
it public is not a design compromise — it's just making the function
available for testing, which is one of its legitimate uses."

## Example from Our Codebase

`DetermineHealthStatus` in `CacheMonitoringService.cs`:

- **Before**: `private static` — 31% coverage, CRAP 9.3
- **Change**: `public static` — tests cover all 4 branches
- **Expected result**: Coverage → ~95%, CRAP → ~3.0

Same pattern applies to `AnalyzePerformanceIssues` (CRAP 8.1) and
`ShouldDisplayComponent` (CRAP 9.2) — private pure functions with
low coverage that can become public static.

## Related

- `docs/specs/quality-gates-cleanup-workflow-spec.md` (if created)
- `.opencode/skills/redmuffin-standards/rm-guide-cleanup/SKILL.md` §2
  (characterization tests)
- `.opencode/skills/redmuffin-standards/rm-guide-testing/SKILL.md` §2
  (characterization test patterns)
