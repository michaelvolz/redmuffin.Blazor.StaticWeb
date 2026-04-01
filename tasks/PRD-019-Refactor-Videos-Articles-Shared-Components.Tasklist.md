# Task List: PRD-019 Refactor Videos/Articles Pages with Shared Components

## Instructions

- [ ] Check off tasks as they are completed
- [ ] Each parent task (0.0, 1.0, etc.) represents a major milestone
- [ ] Complete all sub-tasks before marking parent task as complete
- [ ] Run `dotnet build` after each major file change to catch errors early
- [ ] Run relevant tests after completing test-related tasks

## Relevant Files

### New Files to Create

- `src/redmuffin.Blazor.StaticWeb/Features/Common/RaindropPageBase.cs`
- `src/redmuffin.Blazor.StaticWeb/Features/Common/Components/RaindropItemCard.razor`
- `src/redmuffin.Blazor.StaticWeb/Features/Common/Components/RaindropItemCard.razor.cs`
- `tests/redmuffin.Blazor.StaticWeb.Tests/Features/Common/Components/RaindropItemCardTests.cs`
- `tests/redmuffin.Blazor.StaticWeb.Tests/Features/Common/Components/RaindropItemCardTests.Helpers.cs`
- `tests/redmuffin.Blazor.StaticWeb.Tests/Features/Common/Components/RaindropItemCardTests.EdgeCases.cs`

### Modified Files

- `src/redmuffin.Blazor.StaticWeb/Features/Pages/VideosPage/Videos.razor`
- `src/redmuffin.Blazor.StaticWeb/Features/Pages/VideosPage/Videos.razor.cs`
- `src/redmuffin.Blazor.StaticWeb/Features/Pages/ArticlesPage/Articles.razor`
- `src/redmuffin.Blazor.StaticWeb/Features/Pages/ArticlesPage/Articles.razor.cs`
- `tests/redmuffin.Blazor.StaticWeb.Tests/Features/Pages/VideosPage/VideosTests.cs`
- `tests/redmuffin.Blazor.StaticWeb.Tests/Features/Pages/VideosPage/VideosTests.Helpers.cs`
- `tests/redmuffin.Blazor.StaticWeb.Tests/Features/Pages/ArticlesPage/ArticlesTests.cs`
- `tests/redmuffin.Blazor.StaticWeb.Tests/Features/Pages/ArticlesPage/ArticlesTests.Helpers.cs`
- `src/redmuffin.Blazor.StaticWeb/wwwroot/mockdata/videos.json`
- `src/redmuffin.Blazor.StaticWeb/wwwroot/mockdata/articles.json`

### Preserved Files (No Changes Required)

- `src/redmuffin.Blazor.StaticWeb/Core/ImagePlaceholder/Services/ImagePlaceholderService.cs`
- `src/redmuffin.Blazor.StaticWeb/scss/components/_card.scss`
- `src/redmuffin.Blazor.StaticWeb/scss/components/_masonry.scss`

---

## Tasks

### 0.0 Create Feature Branch

- [ ] 0.1 Check current git status to ensure working directory is clean
- [ ] 0.2 Create new branch `feature/019-refactor-videos-articles-shared-components`
- [ ] 0.3 Switch to the new branch
- [ ] 0.4 Verify branch creation with `git branch --show-current`
- [ ] 0.5 Push branch to remote with `git push -u origin feature/019-refactor-videos-articles-shared-components`

### 1.0 Create RaindropPageBase Abstract Base Class

