---
title: Sidenote Titles Should Stay Long but Near the Warning Threshold
date: 2026-04-05
category: developer-experience
module: agent-tooling
problem_type: developer_experience
component: tooling
severity: low
applies_when:
  - maintaining sidenote titles
  - reviewing sidenote list output
  - adjusting title-length warnings
tags:
  - sidenotes
  - titles
  - warnings
  - tooling
---

# Sidenote Titles Should Stay Long but Near the Warning Threshold

## Context

Sidenote titles are more useful when they preserve the original meaning instead of being aggressively shortened. But very long titles make the list harder to scan and produce noisy warnings.

## Guidance

Keep titles as descriptive as possible while staying near the preferred limit of about 100 characters and under the hard cap of 110.

For warning text, prefer wording that clearly says the title **exceeds the preferred length** rather than only reporting the raw count.

Examples:

```text
⚠ SN-0012 exceeds the preferred title length: 123 chars (target ~100, max 110)
⚠ SN-0013 exceeds the preferred title length: 431 chars (target ~100, max 110)
```

## Why This Matters

This keeps titles readable and useful while making the warning immediately understandable. The user can tell at a glance that the title is too long without losing the exact length information.

## When to Apply

- When editing sidenote frontmatter titles
- When changing `List-Sidenotes.ps1` warning output
- When deciding whether to shorten a title or keep more detail

## Examples

```yaml
# Good
title: Add a rule that AI-run PowerShell scripts always use -NoProfile for faster, cleaner execution

# Better warning
⚠ SN-0012 exceeds the preferred title length: 123 chars (target ~100, max 110)
```

## Related

- `.opencode/skills/rm-sidenotes/SKILL.md`
- `scripts/List-Sidenotes.ps1`
