# Conversion Patterns

Reference for converting sidenotes into actionable artifacts. The rm-sidenotes skill uses these patterns when the user selects a conversion target type.

## CRITICAL

- **ALWAYS trigger the appropriate skill** — do not manually create artifacts unless the skill is unavailable
- The skill handles the full workflow: dialogue, document creation, review, handoff
- Pass the sidenote text as context when invoking the skill

## Todo Conversion

**Trigger:** Load `todo-create` skill with sidenote text as context

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

**Trigger:** Run `/ce:brainstorm` with the sidenote text as the feature description

**Target:** `docs/brainstorms/`

**Naming:** `YYYY-MM-DD-{topic}-requirements.md`

- Use today's date
- Topic: derive from sidenote title (kebab-case)
- If a file with the same name already exists, append `-2`, `-3`, etc.

**Process:**

1. Invoke `ce:brainstorm` skill with sidenote text as input
2. The skill handles: existing context scan, product pressure test, collaborative dialogue, requirements document creation, document review, handoff
3. The brainstorm skill will create the requirements document in `docs/brainstorms/`
4. After the brainstorm completes, update the sidenote with the path to the created document

## Plan Conversion

**Trigger:** Run `/ce:plan` with the sidenote text as the feature description

**Target:** `docs/plans/`

**Naming:** `YYYY-MM-DD-NNN-{type}-{descriptive-name}-plan.md`

- Use today's date
- Sequence number: check existing plans for today, use next NNN (zero-padded to 3 digits)
- Type: `feat` (default), `fix`, or `refactor` based on sidenote content
- Descriptive name: derive from sidenote title (kebab-case, 3-5 words)

**Process:**

1. Invoke `ce:plan` skill with sidenote text as input
2. The skill handles: requirements analysis, repo scanning, technical design, implementation units, risk assessment
3. The plan skill will create the plan document in `docs/plans/`
4. After the plan completes, update the sidenote with the path to the created document

## Work Conversion (Direct Execution)

**Trigger:** Run `/ce:work` with the sidenote text as the task description

**Process:**

1. Invoke `ce:work` skill with sidenote text as input
2. The skill handles: task execution, code changes, testing, validation
3. After work completes, update the sidenote with a note about what was done

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
