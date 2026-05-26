---
date: 2025-07-26
title: "Dummy RaindropIO Data for Local Development"
tags: [raindrop, api, development, dummy-data, di, wasm]
problem_type: feature
---

## Problem

Running the Blazor WASM app requires both the frontend (`localhost:5233`) and Azure Functions API simultaneously to see any content. This adds ~20 seconds to startup time and creates unnecessary friction for local design and UI testing.

## Root Cause

Videos and Articles pages called Azure Functions directly via `HttpClient`, with no abstraction layer that could substitute dummy data during local development.

## Solution

**Environment-detecting service abstraction** via `IRaindropAPI` + `IRaindropAPIFactory`:

- `localhost:5233` (standalone WASM) → `DummyRaindropAPI` loads JSON from `wwwroot/mockdata/`
- `localhost:4280` (SWA proxy) → `RaindropAPI` calls real Azure Functions
- Factory pattern in `Program.cs` selects implementation based on `NavigationManager.BaseUri`

**Key implementation decisions:**

- Dummy data stored as JSON files under `wwwroot/mockdata/videos.json` and `articles.json`
- HTTP-based loading via `IHttpClientFactory` (no named clients — uses default client with base address)
- `RaindropJsonSerializerContext` enhanced with `LenientOptions`, `StrictOptions`, `DefaultOptions` for robust parsing
- LoggerMessage delegates in `.Logging.cs` partial files for Wasm performance

**Resolved implementation issues (from implementation phase):**

- JSON serialization: must use `JsonSerializer.Deserialize<T>(content, options)` not `options.GetTypeInfo(typeof(T))` — the latter fails with `NotSupportedException` under source-generated contexts
- Test serialization consistency: both test helpers and API implementations must use `RaindropJsonSerializerContext.DefaultOptions` (not `Default.RaindropItemList` for serialize and `DefaultOptions` for deserialize)
- Error handling: `DummyRaindropAPI` returns empty collections for missing files (404, dev-friendly); `RaindropAPI` lets `HttpRequestException` pass through for test assertions; both throw `InvalidOperationException` on complete deserialization failure

**File structure (feature-oriented):**

```
Features/Raindrop/Services/
  IRaindropAPI.cs
  IRaindropAPIFactory.cs
  RaindropAPI.cs + RaindropAPI.Logging.cs
  RaindropAPIFactory.cs + RaindropAPIFactory.Logging.cs
  DummyRaindropAPI.cs + DummyRaindropAPI.Logging.cs
```

## Prevention

Any new API integration should follow the same factory pattern — create an interface, implement real + dummy variants, and register via factory that inspects environment. This prevents tight coupling between components and the Azure Functions backend.
