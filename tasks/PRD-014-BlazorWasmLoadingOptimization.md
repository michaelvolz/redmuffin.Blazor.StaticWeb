# PRD-014: Blazor WebAssembly Loading Time Optimization

## Introduction/Overview

**Problem Statement:**
The current Blazor WebAssembly application has an excessively large bundle size that negatively impacts user experience, particularly for visitors on slower connections or mobile devices. The download size causes extended waiting periods before the application becomes interactive.

**Goal:**
Optimize the Blazor WebAssembly application's loading performance by implementing industry best practices for bundle size reduction, compilation optimization, and resource loading. This includes analyzing and fixing framework configuration gaps, enabling AOT compilation, and implementing lazy loading strategies.

**Scope:**
This PRD covers all technical optimizations related to reducing bundle size and improving loading times for the Blazor WebAssembly application deployed to Azure Static Web Apps.

---

## Goals

1. **Enable Ahead-of-Time (AOT) Compilation** - Compile .NET code directly to WebAssembly for better runtime performance and size optimization
2. **Implement Aggressive Trimming** - Remove unused code and dependencies to minimize bundle size
3. **Enable Lazy Loading** - Defer loading of non-critical assemblies until needed
4. **Optimize Resource Loading** - Implement preloading, prefetching, and compression strategies
5. **Reduce Dependency Size** - Evaluate and minimize NuGet package overhead
6. **Achieve Measurable Improvement** - Document before/after metrics for bundle size and loading time

---

## User Stories

1. **As a** mobile user on a slow connection, **I want** the website to load quickly, **so that** I don't abandon the site before it becomes interactive.

2. **As a** first-time visitor, **I want** to see content quickly, **so that** I have a positive initial experience with the site.

3. **As a** returning visitor, **I want** subsequent visits to load instantly from cache, **so that** I can access content without waiting.

4. **As a** developer, **I want** the application to use modern WebAssembly optimizations, **so that** it performs as well as other modern web applications.

---

## Current State Analysis

### Configuration Gaps Identified

Based on analysis of the current codebase, the following critical optimizations are **MISSING**:

#### Critical Missing Optimizations

1. **No AOT Compilation** (`RunAOTCompilation`)
   - **Current State:** Only enabled in test projects, not in main Blazor app
   - **Impact:** Runtime performance degradation and larger bundle size
   - **Location:** `src/redmuffin.Blazor.StaticWeb/redmuffin.Blazor.StaticWeb.csproj`

2. **Partial Trimming Mode** (`TrimMode`)
   - **Current State:** `TrimMode=partial` in Release
   - **Impact:** Not removing all unused code
   - **Better Option:** `TrimMode=full` for aggressive size reduction

3. **No Lazy Loading Configuration**
   - **Current State:** All assemblies loaded upfront
   - **Impact:** Unnecessary download of code not needed for initial page

4. **Missing WebAssembly Optimizations**
   - `WasmStripILAfterAOT` - Not explicitly configured in Blazor project
   - `BlazorWebAssemblyLoadAllAssembliesOnFirstRender` - Defaults may not be optimal
   - `BlazorEnableCompression` - Relies on defaults

#### Current Configuration (Good)

The following optimizations are already properly configured:

- ✅ `WasmEnableSIMD=true` (Release mode)
- ✅ `PublishTrimmed=true` (Release mode)
- ✅ `InvariantGlobalization=true` (Release mode)
- ✅ `UseSystemResourceKeys=true` (Release mode)
- ✅ `BlazorWebAssemblyPreserveCollationData=false` (Release mode)
- ✅ `WasmStripILAfterAOT=true` (Release mode - in Directory.Build.props)
- ✅ Webcil packaging format (default in .NET 9)
- ✅ Brotli/Gzip compression (enabled by default)

### Current Dependencies

Key NuGet packages that contribute to bundle size:

- `Markdig` (1.1.2) - Markdown processing library
- `Microsoft.AspNetCore.Components.Authorization` (9.0.14)
- `Microsoft.AspNetCore.WebUtilities` (9.0.14)
- `Microsoft.Extensions.Http` (9.0.14)
- `Blazored.LocalStorage` (4.5.0)
- `LZStringCSharp` (1.4.0)

### Project Structure

```
src/redmuffin.Blazor.StaticWeb/
├── redmuffin.Blazor.StaticWeb.csproj    # Main project file - needs AOT
├── Directory.Build.props                # Global build settings - good config
├── wwwroot/
│   ├── index.html                       # Needs resource hints optimization
│   └── css/                            # Minified files present
```

