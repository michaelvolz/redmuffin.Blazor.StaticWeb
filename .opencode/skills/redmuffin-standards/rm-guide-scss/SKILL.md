---
name: rm-guide-scss
description: "SCSS architecture, design system philosophy (Foundation-inspired naming and mobile-first principles), partial conventions, build pipeline, and component styling patterns. Use when writing SCSS, creating new partials, organizing styles, or making design system decisions. NOTE: SCSS pipeline is being phased out in favor of daisyUI v5 + Tailwind CSS v4. See rm-ui-styling and docs/research/daisyui-* for migration details. SCSS rules here remain authoritative until migration is complete."
---

# rm-guide-scss

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

---

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

---

## SCSS Rules

- Never edit compiled CSS directly.
- **Do not use `.razor.css`** for global styles. Use Blazor CSS isolation only for component-scoped, single-use styles (not reusable design system pieces).
- **Use `@use` and `@forward`**, not the deprecated `@import`. Dart Sass `@import` deprecation is live — migrate Foundation sources during extraction (see Foundation Selective Inclusion).
- **CSS custom properties over SCSS variables** for anything that changes at runtime (theming, dark mode). SCSS variables for build-time constants.
- **Third-party CSS inlining**: Use `@import "path/to/file"` (no `.css` extension) in `vendor/` to inline third-party CSS into the compiled single output file. This eliminates extra HTTP requests.

---

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

---

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

---

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

---

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

---

## Do NOT

- Use `.razor.css` for reusable design system components
- Inline styles (`style="..."` in HTML) — ever
- Use `!important` except as a one-time escape hatch for third-party overrides
- Write CSS directly — always SCSS
- Add new Foundation components without first checking if a 10-line SCSS replacement suffices
- Use arbitrary pixel values for spacing — always rem-based, always from the spacing scale
