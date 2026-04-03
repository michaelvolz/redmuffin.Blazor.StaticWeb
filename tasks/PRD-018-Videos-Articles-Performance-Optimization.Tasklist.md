## Relevant Files

### Source Files

- `src/redmuffin.Blazor.StaticWeb/Features/Pages/VideosPage/Videos.razor` - Main Videos page component (Phase 1 & 2 updates)
- `src/redmuffin.Blazor.StaticWeb/Features/Pages/VideosPage/Videos.razor.cs` - Videos page logic (may need minor updates)
- `src/redmuffin.Blazor.StaticWeb/Features/Pages/ArticlesPage/Articles.razor` - Main Articles page component (Phase 1 & 2 updates)
- `src/redmuffin.Blazor.StaticWeb/Features/Pages/ArticlesPage/Articles.razor.cs` - Articles page logic (may need minor updates)
- `src/redmuffin.Blazor.StaticWeb/Features/Shared/Components/RaindropItemCard.razor` - NEW: Shared card component
- `src/redmuffin.Blazor.StaticWeb/Features/Shared/Components/RaindropItemCard.razor.cs` - NEW: Shared card component logic
- `src/redmuffin.Blazor.StaticWeb/scss/components/_masonry.scss` - Masonry layout styles (add CSS containment)
- `src/redmuffin.Blazor.StaticWeb/scss/components/_card.scss` - Card component styles (create or update)

### Test Files

- `tests/redmuffin.Blazor.StaticWeb.Tests/Features/Pages/VideosPage/VideosTests.cs` - Videos page unit tests
- `tests/redmuffin.Blazor.StaticWeb.Tests/Features/Pages/ArticlesPage/ArticlesTests.cs` - Articles page unit tests
- `tests/redmuffin.Blazor.StaticWeb.Tests/Features/Shared/Components/RaindropItemCardTests.cs` - NEW: Shared component tests
- `tests/redmuffin.Blazor.StaticWeb.Tests/Features/Shared/Components/RaindropItemCardTests.Helpers.cs` - NEW: Test helpers

### Configuration

- `tasks/PRD-018-Videos-Articles-Performance-Optimization.md` - The PRD document

### Notes

- Unit tests should typically be placed alongside the code files they are testing
- Use `dotnet test` to run all tests, or `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Videos]"` for feature-specific tests
- After any C# changes, run `dotnet build --verbosity quiet` to check for warnings
- CSS changes require SCSS compilation - check if there's a build script for this

## Instructions for Completing Tasks

**IMPORTANT:** As you complete each task, you must check it off in this markdown file by changing `- [ ]` to `- [x]`. This helps track progress and ensures you don't skip any steps.

Update the file after completing each sub-task, not just after completing an entire parent task.

## Tasks

### Setup

- [ ] 0.0 Create feature branch
  - [ ] 0.1 Create and checkout a new branch: `git checkout -b feature/018-videos-articles-performance-optimization`
  - [ ] 0.2 Verify branch is clean with `git status`
  - [ ] 0.3 Run initial build to ensure baseline is stable: `dotnet build`

### Phase 1: Native Lazy Loading

- [ ] 1.0 Implement lazy loading in Videos.razor
  - [ ] 1.1 Read current Videos.razor file to understand structure
  - [ ] 1.2 Add index variable to foreach loop: `@foreach (var (video, index) in _videoItems.Select((v, i) => (v, i)))`
  - [ ] 1.3 Update img tag with loading attribute: `loading="@(index < 6 ? "eager" : "lazy")"`
  - [ ] 1.4 Add fetchpriority attribute: `fetchpriority="@(index < 6 ? "high" : "low")"`
  - [ ] 1.5 Add decoding attribute: `decoding="async"`
  - [ ] 1.6 Build project: `dotnet build --verbosity quiet`
  - [ ] 1.7 Run Videos tests: `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Videos]"`
  - [ ] 1.8 Verify no build warnings

- [ ] 2.0 Implement lazy loading in Articles.razor
  - [ ] 2.1 Read current Articles.razor file to understand structure
  - [ ] 2.2 Add index variable to foreach loop: `@foreach (var (article, index) in _articleItems.Select((v, i) => (v, i)))`
  - [ ] 2.3 Update img tag with loading attribute: `loading="@(index < 6 ? "eager" : "lazy")"`
  - [ ] 2.4 Add fetchpriority attribute: `fetchpriority="@(index < 6 ? "high" : "low")"`
  - [ ] 2.5 Add decoding attribute: `decoding="async"`
  - [ ] 2.6 Build project: `dotnet build --verbosity quiet`
  - [ ] 2.7 Run Articles tests: `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Articles]"`
  - [ ] 2.8 Verify no build warnings

