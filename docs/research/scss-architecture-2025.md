---
date: 2026-05-14
title: Modern SCSS Architecture & Compilation for Small Blazor WASM Sites (2025-2026)
tags:
  [
    scss,
    architecture,
    blazor,
    research,
    compilation,
    dart-sass,
    node-sass,
    buildwebcompiler,
    libsass,
    sass,
  ]
description: Research on SCSS architecture patterns, .NET compilation tooling, and migration strategy for a small Blazor WASM site with ~25 SCSS partials currently on 7-1 pattern with BuildWebCompiler2022.
module: styles
problem_type: architecture-decision
---

# Modern SCSS Architecture & Compilation for Small Sites (2025-2026)

## Executive Summary

**Recommended architecture**: Simplified 4-folder pattern (abstracts, base,
components, vendor) — drop the 7-1 pattern's pages, themes, and layout folders
for a single-page Blazor WASM app. Co-locate component-specific SCSS with
`.razor` files using CSS isolation where practical.

**Recommended compiler**: Migrate from BuildWebCompiler2022 to
**AspNetCore.SassCompiler** (dart-sass based, 246 GitHub stars, explicit
Blazor WASM support, actively maintained). BuildWebCompiler2022 uses LibSass
which is End-of-Life (October 2025). Dart Sass is the only Sass implementation
receiving new features.

**Urgency**: Medium. Your current setup works but is on deprecated
infrastructure. The `@import` rule is deprecated in Dart Sass 1.80+ and will
be removed in Dart Sass 3.0.0. LibSass (which BuildWebCompiler2022 uses) cannot
compile `@use`/`@forward` module syntax at all.

---

## 1. SCSS Architecture for Small Sites in 2025-2026

### Finding: 7-1 Is Still the Reference, but Widely Considered Overkill

**Confidence: High**

The 7-1 pattern (abstracts, base, components, layout, pages, themes, vendors +
main.scss) by Hugo Giraudel remains the most-cited SCSS architecture in Sass
Guidelines. However, the consensus from multiple 2024-2026 sources is that it
is overkill for small-to-medium projects.

**Key citations:**

