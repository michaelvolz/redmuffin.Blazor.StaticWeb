---
name: rm-css
description: CSS standards, anti-patterns catalog, Baseline decision framework, and 2026 best practices for structure, design, formatting, and modern feature usage. Use when writing, editing, reviewing, modularizing, or auditing CSS — any .css or .scss file, any style tag, any styling decision. Contains the definitive anti-patterns list and Widely-available Baseline definition for this repo.
---

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

- SCSS still active — see `rm-scss` for partial conventions and build pipeline.
- daisyUI + Tailwind migration in progress — see `rm-ui-styling` and `docs/research/daisyui-*`.
- SCSS anti-patterns from this skill apply to both SCSS and compiled CSS.
- Production SCSS compiled via `sass --style=compressed --no-source-map scss/app.scss:wwwroot/css/app.min.css`.
