---
name: rm-styling
description: "CSS framework selection, CSS standards and anti-patterns, SCSS architecture, Blazor styling, and WCAG 2.1 AA accessibility. Current: Foundation 6 + SCSS. Target: daisyUI v5 + Tailwind CSS v4."
---

# UI/Styling

## Current Framework (Foundation 6 + SCSS)

- Zurb Foundation 6 (self-hosted SCSS, 10 of ~38 modules via selective `@include`)
- Font Awesome 6.7.0 (self-hosted, woff2)
- SCSS compiled via `dart-sass` CLI

## Target Migration: daisyUI v5 + Tailwind CSS v4

Foundation 6 is in maintenance mode with no future path (see research docs). The migration target is **daisyUI v5 + Tailwind CSS v4**:

- **Why daisyUI**: Semantic component classes (`btn`, `card`, `alert`, `modal`), 55+ components, 30+ built-in themes, CSS-only (zero JavaScript), MIT license
- **Why Tailwind v4**: Utility classes for layout/spacing/typography, JIT purging (only used classes ship), Rust compiler (fast), standalone CLI (no Node.js)
- **Relationship**: daisyUI is a Tailwind CSS plugin — it runs inside Tailwind's compiler at build time. See `docs/research/daisyui-blazor-wasm-integration.md` §1 for the full dependency analysis.

### Dev Workflow (Post-Migration)

**Development**: CDN scripts provide instant CSS feedback — zero watchers, zero build steps.

```html
<!-- wwwroot/index.html — conditional dev-only CDN loading -->
<script src="_framework/blazor.webassembly.js"></script>
<script>
  if (window.location.hostname === "localhost") {
    var link = document.createElement("link");
    link.rel = "stylesheet";
    link.href = "https://cdn.jsdelivr.net/npm/daisyui@5";
    document.head.appendChild(link);
    var s = document.createElement("script");
    s.src = "https://cdn.jsdelivr.net/npm/@tailwindcss/browser@4";
    document.head.appendChild(s);
  }
</script>
```

- daisyUI CDN: static CSS with all 55+ component classes (~51 KB compressed)
- Tailwind CDN: generates utitility classes on-the-fly from DOM (67 KB JS, 150-300ms render-blocking on initial load — dev-only, irrelevant for localhost)
- MutationObserver detects new class names from hot-reloaded components — CSS updates instantly

**Production**: Precompiled static CSS, generated once via Tailwind CLI.

```bash
./tools/tailwindcss/tailwindcss -i wwwroot/css/input.css -o wwwroot/css/output.css --minify
```

- `output.css` is committed to the repository (like `app.min.css` today)
- MSBuild safety net runs on `dotnet publish` (Release only) to catch forgotten recompiles
- Result: ~10-15 KB gzipped, zero JavaScript, zero CDN dependency

Full workflow details: `docs/research/daisyui-blazor-wasm-integration.md` §2.1.

### daisyUI Component Class Mapping (Foundation → daisyUI)

| Foundation                               | daisyUI                                 | Notes                                           |
| ---------------------------------------- | --------------------------------------- | ----------------------------------------------- |
| `button`                                 | `btn`                                   | Same color modifiers: primary, secondary, error |
| `button-group`                           | `join` + `btn join-item`                | Different pattern, equivalent result            |
| `callout`                                | `alert`                                 | `callout.alert` → `alert alert-error`           |
| `card` / `card-divider` / `card-section` | `card` / `card-title` / `card-body`     | Structural difference in title location         |
| `grid-x grid-padding-x` / `cell`         | `grid grid-cols-N gap-4` / `col-span-N` | CSS Grid replaces XY-Grid                       |
| `breadcrumbs`                            | `breadcrumbs`                           | Direct equivalent                               |
| `input-group`                            | `join` + `input join-item`              | Different pattern                               |

Full component mapping: `docs/research/scss-foundation-tailwind-daisyui-landscape-2026-05-19.md` §3.

