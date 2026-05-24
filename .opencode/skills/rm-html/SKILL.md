---
name: rm-html
description: HTML standards, deprecated elements catalog, anti-patterns, and 2026 best practices for semantics, accessibility, performance, security, and SEO. Use when writing, editing, reviewing, or auditing HTML — any .html, .razor, .cshtml file, or markup in components. Contains the definitive deprecated elements list and anti-patterns for this repo.
---

# rm-html

## Quick Start

Before writing any HTML, determine the Baseline floor:

1. Government / compliance / public service → Widely available + WCAG 2.2 AA
2. Consumer SaaS with analytics → Widely available + data-driven Newly available
3. Developer tools / internal apps → Newly available + WCAG 2.2 A
4. Personal projects → Newly available
5. Unknown audience → Widely available + WCAG 2.2 AA (safe default)

Full decision framework at `docs/research/html-2026-baseline.md`.

## Anti-Patterns Quick Reference

**Never use deprecated elements.** `center`, `font`, `marquee`, `strike`, `big`, `blink`, `spacer`, `frame`, `frameset`, `noframes`, `applet`, `basefont`, `bgsound`, `dir`, `isindex`, `keygen`, `listing`, `multicol`, `nextid`, `nobr`, `plaintext`, `rb`, `rtc`, `tt`, `xmp`, `acronym`. Every one is listed in WHATWG §15.2. Use the modern CSS or semantic replacement.

**Never use `<table>` for page layout.** CSS Grid and Flexbox replace table layouts (Widely available 2017). Layout tables break responsive design and create an accessibility nightmare for screen reader users.

**Never build a page with only `<div>` and `<span>`.** Use `<header>`, `<main>`, `<nav>`, `<footer>`, `<article>`, `<section>`, `<aside>`. Semantic elements give you landmarks, heading hierarchy, and keyboard navigation for free.

**Never skip heading levels** (h1 → h3 → h2). Heading hierarchy is the primary screen reader navigation mechanism. One `<h1>` per page. Progressive levels only.

**Never use `<div onclick="...">` as a button.** `<div>` is not focusable, doesn't respond to Enter/Space, has no button role. Use `<button type="button">` — keyboard, focus, and ARIA for free.

**Never omit `alt` on informative images.** Screen readers read the filename. Every informative `<img>` needs descriptive `alt`. Decorative images use `alt=""`.

**Never remove `outline` without a visible `:focus-visible` replacement.** Keyboard-only users cannot tell where they are on the page.

**Never use `placeholder` as the only label.** Disappears on input, low contrast, not consistently read by screen readers. Every input needs a visible `<label>`.

**Never use `target="_blank"` without `rel="noopener noreferrer"`.** The opened page can redirect yours via `window.opener`. Modern browsers add `noopener` implicitly, but explicit is required for older browsers.

**Never use positive `tabindex` values** (1, 2, 3). Disrupts DOM order. Use `tabindex="0"` or `tabindex="-1"` only.

**Never use inline event handlers** (`onclick="..."`, `onerror="..."`). Violates CSP. Enables XSS. Use `addEventListener()` or framework event binding.

**Never use `javascript:` URLs.** Blocked by CSP. Fails with JS disabled. Use `<button>` for actions.

**Never use `innerHTML` with unsanitized user input.** Stored XSS. Use `textContent`, DOMPurify, or framework auto-escaping.

**Never use `document.write()`.** Chrome blocks it. Use `createElement()` + `appendChild()`.

**Never use `eval()` or `new Function()` on any input.** The gold standard XSS vector. Use `JSON.parse()`.

**Never use the `keywords` meta tag.** Google has ignored it since September 2009. Bing uses it as a spam signal.

## 2026 Best Practices

### Semantic Structure

- **Use landmarks.** `<header>`, `<main>`, `<nav>`, `<footer>` — one `<main>` per page. Screen readers can skip directly to any landmark.
- **Use `<article>` for self-contained content** (blog posts, comments, cards). Use `<section>` for thematic groupings with a heading.
- **Use `<button>` for actions, `<a>` for navigation.** Never use `<a href="#">` or `<div>` for buttons. The `<button>` element gives you keyboard, focus, and ARIA for free.
- **Use `<ul>` / `<ol>` / `<dl>` for actual lists.** Not for visual indentation or spacing.

### Forms

- **Every input has a visible `<label>` with a `for` attribute.** Never rely on `placeholder` alone.
- **Use `autocomplete` attributes.** `autocomplete="name"`, `autocomplete="email"`, etc. Helps users, helps browsers.
- **Use `<fieldset>` + `<legend>` for related form groups.** Radio buttons, checkboxes, multi-field sections.
- **Never disable the submit button without providing feedback.** Users with slow connections assume the form is broken.

### Images & Media

- **Use `<picture>` with `srcset` for responsive images.** Serve WebP/AVIF with JPEG/PNG fallbacks.
- **Use `loading="lazy"` for below-fold images.** Browser-native, no JS required.
- **Always include `width` and `height` attributes** to prevent Cumulative Layout Shift.
- **Use `<video controls>` — never autoplay with sound.** Respect `prefers-reduced-motion`.
- **Provide `<track kind="captions">` for video content.** Required for WCAG AA.

### Accessibility

