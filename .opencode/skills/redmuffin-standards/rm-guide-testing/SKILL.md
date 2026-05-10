---
name: rm-guide-testing
description: "Shortcut: rm:guide-testing. Use when creating or reviewing TUnit tests, test doubles, and test scope helpers."
---

# rm-guide-testing

See also: `rm-gates-cleanup` for characterization tests and mutation
verification workflows. `rm-guide-cleanup` §2 for the characterization
test pattern (Michael Feathers).

## CRITICAL

- Use TUnit with built-in fluent assertions.
- Name test doubles as `[Class]_[Type]`.
- Keep helpers in `[TestClass].Helpers.cs` partial files.
- Use `TestScope` for shared test setup.

## Characterization tests (for untested methods)

Before refactoring a method without adequate tests:

1. Write characterization tests — test ONLY observable inputs/outputs.
   Never test internal implementation details.
2. Use the golden master pattern: capture current output, refactor, verify
   output unchanged.
3. After refactoring, graduate to proper unit tests on extracted pieces.

## Test quality

- Test behavior, not implementation details.
- Prefer real integration paths when a boundary or callback matters.
- Each test should have a meaningful assertion. Avoid coverage-only tests
  that pass but verify nothing.
- After CRAP fixes, run mutation testing on the changed file to verify
  tests actually catch logic errors (see `rm-gates-cleanup` Gate 4).

## WHEN TO LOAD

- Writing new tests or updating existing tests.
- Adding mocks, stubs, spies, or fixture helpers.
- Adding tests to untested legacy methods.

## GUIDANCE

```csharp
await Assert.That(value).IsNotNull();
```

- For table-driven tests, use `[MethodDataSource]` (TUnit) when multiple
  tests differ only in data.
- When parametrizing with `[MethodDataSource]`, keep the data method close
  to the test class.

## NEVER

- Do not add FluentAssertions.
- Do not put helper types in standalone helper files.
- Do not write tests coupled to implementation details.
