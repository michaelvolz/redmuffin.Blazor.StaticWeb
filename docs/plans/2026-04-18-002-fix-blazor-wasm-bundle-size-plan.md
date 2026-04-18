---
title: Optimize Blazor WASM bundle size by fixing ineffective trimming
type: fix
status: completed
date: 2026-04-18
origin: docs/brainstorms/blazor-wasm-size-optimization-report.md
---

# Optimize Blazor WASM Bundle Size by Fixing Ineffective Trimming

## Overview

The Blazor WebAssembly application downloads ~10-12 MB (compressed) on initial load, which is 5-8x larger than expected for a trimmed .NET 9 Blazor WASM app. The root cause is the `TrimmerRoots.xml` configuration using `preserve="all"` on framework assemblies, which defeats the purpose of IL trimming.

## Problem Frame

The user's reported issue: _"In a fresh incognito window, the initial page request downloads >7 MB of assemblies/resources. This is unacceptably large and impacts Time-to-Interactive (TTI) and Lighthouse scores."_

Analysis confirmed:

- **Actual size:** ~40 MB uncompressed, ~10-12 MB Brotli-compressed in \_framework folder
- **Expected size:** 15-20 MB uncompressed, ~4-6 MB Brotli-compressed for a properly trimmed app
- **Gap:** 50-60% of the bundle is untrimmed due to overly permissive linker configuration

The project already has good optimizations in `Directory.Build.props`:

- ✅ `InvariantGlobalization=true` (Release)
- ✅ `BlazorWebAssemblyPreserveCollationData=false` (Release)
- ✅ `BlazorEnableTimeZoneSupport=false` (Release)
- ✅ `EventSourceSupport=false`
- ✅ `PublishTrimmed=true` with `TrimMode=full`

**The missing piece:** TrimmerRoots.xml uses `preserve="all"` which prevents the trimmer from removing unused code.

## Requirements Trace

- R1. Reduce initial download size from ~10-12 MB to ~4-6 MB (50-60% reduction)
- R2. Maintain all existing functionality (no breaking changes)
- R3. Ensure Brotli compression is properly served by Azure Static Web Apps
- R4. Verify improvements via Chrome DevTools Network tab

## Scope Boundaries

- **In scope:** TrimmerRoots.xml optimization, size verification, functionality testing
- **Out of scope:** Lazy assembly loading (deferred - requires more investigation), AOT compilation (intentionally disabled for size)
- **Not needed:** Changing the package references (they are lightweight)

## Context & Research

### Relevant Code and Patterns

- `src/redmuffin.Blazor.StaticWeb/TrimmerRoots.xml` - current trimmer configuration (uses `preserve="all"`)
- `src/redmuffin.Blazor.StaticWeb/redmuffin.Blazor.StaticWeb.csproj` - Release configuration already has trimming enabled
- `Directory.Build.props` (lines 60-76) - Already contains correct Blazor optimizations
- `src/redmuffin.Blazor.StaticWeb/staticwebapp.config.json` - Brotli compression configured

### Institutional Learnings

- The project already made deliberate choice to disable AOT to keep bundle size small
- Brotli compression is already configured at the CDN level
- The project has good patterns for configuration management via Directory.Build.props

### External References

