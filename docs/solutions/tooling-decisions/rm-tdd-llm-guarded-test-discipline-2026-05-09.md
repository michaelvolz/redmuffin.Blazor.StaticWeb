---
title: TDD Discipline with LLM-Specific Guardrails
date: 2026-05-09
category: tooling-decisions
module: testing
problem_type: tooling_decision
component: testing_framework
severity: high
applies_when:
  - Writing new code with tests-first TDD
  - Generating comprehensive tests for existing code
  - Using LLM agents for test generation
  - Preventing over-mocked, implementation-coupled test suites
tags:
  [
    tdd,
    test-driven-development,
    llm-guardrails,
    tunit,
    vertical-slicing,
    test-quality,
  ]
---

# TDD Discipline with LLM-Specific Guardrails

## Context

LLMs have specific failure modes when generating tests: they generate large test suites horizontally (many tests before any implementation), write speculative tests for imagined behaviors, couple tests to implementation details, and over-mock internal collaborators. Standard TDD advice assumes human discipline — LLMs need rigid, repeatable guardrails.

## Guidance

The `rm-tdd` skill enforces non-negotiable guardrails:

1. **Plan first.** Identify public interfaces and present the prioritized behavior list for user confirmation before writing any test.
2. **Vertical slicing only.** One observable behavior fully completed (test → code → refactor → green) before the next. Never generate multiple tests for unimplemented behaviors.
3. **Black-box focus.** Test only public contracts. Never test private methods. This guarantees internal refactoring safety.
4. **One logical assertion per test.** Keeps failure messages precise and tests independently understandable.
5. **Simple in-memory fakes over mocking frameworks.** Mock only at system boundaries (e.g., `IHttpClientFactory`). Prefer hand-rolled fakes for internal collaborators.

The skill activates automatically for any task involving tests-first development or comprehensive test generation. It precedes all other instructions for test generation.

## Why This Matters

Without these guardrails, LLMs produce 500-line test files with tests that break on any refactor. Vertical slicing keeps tests grounded in actual implemented behavior. Black-box testing means internal refactors never break tests. Simple fakes are easier to understand and debug than mocking-framework magic.

## When to Apply

Automatically on any task involving:

- Writing new code with tests first
- Generating comprehensive tests for existing code
- Any test-related work where the `rm-tdd` skill is loaded

## Examples

**Bad (horizontal — multiple tests before implementation):**

```csharp
// Agent generates 5 tests for a service that doesn't exist yet
[Test] public async Task GetById_ReturnsItem() { ... }
[Test] public async Task GetById_NotFound_Throws() { ... }
[Test] public async Task Create_ValidInput_Succeeds() { ... }
// ... more tests for unimplemented behaviors
```

**Good (vertical slicing — one behavior at a time):**

```csharp
// Step 1: Write one test
[Test]
public async Task GetById_WhenItemExists_ReturnsItem()
{
    var fake = new FakeRepository().WithItem("1", new Item("a"));
    var service = new ItemService(fake);
    var result = await service.GetByIdAsync("1");
    await Assert.That(result).IsNotNull();
    await Assert.That(result!.Id).IsEqualTo("a");
}

// Step 2: Implement just enough to pass
// Step 3: Refactor
// Step 4: Confirm green — then next behavior
```

## Related

- `.opencode/skills/rm-tdd/SKILL.md` — Full skill specification
- `.opencode/skills/rm-uncle-bob-martin-agentic-coding/SKILL.md` — Quality gates after TDD