### Migration Strategy: Gradual Coexistence

**Do not big-bang.** Foundation and daisyUI use different CSS class names — no selector collisions. Both can coexist in the same project during migration.

**Load order during migration:**

```html
<!-- daisyUI first, Foundation second (wins reset conflicts) -->
<link href="css/output.css" rel="stylesheet" />
<link href="css/app.min.css" rel="stylesheet" />
```

Foundation loads second so its CSS reset wins any conflicts with Tailwind Preflight. Components using Foundation classes continue to render correctly.

**Grid vocabulary decision:** Accept Tailwind's `grid grid-cols-3 gap-4` syntax. Foundation's `grid-x cell medium-6` is more readable, but Tailwind grid classes have massive LLM training data — every major LLM generates them with ~95%+ accuracy. A custom grid vocabulary would have zero training data and require manual translation of every LLM-generated layout.

**Migration rules:**

1. **Use Foundation classes on unmigrated pages, daisyUI classes on migrated pages.** Simple.
2. **Migrate entire layout contexts together.** Foundation `grid-x` and Tailwind `grid` cannot compose in the same DOM tree.
3. **Migrate card components as complete units.** Both use `.card` but with structurally different internals (`card-divider` vs `card-title`).
4. **Start with low-Foundation pages**: Icons.razor → Redirect.razor → Counter.razor → CallApiExample.razor → NavMenu.razor → Articles.razor → Videos.razor → FoundationExamples.razor → CacheReset.razor → LocalStorageDebug.razor.

**At any point, the site works.** Both stylesheets load. Some pages use Foundation vocabulary, some use daisyUI. Visual inconsistency (two different button styles) is the only user-visible cost — temporary, shrinking with each migrated page.

**Completion:** When zero Foundation classes remain in any `.razor` file, delete `scss/`, `app.min.css`, `lib/foundation-sites/`. The `sass` binary is no longer needed.

Detailed migration plan: `docs/research/daisyui-blazor-wasm-integration.md` §10.

## Pre-Migration: SCSS Rules

- Use SCSS in `scss/`, NEVER modify compiled CSS.
- Partials start with `_`, included in `app.scss`.
- For SCSS conventions, architecture, and naming: see §SCSS Architecture and Build Pipeline below.

## Pre-Migration: SCSS Build

- **Dev**: `sass --watch scss:wwwroot/css` (auto-compiles on save, integrates with `dotnet watch`)
- **Prod**: `sass --style=compressed --no-source-map scss/app.scss:wwwroot/css/app.min.css`
- Requires `dart-sass` installed (`sudo pacman -S dart-sass` on Arch, `winget install Sass.DartSass` on Windows)
- Full toolchain reference: `rm-dev-tools`

## Foundation CSS Patterns (Pre-Migration)

These patterns are current state. After migration to daisyUI, equivalent daisyUI classes replace them (see mapping table above).

### Grid

```html
<div class="grid-container">
  <div class="grid-x grid-padding-x">
    <div class="cell medium-6 large-4">Content</div>
    <div class="cell medium-6 large-8">Content</div>
  </div>
</div>
```

### Buttons

```html
<button class="button">Primary</button>
<button class="button secondary">Secondary</button>
<button class="button hollow">Outlined</button>
<button class="button tiny/small/large">Sizes</button>
```

### Forms

```html
<form>
  <label
    >Input Label
    <input type="text" placeholder="Placeholder" />
  </label>
  <label
    >Textarea
    <textarea placeholder="Placeholder"></textarea>
  </label>
  <fieldset class="fieldset">
    <legend>Checkbox Group</legend>
    <input type="checkbox" id="chk1" /><label for="chk1">Option 1</label>
  </fieldset>
</form>
```

### Visibility

```html
<!-- Show on specific breakpoints -->
<div class="show-for-medium">Visible on medium+</div>
<div class="hide-for-large">Hidden on large</div>

<!-- Screen reader only -->
<div class="show-for-sr">Accessible only</div>
```

