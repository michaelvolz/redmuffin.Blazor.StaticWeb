---
title: Render-and-observe image URL resolution in the Core/ImagePlaceholder pipeline
date: 2026-10-09
category: architecture-patterns
module: image-pipeline
problem_type: architecture_pattern
component: service_object
severity: medium
applies_when:
  - "A Blazor WASM page maps remote content metadata, such as Raindrop covers, to image URLs."
  - "An HTTP probe is considered as a way to validate image renderability before render."
  - "Cover hosts such as X or Twitter send no CORS headers, so probes fail on URLs that render fine in the img element."
tags: [image-pipeline, blazor-wasm, cors, placeholder, raindrop, render-observe]
---

# Render-and-observe image URL resolution in the Core/ImagePlaceholder pipeline

## Context

Pre-fix state: the Articles and Videos pages mapped Raindrop content to images through a validator chain. `ImageValidator` fired an HTTP HEAD request per cover and cached the verdict under `img_validation_*` keys in a 4-week localStorage cache; `ImageUrlResolver.PopulateImageUrlCacheAsync` gated its result on that cache. Each probe failure wrote a failed verdict into that cache, so every card whose cached verdict was negative kept its "no image" placeholder.

The removed design was an accretion (session history): CORS handling first forced cache lookups into the synchronous render path, which cost the Articles page 2–10 seconds before the first article showed; a background `Task.WhenAll` pass with `SemaphoreSlim` concurrency replaced the serialization; an April 2026 plan then proposed a single-service simplification with "one HTTP HEAD request per image URL, cached permanently in localStorage", and that plan never shipped. The 2026-10-09 working-tree change ended the whole lineage by deleting validation as a concept, including `ImageValidator`, `ImageValidationResult`, `IImageValidator`, the persistence wrapper `BrowserStorageService`, and the `img_validation_*` cache dimension. At capture time the deletions are uncommitted working-tree state; no commit or pull request contains them.

X/Twitter covers are the live-QA proven case (observed 2026-10-09 on `redmuffin.net/videos`; the repo carries no X-URL fixture): such covers render fine in a browser `<img>` element, but HEAD probes against them fail, because X does not grant CORS on probe responses. Each probe failure wrote a failed verdict into the cache, so most X/Twitter cards on `/videos` showed the "no image" placeholder although the covers were renderable.

## Guidance

Let the component resolve and render, and let the `<img>` element decide renderability:

- `ImageUrlResolver.PopulateImageUrlCacheAsync` runs synchronously with no network access. For each Raindrop item it maps `item.Link ?? item.Id` to `item.Cover`, with the default SVG placeholder when no cover exists. The one-line body is the point: the cache is a synchronous dictionary mapping, not a validation outcome store. `IImageUrlResolver` documents the contract as "never triggers network requests".
- The native `<img>` element is the sole failure authority. `@onload` and `@onerror` call `HandleImageLoadAsync`; a failed load sets the cache key to `"FAILED"` and the template swaps in the SVG placeholder. The browser judges CORS, redirects, format support, and dead links in one place.
- Keep the failure state in memory for the page session. The persistent localStorage layer serves the Raindrop item caches (`raindrop_cache_*`, `RaindropItemsCache`) and is unrelated to image URL state.

The pattern name the team uses for this is **render-and-observe resolution**: resolve from metadata synchronously, render immediately, observe with element events. The validator removal shrank the DI surface to `AddImagePlaceholderServices()` registering exactly `IImagePlaceholderService`, `IImageUrlResolver`, and `PlaceholderGenerationService`.

## Why This Matters

A probe-based validator is worse than no validator. It adds a second authority that disagrees with the first one on CORS-bound hosts, and it persists its wrong verdicts, so a one-off probe failure becomes a month-long placeholder. The `<img>` element already answers the only question that matters, whether a URL renders, without network code, cache layers, or state races. Pages start fast because resolution writes a dictionary, and users see real images because nothing sits between the cover and the render.

The cost side is real: a failed cover now shows the placeholder for the frames until the error event fires, without a shimmer-and-retry lifecycle. The team judged that drawback acceptable, and no user-facing mitigation cleared a net-benefit bar.

## When to Apply

- Any new content page that pulls images from Raindrop covers or another aggregator's metadata field.
- Any code review where an image feature adds an HTTP probe, a validation result model, or a persistent verdict-cache: treat all three as a return of the removed architecture.
- A page that needs per-failure-reason text: keep the strings derived from the element event, never from a network probe.

## Examples

The mapping in its current form, from `src/redmuffin.Blazor.StaticWeb/Core/ImagePlaceholder/Services/ImageUrlResolver.cs`:

```csharp
foreach (var item in items)
{
    var cacheKey = item.Link ?? item.Id.ToString(CultureInfo.InvariantCulture);
    imageUrlCache[cacheKey] = string.IsNullOrEmpty(item.Cover)
        ? _imagePlaceholderService.GetDefaultPlaceholder()
        : item.Cover;
}

return Task.CompletedTask;
```

The removed verdict cache, quoted from the deleted `ImageValidator.cs` as the pre-fix state:

```csharp
private const string CacheKeyPrefix = "img_validation_";
private const int CacheExpirationMinutes = 40320; // 4 weeks cache
// ...
using var request = new HttpRequestMessage(HttpMethod.Head, uri);
```

## Related

- `docs/solutions/features/video-image-placeholders.md` — the shared-pipeline extraction whose successor this pattern is; its validator split is deleted (uncommitted working-tree deletion at capture time).
- `docs/solutions/features/simple-image-validation-system.md` — the April 2026 simplification plan; the validator concept is fully removed as of the 2026-10-09 working-tree change.
- `docs/solutions/features/fix-articles-image-delay-bug.md` — the serialized-lookup era that moved validation to the background; its data-URI CORS workaround kept the probe assumption.
- `docs/solutions/architecture-patterns/localstorage-caching-azure-function-calls-blazor-wasm.md` — the persistent cache layer that the removed `img_validation_*` verdict cache lived beside.
- `docs/solutions/architecture-patterns/composition-over-inheritance-orchestrator-pattern.md` — the orchestrator pages that consume this pipeline through `ImageUrlCache`.