---

## Functional Requirements

### FR-001: Enable AOT Compilation

**Priority:** High

The system **must** enable Ahead-of-Time (AOT) compilation for the Blazor WebAssembly application in Release mode.

**Implementation:**

- Add `<RunAOTCompilation>true</RunAOTCompilation>` to the Release configuration
- Verify AOT compilation works with existing trimming settings
- Test that runtime performance improves

**Acceptance Criteria:**

- [ ] `RunAOTCompilation` is set to `true` in Release configuration
- [ ] Build succeeds without errors
- [ ] Application runs correctly after AOT compilation
- [ ] Bundle size is measured before and after

### FR-002: Implement Aggressive Trimming

**Priority:** High

The system **must** switch from partial to full trimming mode to maximize bundle size reduction.

**Implementation:**

- Change `TrimMode` from `partial` to `full` in Release configuration
- Create `TrimmerRoots.xml` descriptor file to preserve essential types
- Test thoroughly to ensure no runtime errors from over-trimming

**Acceptance Criteria:**

- [ ] `TrimMode` is set to `full` in Release configuration
- [ ] `TrimmerRoots.xml` is created and configured
- [ ] All existing functionality works correctly
- [ ] No `NullReferenceException` or missing type errors occur

### FR-003: Configure Lazy Loading

**Priority:** High

The system **must** implement lazy loading for assemblies that are not required on initial page load.

**Implementation:**

- Identify assemblies that can be loaded on-demand (e.g., heavy feature pages)
- Configure `LazyAssemblyLoader` in `App.razor` or router
- Use `@using` statements with `OnNavigateAsync` for dynamic loading
- Document which assemblies are lazy-loaded

**Acceptance Criteria:**

- [ ] At least 2 assemblies are configured for lazy loading
- [ ] Initial bundle size is reduced
- [ ] Lazy-loaded assemblies load correctly when accessed
- [ ] User experience remains smooth during lazy loading

### FR-004: Optimize Resource Loading

**Priority:** Medium

The system **must** add resource hints to `index.html` for critical resources.

**Implementation:**

- Add `<link rel="preload">` for critical CSS and JavaScript
- Add `<link rel="prefetch">` for resources needed on next navigation
- Ensure proper `crossorigin` attributes for external resources
- Consider adding `fetchpriority` hints for critical resources

**Acceptance Criteria:**

- [ ] Critical CSS files are preloaded
- [ ] Blazor WebAssembly JavaScript is preloaded
- [ ] External fonts and icons are prefetched
- [ ] Lighthouse performance score improves

### FR-005: Disable Unnecessary Features

**Priority:** Medium

The system **should** disable runtime features not used by the application.

**Implementation:**

- Add `BlazorEnableTimeZoneSupport` set to `false` (if not using timezones)
- Set `EventSourceSupport` to `false`
- Verify `HttpActivityPropagationSupport` is disabled if not using distributed tracing
- Document any features intentionally disabled

**Acceptance Criteria:**

- [ ] Unused runtime features are disabled
- [ ] Application functionality is verified after disabling
- [ ] Bundle size reduction is documented

### FR-006: Evaluate and Optimize Dependencies

**Priority:** Medium

The system **should** review and optimize NuGet package usage.

**Implementation:**

- Analyze each NuGet package's contribution to bundle size
- Consider alternatives for heavy packages (e.g., Markdig)
- Remove unused dependencies
- Document size impact of each major dependency

**Acceptance Criteria:**

- [ ] Dependency analysis document is created
- [ ] Unused packages are removed
- [ ] Heavy packages are evaluated for alternatives
- [ ] Size impact of each optimization is measured

### FR-007: Implement Build-Time Measurements

**Priority:** Low

The system **should** add automated bundle size measurement to the build process.

**Implementation:**

- Create PowerShell or MSBuild target to measure `_framework` folder size
- Log size metrics during CI/CD builds
- Set up alerts if size exceeds threshold

**Acceptance Criteria:**

- [ ] Build script measures and logs bundle size
- [ ] Size metrics are visible in build output
- [ ] Documentation explains how to interpret metrics

---

## Non-Goals (Out of Scope)

1. **PWA/Service Worker Implementation** - This PRD focuses on loading optimization, not offline capabilities
2. **Server-Side Rendering (SSR)** - Application remains pure client-side Blazor WASM
3. **CDN Migration** - Static file hosting remains on Azure Static Web Apps
4. **Code Splitting at Component Level** - Focus is on assembly-level lazy loading
5. **Image Optimization** - Out of scope for this bundle-size focused effort
6. **API Response Optimization** - Focus is on client-side bundle only

