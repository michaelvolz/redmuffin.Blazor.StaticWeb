---
title: fix: Replace placeholder tests with real coverage
type: fix
status: active
date: 2026-04-06
---

# fix: Replace placeholder tests with real coverage

## Overview

Two test classes still contain dummy assertions (`Assert.That(true)`), which creates analyzer noise and leaves real behavior uncovered. This plan replaces those placeholders with meaningful TUnit coverage for the underlying production types.

## Problem Frame

The repository has working patterns for real, behavior-focused tests, but `tests/redmuffin.Blazor.StaticWeb.Tests/Services/CacheMonitoringServiceTests.cs` and `tests/redmuffin.Blazor.StaticWeb.Tests/Configuration/PageLoadSpeedConfigTests.cs` are still placeholder shells. They should verify actual service behavior and configuration logic instead of passing trivially.

## Requirements Trace

- R1. Replace placeholder assertions with tests that exercise real production behavior.
- R2. Keep tests aligned with existing TUnit conventions and repo test structure.
- R3. Eliminate the current `TUnitAssertions0005` warnings caused by constant assertions.
- R4. Preserve test isolation, especially for static configuration state.

## Scope Boundaries

- No production code changes unless a test exposes a genuine defect.
- No broad test suite rewrite; only the dummy tests and any small supporting helpers needed for isolation.
- No change to the public behavior of `CacheMonitoringService` or `PageLoadSpeedConfig`.

## Context & Research

### Relevant Code and Patterns

- `tests/redmuffin.Blazor.StaticWeb.Tests/Core/ImagePlaceholderServiceTests.cs`
- `tests/redmuffin.Blazor.StaticWeb.Tests/Core/ImageValidationCacheServiceTests.cs`
- `tests/redmuffin.Blazor.StaticWeb.Tests/Core/ImageValidationCacheServiceTests.Helpers.cs`
- `tests/redmuffin.Blazor.StaticWeb.Tests/Core/ImageValidationCacheServiceTests.EdgeCases.cs`
- `src/redmuffin.Blazor.StaticWeb/Services/CacheMonitoringService.cs`
- `src/redmuffin.Blazor.StaticWeb/Configuration/PageLoadSpeedConfig.cs`

### Institutional Learnings

- `docs/solutions/best-practices/csharp-standards-final-2026-04-06.md`: use TUnit assertions, keep tests concrete, and avoid placeholder/dummy strategies as a substitute for coverage.
- `docs/solutions/best-practices/csharp-standards-consolidation-resolution.md`: prefer explicit assertions over vague test bodies.
- `docs/solutions/best-practices/test-double-disposable-pattern-2026-04-06.md`: disposable helpers/doubles should be disposed explicitly when used.

### External References

- None required; local repository patterns are sufficient for this narrow test cleanup.

## Key Technical Decisions

- Replace each placeholder with behavior-driven tests for the real production type it names.
- Use the existing TUnit style already present in the repo, including clear categories and direct assertions.
- Keep any shared setup minimal; add helpers only if needed to isolate static state or reduce duplication.
- Treat `PageLoadSpeedConfig` as stateful test surface and restore defaults between cases.

## Open Questions

### Resolved During Planning

- Should the dummy tests remain as placeholders? No; they should become real assertions against production behavior.
- Is external documentation needed? No; local code and test patterns are enough.

### Deferred to Implementation

- Whether the `CacheMonitoringService` tests need a helper partial file depends on the final arrangement of mocks and test data.
- Whether `PageLoadSpeedConfig` needs a dedicated helper partial depends on how much static-state reset logic is shared across test cases.

## Implementation Units

- [ ] **Unit 1: Replace CacheMonitoringService placeholder with real behavior tests**

**Goal:** Turn the dummy cache monitoring test into coverage for actual service outcomes.

**Requirements:** R1, R2, R3

**Dependencies:** None

**Files:**

- Modify: `tests/redmuffin.Blazor.StaticWeb.Tests/Services/CacheMonitoringServiceTests.cs`
- Test: `tests/redmuffin.Blazor.StaticWeb.Tests/Services/CacheMonitoringServiceTests.cs`
- Test: `src/redmuffin.Blazor.StaticWeb/Services/CacheMonitoringService.cs`

**Approach:**

- Replace the placeholder assertion with tests that validate real cache-monitoring results from controlled storage inputs.
- Focus on the public service API and verify returned stats, health, optimization, or recommendation values rather than implementation details.
- Use stable test doubles for `IBrowserStorageService` and logger dependencies.

**Execution note:** Implement test-first coverage for the public service surface before considering any production-code adjustment.

**Patterns to follow:**

- `tests/redmuffin.Blazor.StaticWeb.Tests/Core/ImageValidationCacheServiceTests.cs`
- `tests/redmuffin.Blazor.StaticWeb.Tests/Core/ImageValidationCacheServiceTests.Helpers.cs`
- `tests/redmuffin.Blazor.StaticWeb.Tests/Core/ImageValidationCacheServiceTests.EdgeCases.cs`

**Test scenarios:**

