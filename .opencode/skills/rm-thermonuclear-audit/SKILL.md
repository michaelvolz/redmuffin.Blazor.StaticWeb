---
name: rm-thermonuclear-audit
description: Run a full thermo-nuclear code quality audit on a solution. Activates on phrases like "thermo-nuclear audit", "run thermo-nuclear", "audit with thermo-nuclear", "thermonuclear review", or when the user wants the thermo-nuclear skill run against a full solution. Companion skill to the Cursor vendor thermo-nuclear-code-quality-review skill — handles workflow, batch orchestration, classification, and conflict resolution. Use this for every full-solution thermo-nuclear audit.
---

# rm-thermonuclear-audit

Master orchestrator for full-solution thermo-nuclear code quality audits.
Loads all required references, enforces the classification workflow, and
prevents the mistakes documented in the tools audit lessons learned.

## Pre-Flight — Load Before Starting

Every audit run must load these before the first batch:

1. **Precedence tree**: `docs/thermo-nuclear-gate-precedence-tree.md` —
   resolve every conflict between thermo-nuclear findings and quality
   gates by scanning this tree top-to-bottom, stopping at the first match.

2. **Prior DEFEND list**: `docs/thermo-nuclear-defend-findings.md` (if
   it exists for the target solution). Load it before review starts.
   When the subagent reports a finding that matches a prior DEFEND,
   classify it as DEFEND immediately with a citation — do not re-spend
   classification time.

3. **Audit plan**: `docs/thermo-nuclear-tools-audit-plan.md` — the
   batch breakdown and execution log pattern. The tools audit plan is
   the template; create a plan for the target solution following the
   same structure.

4. **If the target is the tools solution itself**: load the execution
   log (already completed — see the Batch Execution Log table). Only
   run if new code has been added since the last audit.

## Workflow

### Per-Batch Loop

Repeat for each batch in the plan:

1. **SURVEY** — read all files in the batch. Verify build passes
   (`dotnet build --verbosity quiet`). If build fails, fix first.

2. **REVIEW** — spawn the thermo-nuclear subagent with the batch's file
   contents. The subagent type is
   `thermo-nuclear-code-quality-review`. Provide every file in the
   batch — never partial context.

3. **CLASSIFY** — for every finding, scan the precedence tree
   top-to-bottom. Stop at the first match. Output: BLOCKER, IMPROVE,
   DEFEND, or SURFACE.
   - BLOCKER: real bug, data corruption, silent failure. Fix now.
   - IMPROVE: genuine simplification or clarity improvement. Apply now.
   - DEFEND: false positive, algorithm-inherent pattern, scale-appropriate
     design. Document with author citation.
   - SURFACE: cross-cutting change affecting 5+ files across batches.
     Do not fix — report to user.

4. **FIX** — apply all BLOCKERs and IMPROVEs. Verify build passes
   (`dotnet build --verbosity quiet`). Run the test suite. Never change
   production code to make a test pass — tests adapt to correct code,
   never the reverse.

5. **DEFEND REVIEW** — scan every DEFEND in this batch and ask: "did I
   defend this because of batch scope, not because it's wrong?" If yes,
   reclassify as IMPROVE and apply. Batch-scope-as-defense is the #1
   reversal pattern — catch it before committing.

6. **COMMIT** — write a conventional-commit message describing the
   concrete behavior changed. Never group unrelated batches.

### Post-Audit Code-Judo Review

After all batches complete:

1. Read the full DEFEND list.
2. For each DEFEND classified during the audit, ask: "was this defended
   because of batch boundaries that no longer exist?" If yes, it's a
   code-judo refactor — apply now.
3. Apply all code-judo refactors in dependency-ordered commits.
4. Run full test suite. Verify 0/0 build both solutions.

## Abandon Rule

If any batch produces >5 BLOCKERs or >8 IMPROVEs in its first review,
SURFACE the batch to the user instead of fixing. The subagent is
producing noise or the batch is too large.

## Zero-Findings Is Valid

A batch with zero findings after classification is valid — the subagent
found nothing worth fixing. Document it as zero in the execution log and
move on. Never invent findings to fill the log.

## Relevant Files

- `docs/thermo-nuclear-gate-precedence-tree.md` — conflict resolution
- `docs/thermo-nuclear-tools-audit-plan.md` — template and tools audit log
- `docs/thermo-nuclear-defend-findings.md` — tools audit DEFEND list
- `.opencode/skills/vendor/cursor/thermo-nuclear-code-quality-review/SKILL.md`
