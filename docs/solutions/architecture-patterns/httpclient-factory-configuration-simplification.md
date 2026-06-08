---
date: 2025-07-27
title: "HttpClientFactory Configuration Simplification"
tags: [blazor, dotnet, infrastructure]
problem_type: architecture
---

> **Superseded (2026-06-08):** This 2025 doc is superseded by httpclient-to-ihttpclientfactory-migration.md (2026-04-03) which covers the same ground with current patterns. Retained as historical reference for the earlier iteration.

## Problem

The codebase had multiple named `HttpClient` configurations, direct `HttpClient` injection in components and services, and inconsistent usage patterns across the Blazor WebAssembly app, Azure Functions API, and test projects. Developers had to remember client names and wrestle with configuration details instead of focusing on business logic. Local/test environments used yet another pattern (`AddScoped` with a factory lambda).

## Root Cause

The project started without `IHttpClientFactory`, using direct `HttpClient` injection via `builder.Services.AddScoped(_ => new HttpClient { ... })`. Named clients were added ad-hoc as the codebase grew, creating a dual pattern: some code used `IHttpClientFactory` with named clients, other code injected `HttpClient` directly.

## Solution

**Single default `IHttpClientFactory` configuration, no named clients:**

1. **Program.cs registration** — One `AddHttpClient` call with default settings:
   - `BaseAddress = builder.HostEnvironment.BaseAddress`
   - `Timeout = TimeSpan.FromSeconds(30)`

2. **Service/component injection** — All consumers inject `IHttpClientFactory` and call `CreateClient()` without parameters. Example:

   ```csharp
   public sealed class ExampleService(IHttpClientFactory httpClientFactory, ILogger<ExampleService> logger)
   {
       public async Task<T> GetDataAsync<T>(CancellationToken ct = default)
       {
           using var httpClient = httpClientFactory.CreateClient();
           return await httpClient.GetFromJsonAsync<T>("api/data", ct).ConfigureAwait(false);
       }
   }
   ```

3. **External API override** — Consumers needing non-default configuration modify properties after creation:

   ```csharp
   using var httpClient = factory.CreateClient();
   httpClient.BaseAddress = new Uri(externalUrl);
   httpClient.Timeout = TimeSpan.FromMinutes(5);
   ```

4. **Test infrastructure** — All test projects use `IHttpClientFactory` mocks via LightMock.Generator. Test files follow the partial class pattern (`TestClassName.cs` with `[Test]` methods, `TestClassName.Helpers.cs` with TestScope and mocks), organized under `NewTests/` folders.

**Scope**: Blazor WASM app, Azure Functions API, both test projects.

## Prevention

- `IHttpClientFactory` is the default .NET pattern for HttpClient management. Start projects with a single default configuration and only introduce named clients when there is a confirmed need for multiple distinct configurations.
- The default configuration covers 90%+ of internal API call use cases. External API consumers can override properties on the returned client.
