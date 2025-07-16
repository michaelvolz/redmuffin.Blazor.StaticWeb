---
applyTo: "**/*.scss"
---

# Comprehensive SCSS Coding Instructions

## 1. Introduction to SCSS
SCSS (Sassy CSS) is a preprocessor that enhances CSS with features like variables, mixins, functions, and nesting. It compiles into standard CSS, enabling developers to write more maintainable and efficient stylesheets. SCSS is particularly valuable for large projects, as it supports modularity, reusability, and robust code organization.

## 2. Framework & Structure
- Use Zurb Foundation 6.9.0 as UI framework
- Main files: `scss/app.scss`, `scss/foundation-root.scss`
- Foundation located: `wwwroot/lib/foundation-sites/`
- Use Foundation grid system, mixins, and responsive patterns
- Modern CSS: Grid, Flexbox, CSS variables
- **CRITICAL**: Use modern `@use` and `@forward` directives (NO `@import`)
- All Foundation access through namespaces: `@use '../lib/foundation-sites/scss/foundation' as foundation;`

## 3. Architecture  Organization

### Feature-Based SCSS Organization

#### Feature Folders Structure
Organize SCSS files by Blazor feature rather than by type to maintain consistency with the component-based architecture:

```plaintext
scss/
├── features/
│   ├── authentication/
│   │   ├── _login.scss
│   │   ├── _register.scss
│   │   └── _index.scss
│   ├── user-profile/
│   │   ├── _profile-view.scss
│   │   ├── _profile-edit.scss
│   │   └── _index.scss
│   ├── dashboard/
│   │   ├── _dashboard-layout.scss
│   │   ├── _widgets.scss
│   │   └── _index.scss
│   └── shared/
│       ├── _buttons.scss
│       ├── _forms.scss
│       └── _index.scss
├── abstracts/
│   ├── _variables.scss
│   ├── _mixins.scss
│   └── _functions.scss
├── base/
│   ├── _reset.scss
│   └── _typography.scss
├── layout/
│   └── _grid.scss
├── themes/
│   └── _theme-dark.scss
├── vendors/
│   └── _foundation.scss
└── app.scss
```

#### Feature Folder Guidelines

1. **Mirror Blazor Feature Structure**: Each feature folder should correspond to a Blazor feature in `src/redmuffin.Blazor.StaticWeb/Features/`
2. **Lightweight and Modular**: Keep feature SCSS files focused on specific components or pages within that feature
3. **Index Files**: Each feature folder should have an `_index.scss` file that imports all partials in that feature
4. **Cross-Feature Dependencies**: Minimize dependencies between features; use shared abstracts instead
5. **Component-Specific Styles**: Place component-specific styles in the corresponding feature folder

#### Feature Folder Naming Conventions
- Use kebab-case for folder names (e.g., `user-profile`, `content-management`)
- Prefix partial files with underscore (e.g., `_profile-view.scss`)
- Use descriptive names that match the component or page purpose
- Group related components within the same feature folder

#### Feature Index File Pattern
```scss
// features/user-profile/_index.scss
@forward 'profile-view';
@forward 'profile-edit';
@forward 'profile-settings';
```

#### Main App Integration
```scss
// app.scss
@use 'abstracts' as *;
@use 'base' as *;
@use 'layout' as *;
@use 'vendors' as *;

// Feature imports
@use 'features/authentication';
@use 'features/user-profile';
@use 'features/dashboard';
@use 'features/shared';

// Theme imports
@use 'themes' as *;
```

#### Benefits of Feature-Based Organization
- **Maintainability**: Easy to locate and modify styles for specific features
- **Scalability**: New features can be added without affecting existing styles
- **Team Collaboration**: Different developers can work on different features simultaneously
- **Code Splitting**: Supports lazy loading of feature-specific styles
- **Blazor Alignment**: Matches the component-based architecture of Blazor WebAssembly

### Modular Architecture Patterns

