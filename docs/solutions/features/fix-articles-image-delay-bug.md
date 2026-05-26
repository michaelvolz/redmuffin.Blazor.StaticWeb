---
date: 2026-04-03
title: "Fix Articles Page Image Loading Delay Bug"
tags: [image, performance, cache, bug, wasm]
problem_type: bug
---

## Problem

The Articles page took 2-10 seconds to display the first article because `PopulateImageUrlCacheAsync` made synchronous `await` calls to `ValidateImageWithCacheAsync()` for every image during initial render.

## Root Cause

A previous fix to prevent CORS-blocked images from appearing in the `src` attribute added cache-lookup calls that happened synchronously (one at a time, awaited) during the initial render path. This serialized all image validation lookups, creating a cumulative delay proportional to the number of articles.

## Solution

**Two-phase rendering:**

1. **Phase 1 (immediate):** Use cache-only lookups (`GetCachedValidationResultAsync` — no network requests) to populate `_imageUrlCache`. For cached results that are CORS-blocked, use a data URI placeholder. For uncached images, use the original `Cover` URL or best available image immediately. Call `StateHasChanged()` right after.

2. **Phase 2 (background):** Run `ValidateImagesInBackgroundAsync()` with `Task.WhenAll()` and `SemaphoreSlim` (limit 5-8 concurrent requests) to validate uncached images in parallel. Update the UI incrementally as validations complete.

**Key methods:**

- `GetCachedValidationResultAsync(string imageUrl)` — checks memory cache first, then persistent cache; returns `null` if not cached (no network request)
- `ValidateImagesInBackgroundAsync()` — identifies uncached images, runs parallel HTTP HEAD validations, updates cache + UI progressively

**Result:** Articles page renders first article within 500ms while preserving CORS protection. Background validation completes asynchronously without blocking the initial render.
