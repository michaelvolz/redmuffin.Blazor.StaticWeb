# Tasks: PRD-014 Blazor WebAssembly Loading Time Optimization

## Relevant Files

- `src/redmuffin.Blazor.StaticWeb/redmuffin.Blazor.StaticWeb.csproj` - Main Blazor project file (needs AOT, trimming config)
- `src/redmuffin.Blazor.StaticWeb/App.razor` - Router component (needs lazy loading setup)
- `src/redmuffin.Blazor.StaticWeb/wwwroot/index.html` - Entry HTML (needs preload/prefetch hints)
- `src/redmuffin.Blazor.StaticWeb/TrimmerRoots.xml` - NEW FILE - Trimming descriptor
- `Directory.Build.props` - Global build settings (review and optimize)
- `Directory.Packages.props` - Package versions (evaluate dependencies)

### Notes

- Build configuration changes only affect Release mode; Debug builds remain fast
- Test thoroughly after enabling full trimming to catch runtime errors early
- Use Chrome DevTools Network tab to measure actual transfer sizes

## Instructions for Completing Tasks

**IMPORTANT:** As you complete each task, you must check it off in this markdown file by changing `- [ ]` to `- [x]`. This helps track progress and ensures you don't skip any steps.

Example:

- `- [ ] 1.1 Read file` → `- [x] 1.1 Read file` (after completing)

Update the file after completing each sub-task, not just after completing an entire parent task.

## Tasks

- [x] 0.0 Create feature branch
  - [x] 0.1 Create and checkout a new branch: `git checkout -b feature/PRD-014-wasm-loading-optimization`
  - [x] 0.2 Verify you're on the new branch: `git branch`

- [x] 1.0 Enable AOT Compilation (FR-001)
  - [x] 1.1 Read current `src/redmuffin.Blazor.StaticWeb/redmuffin.Blazor.StaticWeb.csproj`
  - [x] 1.2 Add `<RunAOTCompilation>true</RunAOTCompilation>` to the Release configuration PropertyGroup (around line 33)
  - [x] 1.3 Save the file
  - [x] 1.4 Run Release build to verify: `dotnet build -c Release`
  - [x] 1.5 If build fails, check for AOT-related errors and research solutions
  - [x] 1.6 Run tests: `dotnet test`
  - [x] 1.7 Document: Note the build time increase (AOT compilation takes longer)

- [x] 2.0 Implement Aggressive Trimming (FR-002)
  - [x] 2.1 Read current `src/redmuffin.Blazor.StaticWeb/redmuffin.Blazor.StaticWeb.csproj`
  - [x] 2.2 Change `<TrimMode>partial</TrimMode>` to `<TrimMode>full</TrimMode>` in Release configuration
  - [x] 2.3 Create new file `src/redmuffin.Blazor.StaticWeb/TrimmerRoots.xml` with essential preserved types
  - [x] 2.4 Add TrimmerRoots.xml content to preserve Blazor components and essential types
  - [x] 2.5 Save TrimmerRoots.xml
  - [x] 2.6 Build in Release mode: `dotnet build -c Release`
  - [x] 2.7 Run full test suite: `dotnet test`
  - [x] 2.8 Manually test critical features: navigation, authentication, API calls, markdown rendering
  - [x] 2.9 If runtime errors occur, add missing types to TrimmerRoots.xml and rebuild
  - [x] 2.10 Repeat 2.8-2.9 until all features work correctly

- [ ] 3.0 Configure Lazy Loading (FR-003)
  - [ ] 3.1 Analyze project structure to identify assemblies for lazy loading (look in Features/ folder)
  - [ ] 3.2 Read `src/redmuffin.Blazor.StaticWeb/App.razor`
  - [ ] 3.3 Add `LazyAssemblyLoader` injection to App.razor
  - [ ] 3.4 Create `OnNavigateAsync` method in App.razor.cs
  - [ ] 3.5 Configure lazy loading for 2-3 feature assemblies (e.g., Articles, Videos, Weather)
  - [ ] 3.6 Update project file to mark assemblies as lazy-loaded using `<BlazorWebAssemblyLazyLoad>`
  - [ ] 3.7 Test lazy loading: navigate to lazy-loaded pages and verify they load
  - [ ] 3.8 Test error handling when lazy loading fails
  - [ ] 3.9 Verify initial bundle size is reduced

