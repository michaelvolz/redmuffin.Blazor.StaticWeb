# PRD-007: Azure Static Web Apps Performance Optimization - Task List

## Phase 1: Configuration Updates ⚙️

### Task 1.1: Update Cache Headers

- [x] **Update `staticwebapp.config.json` cache settings**
  - [x] Change `_framework/*` cache to `public, max-age=31536000, immutable` (1 year)
  - [x] Verify current cache is 604800 (7 days) - should be 1 year already
  - [x] Keep HTML files with shorter cache duration
  - [x] Test configuration syntax

### Task 1.2: Add Compression Headers

- [x] **Add compression hints to `staticwebapp.config.json`**
  - [x] Add `Content-Encoding` hints for Brotli/Gzip
  - [x] Configure compression settings for different asset types
  - [x] Verify Azure Static Web Apps automatic compression

### Task 1.3: Implement Security Headers

- [x] **Add security headers to `globalHeaders` section**
  - [x] Add `X-Content-Type-Options: nosniff`
  - [x] Add `X-Frame-Options: DENY`
  - [x] Add `Referrer-Policy: strict-origin-when-cross-origin`
  - [x] Review existing CSP headers for conflicts
  - [x] Test headers in browser dev tools

## Phase 2: Resource Hints Implementation 🔗

### Task 2.1: Identify Target Resources

- [x] **Map external CDN dependencies**
  - [x] FontAwesome CDN (cdnjs.cloudflare.com)
  - [x] Google Fonts (fonts.googleapis.com, fonts.gstatic.com)
  - [x] API endpoints (if any)
  - [x] Document current external dependencies

### Task 2.2: Implement Preconnect Links

- [x] **Add resource hints to main layout/index.html**
  - [x] Add `<link rel="preconnect" href="https://cdnjs.cloudflare.com" crossorigin>`
  - [x] Add `<link rel="preconnect" href="https://fonts.googleapis.com" crossorigin>`
  - [x] Add `<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>`
  - [x] Add `<link rel="dns-prefetch">` for API domains if applicable

### Task 2.3: Validate Resource Hints

- [x] **Test resource hints functionality**
  - [x] Verify preconnect in Network tab
  - [x] Check for CSP violations
  - [x] Measure DNS lookup improvement
  - [x] Ensure no console errors

## Phase 3: Bundle Analysis & Optimization 📊

### Task 3.1: Dependency Analysis

- [x] **Review current dependencies**
  - [x] Analyze `packages.lock.json` for unused packages
  - [x] Review Markdig usage patterns
  - [x] Identify potential tree-shaking opportunities
  - [x] Document findings

### Task 3.2: Component-Level Analysis

- [x] **Analyze Blazor components**
  - [x] Review component dependencies
  - [x] Identify heavy components
  - [x] Look for optimization opportunities
  - [x] Document recommendations

### Task 3.3: Bundle Size Verification

- [x] **Run bundle measurement**
  - [x] Execute `scripts/Measure-BundleSize.ps1`
  - [x] Compare with baseline (10.70 MB)
  - [x] Document any changes
  - [x] Ensure no regression

## Phase 4: Testing & Validation ✅

### Task 4.1: Performance Testing

- [x] **Validate cache effectiveness**
  - [x] Test cache headers in browser
  - [x] Verify immutable assets cached properly
  - [x] Check cache duration settings
  - [x] Document cache behavior

### Task 4.2: Security Validation

- [x] **Test security headers**
  - [x] Verify headers in browser dev tools
  - [x] Test CSP compliance
  - [x] Check for security warnings
  - [x] Validate header values

### Task 4.3: Resource Hint Performance

- [x] **Measure resource hint impact**
  - [x] Compare DNS lookup times
  - [x] Test connection establishment
  - [x] Verify preconnect effectiveness
  - [x] Document performance gains

### Task 4.4: Build & Test Validation

- [x] **Ensure build quality**
  - [x] Run `dotnet clean && dotnet build --no-restore --verbosity quiet`
  - [x] Verify zero warnings (except IL2111)
  - [x] Run `dotnet test` - all tests pass
  - [x] Publish and test deployment

## Phase 5: Documentation & Cleanup 📝

### Task 5.1: Update Documentation

- [x] **Document configuration changes**
  - [x] Update README if needed
  - [x] Document new headers
  - [x] Add performance notes
  - [x] Update deployment guide

### Task 5.2: Performance Report

- [x] **Create performance validation report**
  - [x] Document before/after metrics
  - [x] Include bundle size measurements
  - [x] Note security improvements
  - [x] Provide recommendations

### Task 5.3: Future Recommendations

- [x] **Document next steps**
  - [x] Service Worker implementation plan
  - [x] Image optimization strategy
  - [x] Progressive Web App features
  - [x] Advanced caching strategies

## Acceptance Criteria ✨

### Configuration

- [x] Cache headers set to 1 year for immutable assets
- [x] Compression headers properly configured
- [x] Security headers implemented and functional
- [x] No configuration syntax errors

### Performance

- [x] Resource hints reduce DNS lookup time
- [x] Bundle size remains ≤ 10.70 MB
- [x] No performance regression
- [x] Cache effectiveness improved

### Quality

- [x] Zero build warnings maintained
- [x] All tests pass
- [x] No CSP violations
- [x] Security headers validated

### Documentation

- [x] Changes documented
- [x] Performance report created
- [x] Future recommendations provided
- [x] PRD marked as complete

---

**Estimated Effort**: 5-7 hours
**Priority**: Medium
**Dependencies**: PRD-006 (Bundle Size Optimization)
**Status**: Ready to Start

**Notes**:

- Service Worker implementation deferred to future PRD
- Image optimization not applicable (no images yet)
- Critical CSS inlining not needed (current CSS is fine)
- Focus on client-side optimizations only
