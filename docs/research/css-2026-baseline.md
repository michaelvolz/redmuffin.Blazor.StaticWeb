---
date: 2026-05-24
title: CSS-2026 Baseline Decision Framework
tags: [css, baseline, standards, anti-patterns, decision-framework]
description: Universal decision framework for determining safe CSS feature adoption using the Web Platform Baseline standard. Includes anti-patterns catalog.
module: css
---

# CSS-2026 Baseline Decision Framework

## Quick Start

You have a web project. You want to know what CSS is safe to use. Here's how:

1. **Audience** — who uses this site? Government: Widely available only. Consumer SaaS with analytics: data-driven. Unknown: assume Widely available.
2. **Baseline** — what tier? Widely available (safe default), Newly available (with progressive enhancement), Limited availability (avoid).
3. **Fail mode** — what happens if this feature is unsupported? Layout breaks? Cosmetic only? Interaction fails? Each has a different answer.
4. **Decide** — Widely available features are always safe. Newly available features need progressive enhancement. Limited availability features are avoided unless you have analytics to justify them.

That's the framework. The rest of this document explains each step and catalogs what to stop using.

---

## The Web Platform Baseline

In 2023, the WebDX Community Group (Google, Microsoft, Mozilla, Apple) introduced **Baseline** — a formal standard for CSS and JavaScript feature readiness. It replaced "check Can I Use and hope" with a three-tier system:

| Tier                     | Definition                                                                      | When to use                                                  |
| ------------------------ | ------------------------------------------------------------------------------- | ------------------------------------------------------------ |
| **Widely available**     | Interoperable across Chrome, Edge, Firefox, Safari (desktop+iOS) for 30+ months | Safe default for all projects                                |
| **Newly available**      | Interoperable across the core browser set for less than 30 months               | Progressive enhancement for features that degrade gracefully |
| **Limited availability** | Not yet interoperable across all core browsers                                  | Avoid unless analytics data justifies the risk               |

### Core browser set

Chrome, Edge, Firefox, Safari (desktop), Safari (iOS). This is the minimum. For projects targeting specific markets, add regional browsers (Samsung Internet for India/Korea, QQ Browser for China) to the evaluation.

### How Baseline works in practice

MDN, Can I Use, and Chrome DevTools display Baseline badges on every CSS property page. You don't need to memorize tiers — you look up the feature and the badge tells you. The framework teaches you what to DO with that information.