## Accessibility (WCAG 2.1 AA)

### Semantic HTML

Use proper HTML5 elements:

```html
<header>
  <nav>
    <main>
      <section>
        <article>
          <footer></footer>
        </article>
      </section>
    </main>
  </nav>
</header>
```

### ARIA Attributes

Use for interactive Blazor components:

| Pattern      | Code                         |
| ------------ | ---------------------------- |
| Button state | `aria-pressed="true/false"`  |
| Expanded     | `aria-expanded="true/false"` |
| Label        | `aria-label="Close dialog"`  |
| Live region  | `aria-live="polite"`         |
| Invalid      | `aria-invalid="true"`        |

### Focus Management

```csharp
// Blazor: Set focus after render
await ElementRef.FocusAsync();

// Keyboard navigation: tabindex="0" for custom focusable elements
```

### Color Contrast

- Normal text: 4.5:1 ratio
- Large text (18pt+): 3:1 ratio
- UI components: 3:1 ratio

### Screen Reader Testing

Test with:

- NVDA (Windows)
- VoiceOver (macOS)
- Firefox preferred

### Skip Navigation

```html
<a class="show-for-sr" href="#main-content">Skip to main content</a>
<main id="main-content"></main>
```

---

## §CSS Standards and Anti-Patterns (inlined from rm-css)


# rm-css

## Quick Start

Before writing any CSS, determine the Baseline floor:

1. Government / compliance / public service → Widely available only
2. Consumer SaaS with analytics → data-driven (Baseline Checker)
3. Developer tools / internal apps → Newly available
4. Personal projects → Newly available
5. Unknown audience → Widely available (safe default)

**The current safest Baseline is Widely available.** Features interoperable across Chrome, Edge, Firefox, Safari (desktop + iOS) for 30+ months. Check MDN Baseline badges on every CSS property page — never memorize tiers.

## Baseline Decision Framework

Full framework at `docs/research/css-2026-baseline.md`. Summary:

| Tier                 | Definition               | Default rule                   |
| -------------------- | ------------------------ | ------------------------------ |
| Widely available     | Interoperable ≥30 months | Safe everywhere                |
| Newly available      | Interoperable <30 months | Progressive enhancement only   |
| Limited availability | Not interoperable        | Avoid unless analytics justify |

### Feature-category classification

When a CSS feature is unsupported, classify the failure:

| Category        | What breaks                              | Decision                         |
| --------------- | ---------------------------------------- | -------------------------------- |
| Layout-breaking | Content inaccessible or unreadable       | Requires Widely available floor  |
| Cosmetic-only   | Appearance differs, content fully usable | Always safe to enhance           |
| Functional      | Interaction or behavior stops working    | Assess against failure tolerance |

**Never use a layout-breaking feature above the floor.** `display: grid` and `:has()` for layout without Widely available fallback produces inaccessible content. Cosmetic features (`border-radius`, `backdrop-filter`, `accent-color`) are always safe to enhance — they degrade to a fully usable state.

## Anti-Patterns

Full catalog at `docs/research/css-2026-baseline.md` §CSS Anti-Patterns Catalog. Quick reference:

**Never use for layout:** `float`, `clearfix` hacks, `display: inline-block` + `vertical-align`, `display: table` / `table-cell`. Use `flexbox` or `grid` (Widely available 2020).

**Never use named CSS colors** (`red`, `green`, `blue`). Use custom properties (`var(--color-primary)`) or `oklch()`.

**Never use `font-size` in `px` for body text** without `rem` fallback. Use `1rem` for body, `rem` + `clamp()` for headings.

**Never use vendor prefixes on standardized properties.** `-webkit-border-radius`, `-moz-border-radius`, `-ms-border-radius` are dead code. Use unprefixed. Exceptions: `-webkit-line-clamp` (still needed), `-webkit-text-fill-color` (still needed). Verify on MDN before removing any prefix.

