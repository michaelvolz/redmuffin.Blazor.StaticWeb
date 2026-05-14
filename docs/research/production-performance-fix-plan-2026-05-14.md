---
date: 2026-05-14
module: blazor-performance
problem_type: performance
component: tooling
severity: high
tags:
  - performance
  - plan
  - fonts
  - cdn
  - duplication
---

# Production Performance Fix Plan — Top 3

## Verified Baseline (Facts Only, No Guesses)

### Resource Inventory (Production redmuffin.net)

| Resource                             | Source               | Size               |         Duplicated?         |
| ------------------------------------ | -------------------- | ------------------ | :-------------------------: |
| `foundation-root.min.css`            | local                | 127 KB             |  **YES** — fully redundant  |
| `app.min.css`                        | local                | 151 KB             | contains Foundation already |
| `font-awesome/6.7.0/css/all.min.css` | cdnjs.cloudflare.com | ~85 KB             |             no              |
| Google Fonts Outfit (2× @import)     | fonts.googleapis.com | ~30 KB CSS + fonts |         inside CSS          |
| `media-query-debugger.min.css`       | local                | 1.8 KB             |      dev tool in prod       |
| `page-load-timing.min.js`            | local                | 11 KB              |             no              |
| `blazor.webassembly.js`              | local                | framework          |             no              |

### Root Cause: Foundation Duplication

`vendors/_foundation.scss` (line 73) calls `foundation.foundation-everything(true, true)`.
This file is imported by `app.scss` via `@use "vendors/index"` → `@forward "foundation"`.
Therefore `app.min.css` already contains ALL Foundation CSS.

`foundation-root.scss` **also** calls `foundation.foundation-everything(true, true)`.
It compiles to `foundation-root.min.css` — same Foundation CSS, loaded separately.

**Proof:** 91 unique Foundation class selectors in foundation-root.min.css,
92 in app.min.css. Near-identical output. 127 KB completely wasted.

### Root Cause: @import Waterfall

`_typography.scss` line 31 and `_logo.scss` line 12 both use `@import url("https://fonts.googleapis.com/...")`.
Browser cannot discover font URLs until it downloads + parses 151 KB `app.min.css`.
Creates a 4-step waterfall: HTML → CSS parse → Google Fonts CSS → font files.

---

## Implementation Plan

### Step 1: Remove Redundant Foundation CSS

**Files changed:**

1. `compilerconfig.json` — remove `foundation-root.scss` entry (lines 2-5)
2. `wwwroot/index.html` — remove both `<link>` lines for `foundation-root.min.css` (lines 10, 12)
3. Delete `scss/foundation-root.scss`
4. Delete `wwwroot/css/foundation-root.min.css`

**Verification:** `dotnet build` succeeds, 0 warnings, Blazor app renders with Foundation styles intact.

**Impact:** Saves 127 KB download + 1 HTTP request + 1 render-blocking resource.

---

### Step 2: Self-Host Outfit Fonts

**Download Outfit woff2 files:**

- Outfit Regular (400): `outfit-400.woff2`
- Outfit Medium (500): `outfit-500.woff2`
- Outfit Bold (700): `outfit-700.woff2`
- Source: Google Fonts download page (select woff2 only)

**Files changed:**

1. Add font files to `wwwroot/fonts/outfit/`
2. Create `scss/base/_fonts.scss` with `@font-face` declarations for all 3 weights
3. Import `_fonts.scss` from `scss/base/_index.scss` (via `@forward`)
4. Remove `@import url(...)` line 31 from `scss/base/_typography.scss`
5. Remove `@import url(...)` line 12 from `scss/features/branding/_logo.scss`
6. Remove both Google Fonts preconnect + dns-prefetch from `index.html`
   (lines 16-17 and lines 20-21)

**Verification:** `dotnet build -c Debug-Sass` compiles, app.min.css contains `@font-face` with local URLs, no @import for Google Fonts.

