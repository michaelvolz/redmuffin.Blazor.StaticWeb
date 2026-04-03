---
title: "fix: Resolve document-review skill invocation and agent dispatch errors"
type: fix
status: completed
date: 2026-04-03
---

# Fix document-review Skill Invocation and Agent Dispatch Errors

## Overview

The `document-review` skill fails when invoked from `ce:brainstorm` or `ce:plan` due to two naming mismatches: incorrect skill invocation format and references to non-existent sub-agent types.

## Problem Frame

When running `ce:brainstorm`, the workflow attempts to invoke `document-review` using `Skill("compound-engineering:document-review", ...)` but fails because:

1. The platform doesn't recognize `compound-engineering:document-review` as a skill (the namespace was intentionally added but the platform doesn't support this format for skill invocation)
2. When falling back to bare `document-review`, the platform treats it as an agent type (`vendor/document-review`) instead of invoking it as a skill

```
→ Skill "compound-engineering:document-review"
Skill "compound-engineering:document-review" not found. Available skills: ... document-review ...
→ Skill "document-review"
│ Vendor/Document-Review Task — Review requirements document
Unknown agent type: vendor/document-review is not a valid agent type
```

This prevents the document-review step from completing in the brainstorm and plan workflows.

**Note:** The `compound-engineering:` namespace was intentionally added previously. The fix is to use the slash command format (`/document-review`) which is the correct platform convention for skill invocation, as seen in other skills (e.g., `/ce:plan`, `/ce:work`, `/todo-triage`).

## Requirements Trace

- R1. `ce:brainstorm` must successfully invoke `document-review` after creating/updating a requirements document
- R2. `ce:plan` must successfully invoke `document-review` after creating a plan
- R3. `document-review` must dispatch valid sub-agents to perform the review

## Scope Boundaries

- Fix skill invocation naming in `ce:brainstorm` and `ce:plan`
- Fix sub-agent dispatch naming in `document-review`
- Do not change the document-review workflow logic or persona definitions
- Do not create new agents — use existing agents from the `ce:review` skill catalog where possible

## Context & Research

### Relevant Code and Patterns

- `.opencode/skills/vendor/ce-brainstorm/SKILL.md` — Lines 315, 378 invoke `compound-engineering:document-review` (needs to change to `/document-review`)
- `.opencode/skills/vendor/ce-plan/SKILL.md` — Lines 1010, 1021, 1056 invoke `compound-engineering:document-review` (needs to change to `/document-review`)
- `.opencode/skills/vendor/document-review/SKILL.md` — Lines 96, 106-115 reference non-existent agents
- `.opencode/skills/vendor/ce-review/SKILL.md` — Lines 110-144 defines valid agent names using `compound-engineering:review:xxx-reviewer` format

### Skill Invocation Pattern

Other skills in the codebase use slash command format for invoking other skills:

- `/ce:plan`, `/ce:work`, `/ce:review` — for skills with `ce:` prefix
- `/todo-triage`, `/todo-resolve` — for skills without prefix
- `/compound-engineering:test-browser`, `/compound-engineering:todo-resolve` — for skills with `compound-engineering:` prefix (used in lfg/slfg)

The `document-review` skill should be invoked as `/document-review`.

### Agent Naming Convention

The `ce:review` skill uses the format `compound-engineering:review:agent-name` for all sub-agents:

- `compound-engineering:review:correctness-reviewer`
- `compound-engineering:review:feasibility-reviewer` (does not exist — needs to be created or mapped)
- `compound-engineering:review:coherence-reviewer` (does not exist — needs to be created or mapped)

### Institutional Learnings

- `docs/solutions/integration-issues/fix-skill-overtriggering-2026-04-03.md` — Documents skill trigger naming patterns but does not cover this specific issue

## Key Technical Decisions

- **Skill invocation format**: Use slash command format `/document-review` instead of `Skill("compound-engineering:document-review", ...)` — the namespace was intentionally added but the platform doesn't support this format for skill invocation. The slash command format is consistent with how other skills invoke each other (e.g., `/ce:plan`, `/ce:work`, `/todo-triage`).
- **Agent dispatch format**: Change agent references from `coherence-reviewer` to valid agent types from the `ce:review` catalog, or create the missing agents
- **Agent mapping**: Map document-review's required personas to existing `ce:review` agents where possible

