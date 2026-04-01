# PRD-019: Refactor Videos/Articles Pages with Shared Components and Performance Optimization

## 1. Introduction/Overview

The Videos (`/videos`) and Articles (`/articles`) pages share nearly identical UI structure and business logic but currently duplicate ~50 lines of HTML markup in each page. This creates maintenance overhead and increases the risk of inconsistencies when making changes.

This PRD outlines a refactoring effort to:

1. Extract shared card rendering into a reusable `RaindropItemCard` component
2. Create a shared service/base class for common business logic
3. Maintain all existing performance optimizations (lazy loading, CSS containment)
4. Revert mockdata to original 5 items for clean testing
5. Implement comprehensive test coverage

**Goal**: Eliminate code duplication while preserving functionality and improving maintainability.

## 2. Goals

- **Primary Goal**: Eliminate ~100 lines of duplicated HTML/code across Videos and Articles pages
- **Secondary Goals**:
  - Create single source of truth for card rendering logic
  - Maintain native lazy loading performance benefits (60-70% initial payload reduction)
  - Preserve CSS containment optimizations
  - Achieve 100% test coverage for shared components
  - Ensure zero visual or functional regressions
  - Complete within immediate timeline (NOW)

## 3. User Stories

1. **As a** developer **I want** a single `RaindropItemCard` component **so that** layout changes only need to be made in one place
2. **As a** developer **I want** shared business logic extracted into a service/base class **so that** data handling is consistent across pages
3. **As a** site visitor **I want** the pages to load quickly with lazy-loaded images **so that** I can start browsing immediately
4. **As a** QA engineer **I want** comprehensive test coverage **so that** regressions are caught before deployment
5. **As a** maintainer **I want** clean, DRY code **so that** future enhancements are easier to implement

## 4. Functional Requirements

### 4.1 Shared Component Architecture

**FR-1.1**: Create `RaindropItemCard.razor` component in `Features/Common/Components/`
**FR-1.2**: Component must accept these parameters:

- `RaindropItem Item` (required) - The item data to display
- `string ItemType` (required) - Either "video" or "article"
- `int Index` (required) - Position in list for lazy loading logic
- `IDictionary<string, string> ImageUrlCache` (required) - Image validation cache
- `EventCallback<(string ElementId, string ItemLink, bool Success)> OnImageLoad` (required) - Image load callback
  **FR-1.3**: Component must render based on `ItemType`:
- "video": Show `fa-play` icon and "Watch Video" text
- "article": Show `fa-external-link-alt` icon and "Read Article" text
- Apply CSS class: `video-card` or `article-card` accordingly
  **FR-1.4**: Component must implement lazy loading attributes:
- `loading="@(Index < 6 ? "eager" : "lazy")"`
- `fetchpriority="@(Index < 6 ? "high" : "low")"`
- `decoding="async"`
  **FR-1.5**: Component must handle image load/error events via `OnImageLoad` callback
  **FR-1.6**: Component must show shimmer placeholder while loading
  **FR-1.7**: Component must show fallback placeholder overlay when image fails

### 4.2 Shared Service/Base Class

**FR-2.1**: Create `RaindropPageBase.cs` abstract base class in `Features/Common/`
**FR-2.2**: Base class must provide shared functionality:

- `Dictionary<string, string> ImageUrlCache` property
- `List<RaindropItem>? Items` property
- `string? ErrorMessage` property
- `bool IsLoading` property
- `RefreshBadgeState RefreshBadgeState` property
- `bool IsRefreshing` property
  **FR-2.3**: Base class must provide common methods:
- `LoadItemsAsync()` - Abstract method for loading items
- `PopulateImageUrlCacheAsync()` - Shared cache population logic
- `HandleRefreshClickAsync()` - Shared refresh logic
- `GetImageUrl(RaindropItem item)` - Shared image URL resolution
- `HandleImageLoadAsync()` - Shared image load handling
- `DisplayTitle(RaindropItem item)` - Shared title formatting
- `DisplayExcerpt(RaindropItem item)` - Shared excerpt formatting
- `HasFallbackPlaceholder(RaindropItem item)` - Shared fallback detection
- `GetFallbackReason(RaindropItem item)` - Shared fallback reason
  **FR-2.4**: Both Videos and Articles pages must inherit from `RaindropPageBase`