**Impact:** Eliminates 2 third-party domains, 2 DNS lookups, 2 TLS handshakes, 4-step waterfall.

---

### Step 3: Self-Host Font Awesome

**Download Font Awesome 6.7.0 for web:**

- CSS file: `css/all.min.css`
- Font files: `webfonts/fa-brands-400.woff2`, `fa-regular-400.woff2`, `fa-solid-900.woff2`
- Source: Font Awesome official download or CDN

**Files changed:**

1. Add CSS to `wwwroot/css/fontawesome/all.min.css`
2. Add font files to `wwwroot/fonts/fontawesome/`
3. Update font URL paths in `all.min.css` to point to `/fonts/fontawesome/`
4. Replace CDN `<link>` in `index.html` (line 23) with local reference
5. Remove `cdnjs.cloudflare.com` preconnect + dns-prefetch from `index.html` (lines 15, 19)
6. Optionally: preload the FA font files in `<head>` for priority loading

**Verification:** Font Awesome icons render, no external requests to cdnjs.cloudflare.com.

**Impact:** Eliminates CDN dependency, saves DNS+TLS+connection overhead (~100-200ms).

---

### Step 4: Remove Debugger CSS from Production

**Files changed:**

1. `wwwroot/index.html` — remove line 27 (`media-query-debugger.min.css` link)
2. `compilerconfig.json` — optionally keep entry for dev builds (or remove)

**Verification:** Body no longer shows breakpoint overlay at bottom of page.

**Impact:** Saves 1.8 KB + 1 render-blocking CSS. Minor but dead weight.

---

### Step 5: Clean Up Index.html

After all changes, `index.html` `<head>` goes from:

```html
<!-- Before: 6 preconnect/dns-prefetch, 5 CSS links, 1 CDN link -->
<link rel="preload" href="css/foundation-root.min.css" as="style" />
<link rel="preload" href="css/app.min.css" as="style" />
<link href="css/foundation-root.min.css" rel="stylesheet" />
<link href="css/app.min.css" rel="stylesheet" />
<link rel="preconnect" href="https://cdnjs.cloudflare.com" crossorigin />
<link rel="preconnect" href="https://fonts.googleapis.com" crossorigin />
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
<link rel="dns-prefetch" href="https://cdnjs.cloudflare.com" />
<link rel="dns-prefetch" href="https://fonts.googleapis.com" />
<link rel="dns-prefetch" href="https://fonts.gstatic.com" />
<link rel="stylesheet" href="https://cdnjs.cloudflare.com/..." />
<link href="css/media-query-debugger.min.css" rel="stylesheet" />
```

To:

```html
<!-- After: zero external domains, 1 CSS link -->
<link rel="preload" href="css/app.min.css" as="style" />
<link href="css/app.min.css" rel="stylesheet" />
<link href="css/fontawesome/all.min.css" rel="stylesheet" />
<!-- Preload font files for early discovery -->
<link
  rel="preload"
  href="fonts/outfit/outfit-400.woff2"
  as="font"
  type="font/woff2"
  crossorigin
/>
<link
  rel="preload"
  href="fonts/outfit/outfit-700.woff2"
  as="font"
  type="font/woff2"
  crossorigin
/>
```

**Net change:** 14 lines removed, 3 external domains → 0, 5 CSS links → 2.

---

## Verification Checklist

1. `dotnet build --verbosity quiet` — 0 errors, 0 warnings
2. `dotnet build -c Debug-Sass` — SCSS compiles (Windows only, BuildWebCompiler2022)
3. `dotnet publish -c Release -p:PublishTrimmed=true` — 49 assemblies output
4. Visual check: site renders correctly, all icons visible, Outfit font applied
5. Browser DevTools Network tab: zero third-party requests (no cdnjs, no googleapis)
6. Performance: FCP/LCP improved (requires deploy to measure)
