# UI/Styling Copilot Instructions

This file contains extracted UI and Styling rules from the main copilot-instructions.md. For general rules, see copilot-instructions.md.

## Critical Rules

- **Accessibility**: Ensure WCAG 2.1 AA, semantic HTML, ARIA roles.

## Important Rules

- **Framework**: Zurb Foundation (CDN).
- **Styling**: Use SCSS in `wwwroot/scss/`, NEVER modify CSS or use `.razor.css`. Partials start with `_`, included in `app.scss`.
- **SCSS Build**: Use `Debug-Sass` with `dotnet build --configuration Debug-Sass` and BuildWebCompiler2022.
