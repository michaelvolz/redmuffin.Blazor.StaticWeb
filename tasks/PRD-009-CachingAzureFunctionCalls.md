# Product Requirements Document: Caching Azure Function Calls in LocalStorage for Blazor Pages

## Introduction/Overview

Implement a comprehensive caching system for Azure Function calls on `Videos.razor` and `Articles.razor` pages using LocalStorage in the Blazor WebAssembly application. This feature will optimize performance by providing instant data display from cache while fetching fresh data in the background, with intelligent refresh notifications for users.

The solution addresses the need for faster page loads, reduced API calls, and improved user experience while maintaining data freshness through background updates and user-controlled refresh mechanisms.

## Goals

1. **Performance Optimization**: Reduce initial page load times by displaying cached data instantly
2. **Storage Efficiency**: Minimize LocalStorage usage through data pruning and compression
3. **User Experience**: Provide seamless data updates with non-intrusive refresh notifications
4. **Resource Conservation**: Reduce unnecessary API calls while maintaining data freshness
5. **Consistency**: Implement identical caching behavior across Videos and Articles pages
6. **Reliability**: Ensure graceful fallback when cache is unavailable or corrupted

## User Stories

1. **As a user**, I want pages to load instantly with cached content so that I can start browsing immediately without waiting for API calls.

2. **As a user**, I want to see a subtle notification when newer content is available so that I can choose to refresh and see the latest data.

3. **As a user**, I want the refresh process to be smooth and non-disruptive so that my browsing experience remains pleasant.

4. **As a developer**, I want a reusable caching system so that I can easily implement similar functionality on other pages.

5. **As a developer**, I want comprehensive error handling so that the application gracefully handles cache failures and storage limitations.

## Functional Requirements

1. **Cache Management**
   1.1. Store Azure Function responses in LocalStorage with 4-week expiration
   1.2. Prune unnecessary fields before caching to minimize storage size
   1.3. Compress cached data using LZString or similar low-CPU algorithm
   1.4. Implement cache versioning to handle data structure changes
   1.5. Provide cache invalidation and cleanup mechanisms

2. **Data Loading Strategy**
   2.1. Display cached data immediately on page load
   2.2. Fetch fresh data in background immediately after page load
   2.3. Compare timestamps to determine if fresh data is newer
   2.4. Update cache silently with new data
   2.5. Show refresh badge only when newer data is available

3. **Refresh Badge Implementation**
   3.1. Display small, non-intrusive badge when newer data is available
   3.2. Position badge consistently across both pages
   3.3. Implement smooth animations for badge appearance/disappearance
   3.4. Allow users to click badge to refresh displayed data
   3.5. Hide badge after successful refresh

4. **Data Structure Optimization**
   4.1. Analyze current `RaindropItem` usage on both pages
   4.2. Create pruned data models containing only displayed fields
   4.3. Implement field mapping between full and pruned models
   4.4. Ensure pruned data maintains all necessary information for UI rendering

5. **Error Handling**
   5.1. Handle LocalStorage quota exceeded gracefully
   5.2. Provide fallback when LocalStorage is unavailable
   5.3. Recover from corrupted cache data
   5.4. Log cache operations for debugging
   5.5. Maintain functionality when compression fails

## Non-Goals (Out of Scope)

1. **Cross-Page Cache Sharing**: Cache data is page-specific, not shared between Videos and Articles
2. **Real-time Updates**: No WebSocket or SignalR integration for live data updates
3. **User-Configurable Cache Settings**: Cache duration and behavior are fixed
4. **Cache Analytics**: No detailed cache hit/miss statistics or performance metrics
5. **Offline-First Architecture**: Cache is for performance, not offline functionality
6. **Background Sync**: No periodic background data synchronization

## Design Considerations

### UI/UX Requirements

- **Refresh Badge**: Small, circular badge with subtle animation, positioned in top-right corner of content area
- **Badge Styling**: Use Foundation's color palette with success/info colors, minimal shadow
- **Badge Content**: Simple icon (refresh symbol) with optional "New" text
- **Animations**: Fade-in/fade-out transitions, subtle pulse effect for attention
- **Accessibility**: Proper ARIA labels, keyboard navigation support, screen reader compatibility

### Visual Design

- Badge size: 24px diameter with 16px icon
- Colors: Foundation's `$success-color` background with white icon
- Position: `position: fixed; top: 20px; right: 20px; z-index: 1000`
- Animation: 300ms ease-in-out transitions

## Technical Considerations

### Blazor WebAssembly .NET 9 Implementation

#### Component Architecture

- **Cache Service**: `IRaindropItemsCache` interface with concrete implementation
- **Helper Class**: `RefreshBadgeHelper` for reusable badge functionality
- **Data Models**: Pruned versions of `RaindropItem` for efficient storage
- **Integration**: Minimal changes to existing `Videos.razor.cs` and `Articles.razor.cs`

#### Service Dependencies

- **Blazored.LocalStorage**: For browser storage operations
- **System.Text.Json**: For serialization with AOT compatibility
- **LZString.NET**: For data compression (or similar low-overhead library)
- **IRaindropAPI**: Existing service for data fetching

#### Data Pruning Strategy

Based on analysis of current page usage, retain only:

- `Id` (long): Unique identifier
- `Link` (string): Article/video URL
- `Title` (string): Display title
- `Excerpt` (string): Description text
- `Cover` (string): Image URL
- `Created` (DateTime): Creation timestamp
- `Type` (string): Content type (video/article)

