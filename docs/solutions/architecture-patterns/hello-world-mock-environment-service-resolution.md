---
module: Services
date: 2025-07-27
problem_type: architecture_pattern
component: service_abstraction
severity: low
symptoms:
  - Need a clear, simple example of the environment-based service resolution pattern
  - Developers unfamiliar with the IRaindropAPI / DummyRaindropAPI / RaindropAPI architecture
  - No working example to demonstrate mock vs real API calls in Blazor WASM
root_cause: missing_documentation
resolution_type: implementation_example
tags:
  - blazor-wasm
  - dependency-injection
  - service-abstraction
  - mocking
  - factory-pattern
  - environment-detection
  - tunit
  - testing-patterns
---

# Hello World Mock Example &mdash; Environment-Based Service Resolution Pattern

## Problem

The project uses a factory pattern (`RaindropAPIFactory`) to resolve between a mock service (`DummyRaindropAPI`) and a real service (`RaindropAPI`) based on the host environment (localhost:5233 = mock, localhost:4280 = real). No simple, self-contained example existed to demonstrate this pattern to developers.

## Root Cause

The existing service methods (`GetVideosAsync`, `GetArticlesAsync`) were full-featured implementations. A minimal "Hello World" example was needed &mdash; one that extends the existing interface without creating new infrastructure, so developers could see the mock/real resolution behavior instantly.

## Solution

Extended the existing `IRaindropAPI` interface with a single method:

```csharp
Task<string> GetHelloWorldAsync(CancellationToken cancellationToken = default);
```

Two implementations:

- **`DummyRaindropAPI`**: Returns a hardcoded string via `Task.FromResult()`, with a LoggerMessage delegate for mock response logging.
- **`RaindropAPI`**: Calls the existing `/api/HelloWorld` Azure Function endpoint using the standard HttpClient pattern, with success and error LoggerMessage delegates.

The `CallApiExample` component was updated to inject `IRaindropAPI` and call `GetHelloWorldAsync()` instead of using a direct HttpClient. No new infrastructure, no new Azure Functions, no new interfaces &mdash; just a method addition to the existing abstraction.

### Testing Conventions Established

This PRD's tasklist codified several testing conventions that became project standards:

- **Test behavior, not implementation** &mdash; focus on public contracts and input/output, never internal async details.
- **Custom mocks for internal services, LightMock.Generator for 3rd party dependencies** (e.g., HttpClient).
- **TestScope pattern** with fluent configuration methods (`WithStandardServices()`, etc.).
- **`ConfigureAwait(false)` on service calls only** &mdash; never on Assert statements.
- **Custom mock naming**: `Mock` suffix (e.g., `HttpHandlerMock`, `HttpResultMock`).
- **TUnit fluent chaining** for related assertions: `await Assert.That(result).IsNotNull().And.Contains("expected")`.
- **`Assert.Multiple()`** for unrelated concerns (DOM structure vs logging).
- **Test naming convention**: `Should_Return_HelloWorld_Response_When_Called` (describes expected behavior).

## Prevention

The Hello World method serves as a living reference implementation. When adding new service methods:

1. Add the method signature to `IRaindropAPI` (use default interface implementation for zero-breakage).
2. Implement in both `DummyRaindropAPI` (hardcoded, synchronous) and `RaindropAPI` (HttpClient, async).
3. Add LoggerMessage delegates for both implementations.
4. Write behavior-focused tests following the conventions above.
5. The existing `RaindropAPIFactory` handles environment resolution automatically &mdash; no Program.cs changes needed.
