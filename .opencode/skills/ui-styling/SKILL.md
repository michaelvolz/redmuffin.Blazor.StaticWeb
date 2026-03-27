---
name: ui-styling
description: UI styling standards for Blazor, SCSS compilation, Foundation CSS framework, and accessibility requirements.
invocable: false
---

# UI/Styling

## Framework
- Zurb Foundation (CDN)

## Styling Rules

- Use SCSS in `wwwroot/scss/`, NEVER modify CSS or use `.razor.css`
- Partials start with `_`, included in `app.scss`

## SCSS Build

- Use `Debug-Sass` configuration: `dotnet build --configuration Debug-Sass`
- Requires BuildWebCompiler2022 package (Windows only)

## Accessibility

- Ensure WCAG 2.1 AA compliance
- Use semantic HTML
- Apply ARIA roles where appropriate