- [ ] 3.0 Add CSS performance optimizations
  - [ ] 3.1 Read current \_masonry.scss file
  - [ ] 3.2 Add `contain: content` to `.card` class
  - [ ] 3.3 Add `content-visibility: auto` to `.card` class
  - [ ] 3.4 Add `contain-intrinsic-size: 0 350px` as estimated size hint
  - [ ] 3.5 Check if \_card.scss exists, create if needed
  - [ ] 3.6 Compile SCSS (check for compilation script or use `dotnet build` if integrated)
  - [ ] 3.7 Verify CSS output includes new properties

- [ ] 4.0 Phase 1 Testing & Validation
  - [ ] 4.1 Run Smoke tests: `dotnet test -- --treenode-filter "/*/*/*/*[Category=Smoke]"`
  - [ ] 4.2 Manual browser test: Open Videos page, check Network tab for lazy loading
  - [ ] 4.3 Manual browser test: Open Articles page, check Network tab for lazy loading
  - [ ] 4.4 Verify shimmer loading still works correctly
  - [ ] 4.5 Verify images load as you scroll down
  - [ ] 4.6 Commit Phase 1 changes: `git add . && git commit -m "feat: add native lazy loading to Videos and Articles pages"`

### Phase 2: Shared Component & Virtualization

- [ ] 5.0 Create RaindropItemCard shared component
  - [ ] 5.1 Create directory: `src/redmuffin.Blazor.StaticWeb/Features/Shared/Components/`
  - [ ] 5.2 Create RaindropItemCard.razor file with basic structure
  - [ ] 5.3 Add component parameters: Item, ItemType, ImageUrlCache, OnImageLoad callback
  - [ ] 5.4 Add conditional rendering for button icon and text based on ItemType
  - [ ] 5.5 Add conditional CSS class (video-card vs article-card)
  - [ ] 5.6 Implement shimmer placeholder div with id="shimmer-@Item.Id"
  - [ ] 5.7 Implement img tag with lazy loading attributes
  - [ ] 5.8 Implement fallback placeholder overlay
  - [ ] 5.9 Build project: `dotnet build --verbosity quiet`
  - [ ] 5.10 Verify no build warnings

- [ ] 6.0 Create RaindropItemCard code-behind
  - [ ] 6.1 Create RaindropItemCard.razor.cs file
  - [ ] 6.2 Add DisplayTitle method
  - [ ] 6.3 Add DisplayExcerpt method
  - [ ] 6.4 Add HandleImageLoad method that invokes the callback
  - [ ] 6.5 Inject required services via component parameters (not [Inject])
  - [ ] 6.6 Build project: `dotnet build --verbosity quiet`
  - [ ] 6.7 Verify no build warnings

- [ ] 7.0 Refactor Videos.razor to use Virtualize and RaindropItemCard
  - [ ] 7.1 Read current Videos.razor structure
  - [ ] 7.2 Add `@using Microsoft.AspNetCore.Components.Web.Virtualization` directive
  - [ ] 7.3 Add `@using redmuffin.Blazor.StaticWeb.Features.Shared.Components` directive
  - [ ] 7.4 Replace foreach loop with `<Virtualize>` component
  - [ ] 7.5 Set Virtualize parameters: Items="@\_videoItems", ItemSize="400", OverscanCount="3"
  - [ ] 7.6 Replace inline card HTML with `<RaindropItemCard>` component
  - [ ] 7.7 Set RaindropItemCard parameters: Item="@video", ItemType="video", ImageUrlCache="@\_imageUrlCache", OnImageLoad="@HandleCardImageLoad"
  - [ ] 7.8 Add Placeholder template inside Virtualize with shimmer div
  - [ ] 7.9 Create HandleCardImageLoad method in Videos.razor.cs to forward to ImagePlaceholderService
  - [ ] 7.10 Build project: `dotnet build --verbosity quiet`
  - [ ] 7.11 Run Videos tests: `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Videos]"`