- **Use `:focus-visible` for keyboard focus indicators.** The browser shows focus rings only for keyboard navigation — no ugly outlines on mouse clicks.
- **Convey information through multiple channels.** Never color alone. Pair color with icon, text, or pattern.
- **Test with a screen reader.** If you can navigate your page with VoiceOver or NVDA without seeing the screen, your HTML is correct.
- **Use `aria-label` or `aria-labelledby` only when the accessible name is missing.** The best ARIA is no ARIA — most semantic elements provide the accessible name for free.

### Performance

- **Use `<script defer>` for all non-critical scripts.** Executes after HTML parsing, preserves execution order.
- **Use `<script type="module">` for ES modules.** Deferred by default, scoped, modern.
- **Use `<link rel="preload">` for critical fonts and hero images.** Hints the browser to prioritize.
- **Use `<link rel="preconnect">` for third-party origins.** `fonts.googleapis.com`, `api.example.com`, CDN hosts.

### Security

- **Set CSP headers.** `Content-Security-Policy` prevents XSS, clickjacking, and code injection. Use `script-src 'self'` without `'unsafe-inline'`.
- **Use `Trusted Types`.** Browser-level enforcement that only sanitized strings can reach `innerHTML` and `eval()`-adjacent APIs.
- **Use `<meta http-equiv="Content-Security-Policy">` as a fallback** when you can't control server headers.

## Blazor

Blazor renders `.razor` markup to real DOM HTML. Every general HTML rule applies, plus these Blazor-specific rules:

### `@((MarkupString)rawHtml)` is Blazor's `innerHTML`

Bypasses auto-escaping. Only safe with server-sanitized, known-trusted HTML. If the string contains any user input, it is XSS. Never pass user content through `MarkupString`. Use auto-escaped `@variable` (equivalent to `textContent`) instead.

### `@key` preserves element identity and focus

When rendering lists that can mutate (items inserted/removed/reordered), always use `@key` on each item. Without `@key`, Blazor reuses elements by index position, causing keyboard focus to jump to the wrong item. This is an accessibility requirement for any interactive list.

```razor
@* Correct — focus preserved across re-renders *@
@foreach (var item in Items)
{
    <li @key="item.Id">@item.Name</li>
}
```

### Component boundaries — never add wrapper elements inside table/list context

A Blazor component renders exactly its root markup — it does not add an implicit wrapper `<div>`. However, if YOU add a `<div>` wrapper inside a component used in a table row, list item, or definition list context, you break the HTML. A `<tr>` must contain only `<td>`/`<th>`. A `<li>` must be directly inside `<ul>`/`<ol>`.

```razor
@* Wrong — renders <tr><div>...</div></tr> (broken) *@
<div> ... </div>

@* Correct — renders <tr>...</tr> *@
<tr> ... </tr>
```

For `<Virtualize>` with tables, set `SpacerElement="tr"` to preserve row semantics.

### `AdditionalAttributes` XSS risk

The `[Parameter(CaptureUnmatchedValues = true)]` pattern passes arbitrary HTML attributes through to the rendered element. If any user-controlled value reaches `AdditionalAttributes`, an attacker can inject `onclick="..."`, `onerror="..."`, or `onfocus="..."` event handlers. Never expose `AdditionalAttributes` to untrusted input without sanitization. Use CSP `script-src` without `'unsafe-inline'` as a defense-in-depth measure.

### `EditForm` needs manual accessibility

`EditForm` renders a semantic `<form>`, but it does NOT auto-generate `<label for="...">`, `aria-describedby` for validation errors, or `aria-current` for active fields. These must be added manually:

```razor
<EditForm Model="model">
    <label for="name">Name</label>
    <InputText id="name" @bind-Value="model.Name"
               aria-describedby="name-error" />
    <ValidationMessage For="() => model.Name" id="name-error" />
</EditForm>
```

`ValidationSummary` does render with `role="alert"` automatically.

### `FocusOnNavigate` for SPA accessibility

Set `FocusOnNavigate` with `Selector="h1"` after every navigation. Without it, screen reader focus stays at the top of the page after an SPA transition — the user hears nothing and assumes navigation failed.

```razor
<FocusOnNavigate RouteData="routeData" Selector="h1" />
```

### `NavLink` does NOT auto-add `aria-current` or `rel="noopener"`

`NavLink` renders an `<a>` with an `active` CSS class, but does not add `aria-current="page"`. For `target="_blank"` links, it does not add `rel="noopener noreferrer"`. Add both explicitly:

```razor
<NavLink href="/about"
         AdditionalAttributes='new Dictionary<string,object> { ["aria-current"] = "page" }'>
    About
</NavLink>
<NavLink href="https://external.com" target="_blank"
         AdditionalAttributes='new Dictionary<string,object> { ["rel"] = "noopener noreferrer" }'>
    External
</NavLink>
```

### `@onclick` replaces inline event handlers

Blazor's `@onclick`, `@onchange`, `@onkeydown` directives compile to `addEventListener()` — never raw `onclick=""`. This enables CSP without `'unsafe-inline'` and eliminates the XSS vector of inline handlers.

### `<HeadContent>` for dynamic metadata

Use `<HeadContent>` and `<PageTitle>` (instead of raw `<title>` in `index.html`) for dynamic, per-page metadata:

```razor
<PageTitle>About - My Site</PageTitle>
<HeadContent>
    <meta name="description" content="Learn about our company history" />
</HeadContent>
```

See `rm-css` for CSS-specific standards and `rm-ui-styling` for framework selection.