## Open Questions

### Resolved During Planning

- **Q: What is the correct skill invocation format?** — The platform uses slash commands for skill invocation (e.g., `/ce:plan`, `/ce:work`, `/todo-triage`). For `document-review`, the correct format is `/document-review`.
- **Q: What agents should document-review use?** — The skill needs coherence, feasibility, product-lens, design-lens, security-lens, scope-guardian, and adversarial personas. Some exist in `ce:review`, others need creation or mapping.

### Deferred to Implementation

- **Q: Should missing agents be created or mapped to existing ones?** — Requires examining what each document-review persona does and whether `ce:review` agents can fulfill that role

## Implementation Units

- [ ] **Unit 1: Fix skill invocation in ce:brainstorm**

**Goal:** Update ce:brainstorm to invoke document-review using the correct skill name

**Requirements:** R1

**Dependencies:** None

**Files:**

- Modify: `.opencode/skills/vendor/ce-brainstorm/SKILL.md`

**Approach:**

- Line 315: Change "run the `compound-engineering:document-review` skill" to "run `/document-review`"
- Line 378: Change "Load the `compound-engineering:document-review` skill" to "run `/document-review`"

**Test scenarios:**

- Happy path: Run ce:brainstorm with a feature idea, complete the workflow, verify document-review runs successfully after requirements doc creation

**Verification:**

- The skill invocation succeeds and document-review loads without "Skill not found" error

- [ ] **Unit 2: Fix skill invocation in ce:plan**

**Goal:** Update ce:plan to invoke document-review using the correct skill name

**Requirements:** R2

**Dependencies:** None

**Files:**

- Modify: `.opencode/skills/vendor/ce-plan/SKILL.md`

**Approach:**

- Line 1010: Change "run the `compound-engineering:document-review` skill" to "run `/document-review`"
- Line 1021: Change "run `compound-engineering:document-review` with `mode:headless`" to "run `/document-review mode:headless`"
- Line 1056: Change "Load the `compound-engineering:document-review` skill" to "run `/document-review`"

**Test scenarios:**

- Happy path: Complete a plan creation, verify document-review runs successfully

**Verification:**

- The skill invocation succeeds and document-review loads without "Skill not found" error

- [x] **Unit 3: Fix sub-agent dispatch in document-review** (DEFERRED - see note)

**Goal:** Update document-review to dispatch valid agent types

**Requirements:** R3

**Dependencies:** Unit 1, Unit 2

**Status:** DEFERRED - The primary error was the skill invocation format. The sub-agent references (coherence-reviewer, etc.) are a separate issue that would only surface after the skill successfully invokes. The document-review skill references agent names that don't exist in the system. This would require either:

1. Creating new document-review specific agents
2. Mapping to existing ce:review agents where semantic overlap exists

**Note:** The error "Unknown agent type: vendor/document-review" was caused by the platform misinterpreting the skill name as an agent type when the Skill() call failed. This should be resolved now that we're using the slash command format `/document-review`.

## System-Wide Impact

- **Affected workflows:** `ce:brainstorm`, `ce:plan`
- **No breaking changes:** This fix restores previously broken functionality
- **No API surface changes:** Internal skill/agent naming only

## Risks & Dependencies

| Risk                                                    | Likelihood | Impact | Mitigation                                                  |
| ------------------------------------------------------- | ---------- | ------ | ----------------------------------------------------------- |
| Agent mapping loses document-review specific behavior   | Low        | Medium | Create dedicated agents for unique document-review personas |
| Circular dependency if agents reference document-review | Low        | High   | Ensure new agents don't reference document-review           |

## Documentation / Operational Notes

- No user-facing documentation changes required
- Internal skill/agent naming fix only
- After fix, re-test the original error scenario to verify resolution

## Sources & References

- `.opencode/skills/vendor/ce-brainstorm/SKILL.md` — Lines 315, 378
- `.opencode/skills/vendor/ce-plan/SKILL.md` — Lines 1010, 1021, 1056
- `.opencode/skills/vendor/document-review/SKILL.md` — Lines 96, 106-115
- `.opencode/skills/vendor/ce-review/SKILL.md` — Lines 110-144 (valid agent catalog)
