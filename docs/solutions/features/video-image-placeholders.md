---
date: 2025-07-26
title: "Video Image Placeholders with Shared Services Architecture"
tags: [image, video, placeholder, architecture, refactor, wasm]
problem_type: feature
---

> **Current (2026-10-09):** The validation half is gone: IImageValidator,
> IImageValidationCacheService, and ImageValidationResult were removed, and
> covers resolve through
> [Render-and-observe image URL resolution](../architecture-patterns/render-and-observe-image-url-resolution.md)
> — the resolver fills the URL cache with no network access, and the `<img>`
> load/error events decide renderability. What survives:
> IImagePlaceholderService
> (src/redmuffin.Blazor.StaticWeb.Common/ImagePlaceholder/),
> PlaceholderGenerationService and SvgPlaceholderTemplate
> (src/redmuffin.Blazor.StaticWeb/Core/ImagePlaceholder/). The body below
> records the 2025 extraction.

## Problem

The Videos page lacked image placeholder functionality entirely — if a video cover image was missing or failed to load, the card appeared blank. Meanwhile, the Articles page had its own image placeholder logic, creating duplicated code between the two pages.

## Root Cause

Articles page had image placeholder logic (fallback SVGs, shimmer effects, failure reason text) embedded directly in `Articles.razor.cs` with no reusable abstraction. Videos page simply never had this logic added.

## Solution

**Extract shared image placeholder services** into `src/redmuffin.Blazor.StaticWeb/Core/ImagePlaceholder/`, then consume them from both Articles and Videos pages:

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

> **Current (2026-10-09):** After the validation retirement, the only reason
> inputs are `NO_IMAGE` (no cover) and `LOAD_FAILED` (the `<img>` error
> event), and both map to "Image not available". The four probe-era strings
> above remain in `SvgPlaceholderTemplate.MapFailureReasonToDisplayText` as
> unreachable branches. See
> [Render-and-observe image URL resolution](../architecture-patterns/render-and-observe-image-url-resolution.md).

## Prevention

Any new page displaying images should consume `IImagePlaceholderService` rather than implementing its own placeholder logic. The shared service eliminates code duplication and ensures consistent visual behavior across all content pages.
