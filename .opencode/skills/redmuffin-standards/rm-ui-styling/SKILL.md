---
name: rm-ui-styling
description: "Foundation CSS framework, SCSS compilation, Blazor styling, and WCAG 2.1 AA accessibility. Use when writing SCSS, using Foundation CSS classes, styling Blazor components, or implementing accessibility. For tooling and build commands, see rm-dev-tools. For SCSS conventions, see rm-scss."
---

# UI/Styling

## Framework

- Zurb Foundation 6 (self-hosted SCSS, transitioning to extracted owned components — see `rm-scss` §Foundation Migration Strategy)
- Font Awesome 6.7.0 (self-hosted, woff2)

## Styling Rules

- Use SCSS in `scss/`, NEVER modify compiled CSS.
- Partials start with `_`, included in `app.scss`.
- For SCSS conventions, architecture, and naming: see `rm-scss`.

## SCSS Build

- **Dev**: `sass --watch scss:wwwroot/css` (auto-compiles on save, integrates with `dotnet watch`)
- **Prod**: `sass --style=compressed --no-source-map scss/app.scss:wwwroot/css/app.min.css`
- Requires `dart-sass` installed (`sudo pacman -S dart-sass` on Arch, `winget install Sass.DartSass` on Windows)
- Full toolchain reference: `rm-dev-tools`

## Foundation CSS Patterns

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