---

## Technical Considerations

### Framework Version

- **Current:** .NET 9.0.305
- **Benefits:** Webcil packaging is default, latest optimizations available

### Build Configurations

The project has three configurations:

- **Debug:** Fast builds, optimizations disabled
- **Debug-Sass:** Includes SCSS compilation
- **Release:** Production optimizations (target for all changes)

### Key Files to Modify

1. `src/redmuffin.Blazor.StaticWeb/redmuffin.Blazor.StaticWeb.csproj`
2. `src/redmuffin.Blazor.StaticWeb/wwwroot/index.html`
3. `src/redmuffin.Blazor.StaticWeb/App.razor` (for lazy loading)
4. New file: `src/redmuffin.Blazor.StaticWeb/TrimmerRoots.xml`

### Risks and Mitigations

| Risk                              | Impact | Mitigation                            |
| --------------------------------- | ------ | ------------------------------------- |
| AOT compilation breaks build      | High   | Test in separate branch first         |
| Full trimming removes needed code | High   | Create comprehensive TrimmerRoots.xml |
| Lazy loading causes UX delays     | Medium | Load assemblies in background         |
| Performance regression            | Medium | Benchmark before/after                |

### Testing Strategy

1. **Build Verification:** Ensure Release build succeeds
2. **Functional Testing:** All features work correctly
3. **Size Measurement:** Compare bundle sizes
4. **Performance Testing:** Measure Time-to-Interactive
5. **Cross-Browser:** Test on Chrome, Firefox, Safari, Edge

---

## Success Metrics

### Primary Metrics

1. **Bundle Size Reduction**
   - Measure total size of `_framework` folder
   - Target: 20-50% reduction from baseline
   - Tool: PowerShell script or `du -sh`

2. **Initial Load Time**
   - Measure Time-to-Interactive (TTI)
   - Tool: Chrome DevTools Performance tab
   - Tool: `page-load-timing.js` already in project

3. **Compressed Transfer Size**
   - Measure actual bytes transferred with Brotli/Gzip
   - Tool: Chrome DevTools Network tab

### Secondary Metrics

1. **Lighthouse Performance Score**
   - Target: 80+ for mobile
   - Tool: Chrome DevTools Lighthouse

2. **Core Web Vitals**
   - LCP (Largest Contentful Paint)
   - FID (First Input Delay) / INP (Interaction to Next Paint)
   - CLS (Cumulative Layout Shift)

### Baseline Documentation

Before implementing changes, document:

- Current bundle size (bytes)
- Current load time (seconds)
- Lighthouse score
- Core Web Vitals metrics

---

## Implementation Plan

### Phase 1: Enable AOT Compilation (Week 1)

1. Add `RunAOTCompilation` property to Release configuration
2. Build and verify no errors
3. Measure size impact
4. Document results

### Phase 2: Aggressive Trimming (Week 1-2)

1. Change `TrimMode` to `full`
2. Create `TrimmerRoots.xml` with minimal preserved types
3. Test all features thoroughly
4. Iterate on TrimmerRoots.xml if needed
5. Measure size impact

### Phase 3: Lazy Loading (Week 2)

1. Identify candidates for lazy loading
2. Configure `LazyAssemblyLoader`
3. Implement `OnNavigateAsync` in router
4. Test navigation and loading UX
5. Measure impact on initial load

### Phase 4: Resource Optimization (Week 3)

1. Add preload/prefetch hints to index.html
2. Disable unused runtime features
3. Evaluate and optimize dependencies
4. Final measurements and documentation

---

## Open Questions

1. Are there any reflection-heavy features that might break with full trimming?
2. Which specific pages/features should be prioritized for lazy loading?
3. Is Markdig used heavily, or could it be replaced with a lighter alternative?
4. What is the acceptable minimum browser version for SIMD optimizations?
5. Should we implement a size budget CI check?

---

## References

- [Microsoft Docs: Blazor WebAssembly Performance](https://learn.microsoft.com/en-us/aspnet/core/blazor/performance/)
- [Microsoft Docs: Host and Deploy Blazor WebAssembly](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly/)
- [Microsoft Docs: Configure the Trimmer](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/configure-trimmer/)
- [Microsoft Docs: Lazy Load Assemblies](https://learn.microsoft.com/en-us/aspnet/core/blazor/webassembly-lazy-load-assemblies/)

---

**Document Version:** 1.0  
**Created:** March 30, 2026  
**Next Review:** After Phase 1 completion
