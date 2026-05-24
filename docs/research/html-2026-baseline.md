---
date: 2026-05-24
title: HTML-2026 Baseline Decision Framework
tags: [html, baseline, standards, anti-patterns, deprecated, decision-framework]
description: Universal decision framework for HTML element and attribute selection using the Web Platform Baseline standard. Includes deprecated elements catalog, anti-patterns across 6 categories, and new HTML features (2020-2026).
module: html
---

# HTML-2026 Baseline Decision Framework

## Quick Start

1. **Audience** — Government: semantic HTML, WCAG 2.2 AA at minimum. Internal tool: progressive enhancement for Newly available features. Unknown: Widely available only.
2. **Baseline** — Widely available (safe default), Newly available (progressive enhancement), Limited (avoid).
3. **Deprecation** — Never use formally deprecated elements or attributes. The WHATWG HTML Living Standard §15.2 lists every one. If it's on that list, it must not appear in new code.
4. **Decide** — Widely available elements are always safe. Newly available elements (`<dialog>`, `popover`) need progressive enhancement. Never use deprecated elements even if browsers still render them.

---

## The Web Platform Baseline

The WebDX Community Group (Google, Microsoft, Mozilla, Apple) maintains the Baseline standard. MDN displays Baseline badges on every HTML element reference page. The three tiers:

| Tier                 | Definition                                                               | Default rule                   |
| -------------------- | ------------------------------------------------------------------------ | ------------------------------ |
| Widely available     | Interoperable across all core browsers for 30+ months                    | Safe everywhere                |
| Newly available      | Interoperable in current + previous stable releases of all core browsers | Progressive enhancement only   |
| Limited availability | Not yet interoperable across all core browsers                           | Avoid unless analytics justify |

Core browser set: Chrome (desktop + Android), Edge (desktop), Firefox (desktop + Android), Safari (macOS + iOS).

---

## Audience Decision Matrix

| Audience                        | Baseline floor   | Accessibility floor | Enhancement ceiling     |
| ------------------------------- | ---------------- | ------------------- | ----------------------- |
| Government / public service     | Widely available | WCAG 2.2 AA         | None (Widely only)      |
| Consumer SaaS with analytics    | Data-driven      | WCAG 2.2 AA         | Newly available + guard |
| Developer tools / internal apps | Newly available  | WCAG 2.2 A          | Limited with analytics  |
| Personal / portfolio            | Newly available  | Best effort         | Limited with fallback   |
| Unknown                         | Widely available | WCAG 2.2 AA         | None (Widely only)      |

---

## Formally Deprecated Elements

The WHATWG HTML Living Standard §15.2 "Non-conforming features" defines elements that "must not be used by authors." Every browser still renders them for backward compatibility. Do not use any of these.

| Deprecated Element   | Modern Replacement                                           |
| -------------------- | ------------------------------------------------------------ |
| `acronym`            | `<abbr>`                                                     |
| `applet`             | `<embed>` or `<object>`                                      |
| `basefont`           | CSS `font` properties                                        |
| `bgsound`            | `<audio>`                                                    |
| `big`                | CSS `font-size`                                              |
| `blink`              | CSS animations (if necessary)                                |
| `center`             | CSS `text-align: center`                                     |
| `dir`                | `<ul>`                                                       |
| `font`               | CSS `font` properties                                        |
| `frame` / `frameset` | `<iframe>` + CSS                                             |
| `isindex`            | `<form>` + `<input type="text">`                             |
| `keygen`             | Web Cryptography API                                         |
| `listing`            | `<pre>` + `<code>`                                           |
| `marquee`            | CSS animations                                               |
| `multicol`           | CSS `columns`                                                |
| `nextid`             | Server-generated GUIDs                                       |
| `nobr`               | CSS `white-space: nowrap`                                    |
| `noframes`           | N/A (frameset removed)                                       |
| `plaintext`          | `text/plain` MIME type                                       |
| `rb` / `rtc`         | Ruby base/text directly in `<ruby>`                          |
| `spacer`             | CSS `margin` / `padding`                                     |
| `strike`             | `<del>` (edits) or `<s>` (obsolete content)                  |
| `tt`                 | `<code>`, `<kbd>`, `<samp>`, or `<var>` (depends on context) |
| `xmp`                | `<pre>` + `<code>` with escaped HTML                         |

**Never use any element on this list.** They are non-conforming and will trigger validator errors. Browsers may drop rendering support without notice.

---

## Formally Deprecated Attributes

Organized by what they controlled. The full list is in the WHATWG spec; these are the most commonly encountered.

### Presentation attributes (replace with CSS)

