---
name: rm-guide-csharp-features
description: "Use when adopting modern C# 12/13 features or deciding whether a new language feature fits the repo."
---

# rm-guide-csharp-features

## CRITICAL

- Never use legacy syntax when a modern equivalent is clearer.
- Use collection expressions, primary constructors, `nameof`, and pattern matching where natural.
- Avoid preview features unless the repo already uses them.

## WHEN TO LOAD

- Using new language features in production or tests.
- Modernizing existing code to current C# style.

## GUIDANCE

```csharp
List<int> values = [1, 2, 3];
public class UserService(ILogger<UserService> logger)
{
}
```

- Never use a complex language feature when a simpler one is equally expressive.
- Keep generated code and analyzers in mind before adopting a new syntax.

## NEVER

- Do not use novelty just because it exists.
- Do not mix styles within a single file without a reason.
