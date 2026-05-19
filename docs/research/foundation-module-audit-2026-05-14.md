---
date: 2026-05-14
title: Foundation 6 Module Audit — Selective Inclusion
tags: [scss, foundation, performance, audit, css-size]
description:
  Per-component audit of Foundation 6.9.0 module usage. Identifies which of
  ~38 Foundation modules are actually used across the project. Replaces foundation-everything()
  with individual @include calls in app.scss, dropping CSS output from 152KB to 100KB.
module: styles
problem_type: performance-optimization
---

# Foundation 6 Module Audit — Selective Inclusion

## Executive Summary

Foundation 6.9.0 ships ~38 CSS modules via `foundation-everything()`. An audit
of all Razor components and SCSS files found we use only 8 modules. Replacing
`foundation-everything(true, true)` with individual `@include` calls drops
compiled CSS from 152KB to 100KB (34% reduction, ~52KB saved). No Foundation
library files are modified.

## Audit Method

1. Grep all `.razor` files for Foundation CSS class usage (`grid-x`, `cell`,
   `callout`, `button`, `card`, etc.)
2. Grep all `.scss` files for Foundation mixin/variable usage
3. Map found classes/mixins to Foundation's module list from
   `lib/foundation-sites/scss/foundation.scss` lines 93-154
4. Verify no usage via SCSS-only techniques (`@extend`, `@include`)

## Module Usage Matrix

| Foundation Module               | Used? | Evidence                                                                                            |
| ------------------------------- | :---: | --------------------------------------------------------------------------------------------------- |
| `foundation-global-styles`      |  ✅   | Variables (`$white`, `$light-gray`, `$primary-color`), `rem-calc()`, `breakpoint()` in 7 SCSS files |
| `foundation-xy-grid-classes`    |  ✅   | `grid-x`, `cell`, `small-*`, `medium-*`, `large-*`, `small-up-*` in 5 Razor files                   |
| `foundation-typography`         |  ✅   | `text-center` in Icons.razor; header/body base styles                                               |
| `foundation-button`             |  ✅   | `button` 12× in 8 files; `button-style()`, `button-expand()` in `_buttons.scss`                     |
| `foundation-button-group`       |  ✅   | `button-group` 3× in CacheReset.razor                                                               |
| `foundation-callout`            |  ✅   | `callout` + alert/success/warning/primary/secondary 30× in 6 files                                  |
| `foundation-card`               |  ✅   | `card`, `card-divider`, `card-section` in both Articles + Videos                                    |
| `foundation-prototype-classes`  |  ✅   | `padding-*`, `margin-*` utility classes                                                             |
| `foundation-forms`              |  ✅   | `input-group`, `input-group-field`, `input-group-label` in FoundationExamples.razor                 |
| `foundation-grid`               |  ❌   | Legacy grid — not used                                                                              |
| `foundation-flex-grid`          |  ❌   | Legacy flex grid — not used                                                                         |
| `foundation-close-button`       |  ❌   | No close buttons                                                                                    |
| `foundation-label`              |  ❌   | No labels                                                                                           |
| `foundation-progress-bar`       |  ❌   | No progress bars                                                                                    |
| `foundation-slider`             |  ❌   | No sliders                                                                                          |
| `foundation-switch`             |  ❌   | No switches                                                                                         |
| `foundation-table`              |  ❌   | No tables styled via Foundation                                                                     |
| `foundation-badge`              |  ❌   | No badges                                                                                           |
| `foundation-breadcrumbs`        |  ✅   | `breadcrumbs` in NavMenu.razor (main navigation bar)                                                |
| `foundation-dropdown`           |  ❌   | No dropdowns                                                                                        |
| `foundation-pagination`         |  ❌   | No pagination                                                                                       |
| `foundation-tooltip`            |  ❌   | No tooltips                                                                                         |
| `foundation-accordion`          |  ❌   | No accordions                                                                                       |
| `foundation-media-object`       |  ❌   | No media objects                                                                                    |
| `foundation-orbit`              |  ❌   | No orbit sliders                                                                                    |
| `foundation-responsive-embed`   |  ❌   | No responsive embeds                                                                                |
| `foundation-tabs`               |  ❌   | No tabs                                                                                             |
| `foundation-thumbnail`          |  ❌   | No thumbnails                                                                                       |
| `foundation-menu`               |  ❌   | No menus                                                                                            |
| `foundation-menu-icon`          |  ❌   | No menu icons                                                                                       |
| `foundation-accordion-menu`     |  ❌   | No accordion menus                                                                                  |
| `foundation-drilldown-menu`     |  ❌   | No drilldown menus                                                                                  |
| `foundation-dropdown-menu`      |  ❌   | No dropdown menus                                                                                   |
| `foundation-off-canvas`         |  ❌   | No off-canvas                                                                                       |
| `foundation-reveal`             |  ❌   | No reveal modals                                                                                    |
| `foundation-sticky`             |  ❌   | No sticky elements                                                                                  |
| `foundation-title-bar`          |  ❌   | No title bars                                                                                       |
| `foundation-top-bar`            |  ❌   | No top bars                                                                                         |
| `foundation-float-classes`      |  ❌   | No `float-left`, `float-right`, `clearfix` usage                                                    |
| `foundation-flex-classes`       |  ❌   | One `flex-direction: column` in DebugNavigation.razor — inline style, not Foundation class          |
| `foundation-visibility-classes` |  ❌   | No `show-for-*`, `hide-for-*` usage                                                                 |

