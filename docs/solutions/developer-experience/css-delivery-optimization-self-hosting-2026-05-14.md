---
title: "CSS Delivery Optimization — Self-Hosting, Deduplication, and Script Deferral"
date: 2026-05-14
category: docs/solutions/developer-experience
module: frontend
problem_type: best_practice
component: tooling
severity: medium
applies_when:
  - "Optimizing CSS/font delivery for Blazor WASM or static web apps"
  - "Eliminating third-party CDN dependencies"
  - "Reducing render-blocking resources identified by Lighthouse"
  - "Self-hosting font files and icon libraries"
tags:
  - "css"
  - "performance"
  - "self-hosted"
  - "font-awesome"
  - "foundation-css"
  - "render-blocking"
  - "defer"
  - "cdn"
---

# CSS Delivery Optimization

## Context

A Lighthouse audit of redmuffin.net revealed six render-blocking resources
(~358 KB total) across three third-party domains. Foundation CSS was
loaded twice (127 KB redundant). Font Awesome 6.7.0 and Outfit fonts
loaded from CDNs, requiring 6 `preconnect`/`dns-prefetch` hints.
`page-load-timing.js` loaded synchronously in `<head>`, blocking HTML
parsing.

## Guidance

### 1. Deduplicate Foundation CSS

Delete the standalone `foundation-root.scss` and compiled
`foundation-root.min.css` (127 KB). Foundation loads once via the
canonical vendor partial in `app.scss`:

```scss
// scss/vendors/_foundation.scss
@forward "foundation-sites/scss/foundation" with (
    // configuration overrides here
  );
```

### 2. Self-Host Font Awesome 6.7.0

Download `all.min.css` and 4 `.woff2` font files to:

```
wwwroot/css/fontawesome/all.min.css
wwwroot/fonts/fontawesome/fa-brands-400.woff2
wwwroot/fonts/fontawesome/fa-regular-400.woff2
wwwroot/fonts/fontawesome/fa-solid-900.woff2
wwwroot/fonts/fontawesome/fa-v4compatibility.woff2
```

Update CSS `url()` paths to local. Remove the CDN `<link>` tag and
`preconnect` hint for `cdnjs.cloudflare.com`.

### 3. Self-Host Outfit Fonts

Download 2 `.woff2` files, define `@font-face` in a new SCSS partial:

```scss
// scss/base/_fonts.scss
@font-face {
  font-family: "Outfit";
  src: url("/fonts/outfit/outfit-v11-latin-regular.woff2") format("woff2");
  font-weight: 400;
  font-style: normal;
  font-display: swap;
}
```

Remove `@import url('https://fonts.googleapis.com/...')` from
`_typography.scss` and `_logo.scss`. Remove 4 preconnect/dns-prefetch
hints for `fonts.googleapis.com` and `fonts.gstatic.com`.

### 4. Defer page-load-timing.js

One-line change in `index.html`:

```html
<!-- Before: render-blocking -->
<script src="js/page-load-timing.min.js"></script>

<!-- After: non-blocking -->
<script src="js/page-load-timing.min.js" defer></script>
```

**Safety analysis**: The script reads `performance.timing` and
`performance.getEntriesByType('navigation')` — browser-internal
timestamps fixed at event time, not script execution time. Defer cannot
alter readings. The `scriptLoadTime` field exists but is dead code (never
read by JS or C#). `DOMContentLoaded` fires after deferred scripts. Zero
risk.

## Why This Matters

- **Performance**: Eliminates DNS lookups, TCP handshakes, and TLS
  negotiation for 3 external origins. Fonts load from the same HTTP/2
  connection as the app.
- **Reliability**: No dependency on third-party CDN uptime. Works offline.
- **Security**: Reduces attack surface — no foreign script injection via
  CDN compromise.
- **Simplicity**: 6 `preconnect`/`dns-prefetch` hints deleted. One `<link>`
  tag deleted. One `@import url()` deleted. One `defer` attribute added.

## When to Apply

- When Lighthouse/PageSpeed flags "Preconnect to required origins" or
  "Eliminate render-blocking resources"
- Before deploying to environments with restricted internet access
- When a site loads fonts or CSS frameworks from CDNs
- Whenever a `<script>` in `<head>` lacks `defer`/`async` — run the
  safety analysis first

## Examples

**Before:**

```html
<link rel="preconnect" href="https://fonts.googleapis.com" />
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
<link rel="preconnect" href="https://cdnjs.cloudflare.com" />
<link
  rel="stylesheet"
  href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.7.0/css/all.min.css"
/>
<link
  href="https://fonts.googleapis.com/css2?family=Outfit:wght@400;700&display=swap"
  rel="stylesheet"
/>
<script src="js/page-load-timing.min.js"></script>
```

**After:**

```html
<link rel="stylesheet" href="css/fontawesome/all.min.css" />
<link rel="stylesheet" href="css/app.min.css" />
<script src="js/page-load-timing.min.js" defer></script>
<!-- fonts loaded via @font-face in app.min.css -->
```

**Savings**: 127 KB Foundation dedup, 3 CDN domains eliminated, 6
preconnect/dns-prefetch hints removed, 11.5 KB removed from
render-blocking path.

## Related

- [Production Performance Audit (2026-05-14)](/docs/research/production-performance-audit-2026-05-14.md)
- [SCSS Toolchain Migration + Systemd Dev Server](/docs/solutions/tooling-decisions/dart-sass-migration-systemd-dev-server-2026-05-14.md)
- `rm-scss` skill for Foundation portability conventions
- `rm-dev-tools` skill for cross-platform tool installation
