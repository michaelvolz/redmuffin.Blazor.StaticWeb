---
date: 2025-08-01
title: "LocalStorage Caching for Azure Function Calls in Blazor WASM"
tags: [blazor, azure-functions, caching, performance]
problem_type: architecture
---

## Problem

The `Videos.razor` and `Articles.razor` pages made fresh Azure Function API calls on every page load, causing slow time-to-first-content. There was no caching layer between the Blazor components and the `IRaindropAPI` service. The `RaindropItem` model carried many fields not used in the UI, inflating the data payload unnecessarily.

## Root Cause

No caching infrastructure existed. `IRaindropAPI` fetched fresh data from Azure Functions on every request. The page components called the API directly in `OnInitializedAsync` with no caching or background refresh strategy.

## Solution

**Three-layer caching architecture:**

1. **Cache Service** (`IRaindropItemsCache` / `RaindropItemsCache`) — Wraps Blazored.LocalStorage with:
   - 4-week TTL with versioned cache keys (`raindrop_cache_videos_v1`, `raindrop_cache_articles_v1`)
   - LZString.NET compression for storage efficiency
   - Cache metadata tracking (timestamps, version info)
   - Graceful fallback to direct API call on LocalStorage failure, quota exceeded, or corruption

2. **Pruned Data Model** (`PrunedRaindropItem`) — Contains only fields rendered in the UI: `Id`, `Link`, `Title`, `Excerpt`, `Cover`, `Created`, `Type`. Extension methods convert between full `RaindropItem` and pruned model.

3. **Page Integration Pattern** — On `OnInitializedAsync`:
   - Display cached data immediately (instant render)
   - Fire background fetch from API
   - Compare timestamps; if API data is newer, update cache and show a refresh badge
   - User can click badge to swap displayed data to fresh version with smooth transition

**Refresh badge** — Simple reusable `RefreshBadge.razor` component (not a complex helper class). States: hidden, visible, loading, error. Uses Foundation styling with SCSS partial under `Features/Common/Components/`.

**Key design decisions:**

- LZString.NET chosen over System.IO.Compression for smaller WASM download size
- Cache is page-specific (no cross-page sharing)
- No background sync — refresh only happens on page load
- Existing Azure Functions unchanged — cache sits in front of `IRaindropAPI`

## Prevention

- For any data that changes infrequently (hours/days), implement a caching layer from the start. Blazored.LocalStorage + compression provides a pragmatic cache for Blazor WASM without server-side dependencies.
- Prune data models to UI-relevant fields before caching to minimize LocalStorage footprint.
- Keep refresh mechanisms simple — a badge with basic state management is sufficient; avoid over-engineering helper classes.
