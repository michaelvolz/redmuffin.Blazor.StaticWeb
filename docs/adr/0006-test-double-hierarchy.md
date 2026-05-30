---
date: 2026-05-30
status: accepted
---

# Hand-Rolled Test Double Hierarchy with LightMock Fallback

Test doubles follow a three-tier decision hierarchy. The goal is minimal mock
usage — mock-heavy tests are a code smell that signals insufficient pure
function extraction.

## Decision

| Priority | Strategy                               | When                                                                                                |
| -------- | -------------------------------------- | --------------------------------------------------------------------------------------------------- |
| 1        | Public static pure function extraction | Method has no side effects — pure input→output transform                                            |
| 2        | Protected virtual extract-and-override | Side effects prevent pure extraction, pattern from Feathers' _Working Effectively with Legacy Code_ |
| 3        | Interface parameter                    | Multiple implementations genuinely exist (not created solely for testing)                           |

When none of the above fit and a complex external dependency (e.g.,
`IHttpClientFactory`, `IJSRuntime`) must be controlled in tests, use
**LightMock.Generator** source-generated mocks as a pragmatic fallback.

## Considered Options

**Moq / NSubstitute as primary mocking framework.**
Rejected. Mock-heavy tests couple to implementation details (call counts,
argument matchers, setup ordering) and break during refactoring. The authors
(Martin Fowler, Kent Beck, Michael Feathers) all warn against mock-obsessed
testing. The only valid test of a mock-heavy suite is "does it fail when the
implementation breaks?" — and most mock-heavy suites do not.

**AutoFixture for test data generation.**
Rejected. Obscures the test data shape — readers cannot tell what values are
being tested without tracing through AutoFixture customization. Explicit
fixture data in each test is more readable and intentional.

**Always hand-rolled, no exceptions.**
Rejected by pragmatism. Mocking `IHttpClientFactory` or `IJSRuntime` by hand
requires implementing 5+ members just to return a canned response — wasteful
ceremony that adds zero test value. LightMock.Generator solves this with
source-gen zero-runtime overhead.

**InternalsVisibleTo for test access.**
Rejected. Testability via `InternalsVisibleTo` is a crutch that breaks the
public-API boundary. Extract pure functions to public surface, or use
extract-and-override for side effects. Never crack the assembly open for
testing.

## Consequences

- Test double files follow the `_Fake`, `_Stub`, `_Mock` naming convention
  defined in rm-naming.
- The `rm-testing` skill documents the full decision tree.
- Characterization tests (Feathers pattern) are written before any refactoring
  of existing behavior.
- A preponderance of mocks in a test file signals extraction work — pure
  functions should be pulled out of the mocked dependency.
- LightMock.Generator has zero runtime footprint (all mocking code is
  source-generated at compile time).
