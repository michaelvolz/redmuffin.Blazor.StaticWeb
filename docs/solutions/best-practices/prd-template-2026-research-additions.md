---
date: 2026-06-07
title: "2026 PRD Best Practices Research: Template Additions and Companion Skill Updates"
module: instruction-design
tags:
  - prd
  - skill-design
  - best-practices
  - research
  - acceptance-criteria
  - success-metrics
  - non-functional-requirements
  - living-document
problem_type: template-enhancement
difficulty: pattern
---

# 2026 PRD Best Practices: Template Additions and Cross-Skill Impact

## Problem

Our PRD template had 7 sections: Problem, Solution, Key Technical
Decisions, Modules & Seams, Testing Strategy, Out of Scope, Assumptions.
It was lean and matched our use case (engineering-focused, agent audience).
But 2026 research on PRD best practices revealed three gaps: Success
Metrics, Non-Functional Requirements, and Acceptance Criteria. These gaps
meant the implementing agent had to guess what "done" looked like and what
constraints applied.

## Research

17 sources analyzed: Atlassian, River, Perforce, PRD Creator, Scriptonia,
Inktrail, Addy Osmani (Google), AgentSpec, Inflectra, Boundev, Ulad
Shauchenka, Atticus Li, Upsilon IT, Matt Pocock (to-prd), and the
2026 AI PRD generator market.

### Essential sections by consensus frequency

| Section                     | Consensus | In our template                                           |
| --------------------------- | --------- | --------------------------------------------------------- |
| Problem Statement           | 17/17     | ✅ Always had                                             |
| Solution / What             | 15/17     | ✅ Always had                                             |
| Success Metrics             | 14/17     | ❌ Added                                                  |
| Out of Scope                | 14/17     | ✅ Always had                                             |
| User Stories / Personas     | 13/17     | ❌ Rejected (per design)                                  |
| Assumptions & Constraints   | 12/17     | ✅ Always had                                             |
| Acceptance Criteria         | 12/17     | ❌ Added                                                  |
| Non-Functional Requirements | 11/17     | ❌ Added                                                  |
| Dependencies                | 9/17      | ❌ Rejected (merged into Assumptions)                     |
| Risk Assessment             | 8/17      | ❌ Rejected (covered by Out of Scope + Assumptions)       |
| Edge Cases                  | 7/17      | ❌ Rejected (covered by Testing Strategy)                 |
| Technical / Architecture    | 7/17      | ✅ Always had (Key Technical Decisions + Modules & Seams) |
| Open Questions              | 6/17      | ❌ Rejected (spec drafted before PRD, no open questions)  |
| Timeline / Phases           | 5/17      | ❌ Rejected (not applicable to module-scale work)         |

### Why User Stories were rejected for our template

13/17 sources include user stories, but our audience is the implementing
agent, not a PM or product stakeholder. The Modules & Seams table +
Problem/Solution already scope the work more precisely than "As a user,
I want to..." narratives. For our use case, User Stories are overhead
that adds length without decision value. Matt Pocock's to-prd includes
them because his output targets GitHub issue readers (product/PM
audience). Our audience is different — the distinction matters.

## Three Additions to rm-prd Template

### 1. Success Metrics

```
## Success Metrics
1-3 measurable outcomes that prove this work succeeded. "Handler
response < 50ms p95" or "all existing tests pass unchanged" —
not "improve performance" or "make it better."
```

**Research backing:** 14/17 sources. Without this, the agent doesn't
know what "done" looks like. The issue doc's acceptance criteria
inherit from these metrics.

**Priming hazard fix:** The good pattern is mentioned first, bad
pattern second. This mitigates the Rana 2026 priming effect where
mentioning the forbidden pattern activates rather than suppresses it.

### 2. Non-Functional Requirements

```
## Non-Functional Requirements
Performance, security, reliability, or compatibility constraints.
One sentence per relevant axis. Never leave this section blank —
if none apply, state "None — module adds no runtime overhead."
```

**Research backing:** 11/17 sources (ISO 25010 quality attributes,
River, Perforce, PRD Creator). Without explicit NFRs, the agent
over-engineers or under-constrains. The "never leave blank" rule
prevents omission through neglect.

**Priming hazard fix:** Uses negative constraint ("Never leave blank")
instead of positive directive ("state 'none' rather than omitting").

### 3. Acceptance Criteria

```
## Acceptance Criteria
Verifiable conditions that prove the PRD is complete. Each criterion
must be testable without asking the user for clarification. This is
the implementer's "done" checklist.
```

