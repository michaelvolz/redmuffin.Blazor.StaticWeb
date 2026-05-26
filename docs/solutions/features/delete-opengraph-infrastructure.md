---
date: 2026-04-03
title: "Removing Unused OpenGraph Infrastructure"
tags: [opengraph, cleanup, architecture, maintenance, dead-code]
problem_type: cleanup
---

## Problem

The OpenGraph image fallback infrastructure was fully implemented but completely unused. The Articles page only used the `Cover` property from Raindrop.io data — there was no integration with OpenGraph services in the UI. This added unnecessary complexity, maintenance burden, and bundle size.

## Root Cause

OpenGraph was implemented as a feature but never integrated into the Articles page UI. The full stack (services, models, Azure Functions, tests, cache monitoring) remained in the codebase as dead code.

## Solution

Systematic removal in six phases:

### Deleted Files

**Core services:**

- `Services/IOpenGraphImagesService.cs`
- `Services/OpenGraphImagesService.cs`
- `Features/Pages/ArticlesPage/Models/OpenGraphProcessingState.cs`

**Azure Function:**

- `Functions/GetOpenGraphImages.cs`

**Shared models (7 files):**

- `CachedImageData.cs`, `ArticleImageRequest.cs`, `ArticleImageResponse.cs`, `BatchImageRequest.cs`, `BatchImageResponse.cs`, `BatchImageResult.cs`, `ImageSource.cs`

**Test files (8 files):**

- Service tests, function tests, integration tests, performance tests, and test infrastructure

### Modified Files

- `Program.cs` — removed OpenGraph service registration
- `CacheMonitoringService.cs` — removed `_openGraphImagesService` field and related stats
- `CacheMonitoringStats.cs` — removed `OpenGraphStats` property
- `CacheReset.razor` — removed "Open Graph data cache" UI text
- `redmuffin.Blazor.StaticWeb.Api.csproj` — removed AngleSharp NuGet dependency

### Verification

- Zero build warnings after removal
- All remaining tests pass
- Articles page functionality unchanged
- Cache monitoring works without OpenGraph stats

## Prevention

- Before implementing infrastructure for a feature, confirm the UI integration exists or is planned
- Regularly audit the codebase for dead code — services registered in DI but never consumed
- Use incremental removal with continuous testing rather than bulk deletion
