---
date: 2026-05-12
title: CRAP-Driven Refactoring — DataAnnotations Validation with Custom Attributes
tags: [crap, refactoring, validation, dataannotations, best-practice]
description: >
  How CRAP identified suboptimal validation code, leading to discovery and
  standardization of the .NET DataAnnotations pattern with custom
  ValidationAttribute for the entire repo.
module: quality-gates
problem_type: best-practices
---

## The CRAP-Driven Refactoring Process

CRAP is a **signal**, not a target. The process:

1. **Identify** — CRAP flags a method with high complexity
2. **Analyze** — what is this method's true purpose? (not "validates" — it's a
   _validation pipeline_)
3. **Research** — what is the modern C# best practice for this problem?
4. **Implement** — apply the standard pattern
5. **Standardize** — use as the canonical example for all future cases

Each CRAP violation is an opportunity to discover and standardize an optimal
pattern. Over time, the codebase converges toward standardized, idiomatic C#
code that any .NET developer instantly recognizes.

## Original Code

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

## Investigation Path

| Approach                | Verdict      | Why                                                                    |
| ----------------------- | ------------ | ---------------------------------------------------------------------- |
| Extract helpers         | Rejected     | Shallow modules (Ousterhout) — one-liner wrappers that invert booleans |
| `&&` chain              | Partial      | CC 12→9, but still 13.3 CRAP                                           |
| LINQ `Array.TrueForAll` | Partial      | CC 1, CRAP 1.0, but non-standard pattern                               |
| Switch expression       | Rejected     | No single discriminant — 5 independent fields                          |
| `IValidatableObject`    | Rejected     | Same CC=12; community reserves it for cross-property checks            |
| **DataAnnotations**     | **Accepted** | Standard .NET pattern, every dev recognizes it                         |

## Final Solution

### Custom attribute for reusable validation

```csharp
// src/redmuffin.Blazor.StaticWeb.Common/Validation/AbsoluteUrlAttribute.cs
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class AbsoluteUrlAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext ctx)
    {
        if (value is not string s)
            return ValidationResult.Success;

        return Uri.TryCreate(s, UriKind.Absolute, out _)
            ? ValidationResult.Success
            : new ValidationResult(
                ErrorMessage ?? $"The {ctx.DisplayName} field must be an absolute URL.");
    }
}
```

### Model with declarative validation

```csharp
public sealed class PrunedRaindropItem
{
    [Range(1, long.MaxValue, ErrorMessage = "ID must be a positive value.")]
    public long Id { get; set; }

    [AbsoluteUrl(ErrorMessage = "Link must be a valid absolute URI.")]
    public string? Link { get; set; }

    [AbsoluteUrl(ErrorMessage = "Cover must be a valid absolute URI.")]
    public string? Cover { get; set; }

    [MaxLength(500, ErrorMessage = "Title cannot exceed 500 characters.")]
    public string? Title { get; set; }

    [MaxLength(2000, ErrorMessage = "Excerpt cannot exceed 2000 characters.")]
    public string? Excerpt { get; set; }

    public bool IsValid() =>
        Validator.TryValidateObject(this, new(this), null, validateAllProperties: true);

    public void ValidateOrThrow()
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(this, new(this), results, validateAllProperties: true))
            throw new ValidationException(results[0], null, this);
    }
}
```

## Results

| Method            | Before | After   | CC  | Pattern                       |
| ----------------- | ------ | ------- | --- | ----------------------------- |
| `IsValid`         | 156.0  | **1.1** | 1   | `Validator.TryValidateObject` |
| `ValidateOrThrow` | 156.0  | **2.1** | 2   | `Validator.TryValidateObject` |

0 custom validation logic in the model. Rules are declarative attributes on
properties. Every .NET developer instantly recognizes `[Range]`, `[MaxLength]`,
`Validator.TryValidateObject`. Custom `[AbsoluteUrl]` follows the documented
`ValidationAttribute` extensibility pattern.

## Repo Standard

**For validation in this repo**, the standard pattern is:

1. **Built-in DataAnnotations** (`[Range]`, `[MaxLength]`, `[Required]`,
   `[StringLength]`) for simple property checks
2. **Custom `ValidationAttribute`** for reusable custom rules — subclass
   `ValidationAttribute`, override `IsValid()`, respect `ErrorMessage`
   property
3. **`validator.TryValidateObject`** as the validation entry point — for
   both boolean `IsValid()` and exception-throwing `ValidateOrThrow()`
4. **`IValidatableObject`** only for cross-property validation that
   attributes cannot express

This is the idiomatic .NET validation pattern. Use it for every model
that needs validation.

## Philosophy

The QualityGates process exists to make code **better over time**:

```
CRAP signal → analyze true purpose → research best practice →
implement standard pattern → template for future cases → repeat
```

Each violation is an opportunity to discover and standardize a pattern.
Code converges toward optimal, idiomatic C# that any developer recognizes
instantly. CRAP is a byproduct — standardized code naturally has low
complexity.
