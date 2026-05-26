---
date: 2026-03-31
title: "Videos and Articles Page Performance: Lazy Loading and Virtualization"
tags: [blazor, wasm, performance]
status: abandoned
reason: "RaindropItemCard component never created; Videos/Articles pages refactored via composition-over-inheritance pattern instead"
---

## Problem

The Videos (`/videos`) and Articles (`/articles`) pages loaded all item images immediately on page load. With 50+ items each having cover images, this resulted in multi-megabyte initial payloads (2-5 MB) and slow page rendering (3-5 second load times). DOM node count was proportional to the full item list rather than visible items. The two pages had duplicate inline card rendering code.

## Root Cause

- All `<img>` tags used default eager loading -- every image fetched on page load regardless of viewport position
- No CSS containment or `content-visibility` optimizations applied to card elements
- No virtualization -- all items rendered into the DOM even when off-screen
- Duplicate card rendering markup in both `Videos.razor` and `Articles.razor`

## Solution

Two-phase optimization (not yet implemented at time of documentation):

**Phase 1 -- Native lazy loading**:

- Add `loading="lazy"` to all `<img>` tags (except first 6 visible items using `loading="eager"`)
- Add `fetchpriority="high"` for first 6 items, `fetchpriority="low"` for remainder
- Add `decoding="async"` for non-blocking image decoding
- CSS containment: `contain: content` and `content-visibility: auto` with `contain-intrinsic-size: 0 350px` on `.card` class
- No JavaScript -- pure browser-native features with graceful degradation

**Phase 2 -- Blazor Virtualization**:

- Extract shared `RaindropItemCard` component with parameters: `Item`, `ItemType`, `ImageUrlCache`, `OnImageLoad`
- Wrap item lists in `<Virtualize Items="..." ItemSize="400" OverscanCount="3">` from `Microsoft.AspNetCore.Components.Web.Virtualization`
- `<Placeholder>` template with shimmer loading state for non-rendered items
- Preserve masonry layout (CSS columns) compatible with Virtualize
- Component renders appropriate button: `<i class="fas fa-play"></i> Watch Video` for videos, `<i class="fas fa-external-link-alt"></i> Read Article` for articles

**Performance targets**:
| Metric | Before | Target |
|--------|--------|--------|
| Initial page load | 3-5s | <1.5s |
| Initial payload | 2-5 MB | <300 KB |
| Time to First Paint | 2-3s | <1s |
| Largest Contentful Paint | 3-5s | <1.5s |
| DOM node count | 50+ | ~15-20 |

## Prevention

- **Lazy loading as the default**: Every new list page should use lazy loading for images by default
- **Virtualize for any list over ~20 items**: Component-level virtualization reduces DOM bloat and memory
- **Shared components over duplicate code**: When two pages share card structure, extract a reusable component immediately
- **Performance budget**: Set Lighthouse score target of 80+ mobile; gate on regression
- **Image URL cache integration**: Ensure cache works correctly with Virtualize -- populate only for visible items, persist across scroll
