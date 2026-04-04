# Conversion Patterns

Reference for converting sidenotes into actionable artifacts. The rm-sidenotes skill uses these patterns when the user selects a conversion target type.

## Todo Conversion

**Target:** `.context/compound-engineering/todos/`

**Naming:** `{issue_id}-pending-p3-{description}.md`

- Find highest existing issue_id across both `.context/compound-engineering/todos/` and `todos/`, increment by 1, zero-pad to 3 digits
- Description: derive from sidenote title (kebab-case, brief)

**Frontmatter:**

```yaml
---
status: pending
priority: p3
issue_id: "NNN"
tags: [sidenote]
dependencies: []
---
```

**Body:**

- **Problem Statement:** Use the full sidenote text
- **Findings:** TBD — requires investigation
- **Proposed Solutions:** TBD — requires investigation
- **Recommended Action:** To be filled during triage
- **Acceptance Criteria:** To be defined
- **Work Log:** Initial entry with date and "Converted from sidenote SN-NNNN"

## Brainstorm Conversion

**Target:** `docs/brainstorms/`

**Naming:** `YYYY-MM-DD-{topic}-requirements.md`

- Use today's date
- Topic: derive from sidenote title (kebab-case)
- If a file with the same name already exists, append `-2`, `-3`, etc.

**Structure:** Minimal requirements document with:

- YAML frontmatter: `date`, `topic`, `source: docs/sidenotes/SN-NNNN.md`
- Problem Frame: sidenote text as starting point
- Requirements: to be fleshed out during brainstorm dialogue
- Note at top: "Converted from sidenote SN-NNNN. This document is a starting point — refine through brainstorm dialogue."

## Plan Conversion

**Target:** `docs/plans/`

**Naming:** `YYYY-MM-DD-NNN-{type}-{descriptive-name}-plan.md`

- Use today's date
- Sequence number: check existing plans for today, use next NNN (zero-padded to 3 digits)
- Type: `feat` (default), `fix`, or `refactor` based on sidenote content
- Descriptive name: derive from sidenote title (kebab-case, 3-5 words)

**Structure:** Minimal plan with:

- YAML frontmatter: `title`, `type`, `status: active`, `date`, `source: docs/sidenotes/SN-NNNN.md`
- Overview: sidenote text as starting point
- Problem Frame: brief context
- Note at top: "Converted from sidenote SN-NNNN. This plan is a starting point — refine through /ce:plan."

## Post-Conversion Sidenote Update

After creating any artifact, update the original sidenote file:

```yaml
---
id: SN-NNNN
date: YYYY-MM-DD
status: converted
source-context: "..."
tags: []
converted-to: path/to/artifact.md
---
```
