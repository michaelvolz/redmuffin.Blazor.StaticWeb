---
date: 2026-05-23
title: "Thermo-Nuclear Full Tools Audit Plan"
tags:
  - thermo-nuclear
  - code-review
  - tools
  - quality-gates
  - audit
---

# Thermo-Nuclear Full Tools Audit Plan

## What Belongs in This File

- **Viewpoint**: Agent executing a batched thermo-nuclear review of the
  entire tools solution. Reader knows the thermo-nuclear skill's rubric
  and the tools codebase structure.
- **What belongs**: Batch definitions (files per batch, full paths,
  ordering), classification rules, success criteria, risk mitigations,
  execution log.
- **What does NOT belong**: Per-batch findings or fix details (those go
  in batch-specific implementation notes), general thermo-nuclear skill
  documentation (in the skill file itself), main solution audit scope
  (separate plan).

---

## 1 — Scope

42 source files across 8 batches. Enums, records, options, and YAML
config models are excluded — zero logic to review. `DepthDetector.cs`
already completed (B0).

---

## 2 — Batches

Grouped by domain so the skill sees cross-file patterns. Each batch:
3-6 files, 400-800 lines. All paths relative to
`tools/src/redmuffin.Tools.QualityGates/`.

| Batch  | Domain                           | Files                                                                                                                                                                                                                        | Rationale                                                                    |
| ------ | -------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------- |
| **B1** | Mutation pipeline                | `Analysis/MutationRunner.cs`, `Analysis/MutationDiscoverer.cs`, `Analysis/MutationApplicator.cs`, `Analysis/MutationRules.cs`                                                                                                | Most complex — Roslyn manipulation + process spawning + coverage integration |
| **B2** | Scrap pipeline                   | `Analysis/ScrapDuplication.cs`, `Analysis/ScrapScorer.cs`, `Analysis/ScrapRecommender.cs`, `Analysis/TestNormalizer.cs`, `Analysis/TestMethodParser.cs`                                                                      | Roslyn syntax rewriting + Jaccard similarity                                 |
| **B3** | Dupes + Coverage                 | `Analysis/DupesDetector.cs`, `Analysis/DupesNormalizer.cs`, `Analysis/DupesOutputFormatter.cs`, `Analysis/CoverageGapDetector.cs`, `Analysis/CoberturaMerger.cs`                                                             | Structural similarity + XML parsing                                          |
| **B4** | CRAP pipeline                    | `Analysis/CyclomaticComplexity.cs`, `Analysis/CoverageParser.cs`, `Analysis/CoverageReader.cs`, `Analysis/MethodMapper.cs`, `Analysis/ExtractionPressure.cs`, `Analysis/TestClassDiscovery.cs`                               | Cobertura parsing + CC calculation                                           |
| **B5** | Command orchestration            | `Commands/AllCommand.cs`, `Commands/GateRunResults.cs`, `Commands/GitFileFilter.cs`, `Analysis/SlnxProjectDiscovery.cs`                                                                                                      | Cross-gate orchestration, CLI wiring                                         |
| **B6** | Gate handlers                    | `Commands/CrapCommand.cs`, `Commands/CrapHandler.cs`, `Commands/ScrapCommand.cs`, `Commands/ScrapHandler.cs`, `Commands/DupesCommand.cs`, `Commands/DupesHandler.cs`, `Commands/DepthCommand.cs`, `Commands/DepthHandler.cs` | Handler pattern consistency, coverage pipeline logic                         |
| **B7** | Architecture + mutation commands | `Commands/ArchCommand.cs`, `Commands/ArchHandler.cs`, `Commands/ArchOutputFormatter.cs`, `Commands/MutateCommand.cs`, `Commands/MutateHandler.cs`, `Models/ComponentGraph.cs`, `Models/ProjectGraph.cs`                      | DFS cycle detection + subprocess mutation runner                             |
| **B8** | Entry point                      | `Program.cs`                                                                                                                                                                                                                 | System.CommandLine root wiring                                               |

---

## 3 — Workflow Per Batch

### 3.1 — Gate Conflict Resolution

Quality gates embody our authors (CRAP = Uncle Bob, Depth = Ousterhout,
etc.). When a thermo-nuclear finding conflicts with a gate flag, resolve
using the precedence tree — never ad-hoc judgment.

See `docs/thermo-nuclear-gate-precedence-tree.md` for the decision
tables. The tree grows: every new conflict type gets a row.

### 3.2 — Review and Classify

