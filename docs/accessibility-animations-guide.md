---
title: Accessibility and Animations Guide
date: 2024-12-01
---

## Overview

This document provides comprehensive guidance on handling animations and motion in web applications while maintaining accessibility compliance, particularly focusing on the `prefers-reduced-motion` CSS media query and its implications.

## The `prefers-reduced-motion` Media Query

### What It Does

The `prefers-reduced-motion` CSS media query detects when users have enabled accessibility settings on their devices to minimize animations and motion effects. This is crucial for users with:

- **Vestibular disorders**: Motion can cause dizziness, nausea, or disorientation
- **Attention disorders**: Excessive animation can be distracting
- **Performance preferences**: Reducing animations improves battery life and performance
- **Personal preferences**: Some users simply prefer less visual motion

### How Users Enable Reduced Motion

#### Windows 11

- **Primary method**: Settings → Accessibility → Visual Effects → Toggle "Animation Effects" OFF
- **Alternative**: Settings → Ease of Access → Display → Turn OFF "Show animations in Windows"
- **Advanced**: Control Panel → System and Security → System → Advanced System Settings → Performance Settings → Visual Effects → Uncheck "Animate controls and elements inside windows"

#### Windows 10

- Settings → Ease of Access → Display → Turn OFF "Show animations in Windows"

#### macOS

- System Preferences → Accessibility → Display → Check "Reduce motion"

#### iOS/iPadOS

- Settings → Accessibility → Motion → Toggle "Reduce Motion" ON

#### Android (9+)

- Settings → Accessibility → Remove animations

## Common Implementation Issues

### Problem: Overly Broad Accessibility Rules

**Issue**: Using universal selectors (`*`) in `prefers-reduced-motion` rules can disable essential animations.

```css
/* PROBLEMATIC - Disables ALL animations */
@media (prefers-reduced-motion: reduce) {
  * {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
  }
}
```

**Solution**: Use specific exclusions for essential animations like loading indicators.

```css
/* BETTER - Excludes essential loading animations */
@media (prefers-reduced-motion: reduce) {
  *:not(.shimmer-placeholder):not(.loading-spinner) {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
  }
}
```

### Essential vs. Decorative Animations

#### Essential Animations (Should NOT be disabled)

- Loading indicators (shimmer effects, spinners)
- Progress bars
- State change feedback (success/error indicators)
- Focus indicators for accessibility

#### Decorative Animations (Should be disabled)

- Parallax scrolling
- Hover effects
- Entrance animations
- Background animations
- Carousel auto-play

## Best Practices

### 1. Selective Animation Disabling

```css
@media (prefers-reduced-motion: reduce) {
  /* Disable decorative animations */
  .parallax-container,
  .hover-animation,
  .entrance-fade {
    animation: none;
    transition: none;
  }

  /* Keep essential animations but reduce intensity */
  .loading-spinner {
    animation-duration: 2s; /* Slower than normal */
  }
}
```

### 2. Provide Alternative Feedback

```css
@media (prefers-reduced-motion: reduce) {
  /* Replace animated loading with static indicator */
  .loading-animated {
    display: none;
  }

  .loading-static {
    display: block;
  }
}

@media (prefers-reduced-motion: no-preference) {
  .loading-animated {
    display: block;
  }

  .loading-static {
    display: none;
  }
}
```

### 3. JavaScript Detection

```javascript
// Detect user's motion preference
const prefersReducedMotion = window.matchMedia(
  "(prefers-reduced-motion: reduce)",
).matches;

if (prefersReducedMotion) {
  // Disable JavaScript-based animations
  document.body.classList.add("reduced-motion");
}

// Listen for changes
window
  .matchMedia("(prefers-reduced-motion: reduce)")
  .addEventListener("change", (e) => {
    if (e.matches) {
      document.body.classList.add("reduced-motion");
    } else {
      document.body.classList.remove("reduced-motion");
    }
  });
```

## Testing Reduced Motion

### Browser Developer Tools

1. Open DevTools (F12)
2. Open Command Palette (Ctrl+Shift+P)
3. Type "Emulate CSS prefers-reduced-motion"
4. Select "reduce" or "no-preference"

### Operating System Testing

1. Enable the reduced motion setting in your OS (see settings above)
2. Refresh your web application
3. Verify that appropriate animations are disabled
4. Ensure essential animations (like loading indicators) still work

## Case Study: Shimmer Loading Effect Fix

### Problem

Our shimmer loading animation was being disabled by an overly broad `prefers-reduced-motion` rule:

```css
@media (prefers-reduced-motion: reduce) {
  * {
    animation-duration: 0.01ms !important;
    /* This disabled the shimmer animation */
  }
}
```

### Solution

We excluded the shimmer-placeholder class from the universal rule:

```css
@media (prefers-reduced-motion: reduce) {
  *:not(.shimmer-placeholder) {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-duration: 0.01ms !important;
  }
}
```

### Reasoning

Shimmer loading effects are **essential animations** that provide important feedback about loading states. Disabling them would harm user experience by removing loading indicators.

## Accessibility Guidelines

### WCAG 2.1 Compliance

- **Success Criterion 2.3.3**: Animation from interactions can be disabled, unless the animation is essential to the functionality or the information being conveyed.

### Implementation Checklist

- [ ] Identify all animations in your application
- [ ] Classify animations as essential or decorative
- [ ] Implement `prefers-reduced-motion` rules for decorative animations
- [ ] Preserve essential animations (loading indicators, state feedback)
- [ ] Test with reduced motion enabled
- [ ] Provide alternative feedback methods where needed
- [ ] Document animation decisions for future reference

## Resources

- [MDN: prefers-reduced-motion](https://developer.mozilla.org/en-US/docs/Web/CSS/@media/prefers-reduced-motion)
- [WCAG 2.1: Animation from Interactions](https://www.w3.org/WAI/WCAG21/Understanding/animation-from-interactions.html)
- [CSS-Tricks: prefers-reduced-motion](https://css-tricks.com/almanac/rules/m/media/prefers-reduced-motion/)
- [Web.dev: prefers-reduced-motion](https://web.dev/articles/prefers-reduced-motion)

---

_Last updated: December 2024_
_This document reflects lessons learned from debugging shimmer animation accessibility issues._