- Happy path: when browser storage returns healthy stats, `GetComprehensiveCacheStatsAsync` maps them into a non-empty monitoring snapshot with the expected totals.
- Happy path: `GetCacheHealthMetricsAsync` reports a healthy or warning status based on storage utilization and expired-item ratios derived from the mocked stats.
- Happy path: `GetCacheRecommendationsAsync` produces sensible recommendations when storage pressure or fragmentation crosses the documented thresholds.
- Edge case: zero-item storage returns bounded metrics without divide-by-zero or negative counts.
- Error path: if the browser storage service throws, the service returns the documented fallback result instead of propagating the exception.
- Integration: optimization flow reflects mocked storage-before/storage-after transitions and records the expected action outcome.

**Verification:**

- The cache monitoring test class no longer contains a constant assertion.
- The tests assert on real service outputs and cover both nominal and failure behavior.
- The file builds cleanly without `TUnitAssertions0005` warnings.

- [ ] **Unit 2: Replace PageLoadSpeedConfig placeholder with real configuration tests**

**Goal:** Turn the dummy config test into coverage for the component-display rules and static configuration toggles.

**Requirements:** R1, R2, R3, R4

**Dependencies:** None

**Files:**

- Modify: `tests/redmuffin.Blazor.StaticWeb.Tests/Configuration/PageLoadSpeedConfigTests.cs`
- Test: `tests/redmuffin.Blazor.StaticWeb.Tests/Configuration/PageLoadSpeedConfigTests.cs`
- Test: `src/redmuffin.Blazor.StaticWeb/Configuration/PageLoadSpeedConfig.cs`

**Approach:**

- Replace the placeholder body with explicit tests over `ShouldDisplayComponent` and the static toggle properties.
- Cover the main routing logic for localhost, private-network hosts, and production-like hosts.
- Restore static defaults between tests so the class remains deterministic under repeated execution.

**Execution note:** Add characterization-style coverage around the current static behavior before any code cleanup.

**Patterns to follow:**

- `tests/redmuffin.Blazor.StaticWeb.Tests/Core/ImagePlaceholderServiceTests.cs`
- `tests/redmuffin.Blazor.StaticWeb.Tests/Core/ImagePlaceholderServiceTests.Helpers.cs`

**Test scenarios:**

- Happy path: when enabled and localhost is allowed, `ShouldDisplayComponent` returns true for a localhost URI.
- Happy path: when enabled and localhost is disallowed, `ShouldDisplayComponent` returns false for localhost and true for a production host.
- Edge case: private-network hosts (`127.0.0.1`, `192.168.x.x`, `10.x.x.x`, `172.16.x.x`) are treated as local and excluded when localhost display is disabled.
- Edge case: toggling `IsEnabled` to false suppresses display regardless of host.
- Error path: invalid `baseUri` input is documented by a test that captures the current exception behavior.
- Integration: repeated test execution does not leak static configuration values across cases.

**Verification:**

- The configuration test class contains real assertions against `ShouldDisplayComponent` and static flags.
- Static state is restored between tests, so outcomes are stable and order-independent.
- The file builds cleanly without `TUnitAssertions0005` warnings.

## System-Wide Impact

- **Interaction graph:** Limited to the two test classes and their direct production types.
- **Error propagation:** No production error handling changes; tests only document current behavior.
- **State lifecycle risks:** `PageLoadSpeedConfig` is static and mutable, so test isolation matters.
- **API surface parity:** None; this does not change public APIs.
- **Integration coverage:** The updated tests should exercise real service/config outcomes instead of dummy pass-throughs.
- **Unchanged invariants:** Production behavior, namespaces, and project structure remain as-is.

## Risks & Dependencies

| Risk                                                                      | Mitigation                                                                                    |
| ------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------- |
| Static configuration state leaks between tests                            | Reset `PageLoadSpeedConfig` values within each test or via a tiny shared helper.              |
| Tests become too implementation-coupled                                   | Assert on returned results and observable behavior, not internals.                            |
| Cache monitoring assertions become brittle if they encode exact estimates | Anchor checks to documented thresholds and result shape rather than incidental numeric noise. |

## Documentation / Operational Notes

- No user-facing documentation changes are expected.
- The main operational goal is warning-free test compilation.

## Sources & References

- Related code: `src/redmuffin.Blazor.StaticWeb/Services/CacheMonitoringService.cs`
- Related code: `src/redmuffin.Blazor.StaticWeb/Configuration/PageLoadSpeedConfig.cs`
- Related tests: `tests/redmuffin.Blazor.StaticWeb.Tests/Services/CacheMonitoringServiceTests.cs`
- Related tests: `tests/redmuffin.Blazor.StaticWeb.Tests/Configuration/PageLoadSpeedConfigTests.cs`
- Related tests: `tests/redmuffin.Blazor.StaticWeb.Tests/Core/ImagePlaceholderServiceTests.cs`
- Related tests: `tests/redmuffin.Blazor.StaticWeb.Tests/Core/ImageValidationCacheServiceTests.cs`
