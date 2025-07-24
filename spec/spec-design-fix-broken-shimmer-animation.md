---
title: Fix Broken Shimmer Animation in Videos.razor and Articles.razor
version: 1.0
date_created: 2024-12-19
last_updated: 2024-12-19
owner: Development Team
tags: [design, ui, animation, scss, blazor, shimmer]
---

# Introduction

This specification defines the requirements to restore the shimmer animation functionality in both Videos.razor and Articles.razor components, ensuring consistent visual loading states across the application while maintaining existing SCSS architecture and functionality.

## 1. Purpose & Scope

**Purpose**: Restore the broken shimmer animation effect in Videos.razor and Articles.razor components to provide consistent visual feedback during image loading states.

**Scope**:

- Videos.razor and Articles.razor Blazor components
- SCSS shimmer animation implementation
- Existing SCSS file structure in `wwwroot/scss/`
- Animation keyframes and styling consistency

**Audience**: Frontend developers, SCSS maintainers, and QA testers

**Assumptions**:

- Existing SCSS architecture must be preserved
- Foundation SCSS framework integration remains intact
- No new SCSS files should be created

## 2. Definitions

- **Shimmer Effect**: A subtle animated gradient that moves across a placeholder element to indicate loading state
- **Keyframes**: CSS animation definitions that specify intermediate steps in an animation sequence
- **SCSS Partials**: Modular SCSS files that are imported into the main stylesheet
- **Foundation SCSS**: The CSS framework used as the base styling system
- **Loading State**: The visual state displayed while content (images) is being fetched

## 3. Requirements, Constraints & Guidelines

### Requirements

- **REQ-001**: Restore shimmer animation functionality in Videos.razor shimmer placeholders
- **REQ-002**: Restore shimmer animation functionality in Articles.razor shimmer placeholders
- **REQ-003**: Ensure both components use identical shimmer animation effects
- **REQ-004**: Maintain existing image loading event handlers (@onload, @onerror)
- **REQ-005**: Preserve all existing functionality in both components
- **REQ-006**: Use existing shimmer keyframes defined in `abstracts/_animations.scss`
- **REQ-007**: Utilize existing shimmer variables from `abstracts/_variables.scss`

### Security Requirements

- **SEC-001**: Ensure no XSS vulnerabilities are introduced through animation properties
- **SEC-002**: Validate that animation performance does not impact application security

### Constraints

- **CON-001**: No new SCSS files may be created
- **CON-002**: Existing SCSS file structure must be preserved
- **CON-003**: Foundation SCSS integration must remain intact
- **CON-004**: No duplication of animation code across files
- **CON-005**: Must not generate any build warnings
- **CON-006**: Animation logic must remain in current SCSS partials

### Guidelines

- **GUD-001**: Follow existing SCSS best practices and naming conventions
- **GUD-002**: Maintain consistency with existing animation implementations
- **GUD-003**: Use semantic class names that clearly indicate purpose
- **GUD-004**: Ensure cross-browser compatibility for animations
- **GUD-005**: Optimize animation performance for mobile devices

### Patterns to Follow

- **PAT-001**: Use existing `@use` directive system for SCSS imports
- **PAT-002**: Reference variables using namespace prefixes (e.g., `vars.$shimmer-duration`)
- **PAT-003**: Apply animations through CSS classes, not inline styles
- **PAT-004**: Use existing Foundation grid and component patterns

## 4. Interfaces & Data Contracts

### SCSS File Structure

```scss
// Current shimmer-related files:
// abstracts/_variables.scss - Contains shimmer variables
// abstracts/_animations.scss - Contains shimmer keyframes
// features/shared/_shimmer-loading-effect.scss - Base shimmer styles
// features/content/_articles.scss - Article-specific styles
// features/content/_videos.scss - Video-specific styles (if exists)
```

### Shimmer Variables (from abstracts/_variables.scss)

```scss
$shimmer-duration: 1.5s;
$shimmer-base-color: #f6f7f8;
$shimmer-highlight-color: #edeef1;
$shimmer-video-base: #e8f4f8;
$shimmer-video-highlight: #d1e7dd;
$shimmer-border-radius: 4px;
```

### Animation Keyframes (from abstracts/_animations.scss)

```scss
@keyframes shimmer {
  0% {
    background-position: -200px 0;
  }
  100% {
    background-position: calc(200px + 100%) 0;
  }
}
```

### Component Structure

```html
<!-- Expected HTML structure for both components -->
<div class="shimmer-placeholder" id="shimmer-{item.Id}">
    <img src="{imageUrl}" alt="{altText}"
         @onload="StopShimmerAsync"
         @onerror="StopShimmerAsync" />
</div>
```

## 5. Acceptance Criteria

- **AC-001**: Given a Videos.razor page load, When images are loading, Then shimmer animation displays with moving gradient effect
- **AC-002**: Given an Articles.razor page load, When images are loading, Then shimmer animation displays with moving gradient effect
- **AC-003**: Given both components are loaded, When comparing shimmer effects, Then both animations are visually identical
- **AC-004**: Given an image loads successfully, When @onload event fires, Then shimmer animation stops and image displays
- **AC-005**: Given an image fails to load, When @onerror event fires, Then shimmer animation stops and fallback displays
- **AC-006**: Given the application builds, When SCSS compilation occurs, Then no warnings are generated
- **AC-007**: Given shimmer animation is active, When viewed on mobile devices, Then animation performs smoothly without lag
- **AC-008**: Given shimmer animation is active, When viewed in different browsers, Then animation displays consistently

## 6. Test Automation Strategy

### Test Levels

- **Unit Tests**: SCSS compilation validation
- **Integration Tests**: Component rendering with shimmer states
- **End-to-End Tests**: Full page load scenarios with image loading