| Attribute           | CSS replacement                                |
| ------------------- | ---------------------------------------------- |
| `align`             | `text-align`, `margin`, flexbox/grid alignment |
| `background`        | `background-image`                             |
| `bgcolor`           | `background-color`                             |
| `border` (on img)   | CSS `border`                                   |
| `cellpadding`       | CSS `padding` on `td`/`th`                     |
| `cellspacing`       | CSS `border-spacing`                           |
| `color` (on hr)     | CSS `border-color` or `background-color`       |
| `height` / `width`  | CSS `height` / `width`                         |
| `hspace` / `vspace` | CSS `margin`                                   |
| `size` (on hr)      | CSS `height`                                   |
| `valign`            | CSS `vertical-align`                           |
| `nowrap`            | CSS `white-space: nowrap`                      |
| `clear` (on br)     | CSS `clear`                                    |
| `compact`           | CSS (margins, padding)                         |
| `noshade` (on hr)   | CSS `border-style`                             |

### Behavior/link attributes (replace with standards)

| Attribute                      | Replacement                                       |
| ------------------------------ | ------------------------------------------------- |
| `language` (on script)         | `type` attribute or omit (defaults to JavaScript) |
| `charset` (on a/link)          | HTTP `Content-Type` header                        |
| `name` (on a)                  | `id` attribute                                    |
| `rev` (on a/link)              | `rel` with opposite term                          |
| `longdesc`                     | `<a>` link to description                         |
| `target` (on link)             | Omit (unnecessary)                                |
| `scrolling` (on iframe)        | CSS `overflow`                                    |
| `frameborder`                  | CSS `border`                                      |
| `marginheight` / `marginwidth` | CSS `margin`                                      |

---

## Anti-Patterns Catalog

### 1. Layout Anti-Patterns

**Never use `<table>` for page layout.** Screen readers announce layout tables aloud. CSS Grid and Flexbox are the replacement (Widely available 2017).

**Never use spacer GIFs** (`1x1 transparent.gif` with `width`/`height`). Use CSS `margin`, `padding`, or `gap`.

**Never use `<br>` for vertical spacing.** `<br>` means "line break within content." Multiple `<br>` tags are semantically meaningless. Use CSS `margin-bottom`.

**Never use `&nbsp;` for indentation or spacing.** Non-breaking spaces prevent text wrapping. Use CSS `text-indent`, `padding`, or `margin`.

### 2. Semantic Anti-Patterns

**Never build a page with only `<div>` and `<span>`.** The accessibility tree becomes a flat list of generic nodes. Use `<header>`, `<main>`, `<nav>`, `<footer>`, `<article>`, `<section>`, `<aside>`, and `<h1>`–`<h6>`.

**Never skip heading levels** (h1 → h3 → h2). Heading hierarchy is the primary navigation mechanism for screen reader users. Use progressive levels: h1 → h2 → h3 → h4 → h5 → h6. Choose heading levels by document structure, not default font size.

**Never use `<blockquote>` for indentation only.** Screen readers announce "blockquote" before content. Use CSS `padding-left` or `margin-left` for visual indentation.

**Never use multiple `<h1>` elements.** Screen readers treat the first `<h1>` as the page title. Use one `<h1>` per page, `<h2>`–`<h6>` for subsections.

### 3. Accessibility Anti-Patterns

**Never omit `alt` on informative images.** Screen readers read the filename. Every `<img>` conveying information must have descriptive `alt` text. Decorative images use `alt=""`.

**Never build a button out of a `<div>` with `onclick`.** `<div>` is not focusable, does not respond to Enter/Space, and has no button role. Use `<button type="button">` — it gives you keyboard, focus, and ARIA semantics for free.

**Never remove `outline` without providing a visible focus alternative.** Keyboard-only users cannot tell which element has focus. Use `:focus-visible` with a high-contrast outline.

**Never convey information through color alone.** ~8% of males have color vision deficiency. Pair color with an icon, text label, or pattern. Error states: red border AND error icon AND text message.

**Never use `placeholder` as the only label.** Placeholder disappears when the user types, has low contrast, and is not consistently read by screen readers. Every input must have a visible `<label>`.

**Never use `target="_blank"` without `rel="noopener noreferrer"`.** The opened page can redirect the original page via `window.opener` (tabnabbing). Modern browsers add `noopener` implicitly, but explicit declaration is required for older browsers.

**Never use positive `tabindex` values** (1, 2, 3, ...). Disrupts the natural focus order. Use `tabindex="0"` (add to order) or `tabindex="-1"` (programmatic focus only). Let DOM order define tab sequence.

### 4. Performance Anti-Patterns

**Never place blocking `<script>` in `<head>` without `defer` or `async`.** The browser pauses HTML parsing to download and execute the script. Nothing renders until it finishes. Use `<script defer>` (executes after parsing, in order).

**Never serve a single-resolution image to all devices.** Mobile downloads desktop-sized images. Use `<picture>` with `srcset`, `loading="lazy"` for below-fold images, `width`/`height` for CLS prevention, and WebP/AVIF with JPEG fallbacks.