- [ ] 1.1 Create directory `src/redmuffin.Blazor.StaticWeb/Features/Common/` if it doesn't exist
- [ ] 1.2 Create file `src/redmuffin.Blazor.StaticWeb/Features/Common/RaindropPageBase.cs`
- [ ] 1.3 Add file-scoped namespace declaration: `namespace redmuffin.Blazor.StaticWeb.Features.Common;`
- [ ] 1.4 Add class declaration: `public abstract class RaindropPageBase : ComponentBase`
- [ ] 1.5 Add `using` directives: `Microsoft.AspNetCore.Components`, `Microsoft.JSInterop`, `Common.Raindrop`, `Core.ImagePlaceholder.Abstractions`, `Cache.Enums`, `Raindrop.Services`, `RaindropItems.Services`
- [ ] 1.6 Add protected field: `protected readonly Dictionary<string, string> ImageUrlCache`
- [ ] 1.7 Add protected property: `protected List<RaindropItem>? Items { get; set; }`
- [ ] 1.8 Add protected property: `protected string? ErrorMessage { get; set; }`
- [ ] 1.9 Add protected property: `protected bool IsLoading { get; set; }`
- [ ] 1.10 Add protected property: `protected RefreshBadgeState RefreshBadgeState { get; set; }`
- [ ] 1.11 Add protected property: `protected bool IsRefreshing { get; set; }`
- [ ] 1.12 Add inject property for `ILogger<T>` (use generic type parameter)
- [ ] 1.13 Add inject properties for services: `IJSRuntime`, `NavigationManager`, `IImagePlaceholderService`, `IImageValidationCacheService`, `IRaindropItemsCache`, `IRaindropAPI`
- [ ] 1.14 Declare abstract method: `protected abstract Task LoadItemsAsync(CancellationToken cancellationToken = default);`
- [ ] 1.15 Add virtual method: `protected virtual async Task PopulateImageUrlCacheAsync()` with implementation
- [ ] 1.16 Add virtual method: `protected virtual async Task HandleRefreshClickAsync()` with shared refresh logic
- [ ] 1.17 Add virtual method: `protected virtual string GetImageUrl(RaindropItem item)` using ImagePlaceholderService
- [ ] 1.18 Add virtual method: `protected virtual async Task HandleImageLoadAsync(string elementId, string itemLink, bool loadSuccess)`
- [ ] 1.19 Add static method: `protected static string DisplayTitle(RaindropItem item)` with fallback logic
- [ ] 1.20 Add static method: `protected static string DisplayExcerpt(RaindropItem item)` with truncation logic
- [ ] 1.21 Add virtual method: `protected virtual bool HasFallbackPlaceholder(RaindropItem item)`
- [ ] 1.22 Add virtual method: `protected virtual string GetFallbackReason(RaindropItem item)`
- [ ] 1.23 Build project to verify no compilation errors
- [ ] 1.24 Verify zero build warnings

### 2.0 Verify/Update RaindropItemCard Component (Already Exists from Previous Work)

**Note:** RaindropItemCard component was created during Phase 2 of PRD-018. Verify it meets requirements:

- [ ] 2.1 Verify directory exists: `src/redmuffin.Blazor.StaticWeb/Features/Common/Components/`
- [ ] 2.2 Open file `src/redmuffin.Blazor.StaticWeb/Features/Common/Components/RaindropItemCard.razor`
- [ ] 2.3 Verify `@using` directives for required namespaces are present
- [ ] 2.4 Verify card HTML structure has `card` div with dynamic CSS class
- [ ] 2.5 Verify card-divider has title display
- [ ] 2.6 Verify shimmer-placeholder div has image element
- [ ] 2.7 Verify lazy loading attributes on img tag (loading, fetchpriority, decoding)
- [ ] 2.8 Verify image load and error event handlers are present
- [ ] 2.9 Verify conditional fallback placeholder overlay exists
- [ ] 2.10 Verify card-section has excerpt, date, and action button
- [ ] 2.11 Open code-behind file `RaindropItemCard.razor.cs`
- [ ] 2.12 Verify `[Parameter]` properties: `Item`, `ItemType`, `Index`, `ImageUrlCache`, `OnImageLoad`
- [ ] 2.13 Add `[Inject] private IImagePlaceholderService ImagePlaceholderService { get; set; } = null!;` if not present
- [ ] 2.14 Add `[Inject] private IJSRuntime JsRuntime { get; set; } = null!;` if not present
- [ ] 2.15 Verify helper methods exist: `GetCardClass()`, `GetButtonIcon()`, `GetButtonText()`, `IsEagerLoad`
- [ ] 2.16 Verify static methods exist: `DisplayTitle()`, `DisplayExcerpt()`
- [ ] 2.17 Build project to verify component compiles correctly
- [ ] 2.18 Verify zero build warnings

### 3.0 Update Videos.razor

- [ ] 3.1 Open `src/redmuffin.Blazor.StaticWeb/Features/Pages/VideosPage/Videos.razor`
- [ ] 3.2 Add `@using` directive for `Features.Common.Components`
- [ ] 3.3 Locate the `@foreach` loop rendering video cards
- [ ] 3.4 Replace the entire card div with `<RaindropItemCard>` component
- [ ] 3.5 Set `Item="@video"` parameter
- [ ] 3.6 Set `ItemType="video"` parameter
- [ ] 3.7 Set `Index="@index"` parameter
- [ ] 3.8 Set `ImageUrlCache="@_imageUrlCache"` parameter
- [ ] 3.9 Set `OnImageLoad="@HandleImageLoadAsync"` parameter
- [ ] 3.10 Build project to verify Videos page compiles correctly

### 4.0 Update Videos.razor.cs