#### a. 7-1 Pattern (Sass Guidelines)
The 7-1 pattern organizes SCSS into seven folders and one main file for clarity and maintainability:
- **abstracts/**: Variables, mixins, functions, and placeholders (e.g., `_variables.scss`, `_mixins.scss`).
- **base/**: Base styles like resets and typography (e.g., `_reset.scss`, `_typography.scss`).
- **components/**: Reusable components (e.g., `_button.scss`, `_card.scss`).
- **layout/**: Layout-related styles (e.g., `_grid.scss`, `_header.scss`).
- **pages/**: Page-specific styles (e.g., `_home.scss`).
- **themes/**: Theme-specific styles (e.g., `_theme-dark.scss`).
- **vendors/**: Third-party styles (e.g., `_bootstrap.scss`).
- **main.scss**: The entry point that imports all other files.

**Modern Import Order with `@use` in `main.scss`:**
```scss
// Foundation must be imported first with namespace
@use '../lib/foundation-sites/scss/foundation' as foundation;

// Import abstracts with namespaces
@use 'abstracts/variables' as vars;
@use 'abstracts/mixins' as mixins;
@use 'abstracts/functions' as functions;

// Import other modules
@use 'vendors';
@use 'base';
@use 'layout';
@use 'components';
@use 'features';
@use 'utilities';
```

#### b. SMACSS (Scalable and Modular Architecture for CSS)
SMACSS categorizes styles into five groups:
- **Base**: Default styles for HTML elements (e.g., resets, typography).
- **Layout**: Styles for page structure (e.g., header, footer).
- **Module**: Reusable components (e.g., buttons, forms).
- **State**: Styles for dynamic states (e.g., `.is-active`, `.is-hidden`).
- **Theme**: Styles for visual themes (e.g., color schemes).

**Example:**
```scss
/* Base */
html { margin: 0; font-family: sans-serif; }

