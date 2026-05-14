---
date: 2026-05-14
module: blazor-performance
problem_type: performance_issue
component: tooling
severity: high
tags:
  - performance
  - lighthouse
  - render-blocking
  - fonts
  - cdn
  - blazor-wasm
  - css-optimization
---

# Production Performance Audit — redmuffin.net

## Resources Analyzed

| Resource                             | Source                                   |   Size (minified)    | Render-Blocking |
| ------------------------------------ | ---------------------------------------- | :------------------: | :-------------: |
| `foundation-root.min.css`            | local                                    |       124.1 KB       |        ✓        |
| `app.min.css`                        | local                                    |       147.7 KB       |        ✓        |
| `font-awesome/6.7.0/css/all.min.css` | cdnjs.cloudflare.com                     |    ~85 KB (est.)     |        ✓        |
| `media-query-debugger.min.css`       | local                                    |        1.7 KB        |        ✓        |
| `page-load-timing.min.js`            | local                                    |       11.3 KB        |   synchronous   |
| `_framework/blazor.webassembly.js`   | local                                    | part of 1.6MB Brotli |    deferred     |
| Google Fonts (Outfit 400+500+700)    | fonts.googleapis.com / fonts.gstatic.com |  ~30 KB per weight   | chain-blocking  |
| Total Blazor WASM payload            | local                                    |    1.6 MB Brotli     |    deferred     |

**Total render-blocking CSS:** ~358 KB (271.8 KB local + ~85 KB CDN)

**Third-party domains:** cdnjs.cloudflare.com, fonts.googleapis.com, fonts.gstatic.com (3 DNS lookups, 2 TLS handshakes)

---

## Findings — Ranked by Impact

### 1. Font Awesome — Full CDN Bundle (Impact: 9/10)

**Problem:** `font-awesome/6.7.0/css/all.min.css` loaded from cdnjs.cloudflare.com
is render-blocking. The `all.min.css` includes every icon style (solid, regular,
brands, sharp, duotone — hundreds of icons). The site likely uses fewer than 20
icons.

**Impact chain:**

1. DNS lookup for cdnjs.cloudflare.com (~10-30ms)
2. TCP + TLS handshake (~50-100ms)
3. Download ~85 KB CSS (render-blocking — browser cannot paint)
4. Parse ~85 KB CSS rules (only 5-10% actually used)
5. Third-party dependency: if cdnjs is slow or down, entire site paint stalls

**Recommendation:** Self-host only the icons the site actually uses. Options:

- Tree-shake: extract only used glyphs from the font file, serve locally
- SVG sprite: inline the 15-20 SVGs the site uses, eliminate Font Awesome
  entirely
- Subset build: use the Font Awesome subsetter to generate a CSS+font file
  with only used icons

**Lighthouse impact:** Directly improves FCP (First Contentful Paint) and LCP
(Largest Contentful Paint) by removing the slowest external render-blocking
resource.

---

### 2. Google Fonts — @import Cascade (Impact: 8/10)

**Problem:** Two `@import url("https://fonts.googleapis.com/css2?family=Outfit...")`
statements inside `app.min.css` create a 4-step waterfall:

```
app.min.css download → parse → discover @import →
download Google Fonts CSS → parse → discover font URLs →
download font files from fonts.gstatic.com
```

Each `@import` is a separate HTTP request. The browser cannot discover the font
URLs until it has fully parsed `app.min.css` (147.7 KB). By the time font
downloads start, the critical rendering path is already deep into the waterfall.

**Current state:** `font-display: swap` is present (good — text is visible
during font load), but the font download START is late.

**Recommendation — self-host Outfit:**

1. Download Outfit 400, 500, 700 from Google Fonts (the 3 weights used)
2. Convert to modern woff2 format (all browsers support it)
3. Add `@font-face` declarations directly in CSS:
   ```css
   @font-face {
     font-family: "Outfit";
     src: url("/fonts/outfit-400.woff2") format("woff2");
     font-weight: 400;
     font-style: normal;
     font-display: swap;
   }
   ```
4. Remove both `@import` calls from `app.min.css`
5. Remove preconnect hints for fonts.googleapis.com and fonts.gstatic.com

**Additional benefit:** Eliminates 2 third-party domains entirely.

