---
title: Planning and Implementation Skill Design — Negative Constraints First
date: 2026-06-06
category: best-practices
module: skill-design
problem_type: best_practice
component: development_workflow
severity: medium
applies_when:
  - Designing new agent skills for coding workflows
  - Evaluating whether an existing planning skill is over-engineered
  - Deciding between a multi-agent skill and a single-context lean skill
  - Reviewing a skill for instruction bloat or vague scope
  - Adding instructions to an agent skill and needing to decide what to exclude
  - Choosing a skill architecture for planning, PRD creation, issue breakdown, or implementation
tags:
  - skill-design
  - negative-constraints
  - planning-skills
  - single-context
  - matt-pocock
  - instruction-standards
  - two-question-test
  - sub-agent-noise
---

# Planning and Implementation Skill Design — Negative Constraints First

## Context

The `ce-plan` skill (ported from Claude Code's compound-engineering pack)
used sub-agents to validate and deepen plans. In practice, sub-agents
consistently produced irrelevant or wrong findings because they lacked
the conversation context that formed the plan's basis. The user spent
more time rejecting sub-agent noise than reviewing the plan itself.

The compensatory scaffolding grew to manage the noise: Phase 0.1 through
5.3, synthesis templates, scope claims tables — layers of process trying
to guard against the very noise sub-agents created. `ce-plan` became a
high-ceremony, low-signal skill.

Research into Matt Pocock's skills revealed a different philosophy: **one
context, no sub-agents, synthesize don't discover.** Each skill does one
thing. Templates are compact (50-200 lines). No interviewing for things
already discussed. Combined with Zhang et al. (2026) findings — negative
constraints outperform positive directives — this formed the basis for a
replacement architecture.

## Guidance

### Six Principles for Skill Design

1. **One thing.** Each skill produces one output. If a skill does two
   things, split it. `ce-plan` tried to plan, validate, deepen, and
   output — all in one skill. The replacement is three skills: one for
   the PRD, one for issues, one for implementation.

2. **No sub-agents for creative or judgment work.** Sub-agents lack
   conversation context. They generate noise. Sub-agents are only safe
   for mechanical operations (search, compile, lint) that don't depend
   on understanding what was discussed. Never use sub-agents for
   discovery, validation, or judgment that depends on context.

3. **Synthesize, don't discover.** The skill should synthesize what the
   user already said into structured output. If the skill needs to ask
   questions to proceed, those questions should be targeted and few —
   not open-ended exploration. Never interview the user for things
   already discussed.

4. **Negative constraints first.** The first substantive section of any
   skill should be "what NOT to do." Zhang et al. (2026) showed negative
   constraints reduce hallucination and improve compliance. Use the
   two-question test for every rule:
   - "What would the agent do without this rule?"
   - "What concrete action must the agent take?"
     If you can't answer both, the rule doesn't earn its place.

5. **Compact templates inline.** 50-200 lines per skill. Templates live
   inside the SKILL.md, not in separate reference files. Every line earns
   its place. If a section can't pass the two-question test, cut it.

6. **Verification checklist at end.** The agent knows when it's done.
   No ambiguous stopping conditions. The checklist should check
   behavioral outcomes, not template compliance.

### The Three-Skill Architecture

The model splits the single monolithic plan skill into three discrete
skills, each with a clear output and a review gate:

```
PRD (decisions) → Review → Issues (vertical slices) → Review → Implementation (TDD)
```

| Skill                 | Output                                           | Review loop                            | Autonomy              |
| --------------------- | ------------------------------------------------ | -------------------------------------- | --------------------- |
| `rm-prd`              | PRD doc: decisions, approach, testing strategy   | Yes, iterate until approved            | Presents, waits       |
| `rm-issues-from-prd`  | Issues doc: vertical slices, acceptance criteria | Yes, iterate until approved            | Presents, waits       |
| `rm-implement-issues` | Working code + implementation notes              | Pre-step grounds each issue in reality | Executes autonomously |

### Template Patterns

**PRD sections:**

- Problem (1-2 sentences, what gap)
- Solution (1-2 sentences, what is proposed)
- Key Technical Decisions (architecture choices — NO file paths)
- Modules & Seams (table: module, path, change, test surface)
- Testing Strategy (what makes a good test, what NOT to test)
- Out of Scope (explicitly what is NOT being done — prevents creep)
- Assumptions (things taken as true that could be wrong)

**Issue sections:**

- U-ID (unique, never renumber, gaps from deletion preserved)
- Status: pending / done
- Type: HITL (needs human) / AFK (agent can run)
- Blocked by: dependency on another U-ID
- What to build: one paragraph end-to-end
- Files: repo-relative paths only
- Acceptance criteria: concrete, testable — never "should work"

**Implementation Notes sections:**

- Decisions Not In Plan (invisible at planning time, only visible during coding)
- Key Discoveries (things the code taught you)
- Changes to Plan (plan said X, reality required Y)
- Pending Issues (deferred work, adjacent bugs)
- Final Verification (test counts, build status)

### Review Loop Design

PRD and Issues docs go through user review → approval before the next
skill activates. The review loop is the guardrail against misalignment.
The implement skill does not need a per-issue review loop because the
pre-step (read code, ADRs, tests, name the pattern) grounds it in
reality before any code is written.

### The Implementation Notes Philosophy

The implementation notes are the gap between plan and reality. During
planning you made your best guess. During implementation the code tells
you what is actually true. Every deviation, every discovery that only
became visible in the running code, every assumption that turned out
wrong — these are the decisions you paid for with implementation time
and debugging. They are the most expensive knowledge in the project and
the first thing lost. Never let them disappear.

Implementation notes exist because the plan is always wrong in some
detail. Capturing deviations immediately prevents the plan from becoming
a fiction that gets replayed as if it were accurate.

## Why This Matters

**If you follow these patterns:** Skills produce correct output on first
or second pass. The user reviews substance (architecture, scope,
trade-offs) instead of fixing template issues or rejecting noise.
Implementation is grounded in code reality, not plan fiction.

**If you don't:** Sub-agents generate irrelevant content that the user
must reject. Layers of compensatory scaffolding accumulate (Phase 0.1
through 5.3, synthesis templates, scope claims). The skill grows in
complexity without growing in reliability. The user learns to dread
loading the skill.

## When to Apply

- Creating a new planning or implementation skill — use the three-skill
  decomposition and template patterns above
- Fixing an existing skill that over-asks or over-scaffolds — apply the
  negative-constraint-first principle and compact-template rule
- Evaluating whether sub-agents are appropriate — if the sub-agent's
  work depends on conversation context, it will fail
- Writing any instruction file — use the two-question test

## Examples

### Before: ce-plan structure (~500 lines, 8+ sub-agent calls)

```
Phase 0.1: Load memory
Phase 0.2: Status check
Phase 1: Understand — sub-agents interview user
Phase 2: Research — sub-agents search codebase
Phase 3: Draft plan — sub-agents validate
Phase 3.1: Architecture review — sub-agents review
Phase 3.2: Technical review — sub-agents review
Phase 4: Deepening pass — more sub-agents
Phase 5: Refine
Phase 5.1-5.3: Polish and output
```

Each phase spawns sub-agents with fresh context — they don't know what
the user already decided. The user spends more time rejecting their
findings than reviewing the plan. Compensatory scaffolding (scope claims,
synthesis templates) added to manage noise from sub-agents that
shouldn't exist.

### After: rm-prd structure (~150 lines, zero sub-agents)

```
Core Guardrails: 4 negative constraints
Process: 4 sequential steps (read → produce → present → done)
Template: 7 compact sections inline
Verification: 4-item checklist before presenting
```

The skill synthesizes what was already discussed. No sub-agents. The
user reviews once, requests changes if needed, approves. The self-judge
checklist catches fixable issues before the user sees them.

## Related

- `docs/solutions/architecture-patterns/instruction-architecture-overhaul.md` — broader instruction-file restructuring context
- `docs/solutions/workflow-issues/self-judge-planning-docs-before-presenting.md` — the self-judge workflow used by both planning skills
- `docs/solutions/best-practices/agent-skill-library-naming-organization-conventions.md` — skill naming and library conventions
- `docs/solutions/tooling-decisions/opencode-skill-ecosystem.md` — existing skill ecosystem documentation
- `~/.config/opencode/skills/redmuffin-guides/rm-instruction-standards/SKILL.md` — instruction standards including Zhang (2026) research