### 4.3 Page Refactoring

**FR-3.1**: Update `Videos.razor` to use `RaindropItemCard` component:

```razor
@foreach (var (video, index) in _videoItems.Select((v, i) => (v, i)))
{
    <RaindropItemCard
        Item="@video"
        ItemType="video"
        Index="@index"
        ImageUrlCache="@_imageUrlCache"
        OnImageLoad="@HandleImageLoadAsync" />
}
```

**FR-3.2**: Update `Articles.razor` to use `RaindropItemCard` component (same pattern, ItemType="article")
**FR-3.3**: Both pages must maintain existing page-specific logic (titles, subheaders)
**FR-3.4**: Both pages must maintain existing error handling and loading states
**FR-3.5**: Both pages must maintain existing refresh badge functionality

### 4.4 Performance Preservation

**FR-4.1**: Native lazy loading must continue to work:

- First 6 images load eagerly (`loading="eager"`)
- Remaining images load lazily (`loading="lazy"`)
- Fetchpriority set accordingly
  **FR-4.2**: CSS containment must remain active in `_card.scss`:
- `contain: content`
- `content-visibility: auto`
- `contain-intrinsic-size: 0 350px`
  **FR-4.3**: No JavaScript-based lazy loading solutions

### 4.5 Mockdata Reversion

**FR-5.1**: Revert `videos.json` to original 5 items
**FR-5.2**: Revert `articles.json` to original 5 items
**FR-5.3**: Ensure mockdata works correctly with refactored components

### 4.6 Testing Requirements

**FR-6.1**: Create `RaindropItemCardTests.cs` with comprehensive tests:

- Component renders correctly with video type
- Component renders correctly with article type
- Component applies correct CSS class based on ItemType
- Component shows correct button icon and text
- Component implements lazy loading attributes correctly
- Component handles image load callback
- Component handles image error callback
- Component shows shimmer placeholder initially
- Component shows fallback placeholder on error
  **FR-6.2**: Update `VideosTests.cs` to test refactored page:
- Page inherits from RaindropPageBase
- Page renders RaindropItemCard components
- Page maintains existing functionality
- Page handles refresh correctly
  **FR-6.3**: Update `ArticlesTests.cs` to test refactored page:
- Page inherits from RaindropPageBase
- Page renders RaindropItemCard components
- Page maintains existing functionality
- Page handles refresh correctly
  **FR-6.4**: Create integration tests:
- End-to-end test: Load Videos page, verify cards render
- End-to-end test: Load Articles page, verify cards render
- End-to-end test: Scroll to trigger lazy loading
- End-to-end test: Click refresh, verify data reloads
  **FR-6.5**: All existing tests must continue to pass
  **FR-6.6**: Achieve 100% code coverage for RaindropItemCard component
  **FR-6.7**: Achieve 100% code coverage for RaindropPageBase class

## 5. Non-Goals (Out of Scope)

- **NOT** implementing Virtualize component (masonry layout incompatibility confirmed)
- **NOT** consolidating Videos and Articles into a single parameterized page (cosmetic differences too significant)
- **NOT** changing the visual design or layout structure
- **NOT** modifying the data model or API layer
- **NOT** implementing infinite scroll or pagination
- **NOT** adding new features (badges, tags, ratings, etc.)
- **NOT** changing caching strategy
- **NOT** using JavaScript-based solutions

## 6. Design Considerations

### 6.1 Visual Requirements

- Masonry layout must be preserved (CSS columns)
- Card styling (`video-card` vs `article-card`) must be maintained
- Shimmer loading effect must continue to work
- Button icons and text must match current implementation
- Responsive behavior must be maintained

### 6.2 Component Interface