**Never ship render-blocking CSS without inlining critical styles.** The browser renders nothing until all stylesheets in `<head>` finish loading. Inline critical CSS in `<style>`, load the full stylesheet asynchronously.

### 5. Security Anti-Patterns

**Never use inline event handlers** (`onclick="..."`, `onerror="..."`). Violates CSP, enables XSS if the handler string contains user input. Use `addEventListener()` in JavaScript or framework event binding.

**Never use `javascript:` URLs.** Script executes in the page's origin, fails if JS is disabled, blocked by CSP. Use `<button>` for actions, `<a href="...">` for navigation.

**Never use `eval()` or `new Function()` on user input.** The gold standard XSS vector. Use `JSON.parse()` for data. `setTimeout(func, ms)` (function reference, never a string).

**Never use `document.write()`.** Chrome blocks it in many scenarios. Forces parser reparsing. Use `document.createElement()` + `appendChild()` or `insertAdjacentHTML()`.

**Never use `innerHTML` with unsanitized user input.** Stored XSS if input contains HTML. Use `textContent` for plain text, DOMPurify for rich text, or framework auto-escaping.

### 6. SEO / Meta Anti-Patterns

**Never use the `keywords` meta tag.** Google has ignored it since September 2009. Bing uses it only as a spam signal. Remove it.

**Never write misleading `<title>` tags** that don't match page content. Google rewrites them, click-through rate drops, violates spam policies. `<title>` must uniquely and accurately describe the page.

**Never omit `<meta name="description">`.** Google may auto-generate an incoherent snippet. Every page should have a unique, compelling 150-160 character description.

---

## New HTML Features (2020–2026)

### Widely available (safe everywhere)

| Feature           | Interoperable since | Notes                                       |
| ----------------- | ------------------- | ------------------------------------------- |
| `<dialog>`        | March 2022          | Modal/non-modal dialogs. Safari 15.4.       |
| `<search>`        | October 2023        | Semantic search landmark. Chrome 118+.      |
| `inert` attribute | April 2023          | Removes interactivity from element.         |
| `loading="lazy"`  | July 2020           | Native lazy loading for images and iframes. |
| `<template>`      | Pre-Baseline        | Declarative content fragments.              |

### Newly available (progressive enhancement OK)

| Feature                | Baseline since | Use case                                                                    |
| ---------------------- | -------------- | --------------------------------------------------------------------------- |
| `popover` attribute    | January 2025   | `popover="auto"` / `popover="manual"`. Tooltips, menus, non-modal overlays. |
| Declarative Shadow DOM | February 2024  | `<template shadowrootmode="open">`. Component encapsulation without JS.     |
| View Transitions API   | Various        | `startViewTransition()`. Animated page transitions.                         |
| `fetchpriority`        | 2023           | `<img fetchpriority="high">`. LCP optimization.                             |
| `writingsuggestions`   | 2024           | Control OS writing suggestions on inputs. Chrome/Edge only.                 |
| `elementtiming`        | 2023           | `<img elementtiming="hero-image">`. LCP measurement.                        |

### Limited availability (avoid or guard)

| Feature                | Blocked by      | Notes                                              |
| ---------------------- | --------------- | -------------------------------------------------- |
| `blocking="render"`    | Firefox (17 mo) | In Interop 2026. `<link blocking="render">`.       |
| `dialog closedby`      | Safari (10 mo)  | `closedby="any"`. WebKit position: support.        |
| `hidden="until-found"` | Safari (3 mo)   | Content hidden until Ctrl+F find. WebKit: support. |
| `popover="hint"`       | Safari (2 mo)   | Tooltip popovers. Chrome + Firefox ship.           |
| Customizable select    | Firefox, Safari | `appearance: base-select`. Chrome 135+ only.       |
| `<geolocation>`        | Firefox, Safari | Chrome-only. No signals from other vendors.        |
| `<fencedframe>`        | Firefox, Safari | Chrome-only Privacy Sandbox API.                   |

---

## Practical Application

### What this means for your project

1. **Audit for deprecated elements and attributes.** A validator (W3C Nu Html Checker) catches them automatically. If your codebase has any, replace them before adding new features.

2. **Audit for accessibility anti-patterns.** Lighthouse checks most of these. Run an accessibility audit and fix P1 issues first (missing labels, missing alt, div-as-button, non-visible focus).

3. **Use semantic HTML.** `<header>`, `<main>`, `<nav>`, `<footer>`, `<article>`, `<section>`. One `<h1>`, progressive headings. `<button>` for actions, `<a>` for navigation. The browser gives you accessibility for free when you use the right element.

4. **Security by default.** Never `eval()`, never `innerHTML` with user content, never inline event handlers, never `javascript:` URLs. Use CSP headers and framework auto-escaping.

5. **Progressive enhancement for Newly available features.** `<dialog>` and `popover` are safe now. Use `srcset` + `loading="lazy"` for images. Use `<script defer>` for scripts. Check Baseline status on MDN before adopting any new element.