- [x] 4.0 Optimize Resource Loading (FR-004)
  - [x] 4.1 Read `src/redmuffin.Blazor.StaticWeb/wwwroot/index.html`
  - [x] 4.2 Add `<link rel="preload">` for critical CSS files (foundation-root.min.css, app.min.css)
  - [x] 4.3 Add `<link rel="preload">` for blazor.webassembly.js
  - [x] 4.4 Add `<link rel="prefetch">` for non-critical resources (font files, external scripts)
  - [x] 4.5 Verify proper `crossorigin` attributes are set for external resources
  - [x] 4.6 Save index.html
  - [x] 4.7 Test in browser and verify resources are preloaded (check Network tab)
  - [x] 4.8 Run Lighthouse audit and document performance score

- [ ] 5.0 Disable Unnecessary Runtime Features (FR-005)
  - [ ] 5.1 Read `Directory.Build.props` to review current settings
  - [ ] 5.2 Add `<BlazorEnableTimeZoneSupport>false</BlazorEnableTimeZoneSupport>` to Release configuration
  - [ ] 5.3 Add `<EventSourceSupport>false</EventSourceSupport>` to Release configuration
  - [ ] 5.4 Verify `HttpActivityPropagationSupport` is not explicitly enabled (should be false by default)
  - [ ] 5.5 Save Directory.Build.props
  - [ ] 5.6 Build in Release mode: `dotnet build -c Release`
  - [ ] 5.7 Test application thoroughly to ensure no timezone/EventSource functionality is broken
  - [ ] 5.8 Document which features were disabled

- [ ] 6.0 Evaluate and Optimize Dependencies (FR-006)
  - [ ] 6.1 Run `dotnet list src/redmuffin.Blazor.StaticWeb/redmuffin.Blazor.StaticWeb.csproj package --include-transitive`
  - [ ] 6.2 Document all packages and their versions
  - [ ] 6.3 Research bundle size impact of Markdig (is it worth keeping?)
  - [ ] 6.4 Check if Microsoft.AspNetCore.WebUtilities is actually used
  - [ ] 6.5 Review Microsoft.Extensions.Http usage patterns
  - [ ] 6.6 Identify any unused dependencies
  - [ ] 6.7 Remove unused packages from project file
  - [ ] 6.8 If Markdig is rarely used, create plan to move markdown processing server-side (future PRD)
  - [ ] 6.9 Document size impact analysis

- [ ] 7.0 Document Metrics and Results
  - [ ] 7.1 Measure baseline bundle size before optimizations: check `_framework` folder size in publish output
  - [ ] 7.2 Build Release after all changes: `dotnet publish -c Release`
  - [ ] 7.3 Measure final bundle size: check `_framework` folder size
  - [ ] 7.4 Calculate size reduction percentage
  - [ ] 7.5 Test loading time in Chrome DevTools (throttle to Slow 3G)
  - [ ] 7.6 Document before/after metrics in a comment on this task list
  - [ ] 7.7 Run Lighthouse audit and save results
  - [ ] 7.8 Update PRD-014 with actual results

## Metrics Tracking

Use this section to document your measurements:

### Before Optimizations

- Bundle Size (bytes): \_\_\_
- Bundle Size (MB): \_\_\_
- Lighthouse Performance Score: \_\_\_
- Time to Interactive (Slow 3G): \_\_\_

### After Optimizations

- Bundle Size (bytes): \_\_\_
- Bundle Size (MB): \_\_\_
- Lighthouse Performance Score: \_\_\_
- Time to Interactive (Slow 3G): \_\_\_

### Size Reduction

- Absolute Reduction: \_\_\_ MB
- Percentage Reduction: \_\_\_%

## Acceptance Criteria Checklist

- [ ] AOT compilation enabled and working (FR-001)
- [ ] Full trimming mode enabled (FR-002)
- [ ] TrimmerRoots.xml created and configured (FR-002)
- [ ] At least 2 assemblies lazy-loaded (FR-003)
- [ ] Resource hints added to index.html (FR-004)
- [ ] Unused runtime features disabled (FR-005)
- [ ] Dependency evaluation completed (FR-006)
- [ ] All tests passing
- [ ] Manual testing confirms all features work
- [ ] Metrics documented with before/after comparison
- [ ] Lighthouse score improved
