---
name: rm-guide-testing
description: "Shortcut: rm:guide-testing. Use when creating or reviewing TUnit tests, test doubles, and test scope helpers."
---

# rm-guide-testing

## CRITICAL

- Use TUnit with built-in fluent assertions.
- Name test doubles as `[Class]_[Type]`.
- Keep helpers in `[TestClass].Helpers.cs` partial files.
- Use `TestScope` for shared test setup.

## WHEN TO LOAD

- Writing new tests or updating existing tests.
- Adding mocks, stubs, spies, or fixture helpers.

## GUIDANCE

```csharp
await Assert.That(value).IsNotNull();
```

- Test behavior, not implementation details.
- Prefer real integration paths when a boundary or callback matters.

## NEVER

- Do not add FluentAssertions.
- Do not put helper types in standalone helper files.
