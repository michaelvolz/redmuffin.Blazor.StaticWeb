# PRD-018: Videos & Articles Page Performance Optimization

## 1. Introduction/Overview

The Videos (`/videos`) and Articles (`/articles`) pages currently suffer from significant performance issues due to loading all item images immediately upon page load. This results in multi-megabyte initial payloads and slow page rendering, negatively impacting user experience and Core Web Vitals scores.

This PRD outlines a two-phase optimization strategy to achieve **60-70% reduction in initial page load time** through native browser lazy loading and Blazor virtualization techniques.

## 2. Goals

- **Primary Goal**: Reduce initial page load time by 60-70%
- **Secondary Goals**:
  - Reduce initial network payload from multi-MB to ~100-200KB
  - Improve Time to First Paint (TTFP) and Largest Contentful Paint (LCP)
  - Maintain full functionality (caching, refresh, error handling)
  - Extract reusable `RaindropItemCard` component for both pages
  - Zero JavaScript - use pure C# Blazor and native browser features

## 3. User Stories

1. **As a** site visitor **I want** the Videos/Articles page to load quickly **so that** I can start browsing content immediately without waiting for all images
2. **As a** site visitor **I want** images to load as I scroll **so that** I can see content seamlessly without performance degradation
3. **As a** developer **I want** a reusable card component **so that** both pages share consistent code and styling
4. **As a** developer **I want** to use only C# and native browser features **so that** maintenance is simpler without JavaScript dependencies

## 4. Functional Requirements

### Phase 1: Native Lazy Loading (Immediate - Week 1)

#### 4.1.1 Image Lazy Loading

**FR-1.1**: All item images must use native `loading="lazy"` attribute except the first 6 visible items which use `loading="eager"`
**FR-1.2**: Images must include `fetchpriority` attribute with "high" for first 6 items, "low" for remaining items
**FR-1.3**: Images must include `decoding="async"` attribute for non-blocking image decoding
**FR-1.4**: Implementation must be applied to both `Videos.razor` and `Articles.razor`

#### 4.1.2 CSS Performance Optimizations

**FR-1.5**: Add CSS containment (`contain: content`) to `.card` class in `_masonry.scss`
**FR-1.6**: Add `content-visibility: auto` to `.card` class for off-screen rendering optimization
**FR-1.7**: Verify shimmer loading effect continues to work correctly with new attributes

#### 4.1.3 Browser Compatibility

**FR-1.8**: Native lazy loading is supported in all modern browsers (Chrome 76+, Firefox 75+, Safari 15.4+, Edge 79+)
**FR-1.9**: Browsers without lazy loading support will fall back to eager loading (graceful degradation)

### Phase 2: Blazor Virtualization (Week 2-3)

#### 4.2.1 Shared RaindropItemCard Component

**FR-2.1**: Create new component `Features/Shared/Components/RaindropItemCard.razor`
**FR-2.2**: Component must accept parameters:

- `RaindropItem Item` (required) - The item to display
- `string ItemType` (required) - Either "video" or "article"
- `IDictionary<string, string> ImageUrlCache` (required) - Image URL cache
- `EventCallback<(string ElementId, string ItemLink, bool Success)> OnImageLoad` (required) - Image load callback
  **FR-2.3**: Component must render appropriate button based on `ItemType`:
- "video": `<i class="fas fa-play"></i> Watch Video`
- "article": `<i class="fas fa-external-link-alt"></i> Read Article`
  **FR-2.4**: Component must apply CSS class based on `ItemType`: `video-card` or `article-card`
  **FR-2.5**: Component must maintain all existing functionality: shimmer effect, fallback placeholders, image error handling

#### 4.2.2 Virtualize Implementation

**FR-2.6**: Both `Videos.razor` and `Articles.razor` must use `<Virtualize>` component from `Microsoft.AspNetCore.Components.Web.Virtualization`
**FR-2.7**: Virtualize must be configured with:

- `Items` bound to the full item list
- `ItemSize="400"` (estimated pixel height per card)
- `OverscanCount="3"` (render 3 items above/below viewport)
- `Context` parameter for item access
  **FR-2.8**: Virtualize must include `<Placeholder>` template showing shimmer loading state for non-rendered items
  **FR-2.9**: Implementation must maintain existing masonry layout appearance (CSS columns still work with Virtualize)

#### 4.2.3 Image Cache Integration