```
1. REVIEW
   Spawn thermo-nuclear subagent via task tool. Feed file contents
   as "this is new code in the PR." If the subagent cannot spawn,
   surface to user — never substitute a general review.

2. COLLECT — gather ALL findings from the report.

3. CLASSIFY — categorize every finding:

   BLOCKER  — Real bug. Demonstrated with concrete wrong output.
              Fix required. Write characterization test first.

   IMPROVE  — Structural improvement backed by at least one author
              (Feathers/Ousterhout/Uncle Bob/Fowler/Beck). Name the
              author and cite the principle.

   DEFEND   — No author support, gate conflict, or ceremony cost
              outweighs benefit. Document 1-line rationale.

   SURFACE  — Cross-cutting defect affecting 5+ files, or
              architectural concern outside batch scope. Stop the
              batch. Report to user. Wait for decision.
```

The default classification is DEFEND — the burden of proof is on the
finding to justify any other classification.

**Zero findings is valid.** If the batch is <200 lines of mostly
algorithmic code, zero findings is expected. If >500 lines of complex
logic and zero findings, re-run with a tighter prompt.

**Abandon threshold.** If a batch has >5 BLOCKERs or >8 IMPROVEs,
SURFACE to user. The batch may need splitting or the fix scope may
need limiting.

### 3.3 — Fix and Verify

```
4. FIX — apply ALL BLOCKERs and IMPROVEs together. One
   characterization test per fix. Minimal code changes.

5. VERIFY — single verification pass after all fixes:
   a. dotnet build --verbosity quiet → 0 errors, 0 warnings
   b. Full tools test suite passes
   c. Run all six quality gates → no new FAILs
```

### 3.4 — Commit and Advance

```
6. COMMIT — one commit per batch (split if findings are orthogonal
   concerns). Conventional commit, proper body.

7. LOG — update Batch Execution Log (§5) with counts, commits,
   deferred findings.

8. NEXT — move to next batch. Never start N+1 while batch N has
   unresolved SURFACE findings.
```

---

## 4 — Success Criteria (What "100% Done" Means)

- [ ] Every batch B1-B8 has completed
- [ ] Every BLOCKER fixed, every IMPROVE addressed or documented
- [ ] Every DEFEND has a 1-line rationale in the execution log
- [ ] Every SURFACE finding reported to user and resolved
- [ ] Build: 0 errors, 0 warnings both solutions
- [ ] All six quality gates pass on tools solution
- [ ] Full tools test suite passes (no regression)
- [ ] Zero new CRAP FAIL violations

---

## 5 — Risks

| Risk                                           | Mitigation                                                                                       |
| ---------------------------------------------- | ------------------------------------------------------------------------------------------------ |
| Skill floods review with low-value findings    | Author filter — no author support = DEFEND. Never spend time on unsupported findings.            |
| Scope creep — refactoring spirals into rewrite | One batch at a time. Commit after each. Cross-cutting findings surface to user — never defended. |
| Subagent unavailable (plugin error, restart)   | Surface to user. Never substitute a general review agent.                                        |
| Parallel sessions modify files mid-review      | `git status --porcelain` check before every commit.                                              |

---

## 6 — Batch Execution Log

Updated after each batch completes.

| Batch | Date       | BLOCKERs | IMPROVEs | DEFENDs | SURFACEs | Deferred | Commit(s)             | Notes                                                                |
| ----- | ---------- | -------- | -------- | ------- | -------- | -------- | --------------------- | -------------------------------------------------------------------- |
| B0    | 2026-05-23 | 2        | 3        | 2       | 0        | —        | `e6de9b41` `907cd4fc` | Pilot — proved workflow                                              |
| B1    | 2026-05-23 | 2        | 5        | 3       | 0        | —        | `3fccc46c`            | Process leak, source corruption, dead code, naming                   |
| B2    | 2026-05-23 | 1        | 5        | 2       | 0        | —        | `01fe5a1c`            | Shared metrics calculator, dead code, float compare, bodyless filter |
| B3    | —          | —        | —        | —       | —        | —        | —                     | —                                                                    |
| B4    | —          | —        | —        | —       | —        | —        | —                     | —                                                                    |
| B5    | —          | —        | —        | —       | —        | —        | —                     | —                                                                    |
| B6    | —          | —        | —        | —       | —        | —        | —                     | —                                                                    |
| B7    | —          | —        | —        | —       | —        | —        | —                     | —                                                                    |
| B8    | —          | —        | —        | —       | —        | —        | —                     | —                                                                    |

---

## Related

- `docs/thermo-nuclear-gate-precedence-tree.md` — conflict resolution
  between thermo-nuclear findings and quality gates
- `.opencode/skills/vendor/cursor/thermo-nuclear-code-quality-review/SKILL.md`
- `docs/research/mutation-testing-100-percent-kill-rate-2026-05-14.md`
- `docs/research/crap-driven-functional-refactoring-2026-05-12.md`