- [ ] 4.1 Open `src/redmuffin.Blazor.StaticWeb/Features/Pages/VideosPage/Videos.razor.cs`
- [ ] 4.2 Change class declaration from `public partial class Videos` to `public partial class Videos : RaindropPageBase`
- [ ] 4.3 Add `using` directive for `Features.Common`
- [ ] 4.4 Remove duplicate field declarations that are now in base class: `_imageUrlCache`, `_videoItems` (rename to `_items`), `_errorMessage`, `_refreshBadgeState`, `_isRefreshing`
- [ ] 4.5 Update all references from `_videoItems` to `Items`
- [ ] 4.6 Mark `LoadItemsAsync()` as `protected override` instead of `private`
- [ ] 4.7 In `LoadItemsAsync()`, use cache key `"Videos"` when calling cache service
- [ ] 4.8 In `LoadItemsAsync()`, call `await RaindropAPI.GetVideosAsync()` (not GetArticlesAsync)
- [ ] 4.9 Ensure `HandleRefreshClickAsync()` calls base class implementation
- [ ] 4.10 Remove duplicate `DisplayTitle()` method (keep in base or keep as override)
- [ ] 4.11 Remove duplicate `DisplayExcerpt()` method (keep in base or keep as override)
- [ ] 4.12 If `Videos.Logging.cs` exists, keep LoggerMessage delegates for page-specific logging
- [ ] 4.13 Build project to verify Videos page compiles correctly
- [ ] 4.14 Verify zero build warnings

### 5.0 Update Articles.razor

- [ ] 5.1 Open `src/redmuffin.Blazor.StaticWeb/Features/Pages/ArticlesPage/Articles.razor`
- [ ] 5.2 Add `@using` directive for `Features.Common.Components`
- [ ] 5.3 Locate the `@foreach` loop rendering article cards
- [ ] 5.4 Replace the entire card div with `<RaindropItemCard>` component
- [ ] 5.5 Set `Item="@article"` parameter
- [ ] 5.6 Set `ItemType="article"` parameter
- [ ] 5.7 Set `Index="@index"` parameter
- [ ] 5.8 Set `ImageUrlCache="@_imageUrlCache"` parameter
- [ ] 5.9 Set `OnImageLoad="@HandleImageLoadAsync"` parameter
- [ ] 5.10 Build project to verify Articles page compiles correctly

### 6.0 Update Articles.razor.cs

- [ ] 6.1 Open `src/redmuffin.Blazor.StaticWeb/Features/Pages/ArticlesPage/Articles.razor.cs`
- [ ] 6.2 Change class declaration from `public partial class Articles` to `public partial class Articles : RaindropPageBase`
- [ ] 6.3 Add `using` directive for `Features.Common`
- [ ] 6.4 Remove duplicate field declarations that are now in base class: `_imageUrlCache`, `_articleItems` (rename to `_items`), `_errorMessage`, `_isLoading`, `_refreshBadgeState`, `_isRefreshing`
- [ ] 6.5 Update all references from `_articleItems` to `Items`
- [ ] 6.6 Mark `LoadItemsAsync()` as `protected override` instead of `private`
- [ ] 6.7 In `LoadItemsAsync()`, use cache key `"Articles"` when calling cache service
- [ ] 6.8 In `LoadItemsAsync()`, call `await RaindropAPI.GetArticlesAsync()` (not GetVideosAsync)
- [ ] 6.9 Ensure `HandleRefreshClickAsync()` calls base class implementation
- [ ] 6.10 Remove duplicate `DisplayTitle()` method
- [ ] 6.11 Remove duplicate `DisplayExcerpt()` method
- [ ] 6.12 If `Articles.Logging.cs` exists, keep LoggerMessage delegates for page-specific logging
- [ ] 6.13 Build project to verify Articles page compiles correctly
- [ ] 6.14 Verify zero build warnings

### 7.0 Revert Mockdata to Original 5 Items

- [ ] 7.1 Open `src/redmuffin.Blazor.StaticWeb/wwwroot/mockdata/videos.json`
- [ ] 7.2 Remove all but the first 5 video items from the array
- [ ] 7.3 Verify JSON is still valid with proper closing brackets
- [ ] 7.4 Open `src/redmuffin.Blazor.StaticWeb/wwwroot/mockdata/articles.json`
- [ ] 7.5 Remove all but the first 5 article items from the array
- [ ] 7.6 Verify JSON is still valid with proper closing brackets
- [ ] 7.7 Run the application and verify both pages load with 5 items each
- [ ] 7.8 Run tests to ensure mockdata changes don't break existing tests

### 8.0 Create RaindropItemCardTests

