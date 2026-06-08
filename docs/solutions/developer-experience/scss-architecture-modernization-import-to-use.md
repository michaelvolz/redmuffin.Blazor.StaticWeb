---
module: SCSS Architecture
date: 2026-04-03
problem_type: developer_experience
component: styling
severity: medium
symptoms:
  - Flat SCSS structure with no organizational hierarchy
  - Heavy use of deprecated @import directives
  - Custom styles duplicating Foundation framework features (reset, spacing, typography)
  - No namespace protection causing potential naming conflicts
  - Inline styles in app.scss instead of modular files
root_cause: legacy_architecture
resolution_type: architecture_restructure
tags:
  - scss
  - foundation
  - architecture
  - code-quality
  - modernization
---

# SCSS Architecture Modernization &mdash; @import to @use/@forward Migration

## Problem

The SCSS codebase had a flat structure with `.scss` files directly in the `scss/` directory (now at `src/redmuffin.Blazor.StaticWeb/scss/`). All imports used the deprecated `@import` directive, which makes all variables, mixins, and functions globally available &mdash; no namespace protection, no clear dependency graph. Several custom files duplicated Foundation framework features (reset/normalize, spacing utilities, typography utilities, grid system, card/button/callout components).

## Root Cause

The SCSS was organically grown without an architectural plan. Foundation was imported globally via `foundation-root.scss` with `@include foundation-everything(true, true)`, and custom styles were added ad-hoc in the same flat namespace.

## Solution

### 1. Directory Structure (simplified 4-folder)

The fully-specified 7-1 pattern was reduced to a 4-folder structure matching
the project's actual needs:

```
scss/
├── abstracts/     # Variables, mixins, functions
├── base/          # Reset, typography, fonts
├── components/    # Feature components, app-shell, navigation
└── lib/           # Vendored Foundation framework
```

### 2. @use/@forward Migration

Every file converted from `@import` to `@use` with explicit namespaces:

```scss
// Before (deprecated)
@import "foundation";
.foo {
  color: $primary-color;
}

// After (modern)
@use "../../lib/foundation-sites/scss/foundation" as foundation;
.foo {
  color: foundation.$primary-color;
}
```

Index files use `@forward` to re-export modules:

```scss
// abstracts/_index.scss
@forward "variables";
@forward "mixins";
@forward "functions";
```

### 3. Foundation Redundancy Elimination

Analyzed all custom code against Foundation's feature set and eliminated duplicates:

- Removed custom reset/normalize (Foundation includes normalize.css v8.0.0)
- Removed custom spacing utilities (Foundation provides margin-0 to margin-3, padding-0 to padding-3)
- Removed custom typography utilities (Foundation provides text-hide, text-truncate, text-transform)
- Converted components to extend Foundation components rather than recreate them

### 4. Foundation Integration Rules

- **Foundation has total priority** &mdash; never override core Foundation settings.
- Extend Foundation's color palette; don't create conflicting color variables.
- Use Foundation's `$breakpoints` map directly; don't redefine breakpoint values.
- Use `@include foundation.breakpoint()` for responsive styles; don't write raw media queries.
- Import Foundation with namespace: `@use '...' as foundation;`

## Prevention

- **No more `@import`**: Use `@use` with namespaces for all new SCSS files.
- **Foundation-first**: Before writing custom styles, check if Foundation already provides the feature.
- **Namespace all Foundation access**: Always use `foundation.` prefix, never rely on global availability.
- **New files in correct layer**: abstracts, base, components, features, layout, utilities, or vendors.
- **Index files with `@forward`**: Every directory gets an `_index.scss` that forwards its modules.