**Summary**: 10 used out of ~38 modules (26%). 28 modules shipping unused CSS.

## Files by Foundation Class Count

| File                       | Modules Used                            | Class Instances |
| -------------------------- | --------------------------------------- | --------------- |
| `CacheReset.razor`         | 4 (grid, callout, button, button-group) | 18              |
| `LocalStorageDebug.razor`  | 3 (grid, callout, button)               | 22              |
| `FoundationExamples.razor` | 3 (grid, callout, button)               | 45              |
| `Articles.razor`           | 3 (callout, card, button)               | 5               |
| `Videos.razor`             | 3 (callout, card, button)               | 5               |
| `Icons.razor`              | 2 (grid, typography)                    | 3               |
| `Redirect.razor`           | 1 (callout)                             | 4               |
| `CallApiExample.razor`     | 1 (button)                              | 1               |
| `Counter.razor`            | 1 (button)                              | 2               |

11 of 25 Razor files use Foundation. 14 files use zero Foundation classes.

## SCSS Files Using Foundation Internals

| File             | Foundation API used                                                                                                        |
| ---------------- | -------------------------------------------------------------------------------------------------------------------------- |
| `_card.scss`     | `$white`, `$light-gray`, `$dark-gray`, `$primary-color`, `$global-margin`, `$global-padding`, `rem-calc()`, `breakpoint()` |
| `_buttons.scss`  | `button-style()`, `button-expand()`, `breakpoint()`, `$primary-color`, `$white`, `$global-radius`                          |
| `_masonry.scss`  | `breakpoint()`, `$global-padding`                                                                                          |
| `_logo.scss`     | `breakpoint()`                                                                                                             |
| `_callouts.scss` | (passive — relies on `foundation-callout` output)                                                                          |

## Implementation

Before (`scss/vendors/_foundation.scss` via `vendors/index`):

```scss
@include foundation.foundation-everything(true, true);
@include foundation.foundation-prototype-spacing; // redundant — everything() already includes it
```

After (`scss/app.scss` — direct, no vendor forwarding):

```scss
@use "../lib/foundation-sites/scss/foundation" as foundation;
@use "abstracts/index" as abstracts;

$foundation-palette: (...); // project colors → Foundation role names
$global-flexbox: true !global;

@include foundation.foundation-global-styles;
@include foundation.foundation-xy-grid-classes;
@include foundation.foundation-typography;
@include foundation.foundation-button;
@include foundation.foundation-button-group;
@include foundation.foundation-callout;
@include foundation.foundation-card;
@include foundation.foundation-prototype-classes;
```

