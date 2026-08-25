# rm-agent-browser-companion skill contract

**Audience:** maintainers and agents editing this skill (not the default
runtime path).

## Purpose

Live browser QA, snapshots, screenshots, and navigation (wraps upstream agent-browser).

## Family

`agent`

## Owns

- Browser automation for live-site QA
- Snapshot/screenshot/navigation procedures for agent browser work
- Co-load rules for upstream agent-browser skill

## Never owns

- bUnit / in-process component tests → rm-test-mechanics
- Package installs → rm-gate-package-lifecycle

## Co-loads (triggers, not ownership)

- Upstream agent-browser skill when driving the browser

## Discriminates from

- rm-test-mechanics — component tests not live browser

## Drift rule

If `SKILL.md` and this file disagree, restore the spine to this contract or
revise this file explicitly with a dated note. Do not leave silent divergence.

### Revision log

| Date       | Change                                                                                         |
| ---------- | ---------------------------------------------------------------------------------------------- |
| 2026-08-03 | Frontmatter triggers expanded for instant load (QA, HAR, 5233, browser errors).                |
| 2026-08-03 | Default local path = frontend-only `:5233` synthetic (99%); production/full stack opt-in only. |
| 2026-08-03 | State the current browser path only; no retired-tool footnotes.                                |
| 2026-07-27 | Initial skill contract (Phase 3 library reconstruction).                                       |
