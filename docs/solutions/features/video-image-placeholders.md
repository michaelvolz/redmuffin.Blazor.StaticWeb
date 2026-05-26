---
date: 2025-07-26
title: "Video Image Placeholders with Shared Services Architecture"
tags: [image, video, placeholder, architecture, refactor, wasm]
problem_type: feature
---

## Problem

The Videos page lacked image placeholder functionality entirely — if a video cover image was missing or failed to load, the card appeared blank. Meanwhile, the Articles page had its own image placeholder logic, creating duplicated code between the two pages.

## Root Cause

Articles page had image placeholder logic (fallback SVGs, shimmer effects, failure reason text) embedded directly in `Articles.razor.cs` with no reusable abstraction. Videos page simply never had this logic added.

## Solution

**Extract shared image placeholder services** into `Core/ImagePlaceholder/`, then consume them from both Articles and Videos pages:

```
Core/ImagePlaceholder/
  Abstractions/
    IImagePlaceholderService.cs
    IImageValidationCacheService.cs
  Models/
    ImageValidationResult.cs
    PlaceholderConfiguration.cs
  Services/
    ImagePlaceholderService.cs + .Logging.cs
    ImageValidationCacheService.cs + .Logging.cs
    PlaceholderGenerationService.cs + .Logging.cs
  Templates/
    SvgPlaceholderTemplate.cs
```

**Key methods extracted from `Articles.razor.cs` into shared services:**

- `GetDefaultPlaceholder()` → `IImagePlaceholderService`
- `GenerateSimplePlaceholder(reason)` → `PlaceholderGenerationService`
- `GetImageUrl(item, cache)` → `IImagePlaceholderService`
- `HandleImageLoadAsync()` → `IImagePlaceholderService`
- `HasFallbackPlaceholder()` / `GetFallbackReason()` → `IImagePlaceholderService`

**Videos page integration:** Added `IImagePlaceholderService` and `ISimpleImageValidationService` injection, an `_imageUrlCache` dictionary, and wrapper methods that delegate to services. Template uses `@onload` / `@onerror` handlers with shimmer effects, identical to Articles page behavior.

**Failure reasons displayed in placeholders:** "CORS blocked", "Image not found", "Network error", "Invalid format", "Image not available".

## Prevention

Any new page displaying images should consume `IImagePlaceholderService` rather than implementing its own placeholder logic. The shared service eliminates code duplication and ensures consistent visual behavior across all content pages.
