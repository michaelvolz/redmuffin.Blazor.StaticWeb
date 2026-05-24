---
date: 2026-05-24
title: Design Tokens Skill and Community Design Ecosystem
tags: [design, design-tokens, skills, typography, ecosystem]
module: styling
problem_type: research
---

# Design Tokens Skill and Community Design Ecosystem

## Research Question

Is there a widely-recommended HTML/CSS skill that enforces coherent typography,
spacing, and a unified style guide — solving the problem of random font sizes
and no design system?

## Answer — Yes, the Design Tokens Skill

The community standard is the **Design Tokens** skill by Julian Oczkowski
(`julianoczkowski/designer-skills`). It generates a complete token system
as CSS custom properties:

- **Color**: semantic light/dark palettes (background, text, border, accent,
  status) using brand-tinted neutrals, not pure white/black
- **Spacing**: consistent scale (4px or 8px base depending on philosophy,
  with multipliers)
- **Typography**: full ramp (font families, sizes xs-4xl, weights,
  line-heights, letter-spacing)
- **Motion**: duration scale, easing curves
- **Layout**: max-widths, border radii, shadows
- **Breakpoints**: responsive range (sm through 2xl)

Key behavior: scans existing tokens first, extends rather than replaces.
The type scale output:

```css
--font-family-display: --font-family-body: --font-family-mono: --font-size-xs /
  sm / base / md / lg / xl / 2xl / 3xl / 4xl --font-weight-normal / medium /
  semibold / bold --line-height-tight / normal / relaxed
  --letter-spacing-tight / normal / wide;
```

## The Standard Design Pipeline

The Claude Code ecosystem has converged on:

```
Design Tokens → Baseline UI → Fixing Accessibility → Fixing Motion/Perf
```

**Design Tokens** generates the foundation. **Baseline UI** audits and fixes
spacing, typography, and interactive states. **Fixing Accessibility** handles
WCAG compliance. **Fixing Motion/Perf** handles reduced-motion and performance
budgets.

## Other Notable Tools

| Tool                            | What it does                                                                                               |
| ------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| **UI/UX Pro Max** (62.6k stars) | 240+ styles, 127 font pairings, auto-selects design system for product type                                |
| **Design System Architect**     | Full token system: OKLCH color, 4px grid, typography scale, dark mode, outputs Tailwind v4 `@theme` block  |
| **Taste Skill**                 | Tunable variance/safety knobs — dial in how opinionated the design output should be                        |
| **Frontend Design Pro Demo**    | 11 named aesthetics (Swiss, Brutalist, Glassmorphism, etc.) with working HTML/CSS demos and master prompts |
| **CLAUDE.md Theme Block**       | Not a plugin — 5 lines in CLAUDE.md enforcing an aesthetic direction. Zero install.                        |

The full landscape is catalogued at `wilwaldon/Claude-Code-Frontend-Design-Toolkit`.

## Decision for This Project

**Path A — Applied now**: Deleted all custom font-size/color/spacing from
card SCSS. Foundation 6's type scale governs everything. Heading level
determines size (h3 for page titles, h5 for card titles). Zero custom values.

**Path B — Deferred**: Download and apply the Design Tokens skill to define
a custom modular scale (Major Third or Perfect Fourth) as SCSS variables.
This would replace Foundation's documentation-site heading sizes with
proportions tuned for a card-based reading feed. The Design Tokens skill
would serve as methodology reference.

## Skill Download

Source: `github.com/julianoczkowski/designer-skills`

Ready to download when Path B is pursued.
