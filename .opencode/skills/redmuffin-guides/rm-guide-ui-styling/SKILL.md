---
name: rm-guide-ui-styling
description: "CSS framework selection, Blazor styling, and WCAG 2.1 AA accessibility. Currently: Foundation 6 + SCSS. Target migration: daisyUI v5 + Tailwind CSS v4 (CDN for dev, precompiled for prod). Use when writing CSS, choosing framework classes, styling Blazor components, or implementing accessibility. For SCSS conventions, see rm-scss. For daisyUI migration research, see docs/research/daisyui-*. For dev workflow details, see rm-dev-workflows."
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
- For SCSS conventions, architecture, and naming: see `rm-scss`.

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
