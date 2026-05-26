---
date: 2025-08-04
title: "Test Partial Class Reorganization by Category"
tags: [testing, tunit, partial-classes, organization, categorization]
problem_type: testing
---

## Problem

Test files had grown into monolithic single files mixing all test concerns — basic functionality, error scenarios, infrastructure tests, and behavior tests all in one class. This made it difficult to find related tests, understand the scope of what was being tested, and maintain clear separation between different test categories. The project had 258 tests across 2 test projects with no consistent organizational structure beyond the basic partial class split between tests and helpers.

## Root Cause

Tests were written incrementally without a categorization strategy. Each new test went into the main `[TestClass].cs` file regardless of its nature, creating ever-growing monolithic test classes. The only structural convention was `[TestClass].Helpers.cs` for infrastructure, which addressed one concern but left test methods themselves unorganized.

## Solution

Implemented a partial class reorganization system that splits tests by category into dedicated partial files:

**Category Definitions (from `docs/TestCategorizationRules.md`):**

| File                            | Category       | Contains                                                   |
| ------------------------------- | -------------- | ---------------------------------------------------------- |
| `[TestClass].EdgeCases.cs`      | Edge Cases     | Error, exception, null, invalid input, timeout scenarios   |
| `[TestClass].Infrastructure.cs` | Infrastructure | Lifecycle, logging, DI, authentication, caching, disposal  |
| `[TestClass].Behavior.cs`       | Behavior       | User interactions, clicks, form submissions, workflows     |
| `[TestClass].cs`                | Main           | Basic rendering, property validation, happy path scenarios |
| `[TestClass].Helpers.cs`        | Helpers        | TestScope, mocks, utilities (unchanged)                    |

**Migration process:**

1. Inventory all 258 test methods with PowerShell (100% accuracy required)
2. Apply categorization rules to each test method
3. Create migration mapping (JSON document) showing source → destination
4. Generate partial class files only where tests exist (no empty files)
5. Move tests incrementally by category: EdgeCases first, then Infrastructure, then Behavior
6. Validate build + test pass after each move

**Compliance requirements:**

- `public sealed partial class` declaration across all partial files
- Same namespace across all partial files
- `IDisposable` implementation only in main file
- All using statements preserved correctly
- TestScope and mock implementations remain in `.Helpers.cs`
- All test attributes and configurations preserved unchanged

## Prevention

- All new tests are written directly in the appropriate partial class file from creation
- Categorization rules are consulted before writing any new test
- No monolithic test classes — even small test classes follow the partial structure
- `docs/TestCategorizationRules.md` serves as the definitive reference
