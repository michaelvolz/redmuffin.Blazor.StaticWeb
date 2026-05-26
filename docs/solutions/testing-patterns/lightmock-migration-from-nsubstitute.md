---
date: 2026-04-03
title: "LightMock Migration from NSubstitute"
tags: [testing, tunit, lightmock, nsubstitute, mocking, migration]
problem_type: testing
---

## Problem

The test suite used NSubstitute for mocking, but the project had standardized on LightMock.Generator for its compile-time generation, zero runtime overhead, and AOT compatibility. NSubstitute instances were scattered across multiple test files with varying levels of usage, including skipped tests that had never been migrated. Two frameworks coexisted, creating confusion and inconsistency.

## Root Cause

LightMock.Generator was adopted after many tests were already written with NSubstitute. The migration was planned as incremental to minimize risk, but a critical blocker emerged: LightMock.Generator could not handle interfaces with optional parameters in expression trees, producing CS0854 compilation errors. This blocked migration of cache-dependent services and other tests using interfaces like `IBrowserStorageService`.

## Solution

Incremental migration in five phases, prioritized by NSubstitute usage count (least usage first):

**Migration order (prioritized list):**

1. ImageValidationServiceTests (3 NSubstitute matches)
2. ArticlesPagePerformanceTests (3 matches)
3. ArticlesImageDelayBugFixTests (3 matches)
4. SimpleImageValidationServiceTests (3 matches)

### CS0854 Breakthrough — Explicit Parameter Specification

The critical blocker was solved by explicitly specifying ALL parameters in LightMock `Arrange()`/`Assert()` calls, including optional ones:

```csharp
// ❌ FAILS: CS0854 — expression trees can't handle optional parameters
_mock.Arrange(f => f.GetItemAsync<T>("namespace", "key"))

// ✅ WORKS: Specify all parameters explicitly
_mock.Arrange(f => f.GetItemAsync<T>("namespace", "key", CancellationToken.None))
```

**Pattern for any interface with optional parameters:**

- `CancellationToken cancellationToken = default` → add explicit `CancellationToken.None`
- `int? expiration = null` → add explicit `The<int?>.IsAnyValue` or `null`
- Any other optional → add explicit `default` or appropriate sentinel value

### File Naming Convention

- Migrated tests get `LightMock` suffix: `[ServiceName]TestsLightMock.cs`
- Original files renamed to `[ServiceName]Tests.cs.backup` once migration is verified
- Migration is done in the new LightMock file, never in-place on the original

### Key Migration Rules

- Remove `[Skip]` attributes — fix underlying issues to make tests work
- No shortcuts or hacks — each test must pass with real behavior assertions
- Use `[Before(Test)]` and `[After(Test)]` for lifecycle hooks
- Use `[Arguments]` for data-driven tests
- Dispose resources via `IDisposable` or `[After(Test)]`

### Results

- All targeted tests migrated and passing
- Services already using LightMock correctly were left unchanged

## Prevention

- LightMock.Generator is the exclusive mocking framework for external dependencies
- Custom mocks for internal dependencies (never LightMock)
- When writing LightMock Arrangements, always specify ALL parameters explicitly — never rely on optional parameter defaults
- New interfaces avoid optional parameters where possible to prevent CS0854 issues
- No new NSubstitute dependencies ever introduced
