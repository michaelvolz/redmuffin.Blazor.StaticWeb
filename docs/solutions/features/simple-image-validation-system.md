---
date: 2026-04-03
title: "Simple Image Validation System with localStorage Caching"
tags: [image, validation, cache, performance, refactor, wasm]
problem_type: feature
---

> **Current (2026-06-08):** The simplification was planned but never implemented.
> The over-engineered system described below still exists in the codebase.
> This doc is preserved as architectural intent — the WHY behind the planned
> refactor remains valid and may be executed in a future cleanup pass.

## Problem

The Articles component image validation system had become over-engineered: multiple validation phases, complex state dictionaries, progressive enhancement logic, elaborate background validation orchestration, and redundant caching layers. This made the code hard to understand and maintain while providing no measurable benefit over a simpler approach.

## Root Cause

Incremental feature additions (CORS handling, OpenGraph fallback, progressive loading, cache layers) were bolted onto the component without refactoring the overall architecture, resulting in an accretion of complexity.

## Solution

**Single-service simplification** — replace the multi-layered system with `ISimpleImageValidationService`:

- **One HTTP HEAD request per image URL**, cached permanently in localStorage
- **Cache-first strategy**: check `img_validation_{sha256_hash}` before any network request
- **Automatic cache eviction**: when storage hits 75% quota, drop oldest entries to 50%
- **Fire-and-forget background validation** for uncached images — no complex concurrency management
- **Single SVG placeholder** with dynamic failure reason text overlay (same reasons as image placeholder system)
- **Simple `Dictionary<string, string> _imageUrlCache`** replacing complex state dictionaries

**Service API:**

```csharp
interface ISimpleImageValidationService
{
    Task<ImageValidationResult> ValidateImageAsync(string imageUrl);
    Task<ImageValidationResult?> GetCachedResultAsync(string imageUrl);
    Task<string> GetImageUrlOrPlaceholderAsync(string imageUrl);
}
```

**Organization:** Services live under `Features/Pages/ArticlesPage/Core/` (feature-local, not global `Services/` folder) — `Core/Services/SimpleImageValidationService.cs`, `Core/Models/ImageValidationResult.cs`, `Core/Templates/PlaceholderTemplate.cs`.

**Target outcomes:** 60%+ code reduction, initial page load under 500ms maintained, consistent behavior across page visits, understandable by new developers in under 30 minutes.

## Prevention

When image handling complexity grows, refactor into a single-purpose service rather than adding another validation layer or state dictionary to the component. Feature-local service placement (`Features/<Page>/Core/`) keeps related code together and avoids polluting the global `Services/` namespace.
