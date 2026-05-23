---
date: 2026-05-23
title: "Thermo-Nuclear / Quality Gate Precedence Tree"
tags:
  - thermo-nuclear
  - code-review
  - decision-tree
  - quality-gates
---

# Thermo-Nuclear / Quality Gate Precedence Tree

## What Belongs in This File

- **Viewpoint**: Agent resolving a conflict between a thermo-nuclear
  finding and a quality gate flag, or between two quality gates.
- **What belongs**: Decision tables with verdicts, author citations,
  and resolution rules. Each row is a concrete conflict pattern.
  New rows are added when a new conflict type is encountered.
- **What does NOT belong**: General thermo-nuclear documentation (in
  the skill file), gate execution commands (in tools-guide), per-batch
  findings (in the audit plan execution log).

---

Gates are mandatory. When the skill and a gate disagree, the gate wins
unless the skill has stronger author backing. Scan the tables below —
stop at the first match.

---

## 1 — Decision Tables

Scan top-to-bottom. Stop at the first matching row.

### 1.1 — Thermo-Nuclear recommends extraction

| Condition                                                                    | Gate flag                       | Author resolution                                                                                                                                                                                                                   | Verdict                                                                                         |
| ---------------------------------------------------------------------------- | ------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------- |
| Extraction would create a CRAP FAIL                                          | CRAP                            | Uncle Bob: "The combination of extraction and tests reduces CRAP — one alone is insufficient." Extracting without tests is net zero.                                                                                                | **DEFEND — extract only with characterization tests.** Never extract first and add tests later. |
| Extraction creates a Depth shallow(3) flag, CC reduction <3, caller count <3 | Depth                           | Ousterhout: "Shallow modules increase interface complexity without meaningful implementation." Single-caller 2-line extraction IS a shallow module.                                                                                 | **GATE WINS — inline it.**                                                                      |
| Extraction creates a Depth shallow(3) flag, CC reduction ≥3, caller count ≥3 | Depth                           | Uncle Bob (CC reduction) vs Ousterhout (shallow module). CC drops significantly AND method is widely called — Uncle Bob wins.                                                                                                       | **IMPROVE — extract.** CC reduction + multi-caller outweighs shallow score.                     |
| Extraction creates a Depth shallow(3) flag, CC reduction ≥3, caller count <3 | Depth                           | Ousterhout wins. Single-caller extraction without reuse is interface bloat.                                                                                                                                                         | **GATE WINS — find another way to reduce CC.**                                                  |
| Extraction creates a Depth wrong-abstraction(2) flag                         | Depth                           | If extraction moves existing branching (the method was ALREADY wrong-abstraction): the gate correctly identifies a structural defect — extraction is the first fix step. If extraction creates NEW branching on parameters: DEFEND. | **Existing branching → IMPROVE. New branching → DEFEND.**                                       |
| Extraction is a "code judo" simplification (deletes branches, removes modes) | None, or Depth shallow(3)       | Ousterhout: "Minimize long-term complexity, not just local." Architecture wins over local Depth when complexity decreases globally.                                                                                                 | **IMPROVE — extract.** Document the complexity trade-off.                                       |
| Extraction would increase mutation surface without test coverage             | Mutation (no gate, operational) | Uncle Bob: "Survivors must be killed." If extraction exposes uncovered branches, survivors increase.                                                                                                                                | **IMPROVE with condition — write tests first**, then extract.                                   |

### 1.2 — Thermo-Nuclear recommends inlining

| Condition                                                                             | Gate flag    | Author resolution                                                                                                                                                                                                                         | Verdict                                                                                 |
| ------------------------------------------------------------------------------------- | ------------ | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------- |
| Inline would increase CC past CRAP ceiling (≥8)                                       | CRAP         | Uncle Bob: CC ceiling is non-negotiable.                                                                                                                                                                                                  | **GATE WINS — do not inline.** Extract more or restructure.                             |
| Inline removes a Depth shallow(3) wrapper                                             | Depth        | Ousterhout AND Fowler agree. Fowler: "Inline Function." Ousterhout: "Delete shallow modules." Gates and skill agree.                                                                                                                      | **IMPROVE — inline.**                                                                   |
| Inline would create an Architecture violation (layer boundary leak)                   | Architecture | Uncle Bob: "Dependencies must point inward."                                                                                                                                                                                              | **GATE WINS — do not inline.**                                                          |
| Inline removes a thin abstraction with no author support                              | None         | Ousterhout: "Thin abstractions add complexity without implementation depth." Fowler: "Identity wrappers should collapse."                                                                                                                 | **IMPROVE — inline.**                                                                   |
| Ousterhout (minimize module count) vs Uncle Bob (Small Functions) — inlining question | Neither gate | Fowler: "Inline when the body is as clear as the name." Uncle Bob: "Extract until you drop." When name adds zero semantic value over body: inline (Fowler wins). When name reveals intent body obscures: keep extracted (Uncle Bob wins). | **Semantic-value test.** Name clearer than body → keep. Body as clear as name → inline. |

### 1.3 — Gate conflicts with Gate (no thermo-nuclear)

| Condition                                                                       | Resolution                                                                                                  | Verdict                              |
| ------------------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------------- | ------------------------------------ |
| Depth shallow(3) method has high CC — extracting would help CRAP but hurt Depth | Uncle Bob wins when CC gap ≥3. Ousterhout wins when CC ≤7. The CC threshold is the tiebreaker.              | CC ≥3 gap → extract. CC ≤7 → don't.  |
| Architecture allows dependency, Depth flags the target method                   | Independent concerns. Architecture governs structure; Depth governs method quality within a component.      | Address both independently.          |
| CRAP FAIL and Depth FAIL on same method                                         | Fix Depth first (structural), then CRAP (test coverage). Structural fixes often reduce CC as a side effect. | Depth first. Re-evaluate CRAP after. |
| Mutation survivor on a method with CRAP FAIL                                    | Fix CRAP first. Survivors require tests; tests reduce CRAP. Same fix addresses both.                        | Fix CRAP first.                      |

---

## 2 — Decision Flow

When a thermo-nuclear finding conflicts with any gate:

1. **Is it a real bug?** → BLOCKER. Fix regardless of gates.
2. **Which author backs the finding?** None → DEFEND.
3. **Which author backs the gate?** If same author as finding, they agree.
4. **Scan tables 1.1 → 1.2 → 1.3.** First match wins.
5. **No match?** → SURFACE. New conflict type. Report to user. After resolution, add a row to the matching table. This file grows with every new conflict.

---

## Related

- `docs/thermo-nuclear-tools-audit-plan.md` — batch execution plan
- `.opencode/skills/vendor/cursor/thermo-nuclear-code-quality-review/SKILL.md`
- `docs/research/structural-depth-author-research-2026-05-17.md`
- `docs/research/mutation-testing-100-percent-kill-rate-2026-05-14.md`