- [ ] 8.0 Refactor Articles.razor to use Virtualize and RaindropItemCard
  - [ ] 8.1 Read current Articles.razor structure
  - [ ] 8.2 Add `@using Microsoft.AspNetCore.Components.Web.Virtualization` directive
  - [ ] 8.3 Add `@using redmuffin.Blazor.StaticWeb.Features.Shared.Components` directive
  - [ ] 8.4 Replace foreach loop with `<Virtualize>` component
  - [ ] 8.5 Set Virtualize parameters: Items="@\_articleItems", ItemSize="400", OverscanCount="3"
  - [ ] 8.6 Replace inline card HTML with `<RaindropItemCard>` component
  - [ ] 8.7 Set RaindropItemCard parameters: Item="@article", ItemType="article", ImageUrlCache="@\_imageUrlCache", OnImageLoad="@HandleCardImageLoad"
  - [ ] 8.8 Add Placeholder template inside Virtualize with shimmer div
  - [ ] 8.9 Create HandleCardImageLoad method in Articles.razor.cs
  - [ ] 8.10 Build project: `dotnet build --verbosity quiet`
  - [ ] 8.11 Run Articles tests: `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Articles]"`

### Testing & Quality Assurance

- [ ] 9.0 Create RaindropItemCard unit tests
  - [ ] 9.1 Create RaindropItemCardTests.cs file
  - [ ] 9.2 Write test: Component renders with video type
  - [ ] 9.3 Write test: Component renders with article type
  - [ ] 9.4 Write test: DisplayTitle shows correct title or fallback
  - [ ] 9.5 Write test: DisplayExcerpt truncates correctly
  - [ ] 9.6 Write test: Button has correct icon and text for video
  - [ ] 9.7 Write test: Button has correct icon and text for article
  - [ ] 9.8 Write test: CSS class is applied correctly
  - [ ] 9.9 Write test: Image load callback is invoked
  - [ ] 9.10 Run new tests: `dotnet test -- --treenode-filter "/*/*/*/*[Category=Feature:Shared]"`

- [ ] 10.0 Update existing page tests
  - [ ] 10.1 Review VideosTests.cs for breaking changes
  - [ ] 10.2 Update test helpers if needed for Virtualize component
  - [ ] 10.3 Review ArticlesTests.cs for breaking changes
  - [ ] 10.4 Update test helpers if needed
  - [ ] 10.5 Run full Videos test suite
  - [ ] 10.6 Run full Articles test suite
  - [ ] 10.7 Fix any failing tests

- [ ] 11.0 Integration testing
  - [ ] 11.1 Run all Smoke tests: `dotnet test -- --treenode-filter "/*/*/*/*[Category=Smoke]"`
  - [ ] 11.2 Run all tests: `dotnet test`
  - [ ] 11.3 Verify zero build warnings: `dotnet build --verbosity quiet`

### Performance Validation

- [ ] 12.0 Performance benchmarking
  - [ ] 12.1 Open Videos page in Chrome DevTools
  - [ ] 12.2 Record Network tab: Initial payload size
  - [ ] 12.3 Record Network tab: Number of initial image requests
  - [ ] 12.4 Run Lighthouse audit on Videos page
  - [ ] 12.5 Record LCP (Largest Contentful Paint) score
  - [ ] 12.6 Record TTFP (Time to First Paint) score
  - [ ] 12.7 Repeat steps 12.1-12.6 for Articles page
  - [ ] 12.8 Compare metrics against PRD targets (60-70% improvement)
  - [ ] 12.9 Test scrolling performance on mobile device
  - [ ] 12.10 Verify smooth 60fps scrolling

### Documentation & Cleanup

- [ ] 13.0 Code cleanup and final review
  - [ ] 13.1 Review all modified files for code quality
  - [ ] 13.2 Ensure consistent code style (check .editorconfig)
  - [ ] 13.3 Remove any commented-out code
  - [ ] 13.4 Verify all TODO comments are addressed
  - [ ] 13.5 Check for unused using statements
  - [ ] 13.6 Run final build: `dotnet build --verbosity quiet`
  - [ ] 13.7 Run final test suite: `dotnet test`

- [ ] 14.0 Commit and merge preparation
  - [ ] 14.1 Stage all changes: `git add .`
  - [ ] 14.2 Commit with conventional commit message: `git commit -m "feat(performance): implement lazy loading and virtualization for Videos and Articles pages"`
  - [ ] 14.3 Push branch: `git push origin feature/018-videos-articles-performance-optimization`
  - [ ] 14.4 Create PR description summarizing changes
  - [ ] 14.5 Reference PRD-018 in PR description
  - [ ] 14.6 List performance improvements in PR description

## Post-Implementation Verification

After merging, verify in production:

- [ ] 15.0 Production verification
  - [ ] 15.1 Deploy to staging environment
  - [ ] 15.2 Run performance tests on staging
  - [ ] 15.3 Deploy to production
  - [ ] 15.4 Monitor Core Web Vitals in Google Search Console
  - [ ] 15.5 Verify no increase in error rates
  - [ ] 15.6 Confirm user feedback is positive