**Never use `!important` as override mechanism.** Use `@layer` for cascade ordering (Widely available 2024).

**Never use `@import` in CSS** for performance-sensitive pages. Use `<link>` in HTML (parallel loading) or `@use` in SCSS.

**Never use deeply nested selectors (>3 levels).** Creates specificity wars. Use BEM naming, `@layer`, or component-scoped styles.

**Never write mobile-desktop breakpoints with `max-width`.** Use mobile-first `min-width` — add complexity as the viewport grows, not retroactively.

**Never use `*` as a bare universal selector reset** — pseudo-elements are not covered. Use `*, *::before, *::after` or rely on framework defaults.

**Never use the `padding-bottom` percentage hack for aspect ratios.** `padding-bottom: 56.25%` for 16:9 videos. Use `aspect-ratio: 16 / 9` (Widely available 2022).

**Never use `margin` / `:not(:last-child)` hacks for spacing between flex or grid children.** Use `gap` (Widely available 2021).

**Never use `:focus` for visible focus indicators.** Produces ugly focus rings on mouse clicks. Use `:focus-visible` (Widely available 2022) — shows focus ring only for keyboard navigation.

## 2026 Best Practices

### Structure & Cascade

- **Use `@layer` for cascade priority.** Define layers from lowest to highest priority: `reset → base → components → utilities`. Never rely on source order or specificity hacks.
- **Use `@scope` for component isolation.** Prevents styles leaking into or out of components. Baseline 2025.
- **Use container queries for component-responsive design.** Replace viewport-only breakpoints. `@container` queries respond to the parent container size. Baseline 2023.

### Sizing & Typography

- **Use `min()`, `clamp()`, `max()` for fluid sizing.** `clamp(1rem, 2vw + 0.5rem, 2rem)` replaces three media queries. Widely available 2023.
- **Use `rem` for font sizes and spacing.** Respects user font-size preferences. Never use `px` for body text.
- **Use `text-wrap: balance` for headlines, `text-wrap: pretty` for body.** Prevents orphans. Baseline 2024.

### Color

- **Use `oklch()` for color definitions.** Perceptually uniform, wider gamut, better gradient interpolation. Prefer over `hsl()` and `rgb()`. Baseline 2024.
- **Use `color-mix()` for tinting and shading.** `color-mix(in oklch, var(--color-primary), white 20%)` instead of separate tint variables. Baseline 2024.
- **Define colors as custom properties in a single location.** Never scatter color values across components. Use `:root` or a design-tokens layer.
- **Respect `prefers-color-scheme`.** Provide light and dark mode via `@media (prefers-color-scheme: dark)` and `light-dark()` function (Baseline 2024). Custom properties switch values at the `:root` level — never duplicate color definitions per mode.

### Animations & Motion

- **Respect `prefers-reduced-motion`.** Wrap all animations and transitions in a `@media (prefers-reduced-motion: no-preference)` block.
- **Use `@starting-style` for enter animations.** Replaces JavaScript-based mount animations for elements entering the DOM. Baseline 2025.
- **Never animate `width`, `height`, `top`, `left`.** Triggers layout recalculations. Use `transform` and `opacity` (compositor-only properties).

### Selector & Organization

- **Use CSS nesting for readability.** Reduces repetition of parent selectors. Baseline 2024.
- **Use semantic class names.** Describe what the element IS (`card--featured`, `nav--primary`), not what it looks like (`blue-box`, `big-text`).
- **One class, one responsibility.** Never pack unrelated styles into one class. Compose multiple classes instead.
- **Mobile-first authoring.** Write base styles for smallest viewport. Add complexity in `min-width` media queries or container queries.

### Formatting

- **One property per line.** Never write multiple properties on the same line.
- **Alphabetical property ordering.** No grouping by type — alphabetical is unambiguous and tool-enforceable.
- **No trailing whitespace.** No blank lines at end of file beyond the final newline.
- **Comments on their own line** above the code they describe. Never trailing comments after a property.