---

### 3. @import in CSS — Eliminate Entirely (Impact: 6/10)

**Problem:** The `@import` statements inside `app.min.css` prevent the browser's
preload scanner from discovering font URLs. The preconnect to `fonts.googleapis.com`
and `fonts.gstatic.com` exists in HTML head, but by the time `@import` resolves
inside CSS, these connections may already be stale or closed.

**Recommendation:** No `@import` in any CSS file. All external resources should
be declared via `<link>` in HTML head so the preload scanner can start fetching
them immediately. Font files should be preloaded:

```html
<link rel="preload" href="/fonts/outfit-400.woff2" as="font" crossorigin />
```

---

### 4. Self-Host All Third-Party Resources (Impact: 7/10)

**Problem:** The site depends on 3 external domains:

- cdnjs.cloudflare.com (Font Awesome CSS)
- fonts.googleapis.com (font CSS)
- fonts.gstatic.com (font files)

Each adds DNS + TCP + TLS overhead and is a single point of failure. The
`preconnect` hints mitigate but don't eliminate the cost.

**Current HTML preconnect overhead:**

```html
<link rel="preconnect" href="https://cdnjs.cloudflare.com" crossorigin />
<link rel="preconnect" href="https://fonts.googleapis.com" crossorigin />
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
<link rel="dns-prefetch" href="https://cdnjs.cloudflare.com" />
<link rel="dns-prefetch" href="https://fonts.googleapis.com" />
<link rel="dns-prefetch" href="https://fonts.gstatic.com" />
```

6 lines of HTML, 3 DNS lookups, established connections that may go unused
(fonts.googleapis.com connection is wasted — fonts load via @import, not
from an HTML link).

**Recommendation:** Self-host everything:

1. Outfit font files → `/fonts/`
2. Font Awesome (subset) → inline SVG or local CSS + font files

**Result:** Zero third-party dependencies. Zero DNS lookups for resources.
Site renders entirely from the Azure SWA CDN. All preconnect/dns-prefetch
lines removed from HTML.

---

### 5. Combine CSS Files (Impact: 4/10)

**Problem:** Two separate render-blocking CSS files:

- `foundation-root.min.css` (124.1 KB) — Foundation framework
- `app.min.css` (147.7 KB) — application styles

Each is a separate HTTP request. On HTTP/2, multiplexing reduces the cost of
multiple requests, but a single combined file is still faster (one request,
one parse, one render-blocking resolution).

**Recommendation:** Combine into a single `site.min.css` during the SCSS build.
This eliminates one render-blocking CSS request from the critical path.

**Caveat:** If Foundation is used by other parts of the pipeline or needs
separate caching, keep them separate. But for this site, a single CSS bundle
is optimal.

---

### 6. Defer page-load-timing.min.js (Impact: 3/10)

**Problem:** `page-load-timing.min.js` (11.3 KB) loads synchronously before
Blazor WASM. It calls `window.pageLoadSpeed.init()` immediately on parse,
which registers PerformanceObserver and DOMContentLoaded listeners. While the
code needs to run early to capture navigation timing, the full 11.3 KB of
metric computation logic does not.

**Recommendation:** Split into two files:

1. `timing-capture.min.js` (~2 KB) — only `init()`, `markStart()`, and
   PerformanceObserver registration. Loaded synchronously, early.
2. `timing-compute.min.js` (~9 KB) — all `getComprehensiveMetrics()`,
   `getResourceTiming()`, `formatBytes()`, recommendations. Deferred.

**Alternative:** Add `defer` attribute to the script. The `DOMContentLoaded`
listener will still fire (deferred scripts run before DOMContentLoaded).
The `init()` call can move to the top of the file so it executes immediately
when the deferred script runs.

---

### 7. Remove media-query-debugger.min.css from Production (Impact: 1/10)

**Problem:** `media-query-debugger.min.css` (1.7 KB) is a development tool
that shows the current breakpoint overlay at the bottom of the viewport. It
is loaded in production as a render-blocking stylesheet and injects visible
content (`body::after` with breakpoint labels).

**Recommendation:** Remove the `<link>` from `index.html` in production builds.
Small win individually (1.7 KB), but it is entirely dead weight.

