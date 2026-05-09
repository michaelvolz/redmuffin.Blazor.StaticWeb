# SCRAP: Test Structural Analyzer with Uncle Bob Thresholds

The second quality gate after CRAP is SCRAP — a test structural analyzer
that detects weak-test smells (zero-assertion, low-assertion, duplicated
setup scaffolding) and computes extraction pressure for helper refactoring.
It replicates Uncle Bob's [scrap](https://github.com/unclebob/scrap) logic
verbatim for C#/TUnit, using Roslyn syntax-node normalization for fuzzy
Jaccard similarity (threshold 0.5) and the exact extraction pressure formula
`D_before = max(0, F-3) * (I-1)^1.5 / (V+1)`. All thresholds are locked to
the scrap source `policy.clj` values — they are not tuned for this repo.

## Considered Options

**Full SCRAP vs subset**: A minimal pass (zero-assertion + low-assertion
only) would be faster to ship, but fuzzy Jaccard duplication and extraction
pressure are what distinguish SCRAP from a simple lint rule. The full tool
gives AI-actionability classes (LEAVE_ALONE, AUTO_TABLE_DRIVE, AUTO_REFACTOR,
MANUAL_SPLIT, REVIEW_FIRST) that agents can act on without human judgment.
Building the subset first and adding duplication later would require
re-architecting the analyzer pipeline — the normalization engine is the
foundation everything else depends on.

**TUnit vs multi-framework**: The repo uses TUnit exclusively. Supporting
xUnit and NUnit from the start would abstract the test-method detection
layer with a `ITestFrameworkDetector` interface before we have a second
concrete implementation to validate the abstraction against. TUnit-only
keeps the initial implementation directly testable against real repo code.

**Syntax-node normalization vs token-stream**: Token-stream normalization
loses InvocationExpression nesting — `Assert.That(x).IsNotNull()` and
`Assert.That(y).IsEqualTo(z)` would normalize to identical token sequences
despite different assertion shapes. Syntax-node normalization preserves
the AST kind structure, so `IsNotNull()` and `IsEqualTo(z)` remain
distinguishable. This is critical for distinguishing harmful duplication
(hooks calls match) from case-matrix repetition (assertion shapes differ).

## Thresholds (verbatim from scrap `policy.clj`)

**Stability**: max-scrap ≤12, effective-duplication ≤3, zero-assertion=0,
low-assertion ≤0.35. Small files (≤2 examples): tighter bounds.

**SPLIT trigger**: avg-scrap ≥10 OR dup-score ≥20 OR subject-repetition ≥12
OR helper-hidden>0, AND ≥12 examples AND (≥2 high-pressure blocks OR
max-scrap ≥35).

**Pressure levels**: CRITICAL ≥55, HIGH ≥35, MEDIUM ≥18.
**Complexity**: saturating curve (cap 25.0, rise-rate 0.18, floor 1.0).

## Consequences

- SCRAP adds one subcommand (`scrap`) to `redmuffin.Tools.QualityGates`,
  following the same System.CommandLine pattern as `crap`.
- The Roslyn workspace is shared with CRAP — CC computation is reused.
- No coverage file needed (pure structural analysis).
- Supports `--changed` for incremental mode, `--write-baseline`/`--compare`
  for refactoring safety, `--json`/`--verbose` for machine/human output.
