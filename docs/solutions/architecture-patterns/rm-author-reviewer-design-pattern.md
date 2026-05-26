---
module: Code Review Subagents
date: 2026-05-24
problem_type: architecture_pattern
component: tooling
severity: medium
applies_when:
  - Designing specialized code review agents with non-overlapping domains
  - Adding a new author-specific reviewer to the CE persona catalog
  - Configuring reviewer selection rules for CE code-review pipeline
symptoms:
  - Vendor Ousterhout reviewer had unstructured output, no CE compatibility
  - Existing Uncle Bob reviewer used float confidence (0.79), not CE discrete anchors (75/100)
  - No structured mechanism to prevent over-review — flood of findings on trivial diffs
root_cause: missing_tooling
resolution_type: tooling_addition
related_components:
  - CE Persona Catalog
  - OpenCode Configuration
tags:
  - code-review
  - subagents
  - author-reviewers
  - ce-persona-catalog
  - confidence-anchors
  - overkill-prevention
---

## Context

The code-review pipeline had one RM author reviewer — Uncle Bob — using float confidence scores
incompatible with the CE discrete-anchor system. Five high-value authors (Ousterhout, Feathers,
Beck, Fowler, Farley) had no dedicated reviewers. The vendor-provided Ousterhout reviewer produced
lightweight, unstructured output with no CE-compatible JSON contract.

## Guidance

Six author-specific C# reviewers now exist, each with a non-overlapping domain. No two reviewers
can find the same thing — domains are designed for zero overlap.

### Reviewer template

All RM reviewers follow a unified ~55-line template:

```markdown
---
description: Conditional code-review persona, selected when [trigger condition]
mode: subagent
temperature: 0.05
top_p: 0.9
permissions:
  edit: deny
  write: deny
  bash: deny
---

# [Author] C# Reviewer

[Domain definition — what this reviewer hunts for, in 4-5 bullet groups]

## Confidence calibration (CE discrete anchors)

Only report at 75 or 100. [Author-specific 75 and 100 criteria]

## What you don't flag

[Domain boundaries — lists other reviewers' domains, stays in lane]

## Overkill prevention

Max 5 findings, concrete suggested_fix required, merge-delay test.

## Output format

CE-compatible JSON.
```

### The six reviewers

| Reviewer                        | Author           | Unique Domain                              | Triggers on                                                    |
| ------------------------------- | ---------------- | ------------------------------------------ | -------------------------------------------------------------- |
| `rm-uncle-bob-csharp-reviewer`  | Robert C. Martin | Structural design, dependency direction    | Architecture boundaries, DI registration, class structure      |
| `rm-ousterhout-csharp-reviewer` | John Ousterhout  | Complexity depth, information hiding       | New public APIs, classes, interfaces, method signature changes |
| `rm-feathers-csharp-reviewer`   | Michael Feathers | Safety before change, seams                | Test files or production code lacking characterization tests   |
| `rm-beck-csharp-reviewer`       | Kent Beck        | Process quality, test intent, simplicity   | Test files or new C# code                                      |
| `rm-fowler-csharp-reviewer`     | Martin Fowler    | Refactoring patterns, domain model clarity | Domain/service/business-logic classes                          |
| `rm-farley-csharp-reviewer`     | Dave Farley      | Deployment safety, pipeline quality        | CI/CD files, build scripts, deployment config                  |

### Zero domain overlap

Each reviewer has an explicit "What you don't flag" section listing the other five reviewers'
domains. This prevents:

- Uncle Bob finding the same shallow module as Ousterhout (Ousterhout owns complexity depth;
  Uncle Bob owns structural design)
- Beck finding the same extract-method opportunity as Fowler (Fowler owns which pattern;
  Beck owns whether the design is minimal)
- Feathers finding the same test gap as Beck (Feathers owns safety; Beck owns test quality)

### Confidence anchors

All six use CE discrete anchors (75, 100). Never report at 50 or below. The confidence anchor
system enables the CE merge/dedup pipeline to combine findings from multiple reviewers without
float-vs-discrete conversion errors.

### Overkill prevention

Every reviewer enforces:

- **Max 5 findings per review** — forces prioritization over exhaustive cataloging
- **Concrete suggested_fix required** — no "have you considered" drive-by observations
- **Merge-delay test** — every finding must answer: "Would I delay the merge for this?"
- **Domain boundaries** — "What you don't flag" prevents scope creep into other reviewers'
  territory

### CE persona catalog integration

The RM Author Reviewers layer was added to the CE persona catalog with selection rules:

- All six are conditional — never spawn just because `.cs` files exist
- Max 3 RM reviewers per review — beyond 3, signal-to-noise degrades
- Domain overlap prevention — if two would flag the same thing, the orchestrator selected wrong
- Supplement, not replace — additive with always-on CE personas

## Why This Matters

Without structured reviewer domains, code review output becomes noise. Reviewers overlap, produce
conflicting findings, and the orchestrator cannot merge or dedup them. The template enforces:

1. **Deterministic output** — temperature 0.05 means the same diff produces the same findings
2. **Pipeline compatibility** — CE discrete anchors enable automated merge/dedup
3. **Actionability** — concrete suggested_fix means the orchestrator can apply fixes programmatically
4. **Review budget** — max 5 findings prevents review fatigue and scope creep

## When to Apply

- **Adding a new author reviewer**: copy the template, define the unique domain, list all other
  reviewers in "What you don't flag," add to persona catalog, register in both `opencode.jsonc` files.
- **Modifying an existing reviewer**: verify domain boundaries still hold after the change. Update
  "What you don't flag" in the five OTHER reviewers if a domain shifts.
- **Selecting reviewers for a code review**: respect the max-3 rule and domain overlap check in the
  persona catalog.
