---
title: "FrozenDictionary Switch Expression Replacement — CC Reduction via Static Lookup"
date: 2026-05-16
category: design-patterns
module: QualityGates
problem_type: design_pattern
component: tooling
severity: medium
applies_when:
  - "A switch expression or statement maps a finite set of values to results with no side effects"
  - "Cyclomatic complexity from a switch is inflating CRAP beyond reasonable thresholds"
  - "The mapping set is stable and known at compile time"
tags:
  [
    frozen-dictionary,
    cyclomatic-complexity,
    switch-expression,
    performance,
    crap,
    lookup-table,
  ]
---

# FrozenDictionary Switch Expression Replacement

## Context

`TestNormalizer.LiteralFeature` had a 6-arm switch expression mapping `SyntaxKind` values to
string constants (`$str`, `$num`, `$bool`, `$defnull`, `$lit`). The switch expression's
`or` patterns (`TrueLiteralExpression or FalseLiteralExpression`) each counted as separate
cyclomatic complexity branches, giving CC=8. At CC=8, the CRAP formula minimum is 8.0 — making
it impossible to PASS below ~96% coverage (the formula floor is CC itself).

## Guidance

Replace switch expressions with `static readonly FrozenDictionary<TKey, TValue>` +
`GetValueOrDefault`. The dictionary lookup has CC=1 (single hash lookup, no branches).
`FrozenDictionary` is preferred over `Dictionary` because it is allocation-free after
initialization — the GC treats it as immortal, so lookups produce zero gen-0 pressure.

**Conversion checklist:**

1. Extract the switch expression into a `static readonly` field
2. Build a `Dictionary<K, V>` with the exact same mappings (split `or` patterns into individual entries)
3. Call `.ToFrozenDictionary()` on the result
4. Replace the switch expression with `.GetValueOrDefault(key, defaultValue)`
5. If the switch has a `_ =>` discard/default arm, that becomes the `defaultValue` parameter

## Why This Matters

- CRAP drops from 8.0 to ~1.0 (the minimum achievable for a lookup)
- FrozenDictionary has zero allocations during lookups — switch expressions may allocate
  pattern-matching machinery
- Semantically identical: the mapping is a pure lookup with no logic
- `or` patterns in switch expressions inflate CC without adding real complexity — each `or`
  arm is +1 branch that Cobertura may not independently instrument

## When to Apply

Any switch expression/statement with ≥4 arms where ALL arms return simple constant values.
Applicable to:

- Enum-to-string mappings
- Status code to HTTP status mappings
- Feature flag dictionaries
- Roslyn SyntaxKind to string classifiers (the motivating case)

Do NOT apply when:

- Arms have side effects (logging, mutation, throwing)
- Arms contain non-trivial expressions (method calls, arithmetic)
- The mapping needs to be dynamic/runtime-configurable (use `Dictionary` instead of `Frozen`)

## Examples

**Before (CC=8, CRAP ≥ 8.0):**

```csharp
public static string LiteralFeature(SyntaxKind kind)
{
    return kind switch
    {
        SyntaxKind.StringLiteralExpression => "$str",
        SyntaxKind.NumericLiteralExpression => "$num",
        SyntaxKind.TrueLiteralExpression
            or SyntaxKind.FalseLiteralExpression => "$bool",
        SyntaxKind.NullLiteralExpression
            or SyntaxKind.DefaultLiteralExpression => "$defnull",
        _ => "$lit",
    };
}
```

CC breakdown: `or` patterns count each alternative as a branch. `TrueLiteralExpression` = 1,
`or FalseLiteralExpression` = +1, etc. Tools report CC=8 for this 6-arm switch.

**After (CC=1, CRAP ~1.0):**

```csharp
private static readonly FrozenDictionary<SyntaxKind, string> LiteralMap =
    new Dictionary<SyntaxKind, string>
    {
        [SyntaxKind.StringLiteralExpression] = "$str",
        [SyntaxKind.NumericLiteralExpression] = "$num",
        [SyntaxKind.TrueLiteralExpression] = "$bool",
        [SyntaxKind.FalseLiteralExpression] = "$bool",
        [SyntaxKind.NullLiteralExpression] = "$defnull",
        [SyntaxKind.DefaultLiteralExpression] = "$defnull",
    }.ToFrozenDictionary();

public static string LiteralFeature(SyntaxKind kind)
    => LiteralMap.GetValueOrDefault(kind, "$lit");
```

No branches. Single hash lookup. CRAP ~1.0. Identical behavior.

## Related

- [CRAP-Driven Functional Refactoring](/docs/solutions/best-practices/crap-driven-functional-refactoring-2026-05-12.md)
  — Methodology that identified this pattern. Note: the investigation table says "Switch
  expression → Rejected" for its specific case. For single-discriminant switches with many
  constant-value arms, FrozenDictionary lookup is often the optimal replacement.
