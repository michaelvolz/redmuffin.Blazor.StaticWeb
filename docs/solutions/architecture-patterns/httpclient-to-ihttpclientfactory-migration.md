---
date: 2026-04-03
title: "HttpClient to IHttpClientFactory Migration"
tags: [blazor, dotnet, testing]
problem_type: architecture
---

## Problem

The codebase used direct `HttpClient` injection in service constructors (`DummyRaindropAPI(HttpClient, ...)`, `RaindropAPI(HttpClient, ...)`), component injection (`@inject HttpClient Http` in WeatherPage.razor), and a manual `AddScoped` factory lambda registration in Program.cs. This pattern:

- Lacked automatic disposal and connection pooling
- Made test mocking more complex (direct `HttpClient` creation vs. factory mocking)
- Prevented centralized HttpClient configuration

## Root Cause

The project started without `IHttpClientFactory` awareness. `HttpClient` was injected directly as a scoped service via a factory lambda — a common pattern before `IHttpClientFactory` became the standard recommendation. Named clients were later added alongside, creating a split pattern across the codebase.

## Solution

**Full migration from direct `HttpClient` to `IHttpClientFactory`:**

**Phase 1 — Service constructors:**

```csharp
// Before
public RaindropAPI(HttpClient httpClient, ILogger<RaindropAPI> logger)

// After
public RaindropAPI(IHttpClientFactory httpClientFactory, ILogger<RaindropAPI> logger)
```

**Phase 2 — Component injection:**

```razor
@* WeatherPage.razor - Before *@
@inject HttpClient Http

@* WeatherPage.razor - After *@
@inject IHttpClientFactory HttpClientFactory
```

```csharp
// WeatherPage.razor.cs
[Inject] private IHttpClientFactory HttpClientFactory { get; set; } = default!;
// Usage:
var httpClient = HttpClientFactory.CreateClient();
```

**Phase 3 — Program.cs cleanup:**

- Removed: `builder.Services.AddScoped(_ => new HttpClient { BaseAddress = ... })`
- Kept: existing `IHttpClientFactory` registrations (named and default)

**Phase 4 — Test infrastructure:**

- Updated `TestScope` to register `IHttpClientFactory` instead of `HttpClient`
- Migrated test doubles to LightMock.Generator `IHttpClientFactory` mocks
- Services already using `IHttpClientFactory` correctly (`ImageValidationService`, `OpenGraphImagesService`) were left unchanged

**Phase 5 — Documentation:** Updated PRD code examples and service registration documentation to reflect the new pattern.

## Prevention

- `IHttpClientFactory` is the Microsoft-recommended pattern for .NET applications. All new services should inject `IHttpClientFactory`, never `HttpClient` directly.
- Register HttpClient configurations via `AddHttpClient<T>()` or `AddHttpClient("name")` in Program.cs.
- Test infrastructure should standardize on `IHttpClientFactory` mocks from the start.
- Conduct a codebase audit whenever a new dependency injection pattern is adopted to catch legacy usages.
