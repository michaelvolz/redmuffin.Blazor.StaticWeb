---
date: 2026-05-14
title: Static Asset Fingerprinting in .NET 10 SDK for Blazor WASM (net9.0)
tags: [dotnet, blazor, wasm, fingerprinting, msbuild, cdn]
description: Evaluation of StaticWebAssetFingerprintPattern for CSS/JS cache busting. JS works, CSS fails with net9.0 target.
module: build
problem_type: investigation
---

## Background

CDN cache staleness: `app.min.css` gets a 7-day `max-age` header from Azure SWA.
Content changes require a manual cache bust (query string or filename change).
.NET 10 SDK introduced `StaticWebAssetFingerprintPattern` to add content hashes
to static asset filenames during publish, solving this automatically.

## Findings

### JS fingerprinting: works

```
<StaticWebAssetFingerprintPattern Include="JS" Pattern="*.js"
  Expression="#[.{fingerprint}]!" />
```

With `<OverrideHtmlAssetPlaceholders>true</OverrideHtmlAssetPlaceholders>` and
a placeholder in `index.html`:

```html
<script src="js/page-load-timing.min#[.{fingerprint}].js" defer></script>
```

After publish: `src="js/page-load-timing.min.jfc0vlv5xk.js"` — content hash
replaces the marker. File on disk also renamed.

**Requires a full clean rebuild** — incremental builds can miss the HTML
replacement step. Delete `obj/` and `bin/` before publishing if the first
attempt fails.

### CSS fingerprinting: does NOT work

Same configuration for CSS files:

```xml
<StaticWebAssetFingerprintPattern Include="CSS" Pattern="*.css"
  Expression="#[.{fingerprint}]!" />
```

With placeholder in `index.html`:

```html
<link href="css/app.min#[.{fingerprint}].css" rel="stylesheet" />
```

Result: files ARE renamed on disk (`app.min.0yg1nygaqq.css`), but the HTML
placeholder is NOT replaced — `index.html` still contains `css/app.min.css`.
This appears to be a .NET 10 SDK bug when targeting net9.0.

Tested pattern variants (none worked):

- `*.css` — matches root only, no replacement
- `css/*.css` — no replacement
- `css/**/*.css` — no replacement
- `css/app.min.css` (explicit path) — no replacement
- `**/*.css` — no replacement

### Framework files: do NOT use custom placeholders

Blazor already fingerprintes `_framework/blazor.webassembly.js` automatically.
Adding a custom `#[.{fingerprint}]` placeholder conflicts with the framework's
own mechanism. Keep the standard `<script src="_framework/blazor.webassembly.js">`.

### Dev workflow: broken

`dotnet run` / `dotnet watch` serve `index.html` without placeholder replacement.
The browser tries to load `page-load-timing.min#[.{fingerprint}].js` literally,
which does not exist. Placeholder replacement is publish-only.

## Recommendation

For CSS cache busting, use a simpler approach: append a query string in the
CI/CD pipeline or accept manual cache busting. The `#[.{fingerprint}]`
mechanism is too fragile for CSS with current SDK versions.

For JS, fingerprinting works but breaks dev. Consider keeping the unfingerprinted
reference in `index.html` and only adding the placeholder during CI publish
(e.g., via `sed` replacement in the workflow YAML).

## Environment

- .NET 10 SDK 10.0.104
- Blazor WASM targeting net9.0
- Azure Static Web Apps (SWA) with 7-day `max-age` on CSS
