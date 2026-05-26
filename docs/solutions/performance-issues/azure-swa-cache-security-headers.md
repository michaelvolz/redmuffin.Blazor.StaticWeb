---
date: 2025-07-27
title: "Azure Static Web Apps Cache and Security Header Optimization"
tags: [blazor, wasm, performance, azure, security]
problem_type: optimization
---

## Problem

Following bundle size optimization (10.70 MB achieved), the Azure Static Web Apps deployment lacked optimal cache headers for WASM framework assets, security headers to harden the application, and resource hints to reduce CDN DNS lookup time. Asset caching was shorter than necessary, and no preconnect links were established for external CDNs.

## Root Cause

Default `staticwebapp.config.json` used 7-day cache for immutable `_framework/*` assets when the framework files are content-hashed and safe to cache for 1 year. Security headers (`X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`) were not explicitly set. The `index.html` had no `<link rel="preconnect">` or `<link rel="dns-prefetch">` hints for FontAwesome CDN or Google Fonts.

## Solution

**Cache headers** in `staticwebapp.config.json`:

```json
{
  "route": "/_framework/*",
  "headers": {
    "Cache-Control": "public, max-age=31536000, immutable"
  }
}
```

Shorter cache for HTML pages; immutable directive only on versioned assets.

**Security headers** via `globalHeaders`:

```json
{
  "X-Content-Type-Options": "nosniff",
  "X-Frame-Options": "DENY",
  "Referrer-Policy": "strict-origin-when-cross-origin"
}
```

Existing CSP headers reviewed for conflicts.

**Resource hints** in `index.html`:

- `<link rel="preconnect" href="https://cdnjs.cloudflare.com" crossorigin>`
- `<link rel="preconnect" href="https://fonts.googleapis.com" crossorigin>`
- `<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>`

All tasks completed. Bundle size verified at <= 10.70 MB with no regression. Zero build warnings maintained. Security headers validated in browser dev tools. CSP violations checked and cleared.

## Prevention

- **Cache header audit on framework upgrade**: When .NET or Blazor version changes, verify hash-based asset URLs still work with 1-year cache
- **Security header validation in CI**: Add header check to deployment pipeline
- **Resource hint maintenance**: When adding or removing CDNs, update preconnect links
- **Bundle size regression gate**: Run `scripts/Measure-BundleSize.ps1` after configuration changes
