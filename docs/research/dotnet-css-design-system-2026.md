---
date: 2026-05-14
title: CSS/Styling Approaches in .NET/Blazor Ecosystem (2025-2026)
tags:
  - blazor
  - css
  - design-systems
  - sass
  - scss
  - nuget
  - research
  - fluent-ui
  - mudblazor
  - tailwind
  - css-isolation
  - foundation
description: >
  Comprehensive research report on CSS/styling approaches adopted by the
  .NET/Blazor community in 2025-2026. Covers NuGet download data, Microsoft's
  official recommendations, SCSS vs vanilla CSS trends, design tokens, CSS
  isolation, and framework comparisons.
module: styling
problem_type: technology-selection
confidence: high
---

# CSS/Styling Approaches in the .NET/Blazor Ecosystem (2025-2026)

## 1. Executive Summary

The Blazor styling landscape in 2025-2026 is mid-transition. Four forces are
reshaping it simultaneously:

1. **Microsoft's strategic pivot**: At Build 2025, Microsoft designated Blazor
   as its "main investment in Web UI for ASP.NET Core" (confidence: **high**,
   [source][devclass-build]). This elevates Fluent UI Blazor from a
   community project to the de facto first-party component library.

2. **Modern CSS absorbing Sass features**: Container queries, native nesting,
   cascade layers, `:has()`, and `@scope` closed most of the gap that
   historically justified SCSS. The CSS Working Group shipped a feature wave
   between 2022-2025 that makes vanilla CSS + PostCSS a defensible choice
   (confidence: **high**, [source][native-css-vs-sass]).