```csharp
// RaindropItemCard.razor.cs
public partial class RaindropItemCard : ComponentBase
{
    [Parameter] public RaindropItem Item { get; set; } = null!;
    [Parameter] public string ItemType { get; set; } = null!;
    [Parameter] public int Index { get; set; }
    [Parameter] public IDictionary<string, string> ImageUrlCache { get; set; } = null!;
    [Parameter] public EventCallback<(string ElementId, string ItemLink, bool Success)> OnImageLoad { get; set; }
}
```

### 6.3 Base Class Structure

```csharp
// RaindropPageBase.cs
public abstract class RaindropPageBase : ComponentBase
{
    protected Dictionary<string, string> ImageUrlCache { get; } = new();
    protected List<RaindropItem>? Items { get; set; }
    protected string? ErrorMessage { get; set; }
    protected bool IsLoading { get; set; }
    protected RefreshBadgeState RefreshBadgeState { get; set; } = RefreshBadgeState.Hidden;

    protected abstract Task LoadItemsAsync(CancellationToken cancellationToken = default);
    protected async Task PopulateImageUrlCacheAsync() { /* shared logic */ }
    protected async Task HandleRefreshClickAsync() { /* shared logic */ }
    protected string GetImageUrl(RaindropItem item) { /* shared logic */ }
    protected async Task HandleImageLoadAsync(string elementId, string itemLink, bool success) { /* shared logic */ }
    protected static string DisplayTitle(RaindropItem item) { /* shared logic */ }
    protected static string DisplayExcerpt(RaindropItem item) { /* shared logic */ }
    protected bool HasFallbackPlaceholder(RaindropItem item) { /* shared logic */ }
    protected string GetFallbackReason(RaindropItem item) { /* shared logic */ }
}
```

## 7. Technical Considerations

### 7.1 Dependencies

- `Microsoft.AspNetCore.Components.Web` (built-in)
- `Microsoft.JSInterop` (built-in)
- Existing services: `IImagePlaceholderService`, `IImageValidationCacheService`, `IRaindropAPI`

### 7.2 File Structure

```
Features/
├── Common/
│   ├── Components/
│   │   ├── RaindropItemCard.razor
│   │   └── RaindropItemCard.razor.cs
│   └── RaindropPageBase.cs
├── Pages/
│   ├── VideosPage/
│   │   ├── Videos.razor (refactored)
│   │   ├── Videos.razor.cs (refactored)
│   │   └── VideosTests/
│   │       ├── VideosTests.cs
│   │       └── VideosTests.Helpers.cs
│   └── ArticlesPage/
│       ├── Articles.razor (refactored)
│       ├── Articles.razor.cs (refactored)
│       └── ArticlesTests/
│           ├── ArticlesTests.cs
│           └── ArticlesTests.Helpers.cs
```

### 7.3 Inheritance Strategy

```csharp
// Videos.razor.cs
public partial class Videos : RaindropPageBase
{
    protected override async Task LoadItemsAsync(CancellationToken cancellationToken = default)
    {
        // Videos-specific loading logic
    }
}

// Articles.razor.cs
public partial class Articles : RaindropPageBase
{
    protected override async Task LoadItemsAsync(CancellationToken cancellationToken = default)
    {
        // Articles-specific loading logic
    }
}
```

## 8. Success Metrics

| Metric            | Target      | Measurement                                              |
| ----------------- | ----------- | -------------------------------------------------------- |
| Code Duplication  | 0%          | Before: ~100 lines duplicated, After: 0 lines duplicated |
| Test Coverage     | 100%        | Coverlet code coverage report                            |
| Build Warnings    | 0           | `dotnet build --verbosity quiet`                         |
| Test Pass Rate    | 100%        | `dotnet test`                                            |
| Performance       | Maintained  | Lighthouse audit (LCP < 1.5s)                            |
| Bundle Size       | No increase | `dotnet publish` output comparison                       |
| Visual Regression | 0           | Manual browser testing                                   |

## 9. Implementation Timeline

### Phase 1: Foundation (Day 1)

- [ ] Create `RaindropPageBase.cs` with shared properties and methods
- [ ] Create `RaindropItemCard.razor` and `.razor.cs` with all parameters
- [ ] Implement lazy loading attributes in component
- [ ] Build and verify no errors

