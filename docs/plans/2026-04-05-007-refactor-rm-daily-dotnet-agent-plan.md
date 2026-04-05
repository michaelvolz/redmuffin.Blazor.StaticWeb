---
title: High-Rigor Daily .NET Agent
type: refactor
status: active
date: 2026-04-05
origin: .opencode/agents/rm-reliable-dotnet-coder.md
---

# High-Rigor Daily .NET Agent

## Overview

Create a lightweight daily-use clone of `rm-reliable-dotnet-coder` that keeps the correctness guardrails but removes the specialist-level ceremony. The intended use case is routine .NET/Blazor work: small fixes, maintenance, quick feature edits, and cleanup where the main risk is accidental breakage or stale assumptions.

## Problem Frame

The current agent is strong for hard problems, but it is too heavy for everyday use because it demands full research and planning before nearly every action. That makes simple work slower than it needs to be and can reduce adoption, even when the user still wants a high bar for correctness.

## Requirements Trace

- R1. Preserve the no-guess, no-blind-edit safety posture.
- R2. Make the default path faster for routine repo-local work.
- R3. Escalate to research and deeper planning only when the task risk justifies it.
- R4. Keep the agent aligned with repo conventions and existing `.opencode` patterns.
- R5. Produce an agent that is practical enough for daily use without losing rigor.

## Scope Boundaries

- Create a new agent file; do not delete or replace `rm-reliable-dotnet-coder`.
- Ignore the obsolete `.github` instruction folder.
- Do not change OpenCode source, global config, or repo build settings.
- Do not add new skills or commands as part of this change.

## Context & Research

### Relevant Code and Patterns

- `.opencode/agents/rm-reliable-dotnet-coder.md`
- `.opencode/agents/rm-janitor.md`
- `AGENTS.md`
- `docs/solutions/integration-issues/opencode-instruction-architecture-pattern-2026-04-03.md`
- `docs/solutions/integration-issues/opencode-instruction-management-lessons-2026-04-03.md`
- `docs/solutions/integration-issues/fix-skill-overtriggering-2026-04-03.md`

### Institutional Learnings

- Keep always-loaded instructions small and reserve heavy guidance for on-demand loading.
- Use explicit triggers and exclusions so a prompt fires when needed without becoming noisy.
- Routine workflows should not pay specialist overhead unless the task is actually risky.

### External References

- None required. Local repo patterns and existing instruction learnings are enough for this prompt-only refactor.

## Key Technical Decisions

- Create a separate daily agent instead of weakening the specialist agent.
- Keep the hard safety rules, but make research conditional instead of mandatory for every task.
- Replace the full-plan requirement with a smaller “plan when needed” rule.
- Remove unconditional response preambles so the agent can stay terse when appropriate.
- Add a clear fast path for low-risk local edits and a clear escalation path for riskier work.

## Open Questions

### Resolved During Planning

- The new agent should target routine maintenance and small feature work, not architecture-level decisions.
- The specialist agent stays available for hard, unfamiliar, or high-risk changes.

### Deferred to Implementation

- Exact final wording for the fast-path and escalation thresholds.
- Final file name for the clone, if testing suggests a better `rm-*` name.

## Implementation Units

- [ ] **Unit 1: Create the daily-use clone file**

**Goal:** Add a new agent markdown file that clearly positions itself as the daily-use alternative to the specialist agent.

**Requirements:** R1, R4, R5

**Dependencies:** None

**Files:**

- Create: `.opencode/agents/rm-daily-dotnet-coder.md`

**Approach:**

- Copy the specialist agent’s structure, but shorten the opening and make the purpose explicit.
- Keep the identity, stack, and guardrails clear so the clone still feels authoritative.

**Execution note:** Prompt-only change; validate by loading the new agent in OpenCode and checking that the name and description are distinct from the specialist agent.

**Patterns to follow:**

- `.opencode/agents/rm-reliable-dotnet-coder.md`
- `.opencode/agents/rm-janitor.md`

**Test scenarios:**

- Happy path: the new agent file is discoverable and reads as a daily-use prompt.
- Edge case: the new name does not collide conceptually with the specialist agent.

**Verification:**

- The new file exists and clearly states its daily-use intent.

- [ ] **Unit 2: Compress the operating protocol**

**Goal:** Rewrite the workflow so ordinary tasks flow through a shorter, lower-friction path.

**Requirements:** R1, R2, R5

**Dependencies:** Unit 1

**Files:**

- Modify: `.opencode/agents/rm-daily-dotnet-coder.md`

**Approach:**

