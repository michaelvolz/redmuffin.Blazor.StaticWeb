# Product Requirements Document: Caching Azure Function Calls in LocalStorage for Blazor Pages - To Do

## Relevant Files

### Blazor Components

- `src/redmuffin.Blazor.StaticWeb/Features/Pages/VideosPage/Videos.razor.cs` - Updated code-behind for Videos component to integrate caching functionality.
- `src/redmuffin.Blazor.StaticWeb/Features/Pages/ArticlesPage/Articles.razor.cs` - Updated code-behind for Articles component to integrate caching functionality.
- `src/redmuffin.Blazor.StaticWeb/Features/Common/RefreshBadge/RefreshBadge.razor` - Reusable refresh badge component for displaying cache update notifications.
- `src/redmuffin.Blazor.StaticWeb/Features/Common/RefreshBadge/RefreshBadge.razor.cs` - Code-behind for refresh badge component with event handling.

### Services

- `src/redmuffin.Blazor.StaticWeb/Features/Cache/Services/IRaindropItemsCache.cs` - Interface for raindrop items caching operations.
- `src/redmuffin.Blazor.StaticWeb/Features/Cache/Services/RaindropItemsCache.cs` - Concrete implementation of caching service with LocalStorage integration.
- `src/redmuffin.Blazor.StaticWeb/Features/Cache/Services/RaindropItemsCache.Logging.cs` - LoggerMessage delegates for caching service.
- `src/redmuffin.Blazor.StaticWeb/Features/Cache/Helpers/RefreshBadgeHelper.cs` - Helper class for managing refresh badge state and behavior.
- `src/redmuffin.Blazor.StaticWeb/Features/Cache/Helpers/RefreshBadgeHelper.Logging.cs` - LoggerMessage delegates for refresh badge helper.

### Shared/Common

- `src/redmuffin.Blazor.StaticWeb.Common/Models/Cache/CachedRaindropItem.cs` - Pruned data model for efficient caching.
- `src/redmuffin.Blazor.StaticWeb.Common/Models/Cache/CacheMetadata.cs` - Metadata model for cache versioning and timestamps.
- `src/redmuffin.Blazor.StaticWeb.Common/Models/Cache/CacheResult.cs` - Result model for cache operations with success/failure states.
- `src/redmuffin.Blazor.StaticWeb.Common/Enums/CacheStatus.cs` - Enumeration for cache operation status.

### Styles

- `src/redmuffin.Blazor.StaticWeb/wwwroot/scss/features/cache/_refresh-badge.scss` - SCSS partial for refresh badge styling.
- `src/redmuffin.Blazor.StaticWeb/wwwroot/scss/features/cache/_index.scss` - Feature SCSS index file for cache-related styles.
- `src/redmuffin.Blazor.StaticWeb/wwwroot/scss/features/_index.scss` - Updated to include cache feature styles.

### Tests

- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Cache/Services/RaindropItemsCacheTests.cs` - TUnit tests for caching service.
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Cache/Services/RaindropItemsCacheTests.Helpers.cs` - TestScope and helper methods for cache service tests.
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Cache/Helpers/RefreshBadgeHelperTests.cs` - TUnit tests for refresh badge helper.
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Cache/Helpers/RefreshBadgeHelperTests.Helpers.cs` - TestScope and helper methods for refresh badge helper tests.
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Common/RefreshBadge/RefreshBadgeTests.cs` - TUnit tests for refresh badge component.
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Common/RefreshBadge/RefreshBadgeTests.Helpers.cs` - TestScope and helper methods for refresh badge component tests.
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Pages/VideosPage/VideosPageCacheTests.cs` - TUnit tests for Videos page caching integration.
- `tests/redmuffin.Blazor.StaticWeb.Tests/NewTests/Features/Pages/ArticlesPage/ArticlesPageCacheTests.cs` - TUnit tests for Articles page caching integration.

### Notes

- Tests use TUnit framework with `[Test]` attribute for test methods and `[Arguments]` for data-driven tests.
- Mocking uses LightMock.Generator ONLY (NSubstitute deprecated).
- Use `dotnet clean && dotnet build --no-restore --verbosity quiet` to verify zero build warnings (except IL2111).
- Use `dotnet test` to run all tests or `dotnet test --filter "FullyQualifiedName~[TestClassName]"` for specific test classes.
- **QUALITY CHECK**: After every step verify TestScope pattern, custom mock pattern, ConfigureAwait(false) compliance following HomeTests*.cs files as prime example.
- Blazor components follow feature-based organization under `src/redmuffin.Blazor.StaticWeb/Features/`.
- Use Zurb Foundation classes for consistent UI styling.
- Component styling uses feature-based SCSS with `@use` directives.
- All async methods must use `ConfigureAwait(false)` and proper error handling.
- Follow StyleCop/Meziantou analyzer rules for code quality.
- Use `dotnet build -c Debug-Sass` to compile SCSS changes.
- SCSS partials must be imported via `@use` and `@forward` in appropriate index files.

## Tasks

- [x] 1.0 Core Caching Infrastructure
  - [x] 1.1 Create `IRaindropItemsCache` interface with methods for Get, Set, Clear, and IsExpired operations
  - [x] 1.2 Implement `RaindropItemsCache` service with LocalStorage integration using Blazored.LocalStorage
  - [x] 1.3 Add LZString.NET compression/decompression for cache data optimization
  - [x] 1.4 Implement cache versioning and expiration logic (4-week TTL)
  - [x] 1.5 Add cache key strategy for Videos and Articles with proper namespacing
  - [x] 1.6 Register cache service in DI container with appropriate lifetime (Scoped)
  - [x] 1.7. Add comprehensive error handling for LocalStorage operations and compression failures

- [x] 2.0 Pruned Data Models
  - [x] 2.1 Create `PrunedRaindropItem` model with essential fields only (Id, Link, Title, Excerpt, Cover)
  - [x] 2.2 Implement extension methods for converting between `RaindropItem` and `PrunedRaindropItem`
  - [x] 2.3 Add JSON serialization attributes for optimal storage size
  - [x] 2.4 Create cache metadata model for timestamps and version tracking
  - [x] 2.5 Implement data validation for pruned models to ensure data integrity

- [x] 3.0 Refresh Badge Component (SIMPLIFIED)
  - [x] 3.1 ~~Create `RefreshBadgeHelper` class~~ (REMOVED - over-engineered)
  - [x] 3.2 Implement `RefreshBadge.razor` component as simple button with basic functionality
  - [x] 3.3 ~~Add complex badge visibility logic~~ (SIMPLIFIED - basic state management only)
  - [x] 3.4 Implement simple click handler for refresh functionality
  - [x] 3.5 ~~Add extensive accessibility features~~ (SIMPLIFIED - basic button accessibility)
  - [x] 3.6 Create basic badge state management (hidden, visible, loading, error states)

- [x] 4.0 Integration with Existing Pages
  - [x] 4.1 Modify `Videos.razor.cs` to integrate cache service and refresh badge
  - [x] 4.2 Update `Videos.razor` component to include refresh badge in UI
  - [x] 4.3 Implement background data fetching with cache comparison for Videos page
  - [x] 4.4 Modify `Articles.razor.cs` to integrate cache service and refresh badge
  - [x] 4.5 Update `Articles.razor` component to include refresh badge in UI
  - [x] 4.6 Implement background data fetching with cache comparison for Articles page
  - [x] 4.7 Add proper error handling and fallback mechanisms for both pages
  - [x] 4.8 Ensure identical badge functionality and styling across both pages

- [x] 5.0 SCSS Styling
  - [x] 5.1 Create `_refresh-badge.scss` partial with Foundation-compatible styles
  - [x] 5.2 Implement badge animations (fade-in, pulse, loading spinner)
  - [x] 5.3 Add responsive design considerations for mobile devices
  - [x] 5.4 Create hover and focus states for accessibility
  - [x] 5.5 Add feature-specific SCSS index file and integrate with main app.scss
  - [x] 5.6 Ensure consistent styling with existing UI components

- [x] 6.0 Comprehensive Testing Suite
  - [x] 6.1 Create unit tests for `IRaindropItemsCache` implementation using TUnit and LightMock.Generator
  - [x] 6.2 Test cache expiration logic and data compression/decompression
  - [x] 6.3 Create unit tests for `PrunedRaindropItem` conversion methods
  - [x] 6.4. Test `RefreshBadgeHelper` class functionality and state management
  - [x] 6.5 Create component tests for `RefreshBadge.razor` using bUnit
  - [x] 6.6 Test integration with `Videos.razor` and `Articles.razor` components
  - [x] 6.7 Add error handling tests for LocalStorage failures and network issues
  - [x] 6.8 Create performance tests for cache operations and compression efficiency
  - [x] 6.9 Test accessibility features and keyboard navigation
  - [x] 6.10. Verify zero build warnings compliance and run full test suite