**How to fix:** Conditionally include in `index.html` only for Debug builds,
or gate with an environment variable check.

---

### 8. Replace Inline setTimeout with CSS-Only Loading State (Impact: 1/10)

**Problem:** An inline `<script>` in the HTML `<head>` runs a 1-second
`setTimeout` to reveal a loading spinner. This is a JavaScript timer during
the critical rendering path.

```html
<script>
  setTimeout(() => {
    var element = document.getElementById("result");
    if (element) element.style = "block";
  }, 1000);
</script>
```

**Recommendation:** Replace with CSS animation-delay:

```css
#result {
  animation: revealSpinner 0.01s 1s forwards;
  display: none;
}
@keyframes revealSpinner {
  to {
    display: block;
  }
}
```

Small win, but removes JavaScript from the critical path entirely.

---

### 9. Font Awesome Preconnect — Unused Connection (Impact: 1/10)

**Problem:** The preconnect to `fonts.googleapis.com` and `fonts.gstatic.com`
opens connections that are never used from an HTML `<link>`. The fonts are
loaded via `@import` inside CSS, which happens much later. By then, the
preconnected socket may already be idle-closed.

**Recommendation:** Remove the font preconnects once fonts are self-hosted
(finding 2). Until then, move font loading from `@import` to `<link>` in
HTML head (finding 3) so the preconnect is actually useful.

---

## Summary — Quick Wins by Effort

| #   | Optimization                        | Impact | Effort  | Type                  | Status |
| --- | ----------------------------------- | :----: | :-----: | --------------------- | :----: |
| 1   | Self-host Font Awesome subset       |   9    | medium  | external dep removal  |   ✅   |
| 2   | Self-host Outfit fonts              |   8    |   low   | external dep removal  |   ✅   |
| 3   | Eliminate @import from CSS          |   6    |   low   | CSS refactor          |   ✅   |
| 4   | Self-host all third-party resources |   7    |   low   | dependency removal    |   ✅   |
| 5   | Combine CSS files                   |   4    |   low   | build pipeline        |   ⚠️   |
| 6   | Defer page-load-timing.js           |   3    |   low   | JS refactor           |   ✅   |
| 7   | Remove debugger CSS from prod       |   1    | trivial | dead code removal     |   ❌   |
| 8   | CSS-only loading state              |   1    | trivial | inline script removal |   —    |
| 9   | Remove wasted preconnects           |   1    | trivial | HTML cleanup          |   ✅   |

**Status notes:**

- **#5 (Combine CSS)**: `foundation-root.min.css` deleted (127 KB saved). Font Awesome `all.min.css` still a separate file — inline into `app.scss` via `@import` pending.
- **#6 (Defer JS)**: Applied 2026-05-14. Safe because all timing data comes from browser-internal Navigation Timing API (`performance.timing`, `PerformanceObserver`) which records timestamps at event time — not script-execution time. The `scriptLoadTime` field (`performance.now()` at script start) is dead code never read by JS or C#. Deferred scripts execute before `DOMContentLoaded`, so event listeners are registered in time. `PerformanceObserver` with buffered entries catches all historical LCP values.
- **#7 (Debugger CSS)**: Intentionally kept — `media-query-debugger.min.css` is a debugging tool visible on the release site, not a production artifact to remove.

**If all 9 applied:**

- Render-blocking CSS: ~358 KB → ~270 KB (combined, local only)
- External domains required: 3 → 0
- DNS lookups: 3 → 0
- TLS handshakes: 2 → 0
- Render-blocking chain depth: 4 steps → 1 step
- Est. FCP improvement: 300-800ms (depending on network conditions)
- Est. Lighthouse performance score improvement: 10-20 points

## CORRECTION

The Google Fonts preconnect hints in HTML head are NOT wasted as initially
thought — the browser's preload scanner discovers them on the first pass and
opens connections immediately. However, the font CSS itself loads via @import
inside app.min.css, so the preconnected sockets may be used for the font file
downloads (from fonts.gstatic.com) but NOT for the font CSS (which goes to
fonts.googleapis.com and must wait for app.min.css parsing). The preconnect
to fonts.googleapis.com is partially useful; fonts.gstatic.com preconnect
is fully useful once the font CSS is downloaded.