- **Remote.com Engineering Blog (Feb 2025)** — "We've started following the
  folder structure that the 7-1 pattern suggests, but on all projects that
  we've worked with, none of them had the need for all the folders." They
  reduced to 5 folders: abstracts, base, components, vendor, pages.
  [Source](https://remote.com/blog/remote-work/how-to-structure-your-sass-project)

- **Educative.io Sass Best Practices (2024-2025)** — Recommends organizing
  into utilities, components, layout, and pages with a single main.scss
  entry point. No mention of 7-1 for small projects.

- **CodeLucky CSS File Organization (2025)** — ITCSS layers pattern
  (Settings → Tools → Generic → Elements → Objects → Components →
  Utilities) as an alternative to 7-1, but notes both are designed for
  large projects.

- **Sass Guidelines by Hugo Giraudel** — The guidelines themselves are
  largely pre-module-system (written before `@use`/`@forward`). The
  architecture section still documents 7-1 but the overall guide has not
  been substantially updated to reflect the module system era.

### Finding: Simplified Alternatives Are the Emerging Consensus

**Confidence: Moderate-High**

The trend is toward smaller, flatter structures that grow organically:

| Pattern              | Folders                                | Best For                             |
| -------------------- | -------------------------------------- | ------------------------------------ |
| 7-1 (classic)        | 7 + main.scss                          | Large multi-page sites               |
| Remote.com 5-folder  | 5 (no themes, no layout)               | Medium sites                         |
| Minimal 3-4 folder   | abstracts, base, components (+ vendor) | Small sites, SPAs                    |
| Component co-located | SCSS next to component files           | Component frameworks (React, Blazor) |
| ITCSS layers         | 7 layers (different grouping)          | Design systems, large teams          |

### Finding: Component Co-Location Is Gaining Traction

**Confidence: High**

With React, Vue, and Blazor all promoting component-based architecture, the
trend is to co-locate styles with components. Blazor supports this natively
via CSS isolation (`Component.razor.css`). With the right compiler
(AspNetCore.SassCompiler supports this), you can use `Component.razor.scss`
for scoped SCSS.

---

## 2. Blazor WASM-Specific SCSS Structure

**Confidence: Moderate** (limited Blazor+SCSS specific content available)

**Recommendations gathered from Reddit r/Blazor, r/dotnet, and tool docs:**

1. **Global styles** in a small `Styles/` directory with a minimal folder
   structure (abstracts, base, components).
2. **Component-scoped styles** using Blazor CSS isolation
   (`ComponentName.razor.scss`).
3. **AspNetCore.SassCompiler** has explicit Blazor WASM support and can
   compile both global SCSS and scoped SCSS.
4. DevExpress Blazor docs recommend using `@import` (though this is now
   deprecated — use `@use` instead) for importing shared stylesheets.

**Blazor-specific considerations:**

- CSS isolation generates scoped CSS with `b-{hash}` attributes
- SCSS compilation must happen _before_ Blazor's CSS isolation pipeline
- AspNetCore.SassCompiler's MSBuild task runs at the right point in the build

---

## 3. .NET SCSS Compilation Landscape (2025-2026)

### BuildWebCompiler2022 — Status

**Confidence: High**

| Attribute       | Detail                                                                                                                |
| --------------- | --------------------------------------------------------------------------------------------------------------------- |
| Package         | BuildWebCompiler2022 v1.14.15                                                                                         |
| Latest release  | March 26, 2025                                                                                                        |
| Source repo     | [github.com/failwyn/WebCompiler](https://github.com/failwyn/WebCompiler) (fork of madskristensen/WebCompiler)         |
| Original repo   | [github.com/madskristensen/WebCompiler](https://github.com/madskristensen/WebCompiler) — **archived/abandoned** (404) |
| Compiler engine | **LibSass** (via node-sass or direct LibSass binding)                                                                 |
| .NET targets    | .NETCoreApp 3.1, .NETFramework 4.8, .NETStandard 2.1                                                                  |
| Downloads       | ~2M total, ~1.3K/day                                                                                                  |
| Maintenance     | Active (Failwyn's fork), but using dead engine                                                                        |

**Critical problem**: BuildWebCompiler2022 uses LibSass, which is
End-of-Life as of October 2025 (announced on sass-lang.com). LibSass has had
no commits since December 2023. It cannot compile the `@use`/`@forward`
module system syntax, and will never support new CSS color spaces or other
modern Sass features.

### node-sass — Dead

**Confidence: High**

- Marked end-of-life July 24, 2024 (official Sass blog announcement)
- npm package marked deprecated
- GitHub repository archived
- No new releases in 1.5+ years as of the announcement

### LibSass — Dead

**Confidence: High**

- End-of-life announced October 23, 2025 (official Sass blog)
- Deprecated since October 2020
- No commits since December 2023
- "LibSass is no longer maintained and will receive no future updates"

### Dart Sass — The Only Active Implementation

**Confidence: High**

- Current version: 1.99.0 (May 2026)
- Only implementation receiving new features
- Supports `@use`/`@forward` module system
- Supports modern CSS color spaces (oklch, display-p3, etc.)
- Available as Dart VM binary, JavaScript npm package, and embedded host

### .NET Dart Sass Compiler Packages

| Package                     | Stars | Compiler                      | Blazor WASM         | Status                        |
| --------------------------- | ----- | ----------------------------- | ------------------- | ----------------------------- |
| **AspNetCore.SassCompiler** | 246   | dart-sass (bundled binary)    | Yes (explicit)      | Active, v1.98.0               |
| **DartSassBuilder**         | 38    | DartSassHost (ClearScript V8) | Yes (implicit)      | Active, .NET 8+               |
| BuildWebCompiler2022        | 86    | LibSass                       | No explicit support | Active fork, dead engine      |
| LibSassBuilder              | N/A   | LibSass                       | Yes (implicit)      | Superseded by DartSassBuilder |

### Recommendation: AspNetCore.SassCompiler

**Confidence: High**

AspNetCore.SassCompiler is the clear winner:

- **246 GitHub stars** (most popular .NET SCSS compiler)
- **110 releases** (most actively maintained)
- **Uses dart-sass** (the only maintained Sass implementation)
- **Explicit Blazor WASM support** with documented sample
- **MSBuild-integrated**: compiles on build and publish
- **Configurable** via `sasscompiler.json` or `appsettings.json`
- **Scoped CSS support** for Blazor
- **Cross-platform**: works on Windows, Linux, macOS (including Alpine with `gcompat`)
- **No Node.js required**

Installation:

```bash
dotnet add package AspNetCore.SassCompiler
```

---

## 4. The @import Deprecation

**Confidence: High**

- Dart Sass 1.80.0 (October 17, 2024): `@import` officially deprecated
- Dart Sass 3.0.0: `@import` will be **removed entirely**
- Replacement: `@use` and `@forward` module system (available since 2019)
- A [migration tool](https://sass-lang.com/documentation/cli/migrator/) is
  available to automate conversion

**Impact on current codebase**: Your SCSS files likely use `@import`
extensively. LibSass (via BuildWebCompiler2022) cannot compile `@use`/`@forward`
at all. This means you cannot modernize your SCSS syntax while staying on
BuildWebCompiler2022. This is the strongest reason to migrate compilers.

---

## 5. Importing Minified CSS into SCSS

**Confidence: High**

### Can you import Font Awesome's `all.min.css` into SCSS?

**Yes, with caveats:**

1. **Sass CSS import**: `@import 'path/to/all.min'` (note: omit `.css`
   extension). Sass will read the file and inline its contents at the
   import point. The CSS is treated as plain CSS — no Sass features
   (variables, mixins, nesting) can be applied to it.

2. **The content is concatenated, not compiled**: The minified CSS is
   copied verbatim into your output. Your output will be a single file
   containing both your compiled SCSS and the Font Awesome CSS.

3. **Better approach: Use SCSS source files**: Font Awesome distributes
   SCSS source files in `@fortawesome/fontawesome-free/scss/`. Importing
   these gives you control over what's included. You can use
   `@use '@fortawesome/fontawesome-free/scss/fontawesome'` and then
   selectively include icon styles.

4. **Simplest approach: CDN/kit**: Font Awesome's kit system serves only
   the icons you use, reducing payload. Add a single `<script>` tag to
   your HTML.

5. **For other minified vendor CSS**: Yes, you can import them into SCSS
   using `@import 'vendor/file'` (without `.css` extension), and they'll
   appear in the compiled output. This is essentially concatenation.

---

## 6. Recommended Migration Path

### Architecture: 7-1 → Simplified 4-folder

Current:

```
Styles/
  abstracts/   (_variables.scss, _mixins.scss, _functions.scss)
  base/        (_typography.scss, _reset.scss, _global.scss)
  components/  (_buttons.scss, _cards.scss, _header.scss, ...)
  features/    (feature-specific partials)
  layout/      (_grid.scss, _footer.scss)
  utilities/   (_helpers.scss)
  vendors/     (_fontawesome.scss)
  app.scss     (entry point with @import for everything)
```

Recommended:

```
Styles/
  abstracts/   (_variables.scss, _mixins.scss, _functions.scss)
  base/        (_reset.scss, _typography.scss, _global.scss)
  components/  (_buttons.scss, _cards.scss, _header.scss, _footer.scss, ...)
  vendor/      (_fontawesome.scss)
  app.scss     (entry point with @use + @forward)
```

Changes:

- Merge `layout/` into `components/` (a grid is a component; a footer is a component)
- Merge `utilities/` into `abstracts/` (helpers are just mixins/functions)
- Drop `features/` — for a SPA, everything is a component
- Rename `vendors/` to `vendor/` (consistency)
- Convert `@import` to `@use`/`@forward` module system
- Co-locate component-specific SCSS with `.razor` files where it makes sense

### Compiler: BuildWebCompiler2022 → AspNetCore.SassCompiler

Steps:

1. Remove `BuildWebCompiler2022` NuGet package and `compilerconfig.json`
2. Add `AspNetCore.SassCompiler` NuGet package
3. Configure `sasscompiler.json`:
   ```json
   {
     "Source": "Styles",
     "Target": "wwwroot/css",
     "Arguments": "--style=compressed",
     "Configurations": {
       "Debug": { "Arguments": "--style=expanded --embed-source-map" }
     }
   }
   ```
4. Run the [Sass migrator](https://sass-lang.com/documentation/cli/migrator/)
   to convert `@import` to `@use`/`@forward`
5. Fix any migration issues (namespacing, variable references)
6. Add generated `.css` files to `.gitignore` (they're regenerated on build)

### Timeline

| Phase                   | Action                                               | Urgency      |
| ----------------------- | ---------------------------------------------------- | ------------ |
| Now                     | Simplify folder structure (no tooling change needed) | Low          |
| Now                     | Audit which partials are actually needed             | Low          |
| Within 3 months         | Migrate compiler to AspNetCore.SassCompiler          | Medium       |
| With compiler migration | Convert @import to @use/@forward                     | Medium       |
| Future                  | Adopt co-located SCSS for new components             | Nice-to-have |

---

## Post-Implementation Status (2026-05-19)

The recommended simplified 4-folder structure was implemented:

```
scss/
  abstracts/       — _variables, _functions, _mixins, _animations, _placeholders
  base/            — _fonts, _reset, _typography, _global, _accessibility
  components/      — 15 files (buttons, cards, navigation, layout, etc.)
  app.scss         — single entry point with 3 @use lines
```

Removed: `features/` (5 subdirectories), `layout/` (2 files), `utilities/`
(2 comment-only placeholder files), `vendors/` (dead after Foundation
trim), `test/` (standalone test).

`app.scss` simplified from 11 `@use` lines to 3:

```scss
@use "abstracts/index" as abstracts;
@use "base/index" as base;
@use "components/index" as components;
```

CSS output unchanged (105,905 bytes). Build 0/0.

**See also**: [SCSS, Foundation, Tailwind, and daisyUI — Landscape Analysis 2026](scss-foundation-tailwind-daisyui-landscape-2026-05-19.md)
for the broader ecosystem context (SCSS declining, Foundation is dead,
daisyUI as Foundation successor).

## Sources

1. Sass Blog — "LibSass Has Reached End-Of-Life" (Oct 23, 2025):
   https://sass-lang.com/blog/libsass-is-end-of-life/

2. Sass Blog — "Node Sass is end-of-life" (Jul 24, 2024):
   https://sass-lang.com/blog/node-sass-is-end-of-life/

3. Sass Blog — "@import is Deprecated" (Oct 17, 2024):
   https://sass-lang.com/blog/import-is-deprecated/

4. Sass Documentation — @import (current):
   https://sass-lang.com/documentation/at-rules/import/

5. Remote.com Engineering Blog — "How to structure your Sass codebase"
   (Feb 5, 2025):
   https://remote.com/blog/remote-work/how-to-structure-your-sass-project

6. Sass Guidelines (Hugo Giraudel) — Architecture section:
   https://sass-guidelin.es/#architecture

7. NuGet — BuildWebCompiler2022 v1.14.15:
   https://www.nuget.org/packages/BuildWebCompiler2022

8. GitHub — failwyn/WebCompiler (BuildWebCompiler2022 source):
   https://github.com/failwyn/WebCompiler

9. GitHub — koenvzeijl/AspNetCore.SassCompiler:
   https://github.com/koenvzeijl/AspNetCore.SassCompiler

10. GitHub — deanwiseman/DartSassBuilder:
    https://github.com/deanwiseman/DartSassBuilder

11. NuGet — AspNetCore.SassCompiler:
    https://www.nuget.org/packages/AspNetCore.SassCompiler

12. Educative.io — "SASS Best Practices: 10 frontend tips" (2024-2025):
    https://www.educative.io/blog/sass-best-practices-frontend-coding-tips

13. CodeLucky — "CSS File Organization" ITCSS coverage (2025):
    https://codelucky.com/css-file-organization/

14. Reddit r/Blazor — "What Are People Using To Convert Sass Files to CSS?":
    https://www.reddit.com/r/Blazor/comments/18t7zmu/

15. Reddit r/dotnet — "A Very Fast .NET Sass Compiler Package That Doesn't Use Node":
    https://www.reddit.com/r/dotnet/comments/nzkzp8/
