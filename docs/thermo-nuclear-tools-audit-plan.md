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

| Batch | Date       | BLOCKERs | IMPROVEs | DEFENDs | SURFACEs | Deferred     | Commit(s)             | Notes                                                                                                                                                                                                                                                                                                                                                                                                                                                       |
| ----- | ---------- | -------- | -------- | ------- | -------- | ------------ | --------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| B0    | 2026-05-23 | 2        | 3        | 2       | 0        | —            | `e6de9b41` `907cd4fc` | Pilot — proved workflow                                                                                                                                                                                                                                                                                                                                                                                                                                     |
| B1    | 2026-05-23 | 2        | 5        | 4       | 0        | —            | `3fccc46c`            | **Fixed**: process leak+kill on timeout, source restore in finally, dead backup removed, redundant disk read, siteIndex param, CancellationToken, MutateComparison→MutateBinaryExpression. **Defended**: MutateConstant pass-through (correct for current rules), delegate-to-MutationRule (cross-batch architectural change), in-memory-temp-file (requires dotnet build validation, high-risk), IAsyncEnumerable (changes public API, no caller benefit). |
| B2    | 2026-05-23 | 1        | 5        | 2       | 0        | —            | `01fe5a1c`            | **Fixed**: shared TestMethodMetricsCalculator, dead SameClass deleted, float→int compare on ZeroAssertionRatio, bodyless methods filtered early, complexity constants unified. **Defended**: syntax-only test attribute detection (intentional — semantic model adds 30-60s startup), union-find without rank (≤dozens of methods, negligible).                                                                                                             |
| B3    | 2026-05-23 | 0        | 6        | 5       | 0        | —            | `31cfb39a`            | **Fixed**: deleted IsCsFile+inlined CandidateComparer, out→nullable return, file-grouped IO, skip empty Cobertura keys, StringBuilder formatting. **Defended**: untyped normalizer (by design — mirrors dry4clj), coverage threshold ≤3≥1% (intentional), file-grouping in detector (pre-opt), typed normalizer record (ceremony > benefit), Jaccard LINQ allocs (acceptable).                                                                              |
| B4    | 2026-05-23 | 1        | 3        | 2       | 5        | —            | `ca0daa89`            | **BLOCKER**: CoverageReader flat HashSet<int> corrupted mutation classification. Fixed: file+line keyed `HashSet<string>`. **IMPROVE**: deleted dead VisitSwitchStatement no-op, made TryParseLineNumber private, removed implementation-coupled test. **Defended**: TestClassDiscovery returns class name for TUnit --treenode-filter, CcWalker pattern-based syntax needs per-node Visitors, ComputeDefore kept as documented Uncle Bob formula.          |
| B5    | 2026-05-23 | 3        | 2        | 1       | 1        | —            | `258c2a79`            | **BLOCKER**: hardcoded /tmp/ path → Path.GetTempPath(), bare catch → logged warning, WaitForExit timeout+stderr fix. **IMPROVE**: CombineExitCodes optional params removed. **Defended**: sequential orchestration (auto-coverage spawns dotnet run — IO contention if parallel), first-file-wins on multi-slnx (rare, --solution override exists).                                                                                                         |
| B6    | 2026-05-23 | 1        | 4        | 1       | 1        | 1 (coverage) | `258c2a79`            | **BLOCKER**: DupesCommand Environment.ExitCode → Func<ParseResult,int>. **IMPROVE**: ValidatePaths int? → bool, ApplyDefault deleted (thin wrappers), CheckDirectoryMissing→IsDirectoryMissing. **Defended**: 3 SetAction lambda styles (cosmetic). **Surfaced**: Coverage generation duplicated across CrapCommand+MutateHandler (cross-batch refactor).                                                                                                   |
| B7    | 2026-05-23 | 2        | 3        | 2       | 0        | —            | `258c2a79`            | **BLOCKER**: ArchHandler.Run always returned 0 (gate silently passed). Culture-sensitive Min() in NormalizeCycle → StringComparer.Ordinal. **IMPROVE**: FormatException now logged, hand-rolled JSON flagged (JsonSerializer), "Default" magic string. **Defended**: hardcoded path separators (both covered), hand-rolled JSON deferred (medium risk).                                                                                                     |
| B8    | 2026-05-23 | 0        | 1        | 1       | 0        | —            | `258c2a79`            | **IMPROVE**: removed dead ConfigureAwait(false). **Defended**: no top-level try/catch (subcommands handle own errors).                                                                                                                                                                                                                                                                                                                                      |

---

## 7 — Lessons Learned (from Tools Audit, May 2026)

These findings apply to every future thermo-nuclear audit run.

### Classification is the value, not the findings

The subagent produced ~50 findings across 8 batches. 10 were BLOCKERs (real
bugs), 28 were IMPROVEs (real improvements), 20 were DEFENDed. Blindly
applying all findings would have broken algorithm-inherent branching patterns
and created shallow wrappers Ousterhout would reject. The precedence tree +
author-backed defense is the product — not the subagent's raw output.

### Batch-scope-as-defense is the #1 reversal pattern

Every wrongly-defended finding shared the same cause: the agent defended it
because it touched files across multiple batch boundaries, not because it
was architecturally wrong. Examples: gate runner abstraction (defended as
"cross-batch refactor"), GateRunResults record (defended as "works fine now").

**Mitigation**: add a post-batch DEFEND review step. Before committing, scan
your own DEFEND list and ask: "did I defend this because of batch scope, not
because it's wrong?" If yes, flag it for reversal after the full audit.

### Pre-load prior DEFEND lists

The subagent re-discovers the same false positives on every run (visitor
dispatch, algorithm-inherent branching, LoggerMessage partial methods,
semantically-named shallow wrappers). Loading the prior audit's DEFEND list
before the next audit prevents re-litigating these. The agent still gets to
report them — they just get classified as DEFEND immediately with a citation
to the prior audit, saving classification time.

### The subagent finds bugs our test suite misses

B1 (orphaned dotnet processes on timeout), B4 (coverage key collision across
files), B7 (ArchHandler silently returning 0) — all genuine data-corruption
or silent-failure bugs. The agent reads code with zero context about "this is
how it's always been seen" — fresh eyes that complement, not replace, our
test suite.

### Post-audit code-judo review catches batch-scope defenses

The 4 code-judo refactors applied after the full audit (gate runner, JSON,
NormalizedNode, GateRunResults) were all defended during their batches on
scope grounds. Reviewing the full DEFEND list after all batches complete
surfaces these — the batch-boundary constraint is gone, so scope-based
defenses become obviously wrong.

---

## Related

- `docs/thermo-nuclear-gate-precedence-tree.md` — conflict resolution
  between thermo-nuclear findings and quality gates
- `.opencode/skills/vendor/cursor/thermo-nuclear-code-quality-review/SKILL.md`
- `docs/research/mutation-testing-100-percent-kill-rate-2026-05-14.md`
- `docs/research/crap-driven-functional-refactoring-2026-05-12.md`
