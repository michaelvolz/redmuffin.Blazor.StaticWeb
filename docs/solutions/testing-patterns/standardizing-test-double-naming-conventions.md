---
date: 2025-08-02
title: "Standardizing Test Double Naming Conventions"
tags: [testing, tunit, test-doubles, mocking, naming-conventions]
problem_type: testing
---

## Problem

Test doubles across the project used inconsistent naming — no suffix convention, no clear distinction between mocks, stubs, spies, fakes, and dummies. LightMock instances coexisted with NSubstitute (later deprecated) and custom mocks with no naming system to distinguish them. This made test code harder to read, review, and maintain. Developers could not immediately identify the role of a test double from its name.

## Root Cause

No naming convention was ever established for test doubles. The project evolved from NSubstitute (subsidiary naming) to LightMock.Generator to custom mock classes, but each migration preserved whatever name the original test had. The `Mock` suffix was inconsistently applied and mixed with `Test` prefix naming from legacy test infrastructure.

## Solution

Established a suffix-based naming convention using underscores for all test doubles:

| Suffix   | Type  | Purpose                                      |
| -------- | ----- | -------------------------------------------- |
| `_Mock`  | Mock  | Verify interactions between objects          |
| `_Stub`  | Stub  | Provide predefined responses to method calls |
| `_Spy`   | Spy   | Record information about interactions        |
| `_Fake`  | Fake  | Fully functional implementation for testing  |
| `_Dummy` | Dummy | Passed around but never actually used        |

**Key rules:**

- LightMock.Generator instances: Always use `_Mock` suffix (they are exclusively mocks for external/3rd-party dependencies)
- Custom test infrastructure classes: Use `Mock` suffix (e.g., `MockFunctionContext`, `NavigationManagerMock`) instead of legacy `Test` prefix
- Never use NSubstitute (fully deprecated and removed)

**Examples:**

```csharp
var userService_Mock = new Mock<IUserService>();
var dataRepository_Stub = new DataRepository_Stub();
```

**Audit process:** All existing test doubles were scanned, classified against the convention, renamed, and verified. `docs/TestingGuidelines.md` was updated to codify the standards.

## Prevention

- All new test doubles must use the suffix-based convention from creation
- LightMock.Generator is the exclusive mocking framework for external dependencies (no alternatives)
- Custom mocks use `Mock` suffix naming (not `Test` prefix)
- `docs/TestingGuidelines.md` serves as the canonical reference for reviewers and AI assistants