**Key design decisions:**

- Foundation palette moved from `_foundation.scss` to `app.scss` (only place it's needed)
- `$global-flexbox: true` set explicitly (was implicit via `everything(true, true)`)
- `@use "vendors/index"` removed from `app.scss` — the single forwarding line no longer needed
- `scss/vendors/_foundation.scss` left intact (not modified, not loaded)
- Foundation library files in `lib/foundation-sites/scss/` untouched

## Implementation Gotchas

Three pitfalls encountered during implementation that the Foundation docs and
diagnostic comments in `_foundation.scss` do not warn about.

### Gotcha 1: Mixin Names Differ from Diagnostic Comments

The "Future structure" comment block in `scss/vendors/_foundation.scss` lists:

```scss
// @include foundation-xy-grid;
// @include foundation-global-styles;
// @include foundation-typography;
// ...
```

These are **wrong**. The actual mixin names in
`lib/foundation-sites/scss/foundation.scss` (the `foundation-everything()` body,
lines 93-154) are:

| Diagnostic Comment             | Actual Mixin Name                                              |
| ------------------------------ | -------------------------------------------------------------- |
| `foundation-xy-grid`           | `foundation-xy-grid-classes`                                   |
| `foundation-flex-grid`         | `foundation-flex-grid` (legacy, not used when `$flex: true`)   |
| `foundation-float-classes`     | `foundation-float-classes` (always included in `everything()`) |
| `foundation-prototype-classes` | `foundation-prototype-classes` (only when `$prototype: true`)  |

**Rule**: Always read `lib/foundation-sites/scss/foundation.scss` directly to
get the real `@include foundation-<name>` calls from the `foundation-everything()`
body. Never trust comments in wrapper files.

### Gotcha 2: `@use` Rules Must Precede All Other SCSS Statements

In the module system, `@use` directives must come before any variable
declaration, `@include`, or style rule. This is enforced by dart-sass.

**Wrong** (variables between `@use` statements):

```scss
@use "../lib/foundation-sites/scss/foundation" as foundation;
$foundation-palette: (...); // ❌ breaks — not before all @use
@use "abstracts/index" as abstracts;
```

**Right** (all `@use` first, then variables, then `@include`):

```scss
@use "../lib/foundation-sites/scss/foundation" as foundation;
@use "abstracts/index" as abstracts;

$foundation-palette: (...);
@include foundation.foundation-global-styles;
```

### Gotcha 3: `$global-flexbox` Must Be Set Explicitly

`foundation-everything(true, true)` sets `$global-flexbox: true !global;`
internally. When bypassing the mixin and calling individual `@include` calls,
Flexbox-dependent modules (xy-grid) will compile without errors but produce
broken output unless this variable is set:

```scss
$global-flexbox: true !global;
@include foundation.foundation-xy-grid-classes;
```

Without it, xy-grid falls back to legacy float grid classes.

## Verification

- Build: 0 errors, 0 warnings
- Tests: 334/334 pass
- Compiled CSS: 152,127 bytes → 100,523 bytes (34% reduction)
- 2026-05-19: Gap fix — added `foundation-breadcrumbs` and `foundation-forms`
  (both were omitted in the original audit, breaking NavMenu and FoundationExamples).
  Compiled CSS: 105,905 bytes (now 10 of ~38 modules).
- Deployment: site renders correctly with all Foundation-dependent pages intact

## Foundation 6 Deprecation Context

Foundation 6.9.0 uses `@import` internally (384 deprecation warnings during
compilation with dart-sass). These are Foundation's own code and cannot be
fixed without modifying library files. Impact is cosmetic only — `@import`
will be removed in dart-sass 4.0, but Foundation 6 is unlikely to be updated
by the volunteer maintainers.

## Future Work

If Foundation is removed entirely (replaced with modern CSS Grid/Flexbox and
custom component styles), the ~100KB residual CSS can be eliminated. Estimated
effort: 3-5 days for a full migration, rewriting ~300 lines of owned SCSS
that currently extends Foundation internals.
