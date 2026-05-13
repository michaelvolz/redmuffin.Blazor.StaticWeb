---
name: rm-guide-dotnet9
description: "Use when deciding whether to use .NET 9 APIs or current runtime best practices."
---

# rm-guide-dotnet9

## CRITICAL

- Prefer current .NET 9 APIs when they simplify code or improve performance.
- Use modern collections, spans, and JSON caching where appropriate.

## WHEN TO LOAD

- Adopting .NET 9 runtime or library improvements.
- Reviewing performance-sensitive code.

## GUIDANCE

- Consider `CountBy`, `AggregateBy`, and other current LINQ additions when they fit.
- Cache reusable `JsonSerializerOptions`.
- Prefer span-based APIs over string slicing in hot paths.

## NEVER

- Do not add new runtime-specific tricks without a measurable reason.