## This Repo

- SCSS still active — see §SCSS Architecture and Build Pipeline below for partial conventions and build pipeline.
- daisyUI + Tailwind migration in progress — see `rm-styling` and `docs/research/daisyui-*`.
- SCSS anti-patterns in this skill apply to both SCSS and compiled CSS.
- Production SCSS compiled via `sass --style=compressed --no-source-map scss/app.scss:wwwroot/css/app.min.css`.

---

## §SCSS Architecture and Build Pipeline (inlined from rm-scss)


# rm-scss

> **Migration notice**: This skill describes the current SCSS pipeline (Foundation 6 + dart-sass). The project is migrating to daisyUI v5 + Tailwind CSS v4 — a CSS-only framework that replaces SCSS entirely. During migration, this skill remains the source of truth for existing SCSS code. New components should use daisyUI classes (see `rm-ui-styling`). Post-migration, this skill will be archived.
>
> Research: `docs/research/daisyui-blazor-wasm-integration.md`, `docs/research/daisyui-long-term-styling-evaluation.md`, `docs/research/scss-foundation-tailwind-daisyui-landscape-2026-05-19.md`

## Design Philosophy

_These principles are portable — they outlive any specific framework._

### Semantic Class Naming

Classes describe **what something IS**, not what it looks like.

| ✅ Good (semantic) | ❌ Bad (presentational) |
| ------------------ | ----------------------- |
| `.callout`         | `.colored-box`          |
| `.button`          | `.blue-rounded-thing`   |
| `.metrics-card`    | `.left-column-widget`   |
| `.cell`            | `.column`               |

A developer reading the HTML understands the **role** of each element without seeing the rendered output.

### Modifier Pattern

Base class is the component. Modifiers are additional classes. Never smash them together.

```html
<!-- ✅ Modifier as separate class -->
<button class="button secondary">Cancel</button>
<div class="callout alert">Error occurred</div>

<!-- ❌ Modifier baked into the name -->
<button class="button-secondary">Cancel</button>
<div class="callout--alert">Error occurred</div>
```

In SCSS, this maps to:

```scss
.button {
  // base button styles
  &.secondary {
    background: $color-secondary;
  }
  &.alert {
    background: $color-alert;
  }
}
```

### Mobile-First Authoring

Base styles target the smallest screen. Breakpoints build **up**, never down.

```scss
// ✅ Mobile-first — base is mobile
.cell {
  flex: 1;
}
@media (min-width: 768px) {
  .cell.medium-6 {
    flex: 0 0 50%;
  }
}

// ❌ Desktop-first — wrong direction
.cell {
  flex: 0 0 50%;
}
@media (max-width: 767px) {
  .cell {
    flex: 1;
  }
}
```

### Consistent Spacing Scale

Use rem-based spacing. Never arbitrary pixel values. Foundation's scale (1rem increments) is the baseline:

| Name         | Size    | Use                       |
| ------------ | ------- | ------------------------- |
| `$space-xs`  | 0.25rem | Tight internal padding    |
| `$space-sm`  | 0.5rem  | Icon padding, inline gaps |
| `$space-md`  | 1rem    | Standard padding/margin   |
| `$space-lg`  | 1.5rem  | Section spacing           |
| `$space-xl`  | 2rem    | Major section separation  |
| `$space-xxl` | 3rem    | Page-level spacing        |

### Single Responsibility

A CSS class does **one thing**. If a class controls both layout AND color, split it.

```scss
// ✅ Separated concerns
.grid-x {
  display: flex;
} // layout only
.callout.alert {
  color: red;
} // color only

// ❌ Mixed concerns
.alert-box {
  display: flex;
  color: red;
  margin: 1rem;
}
```


## Architecture

### Folder Structure — 4-Folder Pattern

The 7-1 pattern is overkill for projects under 50 partials. Use 4 folders:

```
scss/
├── app.scss                  # Single entry point
├── abstracts/                # Variables, mixins, functions, design tokens
│   ├── _index.scss
│   ├── _variables.scss       # Colors, spacing, breakpoints
│   ├── _mixins.scss          # Reusable @mixin blocks
│   └── _design-tokens.scss   # CSS custom properties (:root)
├── base/                     # Element-level defaults, fonts, reset
│   ├── _index.scss
│   ├── _fonts.scss
│   ├── _reset.scss
│   ├── _typography.scss
│   └── _global.scss
├── components/               # Reusable UI components
│   ├── _index.scss
│   ├── _grid.scss
│   ├── _button.scss
│   ├── _callout.scss
│   ├── _card.scss
│   └── …
└── vendor/                   # Third-party (Foundation core, FA imports)
    ├── _index.scss
    └── _foundation-core.scss # ONLY the Foundation pieces we actually use
```

### Entry Point (`app.scss`)

Order matters. Every `@use` imports once, deduplicates automatically.

```scss
// 1. Design tokens first (CSS custom properties at :root)
@use "abstracts/index" as *;

// 2. Vendor core (Foundation subset we depend on)
@use "vendor/index" as *;

// 3. Base element styles
@use "base/index" as *;

// 4. Components (depends on base + abstracts)
@use "components/index" as *;
```

### Partial Naming

- Filenames start with `_` (e.g., `_button.scss`)
- `_index.scss` in each folder `@forward`s its contents
- One component per partial. If a partial exceeds 200 lines, split it.


## SCSS Rules

- Never edit compiled CSS directly.
- **Do not use `.razor.css`** for global styles. Use Blazor CSS isolation only for component-scoped, single-use styles (not reusable design system pieces).
- **Use `@use` and `@forward`**, not the deprecated `@import`. Dart Sass `@import` deprecation is live — migrate Foundation sources during extraction (see Foundation Selective Inclusion).
- **CSS custom properties over SCSS variables** for anything that changes at runtime (theming, dark mode). SCSS variables for build-time constants.
- **Third-party CSS inlining**: Use `@import "path/to/file"` (no `.css` extension) in `vendor/` to inline third-party CSS into the compiled single output file. This eliminates extra HTTP requests.


## Build Pipeline

**Compiler**: `dart-sass` CLI (v1.99.0+). Installed via system package manager — no NuGet, no MSBuild involvement.

| Task            | Command                                                                                    |
| --------------- | ------------------------------------------------------------------------------------------ |
| Dev (watch)     | `sass --watch scss:wwwroot/css`                                                            |
| Prod (one-shot) | `sass --style=compressed --no-source-map scss/app.scss:wwwroot/css/app.min.css`            |
| JS minify       | `npx --yes terser wwwroot/js/page-load-timing.js -o wwwroot/js/page-load-timing.min.js -c` |

**Why CLI, not NuGet**: MSBuild is the wrong tool for SCSS compilation. The `sass` CLI watches files, auto-compiles on save, and integrates cleanly with `dotnet watch` (CSS file changes trigger Blazor hot reload). No manual build step. No Windows-only limitation.

**Install**: `sudo pacman -S dart-sass` (Arch) or `winget install Sass.DartSass` (Windows 11). See `rm-dev-tools` for full toolchain reference.

**Watch integration**: Start `sass --watch` once per session (background). `dotnet watch` detects the compiled CSS changes and reloads the browser automatically. Zero manual compilation during active development.


## Blazor Integration

### CSS Isolation (`.razor.css`)

Use when all three conditions are met:

1. The styles are for a single component only
2. No other component shares these styles
3. The styles never need to be composed or overridden

```razor
@* Counter.razor *@
<button class="increment-btn">+1</button>
```

```css
/* Counter.razor.css — scoped to this component only */
.increment-btn {
  background: green;
}
```

### Global SCSS (this skill)

Use for:

- Design tokens (colors, spacing, typography)
- Reusable component styles (button, callout, grid)
- Layout primitives
- Reset and base element styles