- Keep the core sequence: understand the request, inspect the relevant code, choose the right depth, make the change, verify the result.
- Remove the universal “full plan before action” posture.
- Keep explicit checks for ambiguity so the agent still asks when it truly needs more context.

**Execution note:** Implement as a short, readable workflow rather than a rigid checklist.

**Patterns to follow:**

- The current agent’s safety language
- The lean operational tone from `rm-janitor`

**Test scenarios:**

- Happy path: a one-file local fix stays short and does not trigger a full ceremony loop.
- Edge case: missing context still forces clarification before edits.
- Error path: the agent does not start editing before understanding the request.

**Verification:**

- The prompt reads as concise and action-oriented, not specialist-heavy.

- [ ] **Unit 3: Add conditional research and planning gates**

**Goal:** Keep rigor for risky work while avoiding unnecessary research for obvious local changes.

**Requirements:** R2, R3, R5

**Dependencies:** Unit 2

**Files:**

- Modify: `.opencode/agents/rm-daily-dotnet-coder.md`

**Approach:**

- Research only when the task touches unfamiliar APIs, external services, security, migrations, dependency upgrades, or other high-risk areas.
- Use a lightweight mini-plan only when the work is broad enough to benefit from one.
- Preserve the validation step so the agent still checks its work before finishing.

**Execution note:** Make the gates explicit so the model can choose the smaller path without guessing.

**Patterns to follow:**

- The conditional rigor already implied by the specialist agent
- Repo learnings about keeping instruction surfaces small and targeted

**Test scenarios:**

- Happy path: a documentation or trivial refactor does not force web research.
- Integration: an auth, API, or data-mutation task escalates to research and deeper planning.
- Error path: the agent never treats a risky change like a trivial one.

**Verification:**

- The prompt clearly distinguishes low-risk and high-risk paths.

- [ ] **Unit 4: Add daily-use guidance and boundaries**

**Goal:** Make the clone easy to choose for everyday work by stating when it shines and when it should defer to the specialist agent.

**Requirements:** R4, R5

**Dependencies:** Unit 3

**Files:**

- Modify: `.opencode/agents/rm-daily-dotnet-coder.md`

**Approach:**

- Add a short use-case section for routine maintenance, bug fixes, and small edits.
- Add a short non-goal section for architecture work, major refactors, and other specialist tasks.
- Keep the wording crisp so the agent self-selects appropriately.

**Execution note:** Keep this section brief; it should guide selection, not become a second policy manual.

**Patterns to follow:**

- The existing specialist agent’s strong guardrails
- The repo’s preference for clear boundaries and concise instructions

**Test scenarios:**

- Happy path: daily maintenance work maps naturally to this agent.
- Edge case: a broad architectural change is clearly routed away from the daily clone.

**Verification:**

- The file explains the use case in one glance and makes the boundary with the specialist agent obvious.

## System-Wide Impact

- **Interaction graph:** Only the new `.opencode/agents/` file changes; no runtime code path changes.
- **Error propagation:** Lower-friction defaults should reduce over-processing on simple tasks without weakening high-risk safeguards.
- **State lifecycle risks:** None.
- **API surface parity:** No public APIs or commands change.
- **Integration coverage:** Manual OpenCode smoke checks are enough for this prompt-only change.
- **Unchanged invariants:** `rm-reliable-dotnet-coder` stays intact as the specialist option.

## Risks & Dependencies

| Risk                                                                  | Mitigation                                                                            |
| --------------------------------------------------------------------- | ------------------------------------------------------------------------------------- |
| The clone becomes too loose and loses the value of the original agent | Keep the safety rules, validation, and escalation path intact                         |
| The clone stays too verbose and still feels heavy                     | Trim the workflow to the smallest useful version and keep only the daily-use guidance |
| The file name or wording makes the agent hard to choose               | Test a few representative prompts during smoke review and adjust the label if needed  |

## Documentation / Operational Notes

- No repository documentation changes are required beyond the new agent file.
- The user can test-drive the clone immediately after implementation on routine fixes, then decide whether any wording should be tightened.

## Sources & References

- **Origin document:** `.opencode/agents/rm-reliable-dotnet-coder.md`
- Related code: `.opencode/agents/rm-janitor.md`
- Related docs: `docs/solutions/integration-issues/opencode-instruction-architecture-pattern-2026-04-03.md`
- Related docs: `docs/solutions/integration-issues/opencode-instruction-management-lessons-2026-04-03.md`
- Related docs: `docs/solutions/integration-issues/fix-skill-overtriggering-2026-04-03.md`