### Frameworks

- **SCSS Testing**: Sass compilation via `dotnet build`
- **Component Testing**: Blazor component testing framework
- **Visual Testing**: Browser-based visual regression testing

### Test Data Management

- Use test images with controlled loading times
- Mock slow network conditions for shimmer visibility
- Test with various image sizes and aspect ratios

### CI/CD Integration

- Automated SCSS compilation checks in GitHub Actions
- Visual regression testing in pull request workflows
- Performance testing for animation smoothness

### Coverage Requirements

- 100% SCSS compilation success
- All shimmer animation states tested
- Cross-browser compatibility verified

### Performance Testing

- Animation frame rate monitoring
- Memory usage during animation cycles
- Mobile device performance validation

## 7. Rationale & Context

### Problem Analysis

The shimmer animation appears to be static rather than animated, indicating that either:

1. The animation keyframes are not being applied correctly
2. The CSS animation property is missing or incorrect
3. The shimmer background is not properly configured for animation

### Design Decisions

- **Reuse Existing Keyframes**: The `@keyframes shimmer` already exists in `abstracts/_animations.scss` and should be leveraged
- **Maintain SCSS Architecture**: The current modular SCSS structure with `@use` directives should be preserved
- **Consistent Implementation**: Both components should use the same base shimmer implementation from `features/shared/_shimmer-loading-effect.scss`

### Technical Context

- Foundation SCSS framework provides the base styling system
- Modern `@use` directive system is used for SCSS imports
- Shimmer variables are centralized in the abstracts layer
- Component-specific styles extend the base shimmer implementation

## 8. Dependencies & External Integrations

### External Systems

- **EXT-001**: Foundation SCSS Framework - Required for base styling and grid system

### Third-Party Services

- **SVC-001**: Image CDN Services - Shimmer displays during image loading from external sources

### Infrastructure Dependencies

- **INF-001**: SCSS Compilation Pipeline - Required for processing SCSS to CSS
- **INF-002**: Blazor Server/WASM Runtime - Required for component rendering and event handling

### Data Dependencies

- **DAT-001**: Image URLs - External image sources that trigger loading states
- **DAT-002**: Component State - Blazor component lifecycle and loading states

### Technology Platform Dependencies

- **PLT-001**: .NET 8+ - Required for Blazor component functionality
- **PLT-002**: Modern CSS Support - Required for CSS animations and gradients
- **PLT-003**: SCSS Compiler - Required for processing SCSS files

### Compliance Dependencies

- **COM-001**: Web Accessibility Guidelines - Animations must respect user preferences for reduced motion

## 9. Examples & Edge Cases

### Working Shimmer Implementation

```scss
// Expected shimmer placeholder styling
.shimmer-placeholder {
  position: relative;
  overflow: hidden;
  background-color: vars.$shimmer-base-color;
  background-image: linear-gradient(
    90deg,
    vars.$shimmer-base-color 0px,
    vars.$shimmer-highlight-color 40px,
    vars.$shimmer-base-color 80px
  );
  background-size: 200px 100%;
  background-repeat: no-repeat;
  animation: shimmer vars.$shimmer-duration infinite linear;
  border-radius: vars.$shimmer-border-radius;
}
```

### Edge Cases

1. **Very Fast Image Loading**: Shimmer may not be visible if images load instantly
2. **Network Failures**: Shimmer should stop when @onerror fires
3. **Cached Images**: Browser-cached images may not trigger loading states
4. **Reduced Motion Preference**: Animation should respect `prefers-reduced-motion` media query
5. **Mobile Performance**: Animation should not cause frame drops on low-end devices

### Browser Compatibility

```scss
// Ensure cross-browser animation support
@keyframes shimmer {
  0% { background-position: -200px 0; }
  100% { background-position: calc(200px + 100%) 0; }
}

// Fallback for browsers without calc() support
@supports not (background-position: calc(200px + 100%)) {
  @keyframes shimmer {
    0% { background-position: -200px 0; }
    100% { background-position: 400px 0; }
  }
}
```

## 10. Validation Criteria

### Functional Validation

- [ ] Shimmer animation displays moving gradient effect in Videos.razor
- [ ] Shimmer animation displays moving gradient effect in Articles.razor
- [ ] Animation stops when image loads successfully
- [ ] Animation stops when image fails to load
- [ ] Both components show identical shimmer effects

### Technical Validation

- [ ] SCSS compiles without warnings
- [ ] No duplicate animation code exists
- [ ] Existing SCSS structure is preserved
- [ ] Foundation integration remains intact
- [ ] Performance is acceptable on mobile devices

### Cross-Browser Validation

- [ ] Chrome: Animation displays correctly
- [ ] Firefox: Animation displays correctly
- [ ] Safari: Animation displays correctly
- [ ] Edge: Animation displays correctly
- [ ] Mobile browsers: Animation performs smoothly

### Accessibility Validation

- [ ] Animation respects `prefers-reduced-motion` setting
- [ ] Screen readers can access content during loading
- [ ] Keyboard navigation is not affected

## 11. Related Specifications / Further Reading

- [Foundation SCSS Documentation](https://get.foundation/sites/docs/sass.html)
- [CSS Animations Specification](https://www.w3.org/TR/css-animations-1/)
- [Blazor Component Lifecycle](https://docs.microsoft.com/en-us/aspnet/core/blazor/components/lifecycle)
- [Web Accessibility Guidelines for Animations](https://www.w3.org/WAI/WCAG21/Understanding/animation-from-interactions.html)
- [SCSS @use Directive Documentation](https://sass-lang.com/documentation/at-rules/use)
