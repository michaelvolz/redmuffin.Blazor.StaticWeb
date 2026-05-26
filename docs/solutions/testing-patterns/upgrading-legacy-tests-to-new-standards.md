---
date: 2025-07-28
title: "Upgrading Legacy Tests to New Standards"
tags: [testing, tunit, testscope, partial-classes, mocking, blazor]
problem_type: testing
---

## Problem

The project had 17 legacy test files outside `NewTests` folders that used inconsistent patterns — mixed ConfigureAwait(false) usage, no TestScope pattern, limited dependency injection and mocking, mixed error handling approaches, and no partial class organization. These tests violated project standards for TUnit, C# 13 patterns, zero build warnings, and behavior-focused testing. Every legacy test needed systematic upgrade to the modern TestScope + partial class architecture.

## Root Cause

Testing standards evolved over time (TUnit adoption, C# 13 primary constructors, TestScope pattern, LightMock.Generator migration) but existing tests were never retrofitted. The divergence was exacerbated by rapid toolchain changes including the NSubstitute → LightMock migration and the shift to NewTests folder conventions.

## Solution

Systematic four-phase migration of all 17 legacy test files:

**Phase 1 — Core Infrastructure Tests:** `StringExtensionsTests.cs` and `BlazorCodeBehindEnforcementTests.cs` migrated first as simplest cases. StringExtensions was pure functions (no TestScope needed). CodeBehind tests received full TestScope + partial class treatment.

**Phase 2 — API Function Tests:** `TestDeserialization.cs`, `ArticlesApiVerification_Tests.cs`, `RaindropListArticles_Tests.cs`, `RaindropListVideos_Tests.cs` migrated with TestScope patterns, MockFunctionContext, MockHttpRequestData, and MockHttpResponseData.

**Phase 3 — Helper Infrastructure:** Standalone helper classes (`TestFunctionContext`, `TestHttpRequestData`, `TestHttpResponseData`, `TestFunctionDefinition`, `TestBindingContext`, `TestTraceContext`) integrated into TestScope partial classes with `Mock` naming convention (e.g., `MockFunctionContext`). All standalone helpers marked as `.outdated`.

**Phase 4 — Validation and Cleanup:** Original legacy files renamed with `.outdated` suffix, duplicates removed, full test suite verified (175 tests passing, zero build warnings).

### Key Standards Applied

- **TestScope Pattern:** Sealed class with `IDisposable`, C# 13 primary constructors, fluent builder methods (`WithStandardServices()`, `WithFailingHttpClient()`), automatic resource disposal
- **Partial Class Organization:** `[TestClass].cs` for `[Test]` methods only, `[TestClass].Helpers.cs` for TestScope, mocks, and utilities
- **Mocking Strategy:** Custom mocks for internal dependencies (`NavigationManagerMock`, internal services), LightMock.Generator ONLY for external dependencies (`IHttpClientFactory`, `ILogger<T>`, external APIs). Mock naming uses `Mock` suffix.
- **TUnit Fluent Chaining:** Chained assertions on same object (`.IsNotNull().And.Contains(...)`), `Assert.Multiple()` for unrelated concerns
- **ConfigureAwait(false):** All async calls except at end of Assert statements
- **AAA Structure:** Clear Arrange-Act-Assert comments, single responsibility per test

## Prevention

- All new test code goes in `NewTests/` folders only — never touch legacy locations
- All test classes follow partial class organization from day one
- TestScope pattern is mandatory for all test classes requiring DI or complex setup
- LightMock.Generator reserved for external/3rd-party dependencies only; custom mocks for everything else
- Zero build warnings policy enforced after every C# file change