/* Layout */
.l-header { background: #fcfcfc; }

/* Module */
.button { padding: 10px; }

/* State */
.is-active { background: blue; }

/* Theme */
.theme-dark .button { background: #333; }
```

#### c. CSS Modules
CSS Modules provide locally scoped class names, ideal for component-based frameworks like React:
- Each component has its own SCSS file (e.g., `Button.scss`).
- Use `.scss` for modular styles and `.global.scss` for global styles.
- Configure Webpack to handle modular SCSS (e.g., `css-loader` with `camelCase: true`).

**Example:**
```scss
/* Button.scss */
.button {
  padding: 10px;
}
```

**Compiled Output:**
```css
.Button_button_3rk4 { padding: 10px; }
```

### Project-Specific Structure
```plaintext
scss/
├── abstracts/
│   ├── _variables.scss
│   ├── _mixins.scss
│   └── _functions.scss
├── base/
│   ├── _reset.scss
│   └── _typography.scss
├── components/
│   ├── _button.scss
│   └── _card.scss
├── layout/
│   └── _grid.scss
├── pages/
│   └── _home.scss
├── themes/
│   └── _theme-dark.scss
├── vendors/
│   └── _bootstrap.scss
└── main.scss
```

## 4. **CRITICAL**: Modern SCSS Module System (@use and @forward)

### **MANDATORY**: No More @import - Use @use and @forward Only

**IMPORTANT**: This project uses the modern SCSS module system. The legacy `@import` directive is **FORBIDDEN**. Use only `@use` and `@forward` directives.

#### Foundation Integration Requirements

**Foundation must be imported first with namespace:**
```scss
// ALWAYS import Foundation first in app.scss
@use '../lib/foundation-sites/scss/foundation' as foundation;
```

**Access Foundation features through namespace:**
```scss
// Variables
.example {
  color: foundation.$primary-color;
  margin: foundation.$global-margin;
}

// Functions
.example {
  font-size: foundation.rem-calc(16);
  width: foundation.percentage(1, 3);
}

// Mixins
.example {
  @include foundation.breakpoint(medium) {
    width: 50%;
  }
}
```

#### Module Import Patterns

**1. Index Files with @forward:**
```scss
// abstracts/_index.scss
@forward 'variables';
@forward 'mixins';
@forward 'functions';
@forward 'animations';
@forward 'placeholders';
```

**2. Module Dependencies with @use:**
```scss
// abstracts/_functions.scss
@use 'variables' as vars;
@use '../lib/foundation-sites/scss/foundation' as foundation;

@function custom-function($value) {
  @return foundation.rem-calc($value) + vars.$base-margin;
}
```

**3. Main App.scss Structure:**
```scss
// app.scss - ACTUAL IMPLEMENTATION
@use '../lib/foundation-sites/scss/foundation' as foundation;
@use 'abstracts/variables' as vars;
@use 'abstracts/mixins' as mixins;
@use 'abstracts/functions' as functions;
@use 'vendors';
@use 'base';
@use 'layout';
@use 'components';
@use 'features';
@use 'utilities';
```

#### Namespace Management

**Explicit Dependencies:**
```scss
// Each file must explicitly import what it needs
// layout/_grid.scss
@use '../abstracts/variables' as vars;
@use '../abstracts/functions' as functions;
@use '../lib/foundation-sites/scss/foundation' as foundation;

.custom-grid {
  @include foundation.grid-row();
  padding: vars.$base-padding;
  width: functions.percentage(1, 2);
}
```

**Variable Scoping:**
```scss
// Variables are scoped to their namespace
// WRONG - this won't work:
.example {
  color: $primary-color; // Error: undefined variable
}

// CORRECT - use namespace:
.example {
  color: vars.$primary-color; // Works
}
```

#### Benefits of Modern Module System

1. **Namespace Protection**: Prevents naming conflicts
2. **Explicit Dependencies**: Clear what each file needs
3. **Better Performance**: Only loads what's needed
4. **Future-Proof**: Aligns with modern SCSS standards
5. **Foundation Integration**: Clean namespace separation

#### Rules for New Files

1. **Add to Index**: Update `_index.scss` files with `@forward 'new-file';`
2. **Import Dependencies**: Use `@use` for all dependencies
3. **Use Namespaces**: Access variables/mixins/functions via namespace
4. **Test Compilation**: Run `dotnet build` after changes
5. **Foundation First**: Always import Foundation before custom modules

#### Common Patterns

**Component with Foundation:**
```scss
// components/_card.scss
@use '../abstracts/variables' as vars;
@use '../lib/foundation-sites/scss/foundation' as foundation;

.custom-card {
  @extend foundation.card;
  border-color: vars.$brand-burgundy;
  
  @include foundation.breakpoint(medium) {
    padding: foundation.rem-calc(20);
  }
}
```

**Mixin with Namespace:**
```scss
// abstracts/_mixins.scss
@use 'variables' as vars;
@use '../lib/foundation-sites/scss/foundation' as foundation;

@mixin custom-button($color: vars.$primary-color) {
  @include foundation.button();
  background-color: $color;
  border-radius: foundation.rem-calc(4);
}
```

### **TESTING**: Always verify with dotnet build

After any SCSS changes, run:
```bash
dotnet build
```

This validates the module system works correctly and catches namespace errors early.

## 5. Coding Rules & Best Practices

### a. Variables
- Use variables for values repeated at least twice or likely to change
- Use `!default` for variables that can be overridden in libraries
- Prefer maps for complex value sets (e.g., breakpoints, z-indexes)
- Variables for colors/spacing in `_site-colors.scss`
- **Example:**
  ```scss
  $breakpoints: (
    small: 480px,
    medium: 768px,
    large: 1024px
  );
  $primary-color: #007bff;
  ```

### b. Mixins and Functions
- **Mixins**: Use for reusable property groups (e.g., clearfix, size). Keep under 20 lines.
  ```scss
  @mixin size($width, $height) {
    width: $width;
    height: $height;
  }
  .box { @include size(100px, 100px); }
  ```
- **Functions**: Use for computations.
  ```scss
  @function percentage($value, $total) {
    @return ($value / $total) * 100%;
  }
  ```
- Avoid custom vendor prefix mixins; use Autoprefixer instead

### c. Extend
- Use `@extend` with placeholders (`%`) to share styles without increasing specificity
- **Example:**
  ```scss
  %button-base {
    padding: 10px;
    border: none;
  }
  .primary-button {
    @extend %button-base;
    background: $primary-color;
  }
  ```

### d. Nesting
- Max 3 levels of nesting
- Limit nesting to pseudo-classes, pseudo-elements, and component states
- Avoid deep nesting to prevent overly specific selectors
- **Example:**
  ```scss
  .button {
    padding: 10px;
    &:hover { background: lighten($primary-color, 10%); }
  }
  ```

### e. Responsive Design
- Use general breakpoint names (e.g., 'small', 'medium')
- Manage breakpoints with a mixin
- **Example:**
  ```scss
  @mixin respond-to($breakpoint) {
    @if map-has-key($breakpoints, $breakpoint) {
      @media (min-width: map-get($breakpoints, $breakpoint)) {
        @content;
      }
    } @else {
      @error "Unknown breakpoint: #{$breakpoint}";
    }
  }
  .container {
    width: 100%;
    @include respond-to(medium) { width: 80%; }
  }
  ```

### f. Naming Conventions
- BEM naming convention for custom classes
- Use lowercase hyphen-delimited names (e.g., `$vertical-rhythm-baseline`, `@mixin size`)
- For constants, use all-caps snakerized names (e.g., `$CSS_POSITIONS`)
- Namespace distributed code (e.g., `su-` prefix)

## 6. Accessibility & Quality
- WCAG 2.1 AA color contrast compliance
- Focus states for interactive elements
- **Minimize Specificity**: Use classes over IDs and avoid `!important`
- **Linters**: Use SCSS-lint to enforce code quality
- **Testing**: Test compilation with SassMeister
- **Error Handling**: Use `@error` for critical issues (e.g., missing map keys)

## 7. Reusability
- **Components**: Create independent, reusable components in separate partials (e.g., `_button.scss`)
- **Partials**: Use leading underscores (e.g., `_partial.scss`) and import where needed
- **Avoid Cross-Referencing**: Ensure components do not depend on each other's styles
- Component styles in respective feature folders

## 8. Workflow
- Edit SCSS files directly
- Import new files into main SCSS files
- SCSS files are used for styling and can be processed by build tools as needed
- Use dotnet build to compile SCSS files into CSS

## 9. Customization
- Override Foundation variables before importing Foundation
- Define colors in `_site-colors.scss`

## 10. Code Formatting
- **Indentation**: Use 2 spaces, no tabs
- **Line Length**: Keep under 80 characters
- **Declaration Sorting**: Use alphabetical or type-based sorting (e.g., Concentric CSS)
- **Strings**: Use single quotes, except for `@charset` (double quotes in CSS output)
- **Colors**: Prefer HSL, then RGB, then lowercase shortened hex

## 11. Documentation
- Use SassDoc for documenting reusable elements
- **Example:**
  ```scss
  /// Vertical rhythm baseline used across the codebase.
  /// @type Length
  $vertical-rhythm-baseline: 16px;
  ```

## 12. Tools and Methodologies
- **CSS Modules**: Use for component-based frameworks to scope styles locally
- **BEM**: Apply Block-Element-Modifier naming for clarity (e.g., `.button--primary`)
- **Autoprefixer**: Automate vendor prefixes for cross-browser compatibility
- **Grid Systems**: Consider Bootstrap, Foundation, or Susy

## 13. Practical Examples

### a. Reusable Component
```scss
// components/_button.scss
%button-base {
  padding: 10px 20px;
  border: none;
  cursor: pointer;
}
.button {
  @extend %button-base;
  @include respond-to(medium) { padding: 15px 30px; }
}
.button--primary {
  @extend %button-base;
  background: $primary-color;
  color: white;
}
```

## 13. Additional Notes
- **Simplicity**: Prioritize KISS (Keep It Simple, Stupid) over DRY when appropriate
- **Consistency**: Adhere to a consistent styleguide
- **Integration with C# and Blazor**: For Blazor projects, use CSS Modules or scoped CSS to align with component-based architecture, ensuring compatibility with Visual Studio 2022 workflows