### Never Mix

A style lives in **one** place. If a button variant lives in `_button.scss`, do not override it in a `.razor.css` file.


## Foundation Selective Inclusion (Incremental Extraction)

Instead of calling `foundation-everything(true, true)` which compiles all
~38 Foundation modules, selectively include only the modules we use.

### Pattern

```scss
// app.scss — NOT foundation-everything()
@use "../lib/foundation-sites/scss/foundation" as foundation;

$global-flexbox: true !global;
$prototype: true !global;

// Only the modules we actually use (8 of ~38)
@include foundation.foundation-global-styles;
@include foundation.foundation-xy-grid-classes;
@include foundation.foundation-flex-classes;
@include foundation.foundation-typography;
@include foundation.foundation-button;
@include foundation.foundation-button-group;
@include foundation.foundation-callout;
@include foundation.foundation-card;
@include foundation.foundation-prototype-classes;
```

### Gotchas

1. **Mixin names differ from wrapper comments.** Read
   `lib/foundation-sites/scss/foundation.scss` directly to get the real
   `@include foundation-<name>` calls inside `foundation-everything()`.
   Never trust comments in wrapper files.

2. **`@use` rules must precede all other SCSS statements.** Variables
   and `@include` calls must come after ALL `@use` directives.

3. **`$global-flexbox: true !global` must be set explicitly** when
   bypassing `foundation-everything()`. Without it, xy-grid falls back
   to legacy float grid classes.

### Result

Compiled CSS: 152 KB → 100 KB (34% reduction). No library files modified.
Reference: `docs/research/foundation-module-audit-2026-05-14.md`

Foundation 6 is in maintenance mode (volunteer-run, last release Sept 2024). We are extracting the pieces we actually use:

| Foundation Feature | Our Replacement                        | Complexity                      |
| ------------------ | -------------------------------------- | ------------------------------- |
| XY Grid            | CSS Grid in `_grid.scss`               | Low — CSS Grid is more powerful |
| Button             | `_button.scss`                         | Low — 3 variants                |
| Callout            | `_callout.scss`                        | Low — 5 color variants          |
| Card               | `_card.scss`                           | Low — simple container          |
| Visibility classes | CSS `display` utilities or just delete | None — zero production usage    |
| Forms              | CSS defaults                           | None — zero production usage    |
| Helpers            | CSS utilities                          | None — zero production usage    |

**Goal**: Delete the entire `lib/foundation-sites/` directory and ship zero framework CSS. Target: ~300 lines of SCSS replacing 127 KB of compiled framework CSS.


## Design Tokens (Future Target)

CSS custom properties at `:root` level are the long-term design system backbone:

```scss
// abstracts/_design-tokens.scss
:root {
  // Colors — role-based naming
  --color-brand-black: #1a1a1a;
  --color-brand-burgundy: #800020;
  --color-surface: #ffffff;
  --color-surface-muted: #f5f5f5;
  --color-text: #333333;
  --color-text-muted: #666666;
  --color-accent-good: #0cce6b;
  --color-accent-warning: #ffae42;
  --color-accent-poor: #ff4e42;
  --color-diagnostic: #9e9e9e;

  // Spacing
  --space-md: 1rem;
  --space-lg: 1.5rem;
  --space-xl: 2rem;

  // Typography
  --font-body: "Outfit", sans-serif;
  --font-size-base: 16px;
  --line-height-base: 1.6;
}
```

Role-based naming: `--color-accent-good` not `--color-green`. Names describe purpose, not hex values.


## Do NOT

- Use `.razor.css` for reusable design system components
- Inline styles (`style="..."` in HTML) — ever
- Use `!important` except as a one-time escape hatch for third-party overrides
- Write CSS directly — always SCSS
- Add new Foundation components without first checking if a 10-line SCSS replacement suffices
- Use arbitrary pixel values for spacing — always rem-based, always from the spacing scale
