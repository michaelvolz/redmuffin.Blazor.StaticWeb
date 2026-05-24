---
date: 2026-05-24
topic: css-2026-baseline-framework
---

# CSS-2026 Baseline Decision Framework

## Summary

A document that teaches how to decide what CSS features are safe to use for any web project. Based on the Web Platform Baseline standard. Includes an anti-patterns section listing Widely available CSS features that should be actively avoided because modern replacements exist.

---

## Problem Frame

CSS has stabilized. The WebDX Community Group now defines Baseline — a formal standard for feature readiness adopted by MDN, Can I Use, VS Code, ESLint, Browserslist, and Chrome DevTools. Three things are still missing:

1. **No decision framework.** Developers ask "can I use CSS nesting?" without a structured process for answering that question. The answer depends on audience, failure tolerance, and the specific feature — but no single document teaches how to arrive at it.
2. **No anti-patterns catalog.** Widely available CSS features like `float` for layout and `clearfix` hacks are technically "safe" but actively bad practice. No authoritative source catalogs what to stop using and what replaces it.
3. **No LLM-usable guidance.** An agent dropped into a new repo has no structured way to determine that project's CSS baseline. The agent needs a framework, not a checklist.

---

## Requirements

### Decision framework

- R1. The document defines three evaluation layers applied in order: audience analysis (who uses this site, what browsers), failure tolerance (what happens if a CSS feature is unsupported), and feature-category assessment (does this feature degrade gracefully or break).

- R2. Each layer includes concrete decision rules with rationale — never prescriptive without explaining WHY. Example: "Government sites default to Widely available because WCAG compliance requires features interoperable for 30+ months."

- R3. The document explains the Web Platform Baseline standard: its three tiers (Limited availability / Newly available / Widely available), the 30-month threshold, the core browser set (Chrome, Edge, Firefox, Safari desktop+iOS), and what each tier means for production use.

- R4. The document covers how analytics data changes the decision: when Real User Monitoring exists, the Baseline target can be data-driven. When it doesn't, Widely available is the safe default. References the Baseline Checker (baseline-checker.chrome.dev).

- R5. Progressive enhancement is the mechanism for adopting features above the chosen floor. The document classifies CSS features by what happens when they are unsupported: **layout-breaking** (content becomes inaccessible or unreadable — requires the floor), **cosmetic-only** (appearance differs but content is usable — safe to enhance), **functional** (interaction or behavior is missing — assessed against failure tolerance per layer two). The same feature can land in different categories for different projects.

### Anti-patterns catalog

- R6. The document includes a list of Widely available CSS features that should be actively avoided because a modern, also-Widely-available replacement exists.

- R7. Each entry includes: what to stop using → what to use instead → why the replacement is better → the Baseline year when the replacement became Widely available.

- R8. Representative categories: layout techniques (float, clearfix, table-layout, inline-block hacks), color/typography (named colors, `font-size` in px for body text, `text-rendering: optimizeLegibility` misuses), responsive patterns (global breakpoints without modern alternatives), deprecated properties (vendor prefixes on standardized features, `zoom` where `transform: scale()` works), and outdated selector patterns (`*` universal resets, deeply nested specificity wars).

### LLM-executable guidance

- R9. The document includes a section for LLMs that specifies: the input (a repo with CSS), the steps (load framework, identify audience, select Baseline target, audit CSS against anti-patterns), and the output (a markdown file listing the Baseline target, Newly available features worth adopting, anti-patterns found in existing code, and a reference to the official Widely available list).

- R10. The project-specific guide references the official Widely available list (web.dev/baseline) rather than enumerating it — hundreds of Widely available features exist and listing them is noise.

### Document qualities

- R11. The document is self-contained — comprehensible without external resources beyond the linked Baseline reference.

- R12. The framework acknowledges that accessibility requirements (WCAG, `prefers-reduced-motion`, `forced-colors`) can set the CSS floor higher than audience analysis alone.

---

## Acceptance Examples

- AE1. **Covers R1, R2, R3.** Given a government compliance project, when the framework is applied, the output is "Baseline Widely available as minimum floor" — not "use whatever's newest" or "check with analytics."

- AE2. **Covers R4, R5.** Given a consumer SaaS with analytics data showing 94% of users on browsers supporting Baseline 2024, when the framework is applied, the output is "Baseline 2024 as target, with Newly available features assessed case-by-case for progressive enhancement."

- AE3. **Covers R6, R7.** Given the anti-patterns catalog entry for `float`-based layouts, a developer sees: "Stop: `float: left` for page layout → Use: flexbox or CSS Grid → Why: Both became Widely available in 2020. Float was a text-wrapping property repurposed for layout; flexbox and grid are designed for layout."

- AE4. **Covers R9, R10.** Given an LLM loaded into a new Blazor WASM project, it produces a `docs/css-baseline.md` listing: the chosen Baseline target, 3-5 Newly available features worth adopting, 5-10 anti-patterns found in existing code, and a reference link to the official Widely available list.

---

## Success Criteria

- A developer (or LLM) can load this document into any web project context and produce a defensible CSS feature policy without external research.
- The document produces materially different but equally correct guidance when applied to different project types (e.g., e-commerce vs personal site).
- The anti-patterns catalog finds real issues worth fixing when scanning existing CSS.

---

## Scope Boundaries

- CSS framework selection (daisyUI vs Tailwind vs vanilla) — separate decision space, handled by existing daisyUI research
- SCSS preprocessor features — existing scss-architecture doc covers this
- JavaScript-for-CSS pattern replacement — CSS features only
- HTML anti-patterns — separate work, captured as SN-0046
- CI/CD enforcement tooling — implementation detail, not the framework
- Exhaustive CSS feature catalog — the framework references the official Baseline list, does not duplicate it

---

## Key Decisions

- **Curated anti-patterns over referenced list.** No single authoritative source catalogs CSS anti-patterns with replacements. The document itself is the authority.
- **Widely available as default, not Newly available.** Industry consensus (web.dev guidance, Clearleft, Target.com, Cybozu) supports this.
- **Three-layer model over flat checklist.** Audience, failure tolerance, and feature class are independent dimensions. A flat checklist produces wrong answers for different project types.

---

## Dependencies / Assumptions

- The Web Platform Baseline standard continues to be maintained by the WebDX Community Group.
- MDN and Can I Use continue to display Baseline status badges.

---

## Outstanding Questions

### Deferred to Planning

- Complete inventory of Widely available CSS anti-patterns for the catalog.
- Whether the Baseline "Discouraged" classification (added Dec 2024) contains entries that overlap with our anti-patterns.