**Do not maintain your own Baseline list.** Use the official one at [web.dev/baseline](https://web.dev/baseline). This document is about the decision process, not the feature catalog.

### Year-based Baseline

Some projects reference "Baseline 2024" — the set of features that reached Widely available as of January 1 of that year. This is a shorthand for setting a target. When your analytics data says 94% of users support Baseline 2024, that's the collection of features Widely available by that cutoff.

---

## Layer 1: Audience Analysis

**Question:** Who uses this site, and what browsers are they on?

| Audience                          | Default floor                      | Why                                                                                                                                        |
| --------------------------------- | ---------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------ |
| Government, healthcare, education | Widely available                   | WCAG compliance, broad device support, procurement rules. Features must work on devices 3+ years old.                                      |
| Consumer SaaS with analytics      | Data-driven (use Baseline Checker) | Analytics data tells you what browsers your actual users have. Set the floor to the Widely available set at the lowest common denominator. |
| Developer tools, internal apps    | Newly available or data-driven     | Narrower audience, controlled environments. You can be more aggressive.                                                                    |
| Personal projects, portfolios     | Newly available                    | No external users, no compliance requirements. Use what you want.                                                                          |

**How to get analytics data:** Use the [Baseline Checker](https://baseline-checker.chrome.dev) (connects to Google Analytics API) or the `baseline-browser-mapping` module to map your audience to Baseline tiers.

**When you have no data:** Assume Widely available. This is the safe default for every project serving the general public.

**Accessibility note:** Accessibility requirements (WCAG, `prefers-reduced-motion`, `forced-colors`) can raise the floor above audience analysis alone. Government and compliance projects must treat accessibility media features as baseline requirements regardless of audience data.

---

## Layer 2: Failure Tolerance

**Question:** What happens if a CSS feature is unsupported? Can the user still accomplish their goal?

### Feature-category classification

Every CSS feature falls into one of three categories when unsupported:

| Category            | What breaks                                                                                        | Degradation quality       | Enhancement decision             |
| ------------------- | -------------------------------------------------------------------------------------------------- | ------------------------- | -------------------------------- |
| **Layout-breaking** | Content becomes inaccessible, unreadable, or overlaps. Page structure collapses.                   | Unusable                  | Requires Widely available floor  |
| **Cosmetic-only**   | Appearance differs but all content is still readable and accessible.                               | Degraded but fully usable | Always safe to enhance           |
| **Functional**      | An interaction, animation, or behavior stops working. Form controls, scroll behavior, transitions. | Partially broken          | Assess against failure tolerance |

### Examples

| CSS feature               | Category                                             | Why                                                                                        |
| ------------------------- | ---------------------------------------------------- | ------------------------------------------------------------------------------------------ |
| `display: grid`           | Layout-breaking                                      | Content order changes completely without grid fallback                                     |
| `border-radius`           | Cosmetic-only                                        | Square corners instead of round — content unaffected                                       |
| `scroll-behavior: smooth` | Functional                                           | Instant scroll instead of smooth — content still accessible                                |
| `@layer`                  | Layout-breaking                                      | Cascade order changes without layers                                                       |
| `accent-color`            | Cosmetic-only                                        | Default form colors instead of custom                                                      |
| `:has()`                  | Layout-breaking for layout use, cosmetic for styling | If used for parent-responsive layout, content shifts. If used for hover styling, cosmetic. |
| `backdrop-filter`         | Cosmetic-only                                        | No blur behind element — content unaffected                                                |
| `position: sticky`        | Functional                                           | Element scrolls away instead of sticking                                                   |

### The decision

- **Layout-breaking + Limited/Newly available → NO.** The content is unusable. Use a Widely available alternative or accept the risk.
- **Cosmetic-only + Any tier → YES (progressive enhancement).** The content works. The feature is a visual upgrade.
- **Functional + Newly available → Assess.** Will the missing interaction confuse users or block workflows? If yes, fall back to Widely available. If no, enhance.

---

## Layer 3: Progressive Enhancement

**Progressive enhancement is the mechanism for adopting features above your floor.** You write CSS that works at your floor tier, then add features for newer browsers that support them.

### The rule

Write your base CSS for the Widely available floor. Add Newly available features as enhancements. The base experience must be fully functional without them.

### How to enhance safely

```css
/* Floor: Widely available */
.card {
  width: 100%;
  padding: 1rem;
  border: 1px solid #ccc;
}

/* Enhancement: Newly available (Baseline 2025) */
.card {
  width: min(100%, 600px); /* Falls back to 100% on older browsers */
  border-radius: 0.5rem; /* Square corners on older browsers */
  backdrop-filter: blur(10px); /* No blur on older browsers */
}
```

### When NOT to enhance

If the base experience is broken without a feature, that feature is NOT an enhancement — it's a requirement. Move it to your floor.

Example: using `:has()` to change layout when a child is present. Without `:has()`, the layout is different, not just less pretty. That's layout-breaking, not cosmetic. Either move `:has()` to your floor or use a different approach.

---

## LLM Application Guide

When applied to a project, this framework produces a `docs/css-baseline.md` file with:

1. **Chosen Baseline target** — Widely available, Newly available, or year-based (e.g., Baseline 2024)
2. **Newly available features worth adopting** — 3-5 features above the floor that enhance the experience
3. **Anti-patterns to remediate** — existing CSS patterns that should be replaced with modern alternatives (from the catalog below)
4. **Reference** — link to the official Widely available list at web.dev/baseline

### LLM steps

1. Load this document into context
2. Identify the project's audience from analytics, deployment context, or stated requirements
3. Select a Baseline target using the Layer 1 table
4. Audit the project's CSS files against the anti-patterns catalog below
5. Identify 3-5 Newly available features that would enhance the project (cosmetic-only, safely degradable)
6. Write `docs/css-baseline.md` with the four sections above

---

## CSS Anti-Patterns Catalog

These are Widely available CSS features that should be actively avoided because a modern, also-Widely-available replacement exists. Every entry has a year marker showing how long the replacement has been settled.

### Layout

| Stop using                                                       | Use instead                        | Why                                                                                                            | Since |
| ---------------------------------------------------------------- | ---------------------------------- | -------------------------------------------------------------------------------------------------------------- | ----- |
| `float: left` / `float: right` for page layout                   | `display: flex` or `display: grid` | Float was a text-wrapping property repurposed for layout. Flexbox and grid are designed for layout.            | 2020  |
| `clear: both` / clearfix hacks                                   | `display: flow-root`               | Creates a new block formatting context without pseudo-elements or extra markup.                                | 2023  |
| `display: inline-block` + `vertical-align` for grid-like layouts | `display: flex` with `gap`         | Inline-block leaves whitespace gaps between elements. Flexbox eliminates them and gives you alignment control. | 2020  |
| `display: table` / `display: table-cell` for layout              | `display: flex` or `display: grid` | Table display values were a pre-flexbox hack for vertical centering.                                           | 2020  |

### Color & Typography

| Stop using                                               | Use instead                                                | Why                                                                                                           | Since |
| -------------------------------------------------------- | ---------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------- | ----- |
| Named CSS colors (`red`, `green`, `blue`, `yellow`)      | Custom properties (`var(--color-primary)`) or `oklch()`    | Named colors are inconsistent across browsers, inaccessible, and impossible to theme. Use design tokens.      | 2020  |
| `font-size` in `px` for body text without `rem` fallback | `font-size: 1rem` for body, `rem` + `clamp()` for headings | px values ignore user font-size preferences, breaking accessibility. rem respects the user's browser setting. | 2018  |
| `text-rendering: optimizeLegibility`                     | Remove: browsers enable this by default                    | All modern browsers optimize legibility automatically. The property triggers unnecessary re-rendering.        | 2020  |
| `-webkit-font-smoothing: antialiased` on body text       | Remove, or use only on light-text-on-dark-background       | Antialiasing on light backgrounds makes text thinner and harder to read. Only useful for dark-mode headings.  | 2020  |

### Responsive & Sizing

| Stop using                                                 | Use instead                                        | Why                                                                                                                                      | Since |
| ---------------------------------------------------------- | -------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------- | ----- |
| Single-global-breakpoint layouts with `max-width` wrappers | `min()`, `clamp()`, `min-width`, container queries | `min(100%, 1200px)` replaces a wrapper div + `max-width`. `clamp()` replaces three media queries.                                        | 2023  |
| `@media screen` (media type) on every query                | `@media` with no media type                        | The `screen` type is the default. Adding it only excludes print — and you usually want print to inherit base styles.                     | 2020  |
| `@media (max-width: Npx)` stacking                         | Mobile-first `@media (min-width: Npx)`             | Min-width queries add complexity as the viewport grows — progressive, not retroactive. Max-width requires overriding already-set styles. | 2020  |

### Deprecated & Standardized

| Stop using                                                                                                      | Use instead                        | Why                                                                                                               | Since |
| --------------------------------------------------------------------------------------------------------------- | ---------------------------------- | ----------------------------------------------------------------------------------------------------------------- | ----- |
| Vendor prefixes on standardized properties (`-webkit-border-radius`, `-moz-border-radius`, `-ms-border-radius`) | Unprefixed `border-radius`         | All vendors ship the standard. Prefixes are dead code from the IE/mobile-web era.                                 | 2016  |
| `-webkit-appearance` / `-moz-appearance`                                                                        | `appearance`                       | Standardized in Baseline 2022. The prefixed versions are aliases with subtle bugs.                                | 2022  |
| `-webkit-background-clip: text`                                                                                 | `background-clip: text`            | Standardized. No prefix needed.                                                                                   | 2024  |
| `clip: rect(...)` with `position: absolute`                                                                     | `clip-path: inset(...)`            | `clip` only works on absolutely positioned elements. `clip-path` works on any element and supports more shapes.   | 2020  |
| `zoom: 1` (hasLayout trigger for IE)                                                                            | Remove: IE is dead                 | IE6-7 required `zoom: 1` to trigger hasLayout for `overflow: hidden` and `clear` to work. No browser needs this.  | 2015  |
| `text-size-adjust: 100%` with vendor prefixes                                                                   | `text-size-adjust: 100%` or remove | Standardized. Mobile Safari stopped inflating font sizes in landscape by default — the property is rarely needed. | 2022  |

### Selector & Cascade

| Stop using                                        | Use instead                                                                       | Why                                                                                                                            | Since |
| ------------------------------------------------- | --------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------ | ----- |
| `*` universal box-sizing reset                    | `*, *::before, *::after { box-sizing: border-box }` or rely on framework defaults | The bare `*` selector doesn't cover pseudo-elements. Most frameworks set this correctly.                                       | 2020  |
| Deeply nested selectors (>3 levels)               | BEM naming, `@layer`, component-scoped styles                                     | Nesting past 3 levels creates specificity wars between developers. Flat selectors are easier to override and read.             | 2020  |
| `!important` as override mechanism                | `@layer` for cascade ordering, higher-specificity selectors                       | `!important` creates an unwinnable specificity arms race. `@layer` lets you define cascade priority intentionally.             | 2024  |
| `@import` in CSS files for performance            | `<link>` in HTML, or `@use` in SCSS                                               | `@import` creates a waterfall of sequential downloads. `<link>` loads in parallel. In SCSS, `@use` replaces `@import`.         | 2020  |
| `@import url('https://fonts.googleapis.com/...')` | Self-host fonts with `@font-face`                                                 | Google Fonts @import adds 4-step waterfall (CSS→font CSS→woff2). Self-hosting eliminates two round trips and privacy concerns. | 2020  |

---

## Dependencies

- [Web Platform Baseline](https://web.dev/baseline) (WebDX Community Group)
- [Baseline Checker](https://baseline-checker.chrome.dev) (analytics-to-Baseline mapping)
- [MDN CSS](https://developer.mozilla.org/en-US/docs/Web/CSS) (Baseline badges on every property page)