- [ ] 8.1 Create directory `tests/redmuffin.Blazor.StaticWeb.Tests/Features/Common/Components/` if it doesn't exist
- [ ] 8.2 Create file `RaindropItemCardTests.cs`
- [ ] 8.3 Add `[Category("Feature:Common")]` and `[Category("Unit")]` attributes
- [ ] 8.4 Create test method `Component_Renders_Correctly_With_Video_Type`
- [ ] 8.5 Create test method `Component_Renders_Correctly_With_Article_Type`
- [ ] 8.6 Create test method `Component_Applies_Correct_CSS_Class_Based_On_ItemType`
- [ ] 8.7 Create test method `Component_Shows_Correct_Button_Icon_For_Video`
- [ ] 8.8 Create test method `Component_Shows_Correct_Button_Icon_For_Article`
- [ ] 8.9 Create test method `Component_Shows_Correct_Button_Text_For_Video`
- [ ] 8.10 Create test method `Component_Shows_Correct_Button_Text_For_Article`
- [ ] 8.11 Create test method `Component_Implements_Lazy_Loading_Attributes_Correctly`
- [ ] 8.12 Create file `RaindropItemCardTests.Helpers.cs` with test scope and helper methods
- [ ] 8.13 Create file `RaindropItemCardTests.EdgeCases.cs` with error handling tests
- [ ] 8.14 Run RaindropItemCard tests: `dotnet test --filter "FullyQualifiedName~RaindropItemCardTests"`

### 9.0 Update VideosTests

- [ ] 9.1 Open `tests/redmuffin.Blazor.StaticWeb.Tests/Features/Pages/VideosPage/VideosTests.cs`
- [ ] 9.2 Add test method `Videos_Page_Inherits_From_RaindropPageBase`
- [ ] 9.3 Add test method `Videos_Page_Renders_RaindropItemCard_Components`
- [ ] 9.4 Update existing tests to work with refactored structure
- [ ] 9.5 Add test method `Videos_Refresh_Sets_IsRefreshing_State`
- [ ] 9.6 Add test method `Videos_Refresh_Updates_Items_On_Success`
- [ ] 9.7 Update `VideosTests.Helpers.cs` with new helper methods if needed
- [ ] 9.8 Run Videos tests: `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Videos]"`
- [ ] 9.9 Ensure all existing tests still pass after refactoring

### 10.0 Update ArticlesTests

- [ ] 10.1 Open `tests/redmuffin.Blazor.StaticWeb.Tests/Features/Pages/ArticlesPage/ArticlesTests.cs`
- [ ] 10.2 Add test method `Articles_Page_Inherits_From_RaindropPageBase`
- [ ] 10.3 Add test method `Articles_Page_Renders_RaindropItemCard_Components`
- [ ] 10.4 Update existing tests to work with refactored structure
- [ ] 10.5 Add test method `Articles_Refresh_Sets_IsRefreshing_State`
- [ ] 10.6 Add test method `Articles_Refresh_Updates_Items_On_Success`
- [ ] 10.7 Update `ArticlesTests.Helpers.cs` with new helper methods if needed
- [ ] 10.8 Run Articles tests: `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Articles]"`
- [ ] 10.9 Ensure all existing tests still pass after refactoring

### 11.0 Final Verification

- [ ] 11.1 Run full test suite: `dotnet test`
- [ ] 11.2 Verify all 258+ tests pass
- [ ] 11.3 Run build with zero warnings: `dotnet build --verbosity quiet`
- [ ] 11.4 Verify no build warnings (except IL2111)
- [ ] 11.5 Test Videos page manually in browser
- [ ] 11.6 Test Articles page manually in browser
- [ ] 11.7 Verify lazy loading works (scroll down, images load)
- [ ] 11.8 Verify refresh functionality works on both pages
- [ ] 11.9 Check visual appearance matches original (no regressions)
- [ ] 11.10 Create PR with description of changes

---

## Success Criteria

Before marking this task list complete, verify:

1. **Code Duplication**: Reduced from ~100 lines to 0 lines
2. **Test Coverage**: 100% for `RaindropItemCard` and `RaindropPageBase`
3. **Build Warnings**: 0 warnings (except IL2111)
4. **Test Pass Rate**: 100% (258+ tests)
5. **Performance**: Maintained (lazy loading still works)
6. **Functionality**: No regressions in Videos or Articles pages

---

## Notes

- Keep existing LoggerMessage delegates in each page's Logging.cs file (if exists)
- Maintain backward compatibility with existing cache keys
- Do not modify SCSS files - visual design should remain unchanged
- The `RaindropItemCard` component should be reusable for future item types
