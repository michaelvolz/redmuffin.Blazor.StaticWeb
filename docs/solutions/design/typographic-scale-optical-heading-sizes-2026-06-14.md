---
date: 2026-06-14
title: "Optical Heading Scale: Typographic Hierarchy for Developer Portfolios"
module: design
tags:
  [
    typography,
    heading-scale,
    optical-size,
    design-tokens,
    foundation-override,
    williams-spyridakis,
  ]
problem_type: design-decision
---

## Problem

Foundation 6 ships hardcoded heading sizes (48/40/31/25/20/16px) designed for
marketing sites. redmuffin is a developer portfolio — comparable sites ship
32px h1 (GitHub, Josh Comeau). Foundation's sizes are too large and their
inter-step ratios are inconsistent (−16.7%, −22.5%, −19.4%, −20%, −20%).

## Research

### Empirical Evidence

**Williams & Spyridakis (1992)** — "Visual discriminability of headings in
text." Participants sorted index cards by heading importance. Finding:

> "A difference of approximately 20 percent being the most useful"
> for distinguishing heading levels. Relative, rather than absolute,
> size difference provided the strongest hierarchy indicator.

This is the single most-cited empirical finding in typographic hierarchy
research. Not opinion — a controlled study with measurable outcomes.

### Real-World Measurements

| Site                           | Category       | h1   | body | Ratio |
| ------------------------------ | -------------- | ---- | ---- | ----- |
| Linear                         | SaaS marketing | 64px | 16px | 4.0×  |
| Tailwind CSS                   | Marketing      | 96px | 16px | 6.0×  |
| React                          | Marketing/OSS  | 52px | 17px | 3.1×  |
| **Microsoft Learn**            | Technical docs | 40px | 16px | 2.5×  |
| **MDN**                        | Technical docs | 40px | 16px | 2.5×  |
| **GitHub**                     | Dev platform   | 32px | 14px | 2.3×  |
| **Josh Comeau**                | Dev blog       | 32px | 16px | 2.0×  |
| redmuffin (Foundation default) | Dev portfolio  | 48px | 16px | 3.0×  |

Developer portfolio/documentation sites cluster at 32-40px h1. Marketing
sites cluster at 52-96px. Foundation's 48px is in marketing territory.

### Historical Practice

Optical scaling has been standard since hand-cut type punches (15th century).
Type designers vary x-height proportions at different sizes — larger type
gets finer details, smaller type gets bolder strokes. This is not a digital
invention; it's how human visual perception works. The printing press solved
this 500 years ago.

### 2026 Modular Scale Research

DESIGN-R.AI measured heading ratios across 22 live sites (Stripe, Linear,
Notion, Vercel, Avada). Findings:

- Major Third (1.25) — dominant ratio for standard content hierarchies
- Perfect Fourth (1.333) — editorial and long-form content
- Golden Ratio (1.618) — display-only, not full hierarchies
- Most design systems use TWO scales: tighter for body/UI, wider for display

## Solution: Optical Scale

Uniform mathematical ratios (e.g., 1.149 per step) look wrong because human
perception is not linear. The eye expects:

1. **Display headings (h1-h2)** — close together, authoritative presence
2. **Section headings (h2-h3-h4)** — clear gaps at ~20% for hierarchy
3. **Body-adjacent (h4-h5-h6)** — converging toward body text

### Implemented Scale

| Level | px  | Step   | Tier             |
| ----- | --- | ------ | ---------------- |
| h1    | 32  | —      | Display anchor   |
| h2    | 28  | −12.5% | Display pair     |
| h3    | 23  | −17.9% | Section core     |
| h4    | 19  | −17.4% | Section core     |
| h5    | 17  | −10.5% | Body convergence |
| h6    | 16  | −5.9%  | Body anchor      |

h6=16px is fixed by design constraint (must not go below body text size).
h1=32px is anchored by real-world data (developer portfolio tier).

### Implementation

Mixin in `scss/base/_heading-scale.scss`, invoked in `scss/app.scss` after
Foundation's typography include:

```scss
// _heading-scale.scss
@mixin heading-scale($h1-base-px: 32) {
  $optical-ratios: (1.143, 1.217, 1.211, 1.118, 1.063);

  @media (min-width: 40em) {
    $cumulative: 1;
    @for $i from 1 through 6 {
      $level: nth(h1 h2 h3 h4 h5 h6, $i);
      $size-px: $h1-base-px / $cumulative;
      #{$level},
      .#{$level} {
        font-size: ($size-px / 16) * 1rem;
      }
      @if $i < 6 {
        $cumulative: $cumulative * nth($optical-ratios, $i);
      }
    }
  }
}

// app.scss — invoked after Foundation typography for cascade ordering
@include heading.heading-scale(32);
```

Change the argument to rescale: `@include heading.heading-scale(36)`.
The per-step ratios preserve the optical curve shape at any base size.
CSS cascade override — Foundation's original sizes ship but the `@media`
block comes after and wins. Zero Foundation files touched.

### Why CSS Override, Not SCSS Variable

Foundation uses `@use` which creates module isolation. `$header-styles`
cannot be overridden via `!global` or `with()` without touching Foundation
files. CSS cascade override is framework-agnostic, survives Foundation
removal, and works identically under daisyUI/Tailwind migration.

## What Did NOT Work

- **Uniform ratio (1.149 per step)** — perceived as "antiseptic," not
  natural. Equal steps feel flat to human eyes. The data says 20% is optimal;
  12.9% is below the perception threshold.
- **Foundation's `$header-styles` variable override** — `@use` module
  boundary blocks it. Requires `@import` which conflicts with `@use` ordering
  rules.
- **Copying `_settings.scss`** — unnecessary coupling to Foundation's
  internal file structure. CSS override avoids this entirely.

## References

- Williams, T. R., & Spyridakis, J. H. (1992). "Visual discriminability of
  headings in text." IEEE Transactions on Professional Communication, 35(2),
  64-70.
- Timpany, C. (2025). "Identification of headings in print and screen using
  typographic differentiation." Information Design Journal, 30, 97.
- DESIGN-R.AI (2026). "The Hidden Mathematics of Websites That Convert."
  dev.to.
- Carter, H. (1937/1984). "Optical scale in type design." Cited in Legge,
  G. E., & Bigelow, C. A. (2011). "Does Print Size Matter for Reading?"
  Journal of Vision, 11(5):8.
- Rello, L., Pielot, M., & Marcos, M. C. (2016). "Make It Big! The Effect
  of Font Size and Line Spacing on Online Readability." ACM CHI 2016.
  104 participants, eye-tracking. Font size 18+ improves comprehension.
