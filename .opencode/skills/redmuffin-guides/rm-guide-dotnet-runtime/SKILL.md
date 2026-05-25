---
name: rm-guide-dotnet-runtime
description: "Use when deciding whether to use .NET 9 APIs or current runtime best practices."
---

# rm-guide-dotnet-runtime

## CRITICAL

- Never use deprecated APIs when a .NET 9 equivalent exists.
- Use modern collections, spans, and JSON caching where appropriate.

## WHEN TO LOAD

- Adopting .NET 9 runtime or library improvements.
- Reviewing performance-sensitive code.

## GUIDANCE

- Consider `CountBy`, `AggregateBy`, and other current LINQ additions when they fit.
- Cache reusable `JsonSerializerOptions`.
- Never use string slicing in hot paths where span-based APIs are available.

## NEVER

- Do not add new runtime-specific tricks without a measurable reason.