**FR-2.10**: Image URL cache population must work correctly with Virtualize (cache populated for visible items only)
**FR-2.11**: Cache must persist across scroll operations (don't re-fetch for items that were previously visible)

### Phase 3: Page Refactoring (Week 3)

#### 4.3.1 Videos Page Updates

**FR-3.1**: Replace inline card rendering with `RaindropItemCard` component
**FR-3.2**: Wrap items in `<Virtualize>` component
**FR-3.3**: Maintain all existing page-specific logic (title, subheader, error handling, refresh badge)
**FR-3.4**: Remove duplicate card rendering code

#### 4.3.2 Articles Page Updates

**FR-3.2**: Replace inline card rendering with `RaindropItemCard` component
**FR-3.3**: Wrap items in `<Virtualize>` component
**FR-3.4**: Maintain all existing page-specific logic (title, subheader, error handling, refresh badge, loading state)
**FR-3.5**: Remove duplicate card rendering code

## 5. Non-Goals (Out of Scope)

- **NOT** consolidating Videos and Articles pages into a single parameterized page (too many cosmetic differences)
- **NOT** implementing infinite scroll or pagination (keep current "load all" approach with virtualization)
- **NOT** changing the data model or API layer
- **NOT** modifying caching strategy (keep existing cache-first, background refresh)
- **NOT** using JavaScript-based lazy loading libraries (Intersection Observer API via JS interop)
- **NOT** modifying backend API functions (`RaindropListVideos`, `RaindropListArticles`)
- **NOT** changing the visual design or layout structure

## 6. Design Considerations

### 6.1 Visual Requirements

- Masonry layout must be preserved (CSS columns)
- Shimmer loading effect must continue to work
- Card styling (`video-card` vs `article-card`) must be maintained
- Button icons and text must match current implementation

### 6.2 Responsive Behavior

- Virtualize must work correctly across all breakpoints (mobile, tablet, desktop)
- Masonry column count adjustments must continue to work
- Placeholder shimmer must match card dimensions at each breakpoint

## 7. Technical Considerations

### 7.1 Dependencies

- **Microsoft.AspNetCore.Components.Web.Virtualization** (built into .NET 9, no NuGet package needed)
- Existing services: `IImagePlaceholderService`, `IImageValidationCacheService`, `IRaindropItemsCache`

### 7.2 Performance Targets

- **Initial page load**: 60-70% reduction in load time
- **Initial payload**: Reduce from multi-MB to ~100-200KB (only visible images)
- **Memory usage**: Reduced DOM size (only ~12-16 cards in DOM at once vs all items)
- **Scroll performance**: Smooth 60fps scrolling with virtualization

### 7.3 Browser Support

- Modern browsers with native lazy loading support (98%+ of users)
- Graceful degradation for older browsers

### 7.4 Testing Requirements

- Test with 50+ items to verify virtualization behavior
- Test scrolling performance on mobile devices
- Verify image cache still works correctly
- Test refresh functionality with Virtualize
- Test error states (network failure, image load failure)

## 8. Success Metrics

| Metric                   | Before          | Target          | Measurement Method           |
| ------------------------ | --------------- | --------------- | ---------------------------- |
| Initial page load time   | 3-5 seconds     | <1.5 seconds    | Lighthouse performance audit |
| Initial network payload  | 2-5 MB          | <300 KB         | Chrome DevTools Network tab  |
| Time to First Paint      | 2-3 seconds     | <1 second       | Lighthouse                   |
| Largest Contentful Paint | 3-5 seconds     | <1.5 seconds    | Lighthouse                   |
| DOM node count           | All items (50+) | ~15-20 nodes    | Chrome DevTools Elements tab |
| Memory usage             | High            | Reduced by 70%+ | Chrome DevTools Memory tab   |

## 9. Implementation Timeline

### Week 1: Phase 1 - Native Lazy Loading

- Day 1-2: Update `Videos.razor` with lazy loading attributes
- Day 3: Update `Articles.razor` with lazy loading attributes
- Day 4: Add CSS containment and content-visibility
- Day 5: Testing and verification

### Week 2: Phase 2 - Shared Component

- Day 1-2: Create `RaindropItemCard.razor` component
- Day 3-4: Implement Virtualize in both pages
- Day 5: Testing and refinement

### Week 3: Integration & Testing

- Day 1-2: Integration testing across devices
- Day 3-4: Performance benchmarking
- Day 5: Documentation and deployment preparation

## 10. Open Questions

1. **Q**: Should we preload the first 1-2 critical images using `<link rel="preload">` in `index.html`?
   **A**: To be determined based on Phase 1 results

2. **Q**: Should Virtualize use server-side pagination or still load all items upfront?
   **A**: Keep current "load all" approach - Virtualize only affects rendering, not data fetching

3. **Q**: How should we handle the `_isLoading` flag discrepancy between Videos (missing) and Articles (present)?
   **A**: Standardize both pages to use `_isLoading` for consistency

4. **Q**: Should we add a "load more" button or infinite scroll for very large collections (>100 items)?
   **A**: Out of scope for this PRD - address in future if needed

## 11. Files to Modify

### Phase 1 Files:

- `src/redmuffin.Blazor.StaticWeb/Features/Pages/VideosPage/Videos.razor`
- `src/redmuffin.Blazor.StaticWeb/Features/Pages/ArticlesPage/Articles.razor`
- `src/redmuffin.Blazor.StaticWeb/scss/components/_masonry.scss`
- `src/redmuffin.Blazor.StaticWeb/scss/components/_card.scss` (if exists, or create)

### Phase 2 Files:

- `src/redmuffin.Blazor.StaticWeb/Features/Shared/Components/RaindropItemCard.razor` (NEW)
- `src/redmuffin.Blazor.StaticWeb/Features/Shared/Components/RaindropItemCard.razor.cs` (NEW)
- `src/redmuffin.Blazor.StaticWeb/Features/Pages/VideosPage/Videos.razor` (REFACTOR)
- `src/redmuffin.Blazor.StaticWeb/Features/Pages/ArticlesPage/Articles.razor` (REFACTOR)

### Test Files:

- `tests/redmuffin.Blazor.StaticWeb.Tests/Features/Pages/VideosPage/VideosTests.cs` (UPDATE)
- `tests/redmuffin.Blazor.StaticWeb.Tests/Features/Pages/ArticlesPage/ArticlesTests.cs` (UPDATE)
- NEW: `tests/redmuffin.Blazor.StaticWeb.Tests/Features/Shared/Components/RaindropItemCardTests.cs`

---

## Approval

**Product Owner**: ********\_******** Date: ********\_********

**Technical Lead**: ********\_******** Date: ********\_********

**Stakeholder**: ********\_******** Date: ********\_********
