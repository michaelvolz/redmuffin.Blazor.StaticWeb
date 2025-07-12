---
applyTo: "**/*.scss"
---

# SCSS Coding Instructions

## Framework & Structure
- Use Zurb Foundation 6.9.0 as UI framework
- Main files: `wwwroot/lib/app.scss`, `wwwroot/lib/foundation-root.scss`, `wwwroot/lib/_site-colors.scss`
- Foundation located: `wwwroot/lib/foundation-sites/scss/`

## Coding Rules
- Use Foundation grid system, mixins, and responsive patterns
- Max 3 levels of nesting
- BEM naming convention for custom classes
- Variables for colors/spacing in `_site-colors.scss`
- Component styles in respective feature folders
- Modern CSS: Grid, Flexbox, CSS variables
- WCAG 2.1 AA color contrast compliance
- Focus states for interactive elements

## Workflow
- Edit SCSS files directly
- Import new files into main SCSS files
- SCSS files are used for styling and can be processed by build tools as needed
- Use dotnet build to compile SCSS files into CSS

## Customization
- Override Foundation variables before importing Foundation
- Define colors in `_site-colors.scss`
- Configure Foundation palette in `foundation-root.scss`