- [ASP.NET Core Blazor app download size best practices](https://learn.microsoft.com/en-us/aspnet/core/blazor/performance/app-download-size?view=aspnetcore-9.0)
- [Configure the Trimmer for Blazor](https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/configure-trimmer?view=aspnetcore-9.0)
- [Blazor WebAssembly runtime performance](https://learn.microsoft.com/en-us/aspnet/core/blazor/performance/webassembly-runtime-performance?view=aspnetcore-9.0)

## Key Technical Decisions

- **Decision:** Change `preserve="all"` to `preserve="minimal"` in TrimmerRoots.xml
- **Rationale:** `preserve="all"` prevents the trimmer from removing any type from assemblies. Using `preserve="minimal"` allows the trimmer to remove unused types while preserving only what's needed for the app to function (reflection, serialization, etc.)

- **Decision:** Keep certain assemblies at `preserve="all"` for safety
- **Rationale:** Third-party libraries (Blazored.LocalStorage, Markdig, LZStringCSharp) and DI abstractions may have reflection-based behavior that requires full preservation

- **Decision:** Do not implement lazy loading in this fix
- **Rationale:** Lazy loading requires Azure Static Web Apps to serve assemblies correctly and needs more investigation to ensure it works with the current architecture. The TrimmerRoots fix alone should provide significant reduction.

## Open Questions

### Resolved During Planning

- **Q:** Are the existing Directory.Build.props settings correct?
- **A:** Yes, they already contain the recommended Blazor optimizations. No changes needed there.

### Deferred to Implementation

- **Q:** Will `preserve="minimal"` cause any runtime issues?
- **A:** Unknown until tested. If issues arise, specific types can be added to TrimmerRoots.xml. This is standard trimmer troubleshooting.

## Implementation Units

- [ ] **Unit 1: Optimize TrimmerRoots.xml configuration**

**Goal:** Change from overly permissive `preserve="all"` to more effective `preserve="minimal"` for framework assemblies

**Requirements:** R1, R2

**Dependencies:** None

**Files:**

- Modify: `src/redmuffin.Blazor.StaticWeb/TrimmerRoots.xml`

**Approach:**

1. Change `preserve="all"` to `preserve="minimal"` for app assemblies
2. Change `preserve="all"` to `preserve="minimal"` for Blazor framework assemblies
3. Keep `preserve="all"` for third-party libraries (Blazored.LocalStorage, Markdig, LZStringCSharp)
4. Keep `preserve="all"` for Microsoft.Extensions.DependencyInjection.Abstractions and Logging.Abstractions
5. Use `preserve="minimal"` for System.Text.Json, System.Net.Http, JSInterop assemblies

**Patterns to follow:**

- Current TrimmerRoots.xml structure
- Microsoft documentation on trimmer configuration

**Test scenarios:**

- **Happy path:** Publish succeeds, IL Trimmer runs and removes unused code, app assembies are smaller
- **Edge case:** All pages render correctly after trimming - specific features to verify:
  - Home page loads without console errors
  - Navigation between multiple pages works
  - Local storage (Blazored) persists and retrieves data
  - Markdown rendering displays content correctly
  - HTTP API calls to backend succeed
- **Error path:** If trimmer causes runtime issues (e.g., TypeLoadException, MissingMethodException), add specific types to TrimmerRoots.xml preserve list

**Verification:**

- [ ] Publish succeeds without trimmer warnings
- [ ] IL Trimmer executed (check publish output for "Trimming" messages)
- [ ] \_framework folder size reduced by 50%+
- [ ] All pages load in browser

---

- [ ] **Unit 2: Verify size reduction**

**Goal:** Confirm the optimization achieves target size reduction

**Requirements:** R1, R4

**Dependencies:** Unit 1

**Files:**

- None (verification only)

**Approach:**

1. Run `dotnet publish -c Release -o ./publish-optimized`
2. Calculate total \_framework folder size
3. Compare to baseline (40.3 MB uncompressed)
4. Document baseline state: With `preserve="all"`, trimmer cannot remove unused code from preserved assemblies. The current 40.3 MB represents an incompletely trimmed state.
5. Verify improvement by comparing before/after sizes

**Test scenarios:**

- **Happy path:** \_framework size is 15-20 MB (50%+ reduction)
- **Edge case:** Trimmer ran but size reduction less than 30% - indicates preserve="minimal" may need refinement
- **Error path:** Size increased - check if trimmer encountered errors

**Verification:**

- [ ] Published size measured and recorded
- [ ] Improvement meets or exceeds 50% target

---

- [ ] **Unit 3: Test functionality in browser**

**Goal:** Ensure no breaking changes after trimming optimization

**Requirements:** R2, R4

**Dependencies:** Unit 2

**Files:**

- None (testing only)

**Approach:**

1. Deploy to Azure Static Web Apps (or local testing)
2. Open Chrome DevTools → Network tab
3. Enable "Disable cache" and use incognito mode
4. Reload page and verify:
   - All pages render correctly
   - Navigation works
   - Local storage (Blazored) works
   - Markdown rendering works
   - HTTP API calls work
5. Check for `Content-Encoding: br` headers on .wasm files

**Test scenarios:**

- **Happy path:** All app features work with reduced bundle
- **Edge case:** Navigation between multiple pages
- **Error path:** Check browser console for trimmer-related errors

**Verification:**

- [ ] All pages render correctly
- [ ] Navigation works
- [ ] No console errors related to trimming
- [ ] Brotli compression confirmed in Network tab

---

- [ ] **Unit 4: Document results**

**Goal:** Record the optimization results for future reference

**Requirements:** R1

**Dependencies:** Unit 3

**Files:**

- Modify: `docs/brainstorms/blazor-wasm-size-optimization-report.md` (update with actual results)

**Approach:**

- Update the report with measured before/after sizes
- Note any lessons learned
- Document any additional work items discovered

**Test scenarios:**

- **Test expectation:** None -- documentation update

**Verification:**

- [ ] Report updated with actual results

## System-Wide Impact

- **Interaction graph:** No cross-component changes; this is a configuration-only change
- **Error propagation:** N/A
- **State lifecycle risks:** None
- **API surface parity:** None
- **Integration coverage:** None needed beyond browser testing
- **Unchanged invariants:** App behavior, API contracts, and Azure Functions backend remain unchanged

## Risks & Dependencies

| Risk                                                | Mitigation                                                        |
| --------------------------------------------------- | ----------------------------------------------------------------- |
| Trimmer removes needed types causing runtime errors | Test thoroughly; add specific types to TrimmerRoots.xml if needed |
| Size reduction less than expected                   | Verify trimmer is actually running; check for cached assemblies   |
| Azure SWA not serving Brotli                        | Verify staticwebapp.config.json is correct (already configured)   |

## Documentation / Operational Notes

- No changes to deployment process needed
- Azure Static Web Apps already configured for Brotli compression
- Results should be monitored via Lighthouse CI in future iterations

## Sources & References

- **Origin document:** [docs/brainstorms/blazor-wasm-size-optimization-report.md](blazor-wasm-size-optimization-report.md)
- Related code: `src/redmuffin.Blazor.StaticWeb/TrimmerRoots.xml`, `Directory.Build.props`
- External docs: [Microsoft Learn - Blazor app download size best practices](https://learn.microsoft.com/en-us/aspnet/core/blazor/performance/app-download-size?view=aspnetcore-9.0)
