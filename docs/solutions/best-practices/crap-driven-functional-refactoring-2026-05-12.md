---
date: 2026-05-12
title: CRAP-Driven Functional Refactoring — Validation Pipeline via LINQ Composition
tags: [crap, refactoring, functional, linq, validation, best-practice]
description: >
  How to use CRAP as a signal to find poorly-structured code, then apply
  modern C# functional features (LINQ, Array.TrueForAll, lambda composition)
  to reduce complexity while making the code genuinely better — not just
  moving lines around.
module: quality-gates
problem_type: best-practices
---

## Problem

`IsValid()` in `PrunedRaindropItem.cs` had CRAP 156.0, CC=12, 0% coverage.
Six independent validation checks structured as guard clauses. The initial
reaction — extract helper methods — produced shallow one-liner wrappers
that inverted boolean logic and made the code _less_ readable.

## What Didn't Work

### Shallow extraction (rejected)

```csharp
// Extracted helper — worse than inline
public static bool IsValidUri(string? uri)
    => uri is null || Uri.TryCreate(uri, UriKind.Absolute, out _);

// Call site — reader must jump to definition and mentally negate
if (!IsValidUri(Link)) return false;
```

Ousterhout's "shallow module": interface cost (jump-to-definition, boolean
inversion) exceeds abstraction benefit. Fowler's Inline Function refactoring
exists for exactly this case — when the body is clearer than the name.

### DataAnnotations (researched, rejected)

`[Url]` attribute accepts relative URIs (`UriKind.RelativeOrAbsolute`).
Raindrop API always returns absolute URLs — behavioral regression.
Custom attribute adds net-new code for marginal benefit.

### Switch expression (researched, rejected)

Switch matches a single discriminant against patterns. Five independent
fields with five different rules — no single value to switch on.

## What Worked

### Step 1: Characterization tests first (Feathers)

Eleven tests covering every decision path before touching the code:

```
Id=0 → false, Id=-1 → false, all valid → true, all null → true,
Link invalid → false, Link valid → true, Cover invalid → false,
Cover valid → true, Title=501 → false, Title=500 → true,
Excerpt=2001 → false, Excerpt=2000 → true
```

CRAP dropped from 156.0 to 34.9 (78% reduction) from tests alone — no
code changes. Coverage: 0% → 46%.

### Step 2: Single-expression && chain (functional)

Replaced guard clauses with composed boolean predicates:

```csharp
return Id > 0
    && (Link == null || Uri.TryCreate(Link, UriKind.Absolute, out _))
    && (Cover == null || Uri.TryCreate(Cover, UriKind.Absolute, out _))
    && (Title == null || Title.Length <= 500)
    && (Excerpt == null || Excerpt.Length <= 2000);
```

- Eliminated 3 `is not null` patterns (each adds +1 CC)
- Eliminated 2 `?.` null-conditionals (each adds +1 CC)
- No boolean inversion — reads left-to-right as a pipeline
- CRAP: 13.3, CC: 9, Coverage: 62%

### Step 3: LINQ composition (final)

Extracted validators as a static readonly array of predicates:

```csharp
private static readonly Func<PrunedRaindropItem, bool>[] Validators =
[
    item => item.Id > 0,
    item => item.Link == null || Uri.TryCreate(item.Link, UriKind.Absolute, out _),
    item => item.Cover == null || Uri.TryCreate(item.Cover, UriKind.Absolute, out _),
    item => item.Title == null || item.Title.Length <= 500,
    item => item.Excerpt == null || item.Excerpt.Length <= 2000,
];

public bool IsValid() => Array.TrueForAll(Validators, v => v(this));
```

**Final: CRAP 1.0, CC=1, 100% coverage.**

### Full Journey

| Approach                 | CRAP  | CC  | Coverage |
| ------------------------ | ----- | --- | -------- |
| Original (guard clauses) | 156.0 | 12  | 0%       |
| + Characterization tests | 34.9  | 12  | 46%      |
| && chain (functional)    | 13.3  | 9   | 62%      |
| LINQ `Array.TrueForAll`  | 1.0   | 1   | 100%     |

## Why This Works

1. **`Array.TrueForAll` short-circuits** — stops at the first `false`,
   identical semantics to `&&` chain or guard clauses
2. **Static readonly array** — allocated once, zero per-call overhead
3. **Each lambda is a pure predicate** — independently readable without
   jumping to definitions
4. **Method CC=1** — complexity lives in the lambdas (which Roslyn's
   CC analysis attributes to anonymous functions, not the containing
   method)
5. **Full coverage** — the delegate invocation `v => v(this)` is covered
   by existing characterization tests

## The Process (How to Approach CRAP Properly)

CRAP is a **signal**, not a **target**. The process is:

1. **Identify** what the method really is — `IsValid()` is a validation
   pipeline, not a general-purpose method
2. **Characterize** existing behavior with tests before touching code
   (Feathers: characterize-first rule)
3. **Research** modern C# approaches for the specific problem domain:
   functional composition, pattern matching, LINQ, framework features
4. **Measure** each approach — run CRAP on every iteration to see
   actual CC and coverage impact
5. **Reject** approaches that make the code worse: shallow extraction,
   boolean inversion, unnecessary dependencies
6. **Accept** when the code is genuinely better — CRAP dropping is
   a natural byproduct, not the goal

## Prevention

- When facing a CC≥9 method with independent condition checks, consider
  functional composition before extraction
- `Array.TrueForAll` with a static predicate array is the C# equivalent
  of `all`/`every` in functional languages
- Never extract a one-liner that inverts boolean logic — it's a shallow
  module (Ousterhout) and an inline-function candidate (Fowler)
- The characterization-test-first rule (Feathers §2.1) is non-negotiable:
  you cannot safely refactor code you haven't characterized

## References

- Feathers, _Working Effectively with Legacy Code_ — characterization tests, seam identification
- Ousterhout, _A Philosophy of Software Design_ — deep vs shallow modules
- Fowler, _Refactoring_ — Inline Function, Extract Function
- Metz, _Practical Object-Oriented Design_ — Rule of Three
- `rm-guide-cleanup` §2.1 — Extraction Decision Tree
- `rm-guide-warnings` — Pragma Decision Tree
