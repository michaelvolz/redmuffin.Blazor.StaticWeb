---
aliases: [tunit, test]
description: TUnit testing framework patterns and best practices
---
TUnit testing patterns for this project:

**Framework:**
- [Test] attribute (NOT [Fact])
- [Arguments] for data-driven tests
- Name tests: Component_Behavior_ExpectedOutcome

**Lifecycle:**
- [Before(Test)] / [After(Test)] for setup/teardown
- [Before(Class)] / [After(Class)] for class-level

**Assertions:**
- Use: `await Assert.That(value).IsEqualTo(expected)`
- Chain: `.And`, `.Or`, `.Within(tolerance)`
- All assertions are async - must await

**Data-Driven:**
- [Arguments] = inline data
- [MethodData] = method-based
- [ClassData] = class-based

**Advanced:**
- [Repeat(n)], [Retry(n)], [Skip("reason")]
- Parallel by default, use [NotInParallel] to disable
- [Timeout(milliseconds)] for test timeouts

**Quality:**
- ConfigureAwait(false) on ALL async calls
- Follow AAA pattern (Arrange, Act, Assert)
- Zero build warnings