### Phase 2: Refactor Videos Page (Day 1-2)

- [ ] Update `Videos.razor` to use `RaindropItemCard` component
- [ ] Update `Videos.razor.cs` to inherit from `RaindropPageBase`
- [ ] Test Videos page manually
- [ ] Run Videos tests and fix any failures

### Phase 3: Refactor Articles Page (Day 2)

- [ ] Update `Articles.razor` to use `RaindropItemCard` component
- [ ] Update `Articles.razor.cs` to inherit from `RaindropPageBase`
- [ ] Test Articles page manually
- [ ] Run Articles tests and fix any failures

### Phase 4: Mockdata Reversion (Day 2)

- [ ] Revert `videos.json` to 5 items
- [ ] Revert `articles.json` to 5 items
- [ ] Test with original mockdata

### Phase 5: Comprehensive Testing (Day 3-4)

- [ ] Create `RaindropItemCardTests.cs` with full coverage
- [ ] Update `VideosTests.cs` for refactored page
- [ ] Update `ArticlesTests.cs` for refactored page
- [ ] Create integration tests
- [ ] Run full test suite: `dotnet test`
- [ ] Verify 100% code coverage

### Phase 6: Final Verification (Day 4-5)

- [ ] Manual browser testing (Videos page)
- [ ] Manual browser testing (Articles page)
- [ ] Performance testing (Lighthouse audit)
- [ ] Zero build warnings check
- [ ] Code review and cleanup

**Total Timeline: 5 days maximum**

## 10. Risk Assessment

| Risk                            | Impact | Mitigation                                   |
| ------------------------------- | ------ | -------------------------------------------- |
| Breaking existing functionality | High   | Comprehensive test coverage, gradual rollout |
| Performance regression          | Medium | Lighthouse audit, before/after comparison    |
| Component complexity            | Low    | Simple parameter-based component             |
| Inheritance issues              | Low    | Well-defined base class, clear overrides     |
| Test maintenance burden         | Medium | Good test organization, helpers              |

## 11. Open Questions

1. **Q**: Should the base class use `abstract` methods or `virtual` with default implementations?
   **A**: Use abstract for `LoadItemsAsync()`, virtual with defaults for common helpers

2. **Q**: Should we keep the existing `HandleImageLoadAsync` method signatures or simplify?
   **A**: Simplify to use base class methods where possible

3. **Q**: Do we need to handle the `_isLoading` flag discrepancy between Videos and Articles?
   **A**: Yes, standardize in base class

4. **Q**: Should we add a `RaindropItemCardTests.EdgeCases.cs` for error scenarios?
   **A**: Yes, include in test plan

## 12. Files to Modify

### New Files:

- `Features/Common/RaindropPageBase.cs`
- `Features/Common/Components/RaindropItemCard.razor`
- `Features/Common/Components/RaindropItemCard.razor.cs`
- `tests/.../Features/Common/Components/RaindropItemCardTests.cs`
- `tests/.../Features/Common/Components/RaindropItemCardTests.Helpers.cs`
- `tests/.../Features/Common/Components/RaindropItemCardTests.EdgeCases.cs`

### Modified Files:

- `Features/Pages/VideosPage/Videos.razor`
- `Features/Pages/VideosPage/Videos.razor.cs`
- `Features/Pages/ArticlesPage/Articles.razor`
- `Features/Pages/ArticlesPage/Articles.razor.cs`
- `tests/.../Features/Pages/VideosPage/VideosTests.cs`
- `tests/.../Features/Pages/ArticlesPage/ArticlesTests.cs`
- `wwwroot/mockdata/videos.json`
- `wwwroot/mockdata/articles.json`

### Preserved Files (No Changes):

- `Core/ImagePlaceholder/Services/ImagePlaceholderService.cs`
- `scss/components/_card.scss` (CSS containment stays)
- `scss/components/_masonry.scss`

---

## Approval

**Product Owner**: ********\_******** Date: ********\_********

**Technical Lead**: ********\_******** Date: ********\_********

**Stakeholder**: ********\_******** Date: ********\_********
