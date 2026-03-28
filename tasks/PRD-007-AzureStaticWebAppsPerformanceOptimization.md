# PRD-007: Azure Static Web Apps Performance Optimization

## Overview

Optimize the Azure Static Web Apps configuration for better performance, security, and caching efficiency while maintaining the current 10.70 MB bundle size achievement.

## Background

Following the successful bundle size optimization (PRD-006), we need to implement client-side performance optimizations that are applicable to Azure Static Web Apps deployment without server-side dependencies.

## Objectives

- Optimize cache headers for better performance
- Add compression and security headers
- Implement resource hints for external CDNs
- Perform bundle analysis and dependency optimization
- Maintain zero build warnings policy

## Scope

### In Scope

1. **Cache Header Optimization**
   - Update `staticwebapp.config.json` cache settings
   - Set 1-year cache for immutable assets (`_framework/*`)
   - Optimize cache duration for different asset types

2. **Compression Headers**
   - Add Content-Encoding hints for Brotli/Gzip
   - Optimize compression settings for Azure Static Web Apps

3. **Security Headers Enhancement**
   - Add `X-Content-Type-Options: nosniff`
   - Add `X-Frame-Options: DENY`
   - Add `Referrer-Policy: strict-origin-when-cross-origin`
   - Review and enhance existing CSP headers

4. **Resource Hints Implementation**
   - Add `<link rel="preconnect">` for external CDNs
   - Add `<link rel="dns-prefetch">` for API domains
   - Optimize FontAwesome and Google Fonts loading

5. **Bundle Analysis & Optimization**
   - Review dependencies for unused code
   - Optimize Markdig usage if applicable
   - Analyze component-level optimizations

### Out of Scope

- Service Worker implementation (future PRD)
- Image optimization (no images currently)
- Server-side lazy loading (not applicable)
- Critical CSS inlining (current CSS is fine)

## Technical Requirements

### 1. Cache Header Configuration

```json
{
  "route": "/_framework/*",
  "headers": {
    "Cache-Control": "public, max-age=31536000, immutable"
  }
}
```

### 2. Security Headers

```json
{
  "globalHeaders": {
    "X-Content-Type-Options": "nosniff",
    "X-Frame-Options": "DENY",
    "Referrer-Policy": "strict-origin-when-cross-origin"
  }
}
```

### 3. Resource Hints

```html
<link rel="preconnect" href="https://cdnjs.cloudflare.com" crossorigin />
<link rel="preconnect" href="https://fonts.googleapis.com" crossorigin />
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
<link rel="dns-prefetch" href="//api.yourdomain.com" />
```

## Implementation Plan

### Phase 1: Configuration Updates

1. Update `staticwebapp.config.json`
   - Modify cache headers for different asset types
   - Add compression and security headers
   - Test configuration locally

### Phase 2: Resource Hints

1. Update `index.html` or main layout
   - Add preconnect links for CDNs
   - Add dns-prefetch for API endpoints
   - Verify no CSP violations

### Phase 3: Bundle Analysis

1. Analyze current dependencies
   - Review Markdig usage patterns
   - Identify unused code opportunities
   - Document findings and recommendations

### Phase 4: Testing & Validation

1. Performance testing
   - Measure cache effectiveness
   - Validate security headers
   - Test resource hint performance
2. Bundle size verification
   - Run `Measure-BundleSize.ps1`
   - Ensure no size regression

## Success Criteria

### Performance Metrics

- [ ] Cache headers properly configured (1 year for immutable assets)
- [ ] Security headers implemented and validated
- [ ] Resource hints reduce DNS lookup time
- [ ] Bundle size remains ≤ 10.70 MB
- [ ] Zero build warnings maintained

### Technical Validation

- [ ] `staticwebapp.config.json` passes validation
- [ ] Security headers visible in browser dev tools
- [ ] Resource hints working (Network tab shows preconnect)
- [ ] No CSP violations introduced
- [ ] All tests pass (`dotnet test`)

## Risks & Mitigation

### Risk: Cache Headers Too Aggressive

**Mitigation**: Use `immutable` directive only for versioned assets

### Risk: CSP Violations from Resource Hints

**Mitigation**: Test thoroughly and update CSP if needed

### Risk: Bundle Size Regression

**Mitigation**: Run bundle measurement after each change

## Dependencies

- Azure Static Web Apps platform
- Current bundle size optimization (PRD-006)
- Existing CSP configuration

## Timeline

- **Phase 1**: 1-2 hours (Configuration updates)
- **Phase 2**: 1 hour (Resource hints)
- **Phase 3**: 2-3 hours (Bundle analysis)
- **Phase 4**: 1 hour (Testing)
- **Total**: 5-7 hours

## Deliverables

1. Updated `staticwebapp.config.json` with optimized headers
2. Enhanced `index.html` with resource hints
3. Bundle analysis report with recommendations
4. Performance validation report
5. Updated documentation

## Future Considerations

- Service Worker implementation (separate PRD)
- Image optimization when images are added
- Progressive Web App features
- Advanced caching strategies

## References

- [Azure Static Web Apps Configuration](https://docs.microsoft.com/en-us/azure/static-web-apps/configuration)
- [Web Performance Best Practices](https://web.dev/performance/)
- [Security Headers Guide](https://owasp.org/www-project-secure-headers/)
- [Resource Hints Specification](https://www.w3.org/TR/resource-hints/)

---

**Status**: Draft
**Created**: 2025-01-27
**Last Updated**: 2025-01-27
**Assignee**: AI Assistant
**Priority**: Medium
**Effort**: 5-7 hours