#### Cache Key Strategy

- Videos: `"raindrop_cache_videos_v1"`
- Articles: `"raindrop_cache_articles_v1"`
- Metadata: `"raindrop_cache_meta_{type}_v1"` (timestamps, version info)

### Azure Functions Integration

- **No Changes Required**: Existing `RaindropListVideos` and `RaindropListArticles` functions remain unchanged
- **API Compatibility**: Cache layer sits between components and existing `IRaindropAPI` service
- **Error Handling**: Leverage existing API error handling patterns

### Performance Considerations

- **Compression Ratio**: Target 60-70% size reduction through pruning and compression
- **Cache Size Limits**: Monitor LocalStorage usage, implement cleanup when approaching limits
- **Background Processing**: Use `Task.Run` for compression/decompression to avoid UI blocking
- **Memory Management**: Dispose of large objects promptly, use `ConfigureAwait(false)`

## Success Metrics

1. **Page Load Performance**: 80% reduction in time-to-first-content for cached pages
2. **User Engagement**: 90% of users interact with refresh badge when newer content is available
3. **Cache Hit Rate**: 95% of page loads serve from cache within 4-week window
4. **Error Rate**: Less than 1% of cache operations result in fallback to direct API calls
5. **Compression Efficiency**: Achieve 60%+ size reduction through pruning and compression

## Implementation Notes

### Service Interface Design

```csharp
public interface IRaindropItemsCache
{
    Task<IEnumerable<RaindropItem>> GetCachedItemsAsync(string cacheKey, CancellationToken cancellationToken = default);
    Task SetCachedItemsAsync(string cacheKey, IEnumerable<RaindropItem> items, CancellationToken cancellationToken = default);
    Task<bool> HasNewerDataAsync(string cacheKey, CancellationToken cancellationToken = default);
    Task InvalidateCacheAsync(string cacheKey, CancellationToken cancellationToken = default);
    Task<CacheMetadata> GetCacheMetadataAsync(string cacheKey, CancellationToken cancellationToken = default);
}
```

### Component Integration Pattern

```csharp
protected override async Task OnInitializedAsync()
{
    // 1. Load and display cached data immediately
    await LoadCachedDataAsync();
    StateHasChanged();

    // 2. Fetch fresh data in background
    _ = Task.Run(async () => await RefreshDataInBackgroundAsync());
}
```

### Refresh Badge Helper

```csharp
public sealed class RefreshBadgeHelper
{
    public bool IsVisible { get; private set; }
    public event Action? OnVisibilityChanged;
    public event Action? OnRefreshRequested;

    public void ShowBadge() { /* Implementation */ }
    public void HideBadge() { /* Implementation */ }
    public Task HandleRefreshClickAsync() { /* Implementation */ }
}
```

### Testing Strategy

- **TUnit Framework**: Use `[Test]` attribute with `[Arguments]` for data-driven tests
- **LightMock.Generator**: Mock `ILocalStorageService` and `IRaindropAPI` dependencies
- **Component Testing**: Test cache loading, refresh badge behavior, error scenarios
- **Integration Testing**: Verify end-to-end cache workflow with real LocalStorage

### Code Quality Standards

- **Zero Build Warnings**: Follow StyleCop/Meziantou analyzer rules strictly
- **Async Patterns**: Use `ConfigureAwait(false)` on all awaits
- **Null Validation**: `ArgumentNullException.ThrowIfNull()` for all parameters
- **Resource Management**: Implement `IDisposable` where appropriate
- **Logging**: Use `LoggerMessage` delegates for structured logging
- **Sass Compilation**: Use `dotnet build -c Debug-Sass` to compile modified or created SASS files
- **Sass Compilation**: Sass partials need to be imported via @use to be compiled in the app.scss file
- **Sass Compilation**: Sass partials need to be @forward -ed in the correct \_index.scss

### SCSS Styling

```scss
// _refresh-badge.scss
.refresh-badge {
  position: fixed;
  top: 20px;
  right: 20px;
  z-index: 1000;

  .badge {
    @include button-style($success-color, auto, white);
    border-radius: 50%;
    width: 24px;
    height: 24px;
    transition: all 0.3s ease-in-out;

    &:hover {
      transform: scale(1.1);
    }
  }
}
```

## Open Questions

1. **Compression Library**: Should we use LZString.NET, System.IO.Compression, or a custom solution?
   Answer: which is the simplest and uses less WebAssembly space to download
2. **Cache Cleanup Strategy**: Should we implement LRU eviction or simple time-based expiration?
3. **Badge Positioning**: Should the badge be page-relative or viewport-relative?
4. **Error Recovery**: How should we handle partial cache corruption (some fields missing)?
5. **Performance Monitoring**: Should we add telemetry for cache performance metrics?
6. **Mobile Optimization**: Should badge behavior differ on mobile devices?
7. **Accessibility**: What additional ARIA attributes are needed for screen reader compatibility?
8. **Browser Compatibility**: Are there any LocalStorage limitations we need to handle for older browsers?

---

**Document Version**: 1.0
**Created**: 2024-01-XX
**Technology Stack**: Blazor WebAssembly .NET 9, Azure Functions .NET 8, TUnit Testing, LightMock.Generator
**Target Audience**: Junior Developer
**Estimated Complexity**: Medium (2-3 weeks implementation)
