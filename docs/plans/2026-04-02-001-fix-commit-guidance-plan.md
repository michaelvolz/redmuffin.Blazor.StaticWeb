---
title: fix: strengthen commit guidance
type: refactor
status: active
date: 2026-04-02
---

# fix: strengthen commit guidance

## Overview

The commits skill needs to reflect the repo's actual commitlint rules more explicitly so contributors
and agents stop producing messages that fail validation, especially body lines over 100 characters.

## Problem Frame

The current `commits` skill gives a compact conventional-commit summary, but it omits several repo-
specific validation rules that are enforced by the hook. The result is repeated commit failures even
when the message structure looks close to correct.

## Requirements Trace

- R1. Make the commits skill explicit about the repo's active commitlint rules.
- R2. Highlight the body line-length limit so long paragraphs get wrapped before commit time.
- R3. Preserve the skill's quick-reference value while improving its usefulness for real commit
  creation.
- R4. Keep the guidance aligned with the repository's hook/config behavior rather than generic advice.

## Scope Boundaries

- No changes to `commitlint.config.js`.
- No changes to git hooks, automation, or commit validation tooling.
- No commit-message rewriting utility or helper script.
- No broader documentation consolidation outside the commits skill unless a direct mismatch is found.

## Context & Research

### Relevant Code and Patterns

- `B:\redmuffin.Blazor.StaticWeb\.opencode\skills\commits\SKILL.md`
- `B:\redmuffin.Blazor.StaticWeb\commitlint.config.js`
- `B:\redmuffin.Blazor.StaticWeb\.githooks\commit-msg`
- `B:\redmuffin.Blazor.StaticWeb\README.md` commit guidance references

### External References

- https://commitlint.js.org/reference/rules.html
- https://www.conventionalcommits.org/en/v1.0.0/

## Key Technical Decisions

- Treat `commitlint.config.js` as the source of truth for the rules that must be called out in the
  skill.
- Make the body and footer formatting rules visually obvious, since those are the most common failure
  points.
- Keep the skill terse, but add a dedicated "must not forget" block for rules that prevent lint
  failures.

## Open Questions

### Resolved During Planning

- Which rule is causing the reported failure? `body-max-line-length` at 100 characters.
- What should the skill emphasize? The body formatting rules, breaking-change footer behavior, and
  the allowed commit types/scopes already present in the repo.

### Deferred to Implementation

- Whether any neighboring docs also need a small sync update after the skill is rewritten.
  This can be decided once the updated skill text is drafted and compared to existing references.

## Implementation Units

- [ ] **Unit 1: Align skill content with repo lint rules**

**Goal:** Rewrite the `commits` skill so it explicitly documents the rules enforced by this repo's
commitlint setup.

**Requirements:** R1, R2, R4

**Dependencies:** None

**Files:**

- Modify: `B:\redmuffin.Blazor.StaticWeb\.opencode\skills\commits\SKILL.md`

**Approach:**

- Expand the current `CHECK_ORDER` and `FORMAT` sections into a clearer checklist that mirrors the
  actual lint rules.
- Add the repo-specific rules: body-leading blank line, body must exist, body line length max 100,
  footer-leading blank line.
- Keep the conventional-commits summary, but make the body/footer formatting constraints impossible to
  miss.

**Patterns to follow:**

- Existing terse skill style in `.opencode/skills/commits/SKILL.md`
- Rule names and semantics from `commitlint.config.js`

**Test scenarios:**

- Test expectation: none -- documentation-only change.

**Verification:**

- The skill text explicitly mentions the rules that currently fail in this repo.
- The body line-length limit is called out as 100 characters and tied to the lint rule.

- [ ] **Unit 2: Add failure-prevention examples and quick checks**

**Goal:** Give contributors concrete commit-message examples and a short self-check list that prevents
repeat lint failures.

**Requirements:** R1, R2, R3

**Dependencies:** Unit 1

**Files:**

- Modify: `B:\redmuffin.Blazor.StaticWeb\.opencode\skills\commits\SKILL.md`

**Approach:**

- Add a small set of examples showing a valid short body, a wrapped body, and a footer with the blank
  line separator.
- Call out the common invalid cases: missing body blank line, overlong body line, missing footer blank
  line, and breaking-change formatting mistakes.
- Keep examples short enough to remain readable in the skill, but specific enough to prevent guesswork.

**Patterns to follow:**

- Conventional Commits examples from the spec
- The existing skill's concise bullet-list format

**Test scenarios:**

- Test expectation: none -- documentation-only change.

**Verification:**

- A reader can produce a commit message that passes the repo hook without needing to infer formatting
  rules.
- The skill gives at least one concrete example that demonstrates body wrapping under the 100-character
  limit.

## System-Wide Impact

- **Interaction graph:** affects every commit created by humans and agents in this repo.
- **Error propagation:** bad guidance in the skill leads directly to hook failures at commit time.
- **API surface parity:** commitlint rules in `commitlint.config.js`, the commit-msg hook, and the skill
  should remain aligned.
- **Unchanged invariants:** the hook remains the enforcement mechanism; the skill only improves the
  guidance.

## Risks & Dependencies

| Risk                                                               | Mitigation                                                                                         |
| ------------------------------------------------------------------ | -------------------------------------------------------------------------------------------------- |
| The skill becomes too verbose and loses its quick-reference value. | Keep the rewrite focused on the handful of rules that actually cause failures in this repo.        |
| Guidance drifts from the hook over time.                           | Mirror `commitlint.config.js` naming and keep the body/footer checklist obviously derived from it. |
| Examples still invite overlong bodies.                             | Include a wrapped-body example and explicitly mention the 100-character limit.                     |

## Documentation / Operational Notes

- This is a documentation-only update; no runtime monitoring or rollout steps are needed.
- If any nearby docs repeat commit guidance, they should be checked for consistency after the skill is
  updated.

## Sources & References

- **Related code:** `B:\redmuffin.Blazor.StaticWeb\.opencode\skills\commits\SKILL.md`
- **Related code:** `B:\redmuffin.Blazor.StaticWeb\commitlint.config.js`
- **Related code:** `B:\redmuffin.Blazor.StaticWeb\.githooks\commit-msg`
- **Related docs:** `B:\redmuffin.Blazor.StaticWeb\README.md`
- **External docs:** https://commitlint.js.org/reference/rules.html
- **External docs:** https://www.conventionalcommits.org/en/v1.0.0/
