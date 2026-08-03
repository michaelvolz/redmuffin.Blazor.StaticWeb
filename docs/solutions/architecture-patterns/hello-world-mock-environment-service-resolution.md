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

> **Current (2026-08-03):** ApiHealth owns connectivity/health via Mediator +
> Strategy + `Result<T>`. `IRaindropAPI.GetHelloWorldAsync` was removed.
> Raindrop IO lives in `Modules/Raindrop*` with `AddRaindropModule(bool)`
> Strategy (factory deleted). New modules follow
> `docs/modular-monolith-module-guide-2026-08-03.md`, not this historical
> Hello World factory example.

# Hello World Mock Example — historical environment-based resolution

## Problem

The project used a factory pattern (`RaindropAPIFactory`) to resolve between a mock service (`DummyRaindropAPI`) and a real service (`RaindropAPI`) based on the host environment (localhost:5233 = mock, localhost:4280 = real). No simple, self-contained example existed to demonstrate that pattern to developers.

## Root Cause

The existing service methods (`GetVideosAsync`, `GetArticlesAsync`) were full-featured implementations. A minimal "Hello World" example was added on `IRaindropAPI` so developers could see mock/real resolution without new infrastructure.

## Historical solution (removed 2026-08-03)

`IRaindropAPI` once exposed:

```csharp
Task<string> GetHelloWorldAsync(CancellationToken cancellationToken = default);
```

Two implementations existed:

- **`DummyRaindropAPI`**: Hardcoded string via `Task.FromResult()`, with a LoggerMessage delegate for mock response logging.
- **`RaindropAPI`**: Called `/api/HelloWorld` via HttpClient, with success and error LoggerMessage delegates.

`CallApiExample` injected `IRaindropAPI` and called `GetHelloWorldAsync()`. That method and its dedicated tests are gone; connectivity lives in the ApiHealth module.

### Testing conventions established (still current)

That work codified testing conventions that remain project standards:

- **Test behavior, not implementation** — public contracts and input/output, never internal async details.
- **Custom mocks for internal services, LightMock.Generator for 3rd party dependencies** (e.g., HttpClient).
- **TestScope pattern** with fluent configuration methods (`WithStandardServices()`, etc.).
- **`ConfigureAwait(false)` on service calls only** — never on Assert statements.
- **Custom mock naming**: `Mock` suffix (e.g., `HttpHandlerMock`, `HttpResultMock`).
- **TUnit fluent chaining** for related assertions: `await Assert.That(result).IsNotNull().And.Contains("expected")`.
- **`Assert.Multiple()`** for unrelated concerns (DOM structure vs logging).
- **Test naming convention**: behavior-describing names (e.g. `Should_Return_…_When_…`).

## Prevention

Do not re-add Hello connectivity on Raindrop.

1. New bounded features use the ApiHealth triad pattern and
   `docs/modular-monolith-module-guide-2026-08-03.md`.
2. Raindrop keeps factory resolution only for articles/videos until that feature
   is extracted as a module.
3. Keep the testing conventions above for service and page tests.
