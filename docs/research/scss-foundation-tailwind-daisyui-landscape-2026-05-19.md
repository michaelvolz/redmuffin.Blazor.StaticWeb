---
date: 2026-05-19
title: "SCSS, Foundation, Tailwind, and daisyUI — Landscape Analysis for Blazor WASM (2026)"
tags:
  [scss, foundation, tailwind, daisyui, blazor, css, design-system, research]
description: >
  Comprehensive landscape analysis answering: (1) Do Blazor sites use SCSS?
  (2) What's different from traditional SCSS? (3) What do they use instead?
  (4) Is SCSS still relevant in 2026? Includes Foundation-vs-Tailwind-vs-daisyUI
  component comparison, migration feasibility, and decision framework.
module: styles
problem_type: architecture-decision
---

# SCSS, Foundation, Tailwind, and daisyUI — Landscape 2026

## Executive Summary

- **Blazor + SCSS**: Not the default. Microsoft's templates ship Bootstrap + plain CSS + CSS isolation. SCSS requires workaround NuGet packages (AspNetCore.SassCompiler). Few Blazor projects use SCSS outside component library authors.
- **SCSS overall**: 61% of CSS developers still use Sass (State of CSS 2025), down from 67% in 2024. Declining slowly. 26.5M weekly npm downloads — mostly legacy.
- **What replaced SCSS**: Tailwind CSS (51% usage, #1 framework), native CSS features (nesting, custom properties, `color-mix()`), PostCSS.
- **Foundation is dead**: Last release v6.9.0 (2023). No v7 roadmap. 30+ open PRs. Volunteer-maintained. 384 dart-sass deprecation warnings with no fix path.
- **daisyUI is the closest Foundation successor**: Free, CSS-only, 65 components, semantic class names (`btn`, `card`, `alert`, `breadcrumbs`). Maps 1:1 with Foundation's component model. Built on Tailwind v4 infrastructure but you never write Tailwind utility classes.
- **Migration is feasible but not urgent**: Our 10-module Foundation footprint is small. daisyUI covers 8 of 10 with direct equivalents. The grid is the main pain point. SCSS pipeline stays unchanged.

---

## 1. Blazor + SCSS Landscape

### Official Microsoft Position

Blazor's CSS isolation (`Component.razor.css`) is the first-party styling approach. It rewrites selectors at build time with unique scope identifiers (`h1[b-3xxtam6d07]`). This system does not natively support CSS preprocessors.

From [Microsoft Learn (updated Nov 2025)](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/css-isolation?view=aspnetcore-9.0):

> "While CSS isolation doesn't natively support CSS preprocessors such as Sass or Less, integrating CSS preprocessors is seamless as long as preprocessor compilation occurs before Blazor rewrites the CSS selectors during the build process."

### Default Templates

Every `dotnet new blazor` template ships:

- **Bootstrap 5** — precompiled `bootstrap.min.css` in `wwwroot/css/bootstrap/`
- **Plain CSS** — `wwwroot/css/app.css` for loading screen and layout
- **CSS isolation** — `.razor.css` files co-located with components

### NuGet Packages for SCSS

| Package                     | Mechanism                | Notes                                                                        |
| --------------------------- | ------------------------ | ---------------------------------------------------------------------------- |
| **AspNetCore.SassCompiler** | MSBuild task + dart-sass | 246 GitHub stars. Watcher doesn't work for Blazor WASM. Most popular option. |
| **Delegate.SassBuilder**    | MSBuild-based            | Older. Mentioned in community threads.                                       |
| **BuildWebCompiler2022**    | MSBuild + LibSass        | LibSass is EOL (Oct 2025). We migrated away from this.                       |

**Key limitation**: None integrate with CSS isolation. SCSS must compile _before_ Blazor's scope rewriting. If you want both, you need a pre-build step that most tools don't automate.

### What the Ecosystem Actually Uses

1. Bootstrap 5 (default template, precompiled CSS)
2. CSS isolation (first-party, plain CSS only)
3. Tailwind CSS (surging — CLI scans `.razor` files, zero preprocessor)
4. Component libraries (MudBlazor, Blazorise, Radzen — each bundles its own CSS)
5. SCSS (niche — mostly Bootstrap/Foundation project carryovers)

CSS-in-JS has essentially zero Blazor adoption.

**Source**: Microsoft Learn docs, NDC Conferences 2025 agenda (tailwind talk by Chris Sainty), GitHub dotnet/aspnetcore#60351 (official request for Tailwind template), StackOverflow, Reddit r/Blazor.

---

## 2. SCSS in Broader Web Development (2025–2026)

### Hard Numbers

| Metric                         | Value               | Source                                                       |
| ------------------------------ | ------------------- | ------------------------------------------------------------ |
| Sass usage (State of CSS 2024) | 4,652 / 6,897 (67%) | [2024.stateofcss.com](https://2024.stateofcss.com)           |
| Sass usage (State of CSS 2025) | 2,434 / 3,977 (61%) | [2025.stateofcss.com](https://2025.stateofcss.com)           |
| No preprocessor at all (2024)  | 1,320 / 6,897 (19%) | 2024.stateofcss.com                                          |
| No preprocessor at all (2025)  | 838 / 3,977 (21%)   | 2025.stateofcss.com                                          |
| `sass` npm weekly downloads    | 26.5M               | [npmjs.com](https://www.npmjs.com/package/sass)              |
| `postcss` npm weekly downloads | 215.9M              | npmjs.com (largely transitive through Tailwind/Vite/Next.js) |

**Trend**: Declining 6 percentage points year-over-year. "No preprocessor" rising 2 points. The installed base is enormous but new greenfield projects increasingly skip Sass.

### Native CSS Features That Replaced Sass

| Sass Feature           | Native CSS Replacement         | Browser Support (2026) |
| ---------------------- | ------------------------------ | ---------------------- |
| `$variables`           | `--custom-properties`          | 97%+                   |
| Nesting                | Native CSS nesting             | 90%+                   |
| `lighten()`/`darken()` | `color-mix()` + `light-dark()` | 92%+                   |
| `@import` partials     | `@layer`                       | Baseline-supported     |
| Parent selector        | `:has()`                       | 92%+                   |

**What Sass still uniquely provides**: `@for`/`@each` loops, `@if` conditionals, parametric `@mixin`s, and the `@use`/`@forward` module system. Native CSS has no procedural logic equivalent.

### Sass in Framework Documentation

| Framework   | Primary Recommendation         | Sass Support                            |
| ----------- | ------------------------------ | --------------------------------------- |
| Next.js     | Tailwind CSS (default)         | Optional (`sass` + `.module.scss`)      |
| Angular v21 | CSS (default)                  | First-class CLI option (`--style=scss`) |
| Vue/Nuxt    | Scoped `<style>`               | `<style lang="scss">` via sass-loader   |
| Laravel 12  | Tailwind v4 (all starter kits) | —                                       |
| Rails 8     | Propshaft + plain CSS          | —                                       |
| Blazor      | Bootstrap + CSS isolation      | Not supported natively                  |

**No major framework recommends SCSS as the primary approach in 2026.** Angular is the closest — it offers it as a first-class CLI option and professional Angular shops overwhelmingly choose SCSS.

### Who Migrated Away From Sass

| Organization | System     | Status                                                   | When      |
| ------------ | ---------- | -------------------------------------------------------- | --------- |
| GitHub       | Primer     | PostCSS + native CSS                                     | 2023–2024 |
| Shopify      | Polaris    | CSS custom properties, deprecating Sass                  | 2025      |
| Tailwind CSS | v4         | Rust engine, zero Sass                                   | 2025      |
| Adobe        | Spectrum 2 | Custom build-time CSS macros (Parcel)                    | 2025–2026 |
| Bootstrap    | v5         | Still Sass-native, no migration planned                  | —         |
| Salesforce   | SLDS       | Still SCSS (SLDS 2 is CSS-based but SCSS sources remain) | —         |

### Where SCSS Still Fits

1. **Framework theming** — Bootstrap, Bulma, Foundation are Sass-native. Overriding their variables requires a Sass build.
2. **Procedural logic** — loops, conditionals, parametric mixins for generating large volumes of CSS.
3. **Legacy codebases** — migration cost rarely beats leaving working code alone.
4. **Module organization** — `@use`/`@forward` provide explicit dependency boundaries that native CSS lacks.
5. **Angular professional projects** — SCSS is the de facto standard.

**Source**: State of CSS surveys (2023–2025), npm download statistics, [tech-insider.org/sass-vs-css-2026](https://tech-insider.org/sass-vs-css-2026/), framework official docs, GitHub Primer and Shopify Polaris migration announcements.

---

## 3. Foundation vs Tailwind vs daisyUI — Component Comparison

### Foundation 6 Status

Foundation 6.9.0 was released 2023. The project has:

- **30+ open PRs**, some 2+ years old
- **No paid maintainers** — ZURB core team disbanded
- **No v7 roadmap**
- **Volunteer maintenance only**
- **384 dart-sass `@import` deprecation warnings** — cosmetic now, will become hard errors when dart-sass removes `@import` (dart-sass 4.0, no release date)

Foundation compiles today. It will compile tomorrow. But there is no future path.

### Raw Tailwind CSS

**Tailwind core provides ZERO pre-built components.** Its Preflight CSS reset explicitly removes all default styles (including form element styling). Every visual decision is expressed as utility classes in HTML.

| Foundation Component                     | Raw Tailwind Equivalent                                                                      |
| ---------------------------------------- | -------------------------------------------------------------------------------------------- | -------------------------------------------------------------- |
| `<div class="callout alert">Error</div>` | `<div class="bg-red-50 border-l-4 border-red-500 text-red-700 p-4 rounded mb-4">Error</div>` |
| Card (5 lines, 3 classes)                | 15+ classes across 3+ divs                                                                   |
| Button group (3 classes)                 | 12+ classes across multiple elements                                                         |
| Form styling                             | Global — every element styled automatically                                                  | Needs `@tailwindcss/forms` plugin + utilities on every element |

**Raw Tailwind alone does not replace Foundation unless you accept massive HTML verbosity.**

### daisyUI as a Foundation Successor

[daisyUI](https://daisyui.com/) is a **free, CSS-only, 65-component library** built on Tailwind v4. It provides semantic class names — you write `btn`, `card`, `alert`, `breadcrumbs`, not Tailwind utilities.

**Key properties for Blazor WASM compatibility:**

- CSS-only — no JavaScript dependency
- Framework-agnostic — works with any HTML-rendering system
- MIT license — free for any use
- 30+ built-in themes — dark mode, light mode, custom themes
- Active development — daisyUI v5 in active development (2025–2026)

**Component mapping (Foundation → daisyUI):**

| Foundation           | daisyUI                      | Match Quality                                            |
| -------------------- | ---------------------------- | -------------------------------------------------------- |
| `.callout`           | `.alert`                     | Direct — 4 variants: default, soft, outline, dash        |
| `.callout.success`   | `.alert.alert-success`       | Direct                                                   |
| `.callout.warning`   | `.alert.alert-warning`       | Direct                                                   |
| `.callout.alert`     | `.alert.alert-error`         | Direct (Foundation's "alert" = daisyUI's "error")        |
| `.button`            | `.btn`                       | Direct — same color modifiers                            |
| `.button.primary`    | `.btn.btn-primary`           | Direct                                                   |
| `.button.secondary`  | `.btn.btn-secondary`         | Direct                                                   |
| `.button.alert`      | `.btn.btn-error`             | Direct                                                   |
| `.button-group`      | `.join` + `.btn.join-item`   | Different pattern but equivalent                         |
| `.card`              | `.card`                      | Direct                                                   |
| `.card-divider`      | `.card-title`                | Structural difference — daisyUI's card has title in body |
| `.card-section`      | `.card-body`                 | Direct                                                   |
| `.breadcrumbs`       | `.breadcrumbs`               | Direct                                                   |
| `.input-group`       | `.join` + `.input.join-item` | Different pattern but equivalent                         |
| `.input-group-field` | `.input.join-item`           | Part of join pattern                                     |
| `.input-group-label` | `.join-item` (on span/label) | Part of join pattern                                     |

**Module coverage for our 10 Foundation modules:**

| Foundation Module              | daisyUI Equivalent                            | Covered?         |
| ------------------------------ | --------------------------------------------- | ---------------- |
| `foundation-global-styles`     | Tailwind Preflight (different but equivalent) | ✅               |
| `foundation-xy-grid-classes`   | Tailwind flex/grid utilities                  | ⚠️ Different API |
| `foundation-typography`        | Tailwind Typography plugin (`prose`)          | ✅               |
| `foundation-button`            | daisyUI `btn`                                 | ✅               |
| `foundation-button-group`      | daisyUI `join` + `btn.join-item`              | ✅               |
| `foundation-callout`           | daisyUI `alert`                               | ✅               |
| `foundation-card`              | daisyUI `card`                                | ✅               |
| `foundation-breadcrumbs`       | daisyUI `breadcrumbs`                         | ✅               |
| `foundation-forms`             | daisyUI `input`, `textarea`, `select`, etc.   | ✅               |
| `foundation-prototype-classes` | Tailwind spacing utilities                    | ✅               |

**8 of 10 modules have direct equivalents.** The grid (XY-Grid) and global-styles are the exceptions — they're replaced by Tailwind's different-but-equivalent infrastructure.

### Verbosity Comparison (Foundation vs daisyUI)

| Component     | Foundation                                                                                   | daisyUI                                                                                                  | Raw Tailwind                                                                 |
| ------------- | -------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------- |
| Error callout | `<div class="callout alert">`                                                                | `<div class="alert alert-error">`                                                                        | `<div class="bg-red-50 border-l-4 border-red-500 text-red-700 p-4 rounded">` |
| Card          | `<div class="card"><div class="card-divider">T</div><div class="card-section">C</div></div>` | `<div class="card bg-base-100"><div class="card-body"><h2 class="card-title">T</h2><p>C</p></div></div>` | 15+ classes                                                                  |
| Breadcrumbs   | `<ul class="breadcrumbs"><li><a>Home</a></li></ul>`                                          | `<div class="breadcrumbs"><ul><li><a>Home</a></li></ul></div>`                                           | 10+ classes                                                                  |

daisyUI is **slightly more verbose** than Foundation (1–2 extra classes) but dramatically less verbose than raw Tailwind. The class names are semantic — no utility-class soup.

**Source**: [daisyUI components](https://daisyui.com/components/), [Tailwind Plus UI Blocks](https://tailwindcss.com/plus/ui-blocks/), [Flowbite](https://flowbite.com/), [Preline UI](https://preline.co/).

---

## 4. Migration Feasibility Assessment

### What We Currently Have

- 30 SCSS files across 3 directories (abstracts, base, components)
- 10 Foundation modules via `app.scss` `@include` calls
- 105,905 bytes compiled CSS
- Custom component styles for page-load-speed widget, shimmer effects, articles, videos, branding
- Self-hosted Font Awesome 6.7.0 and Outfit fonts
- dart-sass CLI compilation (no MSBuild involvement)

### Migration Path

1. **Install daisyUI**: `npm install daisyui` (or Bun), add Tailwind v4 + daisyUI to the build pipeline
2. **Replace Foundation `@include` calls**: Delete 10 lines from `app.scss`. daisyUI provides equivalent components as CSS.
3. **Update Razor class names**: Foundation's `.callout.alert` → daisyUI's `.alert.alert-error`, `.button` → `.btn`, etc. This is mostly search-and-replace.
4. **Rewrite grid markup**: Foundation's `grid-x grid-padding-x` → Tailwind's `flex flex-wrap gap-4`. Foundation's `cell small-6 medium-4` → `w-1/2 md:w-1/3`. This is the largest migration surface.
5. **Keep custom SCSS**: All 15 custom component files stay exactly as-is. They use Foundation's `breakpoint()` mixin and variables — those would need to be replaced with native `@media` queries and CSS custom properties, OR a thin migration layer.
6. **Recompile**: Tailwind v4's JIT engine scans `.razor` files and produces only the CSS classes actually used — the output will be smaller than our current 106KB.

### What Does NOT Change

- SCSS compilation pipeline (dart-sass CLI)
- Custom component styles (shimmer, page-load-speed, articles, videos, branding)
- Font self-hosting (Font Awesome, Outfit)
- Build process (`dotnet build`)
- Blazor component architecture
- CSS isolation strategy

### Effort Estimate

- **Grid migration**: 2–3 hours (touches every `.razor` file that uses Foundation grid classes)
- **Component class rename**: 1–2 hours (search-and-replace Foundation → daisyUI class names)
- **Foundation removal**: 30 minutes (delete `@include` calls from `app.scss` + remove `lib/foundation-sites/`)
- **SCSS variable migration**: 1–2 hours (Foundation `$global-margin` → CSS custom properties, `breakpoint()` → `@media`)
- **Testing**: 2–3 hours (visual verification across all pages)

**Total**: 1–2 days for a full migration.

### Risk Assessment

| Risk                                              | Severity | Mitigation                                                    |
| ------------------------------------------------- | -------- | ------------------------------------------------------------- |
| Grid classes don't map 1:1                        | Medium   | Test each page; wrap complex layouts in Blazor components     |
| daisyUI theme doesn't match current design        | Low      | daisyUI has 30+ themes; custom theme via CSS variables        |
| SCSS custom code breaks without Foundation mixins | Medium   | Replace `breakpoint()` mixin first, then Foundation variables |
| Build pipeline complexity increases               | Low      | Tailwind v4 is a single `@import "tailwindcss"` + CLI scan    |

### Why Wait?

Foundation compiles. Our 10-module footprint is small. Our SCSS pipeline works. Migration is a one-way door — once we commit to Tailwind infrastructure, we can't go back to Foundation's semantic class model.

The trigger condition for migration is: Foundation stops compiling. That hasn't happened. When dart-sass 4.0 ships and removes `@import`, Foundation 6's 384 deprecation warnings become hard errors. At that point, migration is forced. Until then, it's optional.

---

## 5. Decision Framework

| Factor                        | Stay on Foundation                              | Migrate to daisyUI                                                    |
| ----------------------------- | ----------------------------------------------- | --------------------------------------------------------------------- |
| **Urgency**                   | None — Foundation compiles today                | None — but Foundation has no future                                   |
| **Component coverage**        | 10/10 modules working                           | 8/10 direct equivalents, grid needs rewrite                           |
| **Maintenance burden**        | Foundation is unmaintained; we own all bugs     | Actively maintained by daisyUI team and Tailwind Labs                 |
| **SCSS pipeline**             | Works. `@import` deprecation is a ticking clock | Works. Tailwind v4 is Sass-free.                                      |
| **Semantic class names**      | Foundation's greatest strength                  | daisyUI preserves this — `btn`, `card`, `alert`, `breadcrumbs`        |
| **Future-proofing**           | Dead end — no v7, no dart-sass 4.0 support      | Long-term — tailwind ecosystem is the CSS industry default            |
| **HTML verbosity**            | Minimal classes                                 | Slightly more (1–2 extra classes per component)                       |
| **Custom SCSS compatibility** | Full Foundation mixin/variable access           | Must migrate `breakpoint()` → `@media`, variables → custom properties |
| **Grid conciseness**          | `small-up-2`                                    | `grid-cols-2` (different API, similar conciseness)                    |

### Recommendation

**Do nothing now.** Foundation works. Our SCSS pipeline is clean and minimal (30 files, 3 directories, 106KB CSS). But document that the migration path exists and that foundation replacement with daisyUI is the preferred direction when Foundation eventually breaks.

When the trigger fires (dart-sass 4.0 or a critical Foundation bug), migrate in 1–2 days following the path outlined in §4.

**If you want to migrate proactively before the trigger**: the primary benefit is eliminating technical debt on an unmaintained dependency, not gaining new capabilities. daisyUI adds 65 components we may not use, better dark mode support, and active maintenance. The cost is 1–2 days of grid rewrites.

---

## 6. References

- [State of CSS 2025 — Pre/Post-Processors](https://2025.stateofcss.com/en-US/other-tools/#pre_post_processors)
- [Microsoft Learn — Blazor CSS Isolation (Nov 2025)](https://learn.microsoft.com/en-us/aspnet/core/blazor/components/css-isolation?view=aspnetcore-9.0)
- [AspNetCore.SassCompiler NuGet](https://www.nuget.org/packages/AspNetCore.SassCompiler)
- [Fresh Caffeine — "How to work with SASS in Blazor" (Sep 2024)](https://www.fresh-caffeine.com/blog/2024/blazor-2-working-with-sass/)
- [Tim Deschryver — "Integrating Tailwind CSS in Blazor" (Jan 2025)](https://timdeschryver.dev/blog/integrating-tailwind-css-in-blazor)
- [GitHub dotnet/aspnetcore#60351 — Tailwind template request](https://github.com/dotnet/aspnetcore/issues/60351)
- [daisyUI — Components](https://daisyui.com/components/)
- [Tech Insider — Sass vs CSS 2026](https://tech-insider.org/sass-vs-css-2026/)
- [PkgPulse — State of CSS-in-JS 2026](https://www.pkgpulse.com/blog/state-of-css-in-js-2026)
- [Foundation 6 GitHub](https://github.com/foundation/foundation-sites)
- [Masuga — Converting Foundation's Grid to Tailwind](https://www.gomasuga.com/articles/converting-foundations-grid-to-tailwind)
- [Tailwind CSS Responsive Design Docs](https://tailwindcss.com/docs/responsive-design)
- [Tailwind Plus UI Blocks](https://tailwindcss.com/plus/ui-blocks/)

## Related Research (2026-05-20)

Subsequent research expanded on this landscape analysis:

- **[daisyUI + Blazor WASM Integration](daisyui-blazor-wasm-integration.md)** — Full integration feasibility: CDN dev workflow, conditional loading, CLI performance benchmarks (5ms incremental rebuilds), CSS isolation analysis, known issues, migration plan
- **[daisyUI Long-Term Styling Evaluation](daisyui-long-term-styling-evaluation.md)** — Multi-vector evaluation: project health (daisyUI 41k stars, Tailwind Labs 75% layoffs), bus factor analysis, decision matrix (daisyUI scores 42/55 vs Foundation 26), multi-project standardization strategy