3. **CSS isolation is production-ready but bounded**: Blazor's scoped CSS
   works well for user-authored components but cannot style third-party
   components without workarounds. An open proposal
   ([dotnet/aspnetcore#63091][css-isolation-proposal]) aims to fix this
   (confidence: **high**).

4. **Component libraries dominate, not CSS frameworks**: Blazor developers
   overwhelmingly choose a component library (MudBlazor, Fluent UI, Radzen)
   rather than a standalone CSS framework. The library _is_ the design system.

**Bottom line for a Foundation 6 + SCSS site**: Foundation is effectively
abandoned. SCSS is viable but declining as a default. Migrating to a
component-library-based approach is the Blazor-idiomatic path forward.

---

## 2. Microsoft's Official Stance on Blazor Styling

### Build 2025: Blazor Crowned as Primary Web UI Investment

At Microsoft Build 2025 (May 2025), the ASP.NET team stated:

> "Blazor is our main investment in Web UI for ASP.NET Core"

— Daniel Roth, Principal Program Manager, ASP.NET ([DevClass, May 29
2025][devclass-build])

This is the strongest public commitment Microsoft has made to Blazor. It
means Blazor is not an experiment or a niche — it is the forward path for
.NET web UI.

### What Microsoft Ships by Default

| Template                          | CSS/UI Framework               | Notes                                                                           |
| --------------------------------- | ------------------------------ | ------------------------------------------------------------------------------- |
| `dotnet new blazor`               | Bootstrap 5.3.x                | Default ASP.NET Core template; updated with each .NET release                   |
| `dotnet new fluentblazor`         | Fluent UI Blazor 4.x           | Microsoft-maintained NuGet template (`Microsoft.FluentUI.AspNetCore.Templates`) |
| `dotnet new fluentblazorwasm`     | Fluent UI Blazor 4.x           | Standalone WASM variant                                                         |
| `dotnet new fluentaspire-starter` | Fluent UI Blazor + .NET Aspire | Full-stack starter                                                              |

**Key insight**: Bootstrap remains the _default_ template, but Fluent UI
Blazor is the _recommended_ upgrade path. The Fluent templates ship with
proper interactive render mode selection, auth scaffolding, and design
token configuration out of the box.

### Fluent UI Blazor v5 (2026)

Version 5.0.0-rc.2 shipped April 8, 2026. Key changes:

- **Complete removal of FAST web-components dependency**. v5 is built on
  Fluent UI Web Components v3, with a new `@fluentui/tokens`-based theme
  system.
- **Comprehensive Theme API**: Full control over design tokens with
  built-in persistence and a live Theme Designer.
- **Breaking change from v4**: The underlying web component layer changed,
  but Razor markup (`FluentButton`, `FluentSelect`, etc.) stays the same
  for most components.

NuGet stats: **2.5M total downloads**, ~4,800/day average (as of May 2026).
([NuGet: FluentUI.AspNetCore.Components][nuget-fluent])

### Official Documentation Position

The [ASP.NET Core Blazor CSS isolation docs][ms-css-isolation] (updated for
.NET 10) position CSS isolation as the recommended approach for
component-level styling. The docs explicitly note:

> "CSS preprocessors are useful for improving CSS development by utilizing
> features such as variables, nesting, modules, mixins, and inheritance.
> While CSS isolation doesn't natively support CSS preprocessors such as
> Sass or Less, integrating CSS preprocessors is seamless as long as
> preprocessor compilation occurs before Blazor rewrites the CSS selectors
> during the build process."

Translation: SCSS still works, but Microsoft views it as a pre-build step,
not a first-class part of the pipeline.

---

## 3. Blazor Component Library Landscape

### NuGet Download Data (May 2026)

| Library                        | Total Downloads | Daily Average  | License                 | Design System                         |
| ------------------------------ | --------------- | -------------- | ----------------------- | ------------------------------------- |
| **MudBlazor**                  | 28.7M           | ~70,000        | MIT                     | Material Design (MD2)                 |
| **Microsoft Fluent UI Blazor** | 2.5M            | ~4,800         | MIT                     | Fluent Design System                  |
| Radzen Blazor                  | ~3-5M (est.)    | ~10,000 (est.) | MIT (free tier)         | Material + Fluent themes              |
| Syncfusion Blazor              | Commercial      | Commercial     | Commercial              | Bootstrap, Fluent, Tailwind, Material |
| DevExpress Blazor              | Commercial      | Commercial     | Commercial              | Fluent, Material, Bootstrap           |
| Telerik Blazor                 | Commercial      | Commercial     | Commercial              | Multiple themes                       |
| Blazorise                      | ~2-3M (est.)    | ~5,000 (est.)  | Apache 2.0 / Commercial | Bootstrap, Material, AntDesign, etc.  |

_Sources_: [NuGet: MudBlazor][nuget-mud], [NuGet: FluentUI][nuget-fluent],
[turbogeek.org][turbogeek-mud]

### MudBlazor Dominance

MudBlazor is the clear community favorite with **28.7M total downloads** and
**10,000+ GitHub stars**. Reasons for dominance:

- First-mover advantage (launched 2020)
- Massive component coverage (DataGrid, Charts, DatePicker, etc.)
- Strong documentation with interactive playground
- Material Design familiarity from web developers
- MIT license with no commercial tier

**Concerns**:

- MudBlazor's Material Design is based on MD2 (Android Material Design 2),
  not the newer MD3 (Material You). The Fluent UI team has publicly noted
  this as a differentiation point.
- MudBlazor components don't natively support Blazor CSS isolation (scoped
  `b-xxxxx` attributes don't apply to their internal HTML). See
  [MudBlazor#12391][mudblazor-css-iso].
- The v9 roadmap was described by some community members as "underwhelming"
  ([r/Blazor][reddit-v9-roadmap]).

### Fluent UI Blazor Growth Trajectory

Fluent UI Blazor is growing faster in percentage terms (2.5M downloads from
near-zero in 2023) but remains far behind MudBlazor in absolute numbers. Its
strategic advantages:

- **First-party Microsoft backing**: Maintained by Microsoft employees.
  While not officially part of ASP.NET Core, it has implicit endorsement.
- **Fluent Design System alignment**: Matches Office 365, Teams, Azure
  Portal aesthetics — important for internal enterprise apps.
- **Design tokens first**: Theme configuration is centralized and derives
  component colors/spacing automatically.
- **v5 removes FAST dependency**: The library is now self-contained with
  Fluent UI Web Components v3.

### Decision Framework from Community

From [.Net Code Chronicles comparison][fluent-vs-mud-vs-radzen]:

- **Greenfield business app with CRUD + dashboards**: MudBlazor (fast
  start, solid docs).
- **Enterprise app matching Microsoft design**: Fluent UI Blazor (tokens,
  a11y, brand fit).
- **Low-code / visual builder needs**: Radzen (drag-and-drop Blazor Studio).

---

## 4. Foundation 6 Status Assessment

**Confidence**: High.

Foundation for Sites was created by ZURB in 2011. As of 2026:

- ZURB handed off maintenance to community volunteers in **2019**.
- Last stable release: **v6.9.0** (September 2024). That release was
  primarily a compatibility update for Node.js 16+ and Sass mixin fixes.
- The GitHub repository shows minimal activity beyond dependency bumps.
- Foundation is absent from all major 2025-2026 "best CSS frameworks" lists.
- BrowserStack's [2025 CSS frameworks guide][browserstack-css] classifies
  Foundation as "formerly managed by ZURB, now run by volunteers since
  2019."
- The framework still requires **jQuery** for JavaScript components, a
  pattern widely considered legacy.

### Verdict

Foundation 6 is in **maintenance mode with no active feature development**.
It is not abandoned in the strict sense (the repo accepts critical fixes),
but it is no longer a competitive choice for new projects. Continuing to
depend on Foundation 6 carries:

- **jQuery dependency** (unnecessary in Blazor WASM)
- **No design token system** for theming
- **No Blazor component wrappers** (unlike every competing approach)
- **No integration with Blazor CSS isolation**

---

## 5. SCSS vs. Vanilla CSS in 2026

### The Modern CSS Feature Wave (2022-2025)

The CSS Working Group shipped features that directly replace SCSS's
historical advantages ([Riad Kilani, 2026][native-css-vs-sass]):

| SCSS Feature             | Modern CSS Equivalent                              | Browser Support (2026) |
| ------------------------ | -------------------------------------------------- | ---------------------- |
| `$variables`             | Custom Properties (`--var`)                        | Universal (96%+)       |
| Nesting                  | Native CSS Nesting (`&`)                           | 93%+ (Baseline 2024)   |
| `@mixin` / `@include`    | No direct equivalent (CSSWG discussing)            | Not yet                |
| `@function` (color math) | `color-mix()`, `oklch()`, relative color syntax    | 93%+                   |
| `@extend`                | Cascade layers (`@layer`) for priority management  | 94%+                   |
| Partials (`@import`)     | CSS `@import` (or bundler concatenation)           | Universal              |
| Darken/lighten           | `color-mix(in oklch, var(--color), black 20%)`     | 93%+                   |
| Loops (`@for`)           | No equivalent (rarely needed with utility classes) | N/A                    |

### Industry Sentiment

- **Sass downloads**: 26M+/week (dominating npm). But this reflects
  _existing_ projects and Bootstrap-derived themes, not new starts.
- **Job listings**: "[Frontend roles in 2025 and 2026 increasingly list
  'modern CSS' or 'vanilla CSS' as the core requirement, with Sass
  relegated to 'nice to have'"](https://tech-insider.org/sass-vs-css-2026/)
  ([source][tech-insider-sass]).
- **CSS-Tricks**: Published "Is it Time to Un-Sass?" in 2024, arguing
  native CSS nesting eliminates the primary reason for SCSS.
- **Reddit r/css**: Consensus in 2025 threads: "I wouldn't start learning
  Sass in 2025 without a real need for it."

### SCSS Remains Valid When

1. You need `@mixin` with arguments (the one major gap)
2. You maintain a Bootstrap-derived theme (Bootstrap's source is SCSS)
3. You use Foundation 6 (SCSS is its native format)
4. You have a large existing SCSS codebase with custom functions
5. You need build-time color math that exceeds `color-mix()` capabilities

### SCSS + CSS Isolation Gotcha

Microsoft's docs note SCSS works with CSS isolation **only if** SCSS
compilation occurs _before_ Blazor's build-time CSS selector rewriting.
This means `.razor.scss` files are not natively supported — you must compile
SCSS to `.razor.css` as a pre-build step or use a separate SCSS pipeline for
global styles while using vanilla CSS in `.razor.css` files.

---

## 6. CSS Isolation in Blazor: Production Readiness

### What Works (High Confidence)

CSS isolation is **production-ready** in .NET 8, 9, and 10. The core
mechanism:

1. Create `Component.razor.css` alongside `Component.razor`
2. Blazor adds a scoped attribute (`b-12ab34cd`) to the component's HTML
3. CSS selectors are rewritten to match only elements with that attribute
4. All `*.razor.css` files are bundled into `{Project}.styles.css`

This works reliably for:

- Standalone pages and components
- Components nested inside parent components that have scoped CSS
- `::deep` combinator for child component descendants
- NuGet packages and Razor Class Libraries (RCLs) that ship `.razor.css`

### Known Limitations (High Confidence)

1. **Third-party component incompatibility** (critical): CSS isolation is
   build-time only. Pre-compiled third-party components (MudBlazor, Radzen,
   Fluent UI) cannot receive scoped attributes. Your `.razor.css` cannot
   style internal elements of a `<MudButton>` or `<FluentCard>`. The
   workaround is `::deep` with a wrapping `<div>` ([Jonathan
   Crozier][crozier-deep]).

2. **Popup/overlay components** (moderate): Components rendered outside the
   DOM hierarchy (portals, modals, toasts) live outside the scoped tree.
   DevExpress explicitly documents that `DxPopup`, `DxDropDown`, and
   similar components are exceptions.

3. **CSS caching in deployments** (moderate): Blazor aggressively caches
   `*.styles.css`. Community reports of changes not reflecting in
   production deployments without cache-busting.

4. **MAUI Blazor Hybrid in .NET 10** (moderate): A [reported
   bug][maui-css-iso-bug] where the scoped CSS bundle file is never
   generated in .NET 10 MAUI Hybrid projects.

5. **No support for `@import` in Razor code blocks**: Imports must be at
   the CSS file level.

### Proposed Fix: `blazor-css-scope` Attribute

An active proposal ([dotnet/aspnetcore#63091][css-isolation-proposal]) would
allow third-party libraries to opt into CSS isolation by adding a
`blazor-css-scope` attribute. This is not yet implemented as of .NET 10.

### Recommendation

CSS isolation is ideal for **application-specific styling** (layout, custom
components, page-level overrides). It is not suitable for **overriding
third-party component internals**. Most Blazor developers use a hybrid:

- Component library for base components (MudBlazor/Fluent UI/Radzen)
- `.razor.css` for page/feature-level custom styles
- Global SCSS/CSS for design tokens, typography, and utility classes

---

## 7. Design Tokens in .NET — The Emerging Pattern

### What Are Design Tokens?

Design tokens are platform-agnostic, named design decisions (colors, spacing,
typography, shadows) stored as data. They enable consistent theming across
platforms and frameworks. The [W3C Design Tokens Community Group][w3c-dtcg]
is standardizing the format.

### .NET Implementations

#### Fluent UI Blazor (v5) — Most Advanced

- **Theme API**: Set brand colors, density, border radius as tokens.
- **Live Theme Designer**: Visual editor for token configuration.
- **Persistence**: Tokens can be saved to/loaded from JSON.
- **Underlying engine**: `@fluentui/tokens` from Fluent UI Web Components v3.
- **Dark/light mode**: Automatic derivation from token values.

#### Radzen Blazor — Design Tokens System

- Described as "Tailwind but for Blazor components" ([Toxigon][toxigon-radzen]).
- Define colors and spacing scales once; all components derive from them.
- Supports Material Design and Fluent themes with token-based switching.

#### DevExpress — Foundation Tokens + Theme Collections

- Shared Foundation Tokens for core design values.
- Theme-specific token collections (Fluent theme, Material theme).
- Tokens available in both code and Figma (designer-developer handoff).

### The Gap: No .NET-Native Token Generator

Unlike the JavaScript ecosystem (Style Dictionary, Tokens Studio), there is
no mature .NET-native design token pipeline that:

- Reads tokens from a JSON/YAML spec
- Generates C# constants, SCSS variables, and CSS custom properties
- Integrates with MSBuild

Teams building custom design systems in .NET currently roll their own
token-to-CSS generation or use PostCSS plugins.

---

## 8. Tailwind CSS in Blazor

### Adoption Status

Tailwind CSS is gaining traction in the Blazor community as an alternative
to component libraries. Key patterns:

1. **Tailwind v4 Standalone + Blazor WASM**: A 2026 article on
   [dev.to][tailwind-v4-blazor] demonstrates a "disciplined integration
   walkthrough" using the Tailwind v4 standalone CLI. The approach avoids
   npm entirely — the standalone binary is invoked as a build step.

2. **Hot Reload improvements**: .NET 9 fixed Tailwind hot reload issues
   that previously required restarting the app on every class change
   ([Medium][tailwind-hot-reload]).

3. **"Pure Blazor + Tailwind"**: A growing faction on r/Blazor and
   r/dotnet advocates using Blazor with Tailwind CSS and _no component
   library at all_. Quote from [r/dotnet][reddit-pure-blazor]: "on my next
   project, i decided to use pure blazor with tailwind css, no frameworks."

4. **Syncfusion themes**: Syncfusion Blazor components now ship with
   Tailwind CSS theme files, making them compatible with Tailwind-based
   layouts.

### Blazor Blueprint (shadcn/ui-inspired)

[Blazor Blueprint](https://github.com/BlazorBlueprint/BlazorBlueprint) is a
new open-source project offering 65+ components inspired by shadcn/ui. It
demonstrates the "copy-paste components, not a dependency" approach in
Blazor, paired with Tailwind utility classes.

### Strengths for Blazor

- No JavaScript build pipeline required (Tailwind standalone CLI)
- Works naturally with `.razor.css` isolation (utility classes are global,
  custom styles are scoped)
- Avoids lock-in to any one component library's design system
- Full control over HTML structure (no black-box component internals)

### Weaknesses for Blazor

- No pre-built interactive components (DataGrid, DatePicker, Charts) —
  you build or integrate these separately
- Higher initial effort than dropping in MudBlazor
- Blazor's component model makes some Tailwind patterns (like `@apply` in
  component-specific CSS) feel redundant

---

## 9. The "No Framework" / Vanilla CSS Movement

### Who's Doing It

A vocal minority of Blazor developers are dropping CSS frameworks entirely
and using modern CSS features directly:

- **Cascade layers** (`@layer`) replace framework specificity hacks
- **Container queries** (`@container`) replace framework-responsive grids
- **Custom properties** (`--var`) replace SCSS variables
- **Native nesting** (`&`) replaces SCSS nesting
- **`:has()` selector** replaces JavaScript state management for UI states

### When It Makes Sense

- Small-to-medium Blazor WASM sites with ~10-20 pages
- Teams with strong CSS expertise (not primarily C# developers)
- Projects where bundle size matters (no framework CSS overhead)
- Sites with custom designs that don't match Material/Fluent/Bootstrap

### When It Doesn't

- Enterprise apps needing DataGrids, Charts, Schedulers, DatePickers
- Teams where C# developers do the CSS (component libraries provide
  sensible defaults)
- Rapid prototyping or internal tools where speed matters more than
  pixel-perfect design

---

## 10. Comparative Analysis

### Foundation 6 + SCSS (Current) vs. Alternatives

| Criterion                | Foundation 6 + SCSS        | MudBlazor                   | Fluent UI Blazor            | Pure Blazor + Tailwind |
| ------------------------ | -------------------------- | --------------------------- | --------------------------- | ---------------------- |
| Maintenance              | Volunteer, near-dormant    | Active (monthly releases)   | Active (Microsoft-backed)   | Self-maintained        |
| Blazor native            | No wrappers                | Full component library      | Full component library      | N/A (you build)        |
| jQuery required          | Yes                        | No                          | No                          | No                     |
| Design tokens            | No                         | MudTheme                    | Full Theme API v5           | Tailwind config        |
| CSS isolation compatible | Partial (global SCSS only) | Limited (::deep workaround) | Limited (::deep workaround) | Fully compatible       |
| Learning curve           | Low (if you know SCSS)     | Low                         | Low-Medium                  | Medium-High            |
| Bundle size              | ~100KB (Foundation)        | ~200KB (MudBlazor CSS+JS)   | ~150KB (Fluent UI)          | ~4KB (purged Tailwind) |
| a11y compliance          | Basic                      | Good                        | Excellent (Fluent spec)     | Depends on you         |
| Dark mode                | Manual                     | MudThemeProvider            | Theme API (automatic)       | Tailwind dark: prefix  |

### Migration Difficulty from Foundation 6 + SCSS

| To                | Difficulty | Rationale                                                                              |
| ----------------- | ---------- | -------------------------------------------------------------------------------------- |
| Fluent UI Blazor  | Medium     | Replace grid/layout with Fluent components; SCSS → design tokens; drop jQuery          |
| MudBlazor         | Medium     | Same as above but Material Design look                                                 |
| Tailwind + Blazor | High       | Rewrite all markup with utility classes; build interactive components                  |
| Vanilla CSS       | Low-Medium | Keep existing markup; replace Foundation classes with custom CSS using modern features |

---

## 11. Recommendations for This Project

### Immediate (Near-Term)

1. **Stop depending on Foundation 6 for new features**. It adds jQuery to
   a Blazor WASM app, has no design token system, and is in maintenance
   mode. New pages/components should use Blazor-native approaches.

2. **Keep SCSS for global styles only**. Do not invest further in the
   7-1 SCSS pattern. Move new component styles to `.razor.css` files with
   vanilla CSS (native nesting is adequate for component-scoped styles).

3. **Adopt CSS custom properties for theming**. Replace SCSS variables
   (which are compile-time only) with CSS custom properties on `:root`.
   This enables runtime theme switching (dark mode) without SCSS
   recompilation.

### Medium-Term (Next 3-6 Months)

4. **Evaluate Fluent UI Blazor vs. MudBlazor**. If the site needs
   interactive components (DataGrid, Forms, Dialogs), adopting a component
   library eliminates custom component maintenance. Decision matrix:
   - **Fluent UI Blazor**: If the app serves an enterprise/Office 365
     audience or needs to match Microsoft design language.
   - **MudBlazor**: If the app serves general users and needs the broadest
     component coverage with the fastest development speed.

5. **Plan Foundation 6 removal**. Strategy:
   - Phase 1: Replace Foundation's grid with CSS Grid (modern, native, no
     framework).
   - Phase 2: Replace Foundation's UI components (buttons, forms, modals)
     with Blazor components or a component library.
   - Phase 3: Remove Foundation JS and jQuery dependency.

### Long-Term (6-12 Months)

6. **Adopt a design-token-driven architecture**. Regardless of which
   component library or CSS approach you choose, centralize design
   decisions as tokens:
   - Colors, spacing, typography scale, border radius, shadows
   - Store as CSS custom properties on `:root`
   - Reference in both global CSS and `.razor.css` files
   - This makes framework/library migration a matter of redefining tokens,
     not rewriting styles.

7. **Consider Tailwind CSS for utility-driven development**. If you enjoy
   the component-authoring control of Blazor but want faster styling,
   Tailwind offers the best of both worlds: utility classes for rapid
   iteration with full control over HTML structure.

---

## 12. Sources & Citations

| Source                                                                               | Type                 | Confidence   |
| ------------------------------------------------------------------------------------ | -------------------- | ------------ |
| [Microsoft: Blazor CSS Isolation docs (.NET 10)][ms-css-isolation]                   | Official docs        | High         |
| [DevClass: Microsoft designates Blazor as main web UI investment][devclass-build]    | Tech press           | High         |
| [NuGet: MudBlazor v9.4.0][nuget-mud]                                                 | Package registry     | High         |
| [NuGet: Microsoft.FluentUI.AspNetCore.Components v4.14.1][nuget-fluent]              | Package registry     | High         |
| [Fluent UI Blazor v5.0 RC2 blog post][fluent-v5-rc2]                                 | Maintainer blog      | High         |
| [Riad Kilani: Native CSS vs Sass in 2026][native-css-vs-sass]                        | Blog analysis        | High         |
| [Tech Insider: Sass vs CSS 2026][tech-insider-sass]                                  | Industry analysis    | Moderate     |
| [CSS-Tricks: Is it Time to Un-Sass?][css-tricks-un-sass]                             | Industry publication | High         |
| [GitHub: CSS isolation for third-party components proposal][css-isolation-proposal]  | Open source issue    | High         |
| [Jonathan Crozier: Styling Blazor child components with CSS isolation][crozier-deep] | Developer blog       | High         |
| [MudBlazor: CSS isolation limitation issue #12391][mudblazor-css-iso]                | Open source issue    | High         |
| [.Net Code Chronicles: Fluent UI vs MudBlazor vs Radzen][fluent-vs-mud-vs-radzen]    | Blog comparison      | Moderate     |
| [Tailwind CSS v4 Standalone in Blazor WASM][tailwind-v4-blazor]                      | Developer tutorial   | Moderate     |
| [MAUI CSS Isolation bug #33718][maui-css-iso-bug]                                    | Open source issue    | High         |
| [Reddit r/Blazor: Future of Blazor][reddit-blazor-future]                            | Community discussion | Low-Moderate |
| [BrowserStack: Top CSS Frameworks 2025][browserstack-css]                            | Industry roundup     | Moderate     |

---

## Appendix: Key Version Numbers (May 2026)

| Technology                    | Version                     | Status                          |
| ----------------------------- | --------------------------- | ------------------------------- |
| .NET SDK                      | 10.0.x                      | Current (builds net9.0 targets) |
| ASP.NET Core                  | 10.0.x                      | Current                         |
| Blazor                        | 10.0.x                      | Current                         |
| MudBlazor                     | 9.4.0                       | Stable                          |
| Fluent UI Blazor              | 4.14.1 (stable), 5.0.0-rc.2 | Stable + RC                     |
| Foundation for Sites          | 6.9.0                       | Maintenance mode                |
| Bootstrap (ASP.NET templates) | 5.3.x                       | Default template                |
| SCSS (Dart Sass)              | 1.83.x                      | Stable                          |
| Tailwind CSS                  | 4.0.x                       | Stable                          |
| CSS Nesting                   | Baseline 2024               | Widely available                |
| Container Queries             | Baseline 2023               | Widely available                |
| Cascade Layers                | Baseline 2022               | Widely available                |

[ms-css-isolation]: https://learn.microsoft.com/en-us/aspnet/core/blazor/components/css-isolation?view=aspnetcore-10.0
[devclass-build]: https://devclass.com/2025/05/29/microsoft-designates-blazor-as-its-main-future-investment-in-web-ui-for-net/
[nuget-mud]: https://www.nuget.org/packages/MudBlazor/
[nuget-fluent]: https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components/
[fluent-v5-rc2]: https://baaijte.net/blog/microsoft-fluentui-aspnetcore.components-50-rc2/
[native-css-vs-sass]: https://blog.riadkilani.com/native-css-vs-sass-2026/
[tech-insider-sass]: https://tech-insider.org/sass-vs-css-2026/
[css-tricks-un-sass]: https://css-tricks.com/is-it-time-to-un-sass/
[css-isolation-proposal]: https://github.com/dotnet/aspnetcore/issues/63091
[crozier-deep]: https://jonathancrozier.com/blog/styling-blazor-child-components-with-css-isolation-what-you-really-need-to-know
[mudblazor-css-iso]: https://github.com/MudBlazor/MudBlazor/issues/12391
[fluent-vs-mud-vs-radzen]: https://medium.com/net-code-chronicles/fluentui-vs-mudblazor-vs-radzen-ae86beb3e97b
[tailwind-v4-blazor]: https://dev.to/cristiansifuentes/tailwind-css-v4-standalone-in-blazor-webassembly-a-clean-native-integration-for-the-net-26lk
[maui-css-iso-bug]: https://github.com/dotnet/maui/issues/33718
[reddit-blazor-future]: https://www.reddit.com/r/Blazor/comments/1ljcvbn/future_of_blazor/
[browserstack-css]: https://www.browserstack.com/guide/top-css-frameworks
[turbogeek-mud]: https://turbogeek.org/getting-started-with-blazor-build-your-first-interactive-web-app-in-c/
[toxigon-radzen]: https://toxigon.com/blazor-in-2025
[reddit-pure-blazor]: https://www.reddit.com/r/dotnet/comments/1p8kb34/ui_frameworks_paid_or_free_for_blazor_web_app_and/
[w3c-dtcg]: https://www.w3.org/community/design-tokens/
[reddit-v9-roadmap]: https://www.reddit.com/r/Blazor/comments/1ljcvbn/future_of_blazor/
[tailwind-hot-reload]: https://medium.com/@pinyo.rungoral/tailwind-css-in-net-9-blazor-fixing-hot-reload-issues-5ccc49a37954

## 13. Related Research

- [SCSS, Foundation, Tailwind, and daisyUI — Landscape Analysis 2026](scss-foundation-tailwind-daisyui-landscape-2026-05-19.md) —
  Updated 2026 landscape with Foundation-vs-daisyUI component mapping,
  migration feasibility, and decision framework.
