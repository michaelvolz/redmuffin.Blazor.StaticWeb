---
title: Standardizing Test Doubles
version: 1.0
date_created: 2025-08-02
---

# Introduction

This document aims to standardize the naming, usage, and implementation of test doubles in the Blazor WebAssembly project to improve clarity and ease of understanding.

## 1. Purpose & Scope

The goal of this PRD is to facilitate consistent naming conventions for test doubles, ensuring each object is immediately identifiable by its name. This will make test code more comprehensible and easier to review, thereby enhancing collaboration among developers.

## 2. Definitions

- **Test Doubles**: General term for any object used in unit testing to replace real dependencies.
  - **Mocks**: Used to verify interactions between objects.
  - **Stubs**: Provide predefined responses to method calls.
  - **Spies**: Record information about interactions.
  - **Fakes**: Fully functional implementations, for testing scenarios.
  - **Dummies**: Passed around but never actually used.

## 3. Requirements, Constraints & Guidelines

- **REQ-001**: Implement a suffix-based naming convention using underscores (e.g., `Something_Mock`).
- **REQ-002**: Validate all existing test doubles and classify them correctly under the new convention.
- **REQ-003**: Ensure LightMock is used exclusively as a mock and ONLY for external/3rd-party dependencies.

## 4. Interfaces & Data Contracts

Not applicable.

## 5. Acceptance Criteria

- **AC-001**: All test doubles must follow the standardized naming convention.
- **AC-002**: All LightMock instances are classified as mocks, with clear documentation.

## 6. Test Automation Strategy

Strategy includes reviewing and updating test double usage in unit tests to ensure compliance with new standards.

## 7. Rationale & Context

Clear and consistent naming of test doubles provides immediate clarity for developers, simplifying code reviews and maintenance.

## 8. Dependencies & External Integrations

- **LightMock Library**: Ensure compatibility and exclusive usage for mocks.

## 9. Examples & Edge Cases

```csharp
var userService_Mock = new Mock<IUserService>();
var dataRepository_Stub = new DataRepository_Stub();
```

## 10. Validation Criteria

Verify that all test doubles in the project adhere to the new naming convention and LightMock is used appropriately.

## 11. Related Specifications / Further Reading

- [LightMock Documentation](https://lightmock.github.io/)
- [Best Practices for Mocks and Stubs](https://www.martinfowler.com/articles/mocksArentStubs.html)