**Research backing:** 12/17 sources (Scriptonia, River, Atticus Li,
PRD Creator). Distinct from Testing Strategy (which describes HOW to
test). AC describes WHAT "done" means. The implementing agent uses
these as a checklist — overlapping with issues doc acceptance criteria
but at PRD scope level.

## Companion Skill Updates

### rm-implement-issues — two additions

**Pre-step: Read the PRD (step 1).** Before reading code, tests, ADRs,
or CONTEXT.md, the implementer must re-read the PRD. Success Metrics,
Non-Functional Requirements, and Acceptance Criteria are the contract.
Without this, the agent implements to its own notion of "done" rather
than the documented one. Added as step 1 in the pre-step sequence,
renumbering 1-6 to 1-2-3-4-5-6-7.

**Living document note in deviation logging.** If implementation
contradicts the PRD — changed file paths, invalidated assumption, new
constraint — the agent must update the PRD and re-present for review.
Previously deviations only went into implementation notes without
touching the PRD. A stale PRD actively misleads future readers.

### rm-issues-from-prd — no changes needed

Already aligned with all research findings. The skill has acceptance
criteria per issue, dependency ordering via Blocked by field, scope
boundaries via Deferred section, and HITL/AFK classification. No gaps.

## The Living Document Principle

Multiple 2026 sources (Atlassian, Boundev, Inflectra, Perforce)
converge on: **the PRD is not frozen at approval.** If implementation
reveals decisions that contradict it — changed file paths, invalidated
assumptions, new constraints — update the PRD and re-present for
review. A stale PRD is worse than no PRD.

Added as step 8 in rm-prd's process and referenced in rm-implement-issues
deviation logging.

## Priming Hazard in Template Instructions

The instruction-standards skill (citing Rana 2026, arXiv 2601.08070)
identifies a priming failure mode: mentioning the forbidden pattern
first activates rather than suppresses its representation. Template
instructions (reference material) are less critical than behavioral
rules, but two locations in the original edits violated this:

1. **Success Metrics:** `Not "improve performance" — "handler response < 50ms"`
   → `"Handler response < 50ms p95" — not "improve performance"`
2. **Non-Functional Requirements:** `"rather than omitting"` (positive directive)
   → `"Never leave this section blank"` (negative constraint)

Both fixed in the final pass.

## Curse of Instructions Awareness

Addy Osmani (2026) identified that too many simultaneous directives
cause agents to follow none well. rm-implement-issues loads 3 mandatory
skills + up to 7 conditional skills — up to 10 skill loads per issue.
Each load adds instruction budget consumption. This is tracked but not
yet addressed; the alternative (baking all patterns into the skill body)
would make the skill too long and violate progressive disclosure.

## What This Changes for the Three-Skill Architecture

All three skills are now aligned with 2026 best practices:

| Skill                 | Sections / Capabilities                                                     | Source                  |
| --------------------- | --------------------------------------------------------------------------- | ----------------------- |
| `rm-prd`              | 10 sections (7 original + 3 new)                                            | 2026 research consensus |
| `rm-issues-from-prd`  | Vertical slices, AC, HITL/AFK, Blocked by, Deferred                         | No changes needed       |
| `rm-implement-issues` | Pre-step reads PRD, TDD loop, logs deviations, updates PRD on contradiction | 2 additions             |

The handoff between skills: PRD → Issues → Implementation notes, with
the PRD remaining a living document throughout.

## Key Design Decisions

1. **Template sections grow but instructions stay concise.** Adding 3
   sections grew the skill from 145 to 179 lines. Each new section is
   3-5 lines of template guidance, not behavioral rules.

2. **Negative constraints in template instructions.** Even reference
   material benefits from prohibition framing. "Never leave blank"
   outperforms "state 'none' rather than omitting."

3. **Cross-skill references without duplication.** rm-prd has the
   Living document step. rm-implement-issues references it rather than
   duplicating it. "If implementation contradicts the PRD, update it"
   is the cross-reference; the full process lives in rm-prd step 8.

## References

- Atlassian (2026): Product Requirements Document guide
- River Editor (2026): How to Write PRDs Engineering Teams Love
- Scriptonia (2026): AI PRD Generator Definitive Guide
- PRD Creator (2026): How to Write a PRD That AI Coding Tools Can Follow
- Addy Osmani (2026): How to Write a Good Spec for AI Agents
- AgentSpec (2026): /prd Universal Skill
- Matt Pocock (2026): to-prd skill, to-issues skill
- Zhang et al. (2026): Do Agent Rules Shape or Distort? arXiv 2604.11088
- Rana (2026): Semantic Gravity Wells. arXiv 2601.08070
- ISO/IEC 25010: Systems and software Quality Requirements and Evaluation
