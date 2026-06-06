---
title: Self-judge planning docs before presenting
date: 2026-06-06
category: workflow-issues
module: skills-architecture
problem_type: workflow_issue
component: development_workflow
severity: medium
applies_when:
  - Creating or updating PRD, Issues, or plan documents
  - Before presenting any planning document for user review
tags:
  - prd
  - issues
  - self-review
  - quality-gate
  - skills
---

# Self-judge planning docs before presenting

## Context

When producing planning documents (PRDs, Issues docs), presenting them
directly to the user for feedback wastes iteration cycles on fixable
issues: missing sections, imprecise acceptance criteria, inconsistent
paths, implicit dependencies. The user has to tell you what's wrong
instead of reviewing the substance.

## Guidance

Before presenting any planning document, run a systematic self-judge
loop: judge against a check table, fix everything found, re-judge from
scratch, and pass a final gate before the user sees it.

**Loop:** judge → fix → re-judge → final gate → present.

### Check table (adapt to document type)

| Check              | What to verify                                                                                                                                      |
| ------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Completeness**   | All required sections present. Nothing missing.                                                                                                     |
| **Precision**      | Every path is repo-relative. Every name is the settled decision, not a placeholder.                                                                 |
| **Hallucination**  | Nothing was invented. Every statement traces to conversation or spec.                                                                               |
| **Editorializing** | No marketing language, no surplus framing, no "why it matters" narratives. Documents state decisions, not justifications.                           |
| **Implicitness**   | Nothing forces the implementer to read another file to figure out what's needed. Dependencies, versions, and references are explicit.               |
| **Consistency**    | File paths use consistent format (all full repo-relative). Old and new paths both fully qualified. Renames list every file, not "others similarly." |

### Issue-specific checks

| Check                     | What to verify                                                                                                                                        |
| ------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Slice depth**           | Each issue is vertical (crosses all layers), not horizontal (single layer).                                                                           |
| **Blocking chain**        | Every `Blocked by` is correct. No missing or circular deps.                                                                                           |
| **Package management**    | All new NuGet packages have version entries in `Directory.Packages.props` (or equivalent). `.csproj` entries annotate package and project references. |
| **Solution registration** | New projects are added to the solution file. Never implicit.                                                                                          |
| **Acceptance criteria**   | Every criterion is concrete and testable — specific mock return values, exact assertions, build commands. Never "should work" or "pass-through."      |
| **Grep scope**            | "Zero hits" searches scoped to `src/` and `tests/` — not the entire repo (excludes git history and doc files).                                        |

### The final gate

After zero issues remain, ask three questions:

- **Is everything unambiguous?** Would two implementers interpret this the same way?
- **Is everything precise?** Every path, version, and name is exact.
- **Is everything complete?** No missing sections, files, or dependencies.

Only proceed when all three answer "yes."

### Re-judge discipline

After fixing, re-judge from scratch. Do not assume previous fixes held.
Repeat until zero issues remain. Do not stop at "looks reasonable" —
verify every criterion.

## Why This Matters

Every iteration cycle spent on fixable issues is time the user should
spend on substance — architecture decisions, scope trade-offs, design
quality. The self-judge loop shifts the burden from the user to the
agent, producing cleaner documents faster.

Session history patterns: repeated user corrections for missing details
(acceptance criteria vagueness, implicit dependencies, inconsistent
paths) drove this workflow's adoption.

## When to Apply

- After writing a PRD or Issues doc, before presenting
- After any user-requested change to the doc, before re-presenting
- Not needed for one-shot replies or quick edits

## Related

- `docs/solutions/architecture-patterns/instruction-architecture-overhaul.md` — instruction file discipline context
- `rm-prd` skill — PRD template and process
- `rm-issues-from-prd` skill — issues doc template and process
- `~/.config/opencode/skills/redmuffin-guides/rm-prd/SKILL.md` (self-judge workflow)
- `~/.config/opencode/skills/redmuffin-guides/rm-issues-from-prd/SKILL.md` (self-judge workflow)
