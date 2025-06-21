---
applyTo: "**/*.scss"
---

# SCSS Guidelines for redmuffin.Blazor.StaticWeb

## Overview

This project uses SCSS (Sass) for styling, with [Zurb Foundation](https://get.foundation/) as the UI framework. SCSS files are preprocessed into CSS using the WebCompiler tool.

## Directory Structure

- **Main SCSS Files:**
  - `wwwroot/lib/app.scss` - Main application styles
  - `wwwroot/lib/foundation-root.scss` - Foundation framework configuration
  - `wwwroot/lib/media-query-debugger.scss` - Responsive design debugging utilities
  - `wwwroot/lib/_site-colors.scss` - Global color variables

- **Foundation Framework:**
  - Foundation 6.9.0 is included via LibMan
  - Located in `wwwroot/lib/foundation-sites/scss/`

## Working With SCSS Files

### Style Guidelines

1. **Follow Foundation Conventions:**
   - Use Foundation's grid system and utility classes
   - Leverage Foundation's mixins and functions
   - Adhere to Foundation's responsive design patterns

2. **Best Practices:**
   - Use variables for colors, spacing, and other repeated values
   - Nest selectors appropriately (max 3 levels of nesting)
   - Follow BEM (Block Element Modifier) naming convention
   - Keep component-specific styles in their respective feature folders
   - Use modern CSS features like Grid, Flexbox, and CSS variables

3. **Accessibility:**
   - Ensure adequate color contrast (WCAG 2.1 AA compliant)
   - Test with screen readers and keyboard navigation
   - Add focus states for interactive elements

### Compilation Process

1. **Edit SCSS Files:**
   - Modify existing files or create new ones as needed
   - Import new files into one of the main SCSS files

2. **Compile to CSS:**
   - Run `.\scripts\compile-webcompiler.ps1` from the project root
   - This uses WebCompiler with the configuration in `webcompilerconfiguration.json`
   - SCSS files listed in `webcompiler.conf` will be compiled

3. **Configuration:**
   - Add new SCSS files to `webcompiler.conf` if they should be compiled independently
   - WebCompiler settings in `webcompilerconfiguration.json` control output formatting

## Custom Theming

1. **Color Palette:**
   - Main colors are defined in `_site-colors.scss`
   - Foundation palette is configured in `foundation-root.scss`

2. **Component Customization:**
   - Override Foundation variables before importing Foundation
   - Create component-specific SCSS files for custom components

## Troubleshooting

- If styles are not updating, ensure the SCSS files are properly imported
- Check compilation errors in the terminal output when running the compile script
- For large style changes, consider using a watch task for automatic compilation

